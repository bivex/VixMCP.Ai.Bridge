using System.ComponentModel;
using ModelContextProtocol.Server;
using System.Runtime.InteropServices; // Added for Marshal
using VixBindings; // Added to access VixApi

namespace mcpWinAuditServer.Tools {

/// <summary>
/// Represents the result of a VIX API operation.
/// </summary>
/// <param name="Success">Indicates if the operation was successful.</param>
/// <param name="ErrorMessage">An optional error message if the operation failed.</param>
/// <param name="Value">An optional value returned by the operation.</param>
public record VixOperationResult ( bool Success, string? ErrorMessage = null, object? Value = null );

[McpServerToolType]
public static class ProcessTool {

    private static int _sessionHostHandle = VixApi.VIX_INVALID_HANDLE;
    private static int _sessionVmHandle = VixApi.VIX_INVALID_HANDLE;
    private static string? _sessionGuestUser;
    private static string? _sessionGuestPassword;

    [McpServerTool, Description ( "Lists all running processes on the system inside a guest VM using VIX API." )]
    public static async Task<object> ListAllProcessesInGuest (
        string guestTempPath = "/tmp", // Default for Linux
        string hostTempPath = "C:\\temp" ) // Default for Windows host
    {
        if ( _sessionVmHandle == VixApi.VIX_INVALID_HANDLE )
        { return new { success = false, error = "No active session. Please connect and login first." }; }

        try
        {
            // Убедиться, что guestTempPath существует
            string createTempDirCommand = $"mkdir -p {guestTempPath}";
            VixApiHelper.RunGuestCommandAndGetOutput (
                _sessionVmHandle,
                createTempDirCommand,
                $"{guestTempPath}/create_temp_dir_output.txt",
                $"{hostTempPath}/create_temp_dir_host_output.txt"
            );

            // Выполнить ps aux и получить вывод
            string psCommand = "ps aux";
            string guestPsOutputPath = $"{guestTempPath}/ps_aux_output.txt";
            string hostPsOutputPath = $"{hostTempPath}/ps_aux_host_output.txt";
            CommandResult result = VixApiHelper.RunGuestCommandAndGetOutput (
                                       _sessionVmHandle,
                                       psCommand,
                                       guestPsOutputPath,
                                       hostPsOutputPath
                                   );

            if ( result.success )
            {
                string processesOutput = result.output;
                return new { success = true, output = processesOutput };
            }
            else
            {
                string error = result.error;
                return new { success = false, error = error };
            }
        }
        catch ( Exception ex )
        {
            return new { success = false, error = ex.Message };
        }
    }

    [McpServerTool, Description ( "Checks if guest login is possible (without running commands)." )]
    public static async Task<object> CheckGuestConnection (
        string hostName,
        int hostPort,
        string hostUserName,
        string hostPassword,
        string vmxFilePath,
        string guestUserName,
        string guestPassword )
    {
        int hostHandle = VixApi.VIX_INVALID_HANDLE;
        int vmHandle = VixApi.VIX_INVALID_HANDLE;
        int jobHandle = VixApi.VIX_INVALID_HANDLE;
        try
        {
            // Подключение к хосту
            VixOperationResult hostConnectResult = await ConnectToHostAsync ( hostName, hostPort, hostUserName, hostPassword );
            if ( !hostConnectResult.Success )
            {
                return new { success = false, error = hostConnectResult.ErrorMessage };
            }
            hostHandle = ( int ) hostConnectResult.Value!;

            // Открытие VM
            VixOperationResult openVmResult = await OpenVmAsync ( hostHandle, vmxFilePath );
            if ( !openVmResult.Success )
            {
                return new { success = false, error = openVmResult.ErrorMessage };
            }
            vmHandle = ( int ) openVmResult.Value!;

            // Ожидание VMware Tools
            VixOperationResult waitForToolsResult = await WaitForToolsInGuestAsync ( vmHandle );
            if ( !waitForToolsResult.Success )
            {
                return new { success = false, error = waitForToolsResult.ErrorMessage };
            }

            // Логин в гостя
            VixOperationResult loginResult = await LoginInGuestAsync ( vmHandle, guestUserName, guestPassword );
            if ( !loginResult.Success )
            {
                return new { success = false, error = loginResult.ErrorMessage };
            }

            return new { success = true, message = "Successfully logged in to guest OS." };
        }
        catch ( Exception ex )
        {
            return new { success = false, error = ex.Message };
        }
        finally
        {
            if ( vmHandle != VixApi.VIX_INVALID_HANDLE )
            { VixApi.Vix_ReleaseHandle ( vmHandle ); }
            if ( hostHandle != VixApi.VIX_INVALID_HANDLE )
            { VixApi.VixHost_Disconnect ( hostHandle ); }
            if ( jobHandle != VixApi.VIX_INVALID_HANDLE )
            { VixApi.Vix_ReleaseHandle ( jobHandle ); }
        }
    }

