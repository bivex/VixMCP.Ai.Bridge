using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace VixBindings.Samples {
public class Sample {
    // Replace with your actual values
    private const string HostName = ""; // Or your ESXi/vCenter IP/hostname
    private const int HostPort = 0; // Default port
    private const string UserName = ""; // Your host username
    private const string Password = ""; // Your host password
    private const string VmxFilePath = "c:\\Users\\Admin\\Documents\\Virtual Machines\\Ubuntu\\Ubuntu.vmx"; // Path to your VM's .vmx file
    private const string GuestUserName = "newvixuser"; // Guest OS username
    private const string GuestPassword = "P@ssw0rd123"; // Guest OS password
    private const string HostFilePath = "C:\\temp\\host_file.txt"; // File on host to copy
    private const string GuestFilePath = "/tmp/guest_file.txt"; // Destination path in guest
    private const string CopiedGuestFilePath = "/tmp/guest_file_copied.txt"; // File to copy from guest
    private const string CopiedHostFilePath = "C:\\temp\\host_file_copied.txt"; // Destination path on host
    private const string GuestCommandOutputPath = "/tmp/whoami_output.txt"; // Temp file for command output in guest
    private const string HostCommandOutputPath = "C:\\temp\\whoami_output_host.txt"; // Temp file for command output on host

    private static AutoResetEvent _jobEvent = new AutoResetEvent ( false );
    private static ulong _jobResult = VixApi.VIX_OK;

    public static void Main ( string[] args )
    {
        // Register code page provider for encodings like CP866
        System.Text.Encoding.RegisterProvider ( System.Text.CodePagesEncodingProvider.Instance );

        int hostHandle = VixApi.VIX_INVALID_HANDLE;
        int vmHandle = VixApi.VIX_INVALID_HANDLE;
        int jobHandle = VixApi.VIX_INVALID_HANDLE;

        try
        {
            Console.WriteLine ( "Connecting to host..." );
            jobHandle = VixApi.VixHost_Connect (
                            VixApi.VIX_API_VERSION,
                            VixApi.VixServiceProvider.VMwareWorkstation,
                            HostName,
                            HostPort,
                            UserName,
                            Password,
                            VixApi.VixHostOptions.VerifySslCert,
                            VixApi.VIX_INVALID_HANDLE,
                            JobCallback,
                            IntPtr.Zero );

            ulong connectionResult = VixApi.VixJob_Wait ( jobHandle, VixApi.VIX_PROPERTY_JOB_RESULT_HANDLE, out hostHandle, VixApi.VIX_PROPERTY_NONE );
            if ( VixApi.VIX_OK != connectionResult )
            {
                IntPtr errorTextPtr = VixApi.Vix_GetErrorText ( connectionResult, ( string? ) null );
                string errorMessage = Marshal.PtrToStringAnsi ( errorTextPtr ) ?? "Unknown error";
                Console.WriteLine ( $"Failed to connect to host: {errorMessage}" );
                VixApi.Vix_FreeBuffer ( errorTextPtr );
                return;
            }
            Console.WriteLine ( "Connected to host." );
            VixApi.Vix_ReleaseHandle ( jobHandle );

            Console.WriteLine ( "Opening VM..." );
            jobHandle = VixApi.VixHost_OpenVM (
                            hostHandle,
                            VmxFilePath,
                            VixApi.VixVMOpenOptions.Normal,
                            VixApi.VIX_INVALID_HANDLE,
                            JobCallback,
                            IntPtr.Zero );

            ulong openVmResult = VixApi.VixJob_Wait ( jobHandle, VixApi.VIX_PROPERTY_JOB_RESULT_HANDLE, out vmHandle, VixApi.VIX_PROPERTY_NONE );
            if ( VixApi.VIX_OK != openVmResult )
            {
                IntPtr errorTextPtr = VixApi.Vix_GetErrorText ( openVmResult, ( string? ) null );
                string errorMessage = Marshal.PtrToStringAnsi ( errorTextPtr ) ?? "Unknown error";
                Console.WriteLine ( $"Failed to open VM: {errorMessage}" );
                VixApi.Vix_FreeBuffer ( errorTextPtr );
                return;
            }
            Console.WriteLine ( "VM opened." );
            VixApi.Vix_ReleaseHandle ( jobHandle );

            Console.WriteLine ( "Waiting for tools in guest..." );
            jobHandle = VixApi.VixVM_WaitForToolsInGuest (
                            vmHandle,
                            600, // Timeout in seconds
                            JobCallback,
                            IntPtr.Zero );

            VixApi.VixJob_Wait ( jobHandle, VixApi.VIX_PROPERTY_NONE );
            if ( VixApi.VIX_OK != _jobResult )
            {
                IntPtr errorTextPtr = VixApi.Vix_GetErrorText ( _jobResult, ( string? ) null );
                string errorMessage = Marshal.PtrToStringAnsi ( errorTextPtr ) ?? "Unknown error";
                Console.WriteLine ( $"Failed to wait for tools: {errorMessage}" );
                VixApi.Vix_FreeBuffer ( errorTextPtr );
                return;
            }
            Console.WriteLine ( "Tools are running in guest." );
            VixApi.Vix_ReleaseHandle ( jobHandle );

            Console.WriteLine ( "Logging in to guest OS..." );
            jobHandle = VixApi.VixVM_LoginInGuest (
                            vmHandle,
                            GuestUserName,
                            GuestPassword,
                            0, // Options: Reverted to default options
                            JobCallback,
                            IntPtr.Zero );

            VixApi.VixJob_Wait ( jobHandle, VixApi.VIX_PROPERTY_NONE );
            if ( VixApi.VIX_OK != _jobResult )
            {
                IntPtr errorTextPtr = VixApi.Vix_GetErrorText ( _jobResult, ( string? ) null );
                string errorMessage = Marshal.PtrToStringAnsi ( errorTextPtr ) ?? "Unknown error";
                Console.WriteLine ( $"Failed to login to guest: {errorMessage}" );
                VixApi.Vix_FreeBuffer ( errorTextPtr );
                return;
            }
            Console.WriteLine ( "Logged in to guest OS." );
            VixApi.Vix_ReleaseHandle ( jobHandle );

            // Ensure /tmp directory exists in guest
            Console.WriteLine("Ensuring /tmp directory exists in guest...");
            // Using mkdir -p for Linux to create parent directories if they don't exist
            string createTempDirCommand = "mkdir -p /tmp";
            string createTempDirOutput = ExecuteCommandInGuestAndGetOutput(
                                            vmHandle,
                                            GuestUserName,
                                            GuestPassword,
                                            createTempDirCommand,
                                            "/tmp/create_temp_dir_output.txt", // Use a temporary output file for this command
                                            "C:\\temp\\create_temp_dir_host_output.txt"); // Use a temporary output file for this command on host

            // In Linux, mkdir -p typically doesn't produce output on success, only on error
            if (!string.IsNullOrEmpty(createTempDirOutput) && !createTempDirOutput.Contains("File exists"))
            {
                Console.WriteLine($"Warning: Could not verify or create /tmp directory in guest (Output: {createTempDirOutput.Trim()}). Subsequent file operations might fail.");
            }
            else
            {
                Console.WriteLine("Successfully ensured /tmp directory exists in guest.");
            }

            Console.WriteLine ( $"DEBUG: GuestPassword: '{new string('*', GuestPassword.Length)}'" ); // Mask password for security

            // Call the new generic function to execute a command and get its output
            string whoamiPrivCommand = "id"; // Changed to 'id' for Linux
            string whoamiPrivOutput = ExecuteCommandInGuestAndGetOutput(
                                        vmHandle,
                                        GuestUserName,
                                        GuestPassword,
                                        whoamiPrivCommand,
                                        GuestCommandOutputPath,
                                        HostCommandOutputPath);

            if (!string.IsNullOrEmpty(whoamiPrivOutput))
            {
                Console.WriteLine ( $"Command Output for '{whoamiPrivCommand}':\n{whoamiPrivOutput}" );
            }
            else
            {
                Console.WriteLine ( $"Failed to get output for command '{whoamiPrivCommand}'." );
            }

            // Original file copy example follows:
            // Create a dummy file on the host for copying
            System.IO.File.WriteAllText ( HostFilePath, "This is a test file from the host." );
            Console.WriteLine ( $"Created dummy file on host: {HostFilePath}" );

            Console.WriteLine ( $"Copying file from host to guest: {HostFilePath} -> {GuestFilePath}..." );
            jobHandle = VixApi.VixVM_CopyFileFromHostToGuest (
                            vmHandle,
                            HostFilePath,
                            GuestFilePath,
                            0, // Options
                            VixApi.VIX_INVALID_HANDLE,
                            JobCallback,
                            IntPtr.Zero );

            VixApi.VixJob_Wait ( jobHandle, VixApi.VIX_PROPERTY_NONE );
            if ( VixApi.VIX_OK != _jobResult )
            {
                IntPtr errorTextPtr = VixApi.Vix_GetErrorText ( _jobResult, ( string? ) null );
                string errorMessage = Marshal.PtrToStringAnsi ( errorTextPtr ) ?? "Unknown error";
                Console.WriteLine ( $"Failed to copy file from host to guest: {errorMessage}" );
                VixApi.Vix_FreeBuffer ( errorTextPtr );
            }
            else
            {
                Console.WriteLine ( "File copied from host to guest successfully." );
            }
            VixApi.Vix_ReleaseHandle ( jobHandle );

            // Verify if the file exists in the guest before attempting to copy back
            Console.WriteLine ( $"Checking if {GuestFilePath} exists in guest..." );
            jobHandle = VixApi.VixVM_FileExistsInGuest (
                            vmHandle,
                            GuestFilePath,
                            JobCallback,
                            IntPtr.Zero );

            VixApi.VixJob_Wait ( jobHandle, VixApi.VIX_PROPERTY_NONE );
            if ( VixApi.VIX_OK != _jobResult )
            {
                IntPtr errorTextPtr = VixApi.Vix_GetErrorText ( _jobResult, ( string? ) null );
                string errorMessage = Marshal.PtrToStringAnsi ( errorTextPtr ) ?? "Unknown error";
                Console.WriteLine ( $"File {GuestFilePath} does NOT exist in guest or check failed: {errorMessage}" );
                VixApi.Vix_FreeBuffer ( errorTextPtr );
            }
            else
            {
                Console.WriteLine ( $"File {GuestFilePath} exists in guest. Proceeding with copy to host." );

                // Changed CopiedGuestFilePath to GuestFilePath to copy the created file back
                Console.WriteLine ( $"Copying file from guest to host: {GuestFilePath} -> {CopiedHostFilePath}..." );
                jobHandle = VixApi.VixVM_CopyFileFromGuestToHost (
                                vmHandle,
                                GuestFilePath, // Use the file that was just copied to the guest
                                CopiedHostFilePath,
                                0, // Options
                                VixApi.VIX_INVALID_HANDLE,
                                JobCallback,
                                IntPtr.Zero );

                VixApi.VixJob_Wait ( jobHandle, VixApi.VIX_PROPERTY_NONE );
                if ( VixApi.VIX_OK != _jobResult )
                {
                    IntPtr errorTextPtr = VixApi.Vix_GetErrorText ( _jobResult, ( string? ) null );
                    string errorMessage = Marshal.PtrToStringAnsi ( errorTextPtr ) ?? "Unknown error";
                    Console.WriteLine ( $"Failed to copy file from guest to host: {errorMessage}" );
                    VixApi.Vix_FreeBuffer ( errorTextPtr );
                }
                else
                {
                    Console.WriteLine ( "File copied from guest to host successfully." );
                }
                VixApi.Vix_ReleaseHandle ( jobHandle );
            }

            Console.WriteLine ( "Logging out from guest OS..." );
            jobHandle = VixApi.VixVM_LogoutFromGuest (
                            vmHandle,
                            JobCallback,
                            IntPtr.Zero );

            VixApi.VixJob_Wait ( jobHandle, VixApi.VIX_PROPERTY_NONE );
            if ( VixApi.VIX_OK != _jobResult )
            {
                IntPtr errorTextPtr = VixApi.Vix_GetErrorText ( _jobResult, ( string? ) null );
                string errorMessage = Marshal.PtrToStringAnsi ( errorTextPtr ) ?? "Unknown error";
                Console.WriteLine ( $"Failed to logout from guest: {errorMessage}" );
                VixApi.Vix_FreeBuffer ( errorTextPtr );
            }
            else
            {
                Console.WriteLine ( "Logged out from guest OS." );
            }
            VixApi.Vix_ReleaseHandle ( jobHandle );
        }
        catch ( Exception ex )
        {
            Console.WriteLine ( $"An unexpected error occurred: {ex.Message}" );
        }
        finally
        {
            if ( vmHandle != VixApi.VIX_INVALID_HANDLE )
            {
                VixApi.Vix_ReleaseHandle ( vmHandle );
                Console.WriteLine ( "VM handle released." );
            }
            if ( hostHandle != VixApi.VIX_INVALID_HANDLE )
            {
                VixApi.VixHost_Disconnect ( hostHandle );
                Console.WriteLine ( "Disconnected from host." );
            }
            if ( jobHandle != VixApi.VIX_INVALID_HANDLE )
            {
                VixApi.Vix_ReleaseHandle ( jobHandle );
                Console.WriteLine ( "Job handle released." );
            }
        }
    }

    private static void JobCallback ( int handle, int eventType, int moreEventInfo, IntPtr clientData )
    {
        if ( eventType == ( int ) VixApi.VixEventType.JobCompleted )
        {
            _jobResult = VixApi.VixJob_GetError ( handle );
            _jobEvent.Set();
        }
    }

    private static int GetJobResultHandle ( int jobHandle )
    {
        int resultHandle = VixApi.VIX_INVALID_HANDLE;
        ulong err = VixApi.VixJob_Wait ( jobHandle, VixApi.VIX_PROPERTY_JOB_RESULT_HANDLE, out resultHandle, VixApi.VIX_PROPERTY_NONE );
        if ( VixApi.VIX_OK == err )
        {
            return resultHandle;
        }
        return VixApi.VIX_INVALID_HANDLE;
    }

    private static string ExecuteCommandInGuestAndGetOutput ( int vmHandle,
            string guestUserName,
            string guestPassword,
            string command,
            string guestCommandOutputPath,
            string hostCommandOutputPath )
    {
        int jobHandle = VixApi.VIX_INVALID_HANDLE;
        string commandOutput = string.Empty;

        Console.WriteLine ( $"Executing '{command}' in guest OS and redirecting output..." );
        jobHandle = VixApi.VixVM_RunProgramInGuest (
                        vmHandle,
                        "/bin/bash", // Program to run (bash for Linux)
                        $"-c \"{command} > {guestCommandOutputPath}\"", // Arguments: -c to execute command and redirect output
                        VixApi.VixRunProgramOptions.ReturnImmediately, // Options: ReturnImmediately for console app
                        VixApi.VIX_INVALID_HANDLE, // PropertyListHandle
                        JobCallback,
                        IntPtr.Zero );

        VixApi.VixJob_Wait ( jobHandle, VixApi.VIX_PROPERTY_NONE );
        if ( VixApi.VIX_OK != _jobResult )
        {
            IntPtr errorTextPtr = VixApi.Vix_GetErrorText ( _jobResult, ( string? ) null );
            string errorMessage = Marshal.PtrToStringAnsi ( errorTextPtr ) ?? "Unknown error";
            Console.WriteLine ( $"Failed to execute command in guest (Job Wait Error): {errorMessage} (Raw Error: { _jobResult:X})" );
            VixApi.Vix_FreeBuffer ( errorTextPtr );
        }
        else
        {
            Console.WriteLine ( $"Command '{command}' executed successfully in guest. Output redirected to {guestCommandOutputPath}." );

            // Add a small delay to ensure file is written
            Console.WriteLine ( "Waiting for 2 seconds for guest to write the output file..." );
            System.Threading.Thread.Sleep ( 2000 );

            // Verify if the output file exists in the guest before attempting to copy
            Console.WriteLine ( $"Checking if {guestCommandOutputPath} exists in guest..." );
            jobHandle = VixApi.VixVM_FileExistsInGuest (
                            vmHandle,
                            guestCommandOutputPath,
                            JobCallback,
                            IntPtr.Zero );

            VixApi.VixJob_Wait ( jobHandle, VixApi.VIX_PROPERTY_NONE );
            if ( VixApi.VIX_OK != _jobResult )
            {
                IntPtr errorTextPtr = VixApi.Vix_GetErrorText ( _jobResult, ( string? ) null );
                string errorMessage = Marshal.PtrToStringAnsi ( errorTextPtr ) ?? "Unknown error";
                Console.WriteLine ( $"Output file {guestCommandOutputPath} does NOT exist in guest or check failed: {errorMessage}" );
                VixApi.Vix_FreeBuffer ( errorTextPtr );
            }
            else
            {
                Console.WriteLine ( $"Output file {guestCommandOutputPath} exists in guest. Proceeding with copy to host." );

                // Copy the output file from guest to host
                Console.WriteLine ( $"Copying command output from guest to host: {guestCommandOutputPath} -> {hostCommandOutputPath}..." );
                jobHandle = VixApi.VixVM_CopyFileFromGuestToHost (
                                vmHandle,
                                guestCommandOutputPath,
                                hostCommandOutputPath,
                                0, // Options
                                VixApi.VIX_INVALID_HANDLE,
                                JobCallback,
                                IntPtr.Zero );

                VixApi.VixJob_Wait ( jobHandle, VixApi.VIX_PROPERTY_NONE );
                if ( VixApi.VIX_OK != _jobResult )
                {
                    IntPtr errorTextPtr = VixApi.Vix_GetErrorText ( _jobResult, ( string? ) null );
                    string errorMessage = Marshal.PtrToStringAnsi ( errorTextPtr ) ?? "Unknown error";
                    Console.WriteLine ( $"Failed to copy command output file from guest to host: {errorMessage}" );
                    VixApi.Vix_FreeBuffer ( errorTextPtr );
                }
                else
                {
                    Console.WriteLine ( $"Command output file copied from guest to host: {hostCommandOutputPath}." );
                    // Read the output from the copied file
                    try
                    {
                        string output = System.IO.File.ReadAllText(hostCommandOutputPath, System.Text.Encoding.UTF8); // Changed to UTF8 for Linux
                        commandOutput = output;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error reading command output file on host: {ex.Message}");
                    }
                }
            }
            VixApi.Vix_ReleaseHandle ( jobHandle );

            // Clean up: delete the temporary output file in guest
            Console.WriteLine ( $"Deleting temporary output file in guest: {guestCommandOutputPath}..." );
            jobHandle = VixApi.VixVM_DeleteFileInGuest (
                            vmHandle,
                            guestCommandOutputPath,
                            JobCallback,
                            IntPtr.Zero );
            VixApi.VixJob_Wait ( jobHandle, VixApi.VIX_PROPERTY_NONE );
            if ( VixApi.VIX_OK != _jobResult )
            {
                IntPtr errorTextPtr = VixApi.Vix_GetErrorText ( _jobResult, ( string? ) null );
                string errorMessage = Marshal.PtrToStringAnsi ( errorTextPtr ) ?? "Unknown error";
                Console.WriteLine ( $"Warning: Failed to delete temporary output file in guest: {errorMessage}" );
                VixApi.Vix_FreeBuffer ( errorTextPtr );
            }
            else
            {
                Console.WriteLine ( $"Temporary output file deleted in guest." );
            }

            // Clean up: delete the temporary output file on host
            try
            {
                if ( System.IO.File.Exists ( hostCommandOutputPath ) )
                {
                    System.IO.File.Delete ( hostCommandOutputPath );
                    Console.WriteLine ( $"Temporary output file deleted on host: {hostCommandOutputPath}." );
                }
            }
            catch ( Exception ex )
            {
                Console.WriteLine ( $"Warning: Failed to delete temporary output file on host: {ex.Message}" );
            }
        }
        return commandOutput;
    }
}
}