    [McpServerTool, Description ( "Connects to VM and logs in to guest, storing session for future commands." )]
    public static async Task<object> ConnectAndLogin (
        string hostName,
        int hostPort,
        string hostUserName,
        string hostPassword,
        string vmxFilePath,
        string guestUserName,
        string guestPassword )
    {
        // Освобождаем предыдущую сессию, если есть
        if ( _sessionVmHandle != VixApi.VIX_INVALID_HANDLE )
        { VixApi.Vix_ReleaseHandle ( _sessionVmHandle ); }
        if ( _sessionHostHandle != VixApi.VIX_INVALID_HANDLE )
        { VixApi.VixHost_Disconnect ( _sessionHostHandle ); }

        _sessionHostHandle = VixApi.VIX_INVALID_HANDLE;
        _sessionVmHandle = VixApi.VIX_INVALID_HANDLE;
        _sessionGuestUser = null;
        _sessionGuestPassword = null;

        int jobHandle = VixApi.VIX_INVALID_HANDLE;
        try
        {
            // Подключение к хосту
            VixOperationResult hostConnectResult = await ConnectToHostAsync ( hostName, hostPort, hostUserName, hostPassword );
            if ( !hostConnectResult.Success )
            {
                return $"Failed to connect to host: {hostConnectResult.ErrorMessage}";
            }
            _sessionHostHandle = ( int ) hostConnectResult.Value!;

            // Открытие VM
            VixOperationResult openVmResult = await OpenVmAsync ( _sessionHostHandle, vmxFilePath );
            if ( !openVmResult.Success )
            {
                return $"Failed to open VM: {openVmResult.ErrorMessage}";
            }
            _sessionVmHandle = ( int ) openVmResult.Value!;

            // Ожидание VMware Tools
            VixOperationResult waitForToolsResult = await WaitForToolsInGuestAsync ( _sessionVmHandle );
            if ( !waitForToolsResult.Success )
            {
                return $"Failed to wait for tools: {waitForToolsResult.ErrorMessage}";
            }

            // Логин в гостя
            VixOperationResult loginResult = await LoginInGuestAsync ( _sessionVmHandle, guestUserName, guestPassword );
            if ( !loginResult.Success )
            {
                return $"Failed to login to guest: {loginResult.ErrorMessage}";
            }

            _sessionGuestUser = guestUserName;
            _sessionGuestPassword = guestPassword;

            return "Connected and logged in. Session is ready for commands.";
        }
        catch ( Exception ex )
        {
            return $"Exception: {ex.Message}";
        }
        finally
        {
            if ( jobHandle != VixApi.VIX_INVALID_HANDLE )
            { VixApi.Vix_ReleaseHandle ( jobHandle ); }
        }
    }

    [McpServerTool, Description ( "Executes a command in the guest OS using the current session." )]
    public static async Task<object> ExecCommandInGuestSession (
        string command,
        string guestTempPath = "/tmp",
        string hostTempPath = "C:\\temp" )
    {
        if ( _sessionVmHandle == VixApi.VIX_INVALID_HANDLE || string.IsNullOrEmpty ( _sessionGuestUser ) || string.IsNullOrEmpty ( _sessionGuestPassword ) )
            return new { success = false, error = "No active session. Please connect and login first." };

        try
        {
            // Убедиться, что guestTempPath существует
            string createTempDirCommand = $"mkdir -p {guestTempPath}";
            VixApiHelper.RunGuestCommandAndGetOutput (
                _sessionVmHandle,
                createTempDirCommand,
                $"{guestTempPath}/create_temp_dir_output.txt",
                $"{hostTempPath}/create_temp_dir_host_output.txt"
            );

            // Выполнить команду и получить вывод
            string guestOutputPath = $"{guestTempPath}/cmd_output.txt";
            string hostOutputPath = $"{hostTempPath}/cmd_output_host.txt";
            CommandResult result = VixApiHelper.RunGuestCommandAndGetOutput (
                                       _sessionVmHandle,
                                       command,
                                       guestOutputPath,
                                       hostOutputPath
                                   );

            if ( result.success )
            {
                string output = result.output;
                return new { success = true, output = output };
            }
            else
            {
                string error = result.error;
                return new { success = false, error = error };
            }
        }
        catch ( Exception ex )
        {
            return new { success = false, error = ex.Message };
        }
    }

    /// <summary>
    /// Helper method to connect to the VIX host.
    /// </summary>
    private static async Task<VixOperationResult> ConnectToHostAsync ( string hostName, int hostPort, string hostUserName, string hostPassword )
    {
        int jobHandle = VixApi.VIX_INVALID_HANDLE;
        try
        {
            jobHandle = VixApi.VixHost_Connect (
                            VixApi.VIX_API_VERSION,
                            VixApi.VixServiceProvider.VMwareWorkstation,
                            hostName,
                            hostPort,
                            hostUserName,
                            hostPassword,
                            VixApi.VixHostOptions.VerifySslCert,
                            VixApi.VIX_INVALID_HANDLE,
                            VixApiHelper.JobCallback,
                            IntPtr.Zero );

            ulong connectionResult = VixApi.VixJob_Wait ( jobHandle, VixApi.VIX_PROPERTY_JOB_RESULT_HANDLE, out int hostHandle, VixApi.VIX_PROPERTY_NONE );
            if ( VixApi.VIX_OK != connectionResult )
            {
                string errorMessage = GetVixErrorText ( connectionResult );
                return new VixOperationResult ( false, $"Failed to connect to host: {errorMessage}" );
            }
            return new VixOperationResult ( true, Value: hostHandle );
        }
        finally
        {
            if ( jobHandle != VixApi.VIX_INVALID_HANDLE )
            { VixApi.Vix_ReleaseHandle ( jobHandle ); }
        }
    }

    /// <summary>
    /// Helper method to open a virtual machine.
    /// </summary>
    private static async Task<VixOperationResult> OpenVmAsync ( int hostHandle, string vmxFilePath )
    {
        int jobHandle = VixApi.VIX_INVALID_HANDLE;
        try
        {
            jobHandle = VixApi.VixHost_OpenVM (
                            hostHandle,
                            vmxFilePath,
                            VixApi.VixVMOpenOptions.Normal,
                            VixApi.VIX_INVALID_HANDLE,
                            VixApiHelper.JobCallback,
                            IntPtr.Zero );

            ulong openVmResult = VixApi.VixJob_Wait ( jobHandle, VixApi.VIX_PROPERTY_JOB_RESULT_HANDLE, out int vmHandle, VixApi.VIX_PROPERTY_NONE );
            if ( VixApi.VIX_OK != openVmResult )
            {
                string errorMessage = GetVixErrorText ( openVmResult );
                return new VixOperationResult ( false, $"Failed to open VM: {errorMessage}" );
            }
            return new VixOperationResult ( true, Value: vmHandle );
        }
        finally
        {
            if ( jobHandle != VixApi.VIX_INVALID_HANDLE )
            { VixApi.Vix_ReleaseHandle ( jobHandle ); }
        }
    }

    /// <summary>
    /// Helper method to wait for VMware Tools to be available in the guest OS.
    /// </summary>
    private static async Task<VixOperationResult> WaitForToolsInGuestAsync ( int vmHandle )
    {
        int jobHandle = VixApi.VIX_INVALID_HANDLE;
        try
        {
            jobHandle = VixApi.VixVM_WaitForToolsInGuest (
                            vmHandle,
                            600, // Timeout in seconds
                            VixApiHelper.JobCallback,
                            IntPtr.Zero );

            VixApi.VixJob_Wait ( jobHandle, VixApi.VIX_PROPERTY_NONE );
            if ( VixApi.VIX_OK != VixApiHelper._jobResult )
            {
                string errorMessage = GetVixErrorText ( VixApiHelper._jobResult );
                return new VixOperationResult ( false, $"Failed to wait for tools: {errorMessage}" );
            }
            return new VixOperationResult ( true );
        }
        finally
        {
            if ( jobHandle != VixApi.VIX_INVALID_HANDLE )
            { VixApi.Vix_ReleaseHandle ( jobHandle ); }
        }
    }

    /// <summary>
    /// Helper method to log in to the guest OS.
    /// </summary>
    private static async Task<VixOperationResult> LoginInGuestAsync ( int vmHandle, string guestUserName, string guestPassword )
    {
        int jobHandle = VixApi.VIX_INVALID_HANDLE;
        try
        {
            jobHandle = VixApi.VixVM_LoginInGuest (
                            vmHandle,
                            guestUserName,
                            guestPassword,
                            0, // Options
                            VixApiHelper.JobCallback,
                            IntPtr.Zero );

            VixApi.VixJob_Wait ( jobHandle, VixApi.VIX_PROPERTY_NONE );
            if ( VixApi.VIX_OK != VixApiHelper._jobResult )
            {
                string errorMessage = GetVixErrorText ( VixApiHelper._jobResult );
                return new VixOperationResult ( false, $"Failed to login to guest: {errorMessage}" );
            }
            return new VixOperationResult ( true );
        }
        finally
        {
            if ( jobHandle != VixApi.VIX_INVALID_HANDLE )
            { VixApi.Vix_ReleaseHandle ( jobHandle ); }
        }
    }

    /// <summary>
    /// Retrieves the error text for a given VIX error code.
    /// </summary>
    /// <param name="errorCode">The VIX error code.</param>
    /// <returns>The error message string.</returns>
    private static string GetVixErrorText ( ulong errorCode )
    {
        IntPtr errorTextPtr = VixApi.Vix_GetErrorText ( errorCode, ( string? ) null );
        string errorMessage = Marshal.PtrToStringAnsi ( errorTextPtr ) ?? "Unknown error";
        VixApi.Vix_FreeBuffer ( errorTextPtr );
        return errorMessage;
    }
}
}
