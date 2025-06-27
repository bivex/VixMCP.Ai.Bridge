using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace VixBindings {
public static class VixApiHelper {

    // VIX API related fields for helper operations
    public static AutoResetEvent _jobEvent = new AutoResetEvent ( false );
    public static ulong _jobResult = VixApi.VIX_OK;

    // VIX API Job Callback
    public static void JobCallback ( int handle, int eventType, int moreEventInfo, IntPtr clientData )
    {
        if ( eventType == ( int ) VixApi.VixEventType.JobCompleted )
        {
            _jobResult = VixApi.VixJob_GetError ( handle );
            _jobEvent.Set();
        }
    }

    // Helper method to execute a command in guest and get its output
    public static CommandResult RunGuestCommandAndGetOutput ( int vmHandle,
            string command,
            string guestCommandOutputPath,
            string hostCommandOutputPath )
    {
        int jobHandle = VixApi.VIX_INVALID_HANDLE;
        string commandOutput = string.Empty;

        // 1. Выполнить команду в госте
        jobHandle = VixApi.VixVM_RunProgramInGuest (
                        vmHandle,
                        "/bin/bash", // Program to run (bash for Linux)
                        $"-c \"{command} > {guestCommandOutputPath}\"", // Arguments: -c to execute command and redirect output
                        VixApi.VixRunProgramOptions.ReturnImmediately, // Options: ReturnImmediately for console app
                        VixApi.VIX_INVALID_HANDLE, // PropertyListHandle
                        JobCallback, // Use the static JobCallback from this class
                        IntPtr.Zero );

        VixApi.VixJob_Wait ( jobHandle, VixApi.VIX_PROPERTY_NONE );
        if ( _jobResult != VixApi.VIX_OK )
        {
            IntPtr errorTextPtr = VixApi.Vix_GetErrorText ( _jobResult, ( string? ) null );
            string errorMessage = Marshal.PtrToStringAnsi ( errorTextPtr ) ?? "Unknown error";
            VixApi.Vix_FreeBuffer ( errorTextPtr );
            return new CommandResult { success = false, error = $"Failed to execute command in guest: {errorMessage}" };
        }

        // 2. Проверить, что файл вывода существует
        System.Threading.Thread.Sleep ( 2000 );
        jobHandle = VixApi.VixVM_FileExistsInGuest (
                        vmHandle,
                        guestCommandOutputPath,
                        JobCallback,
                        IntPtr.Zero );
        VixApi.VixJob_Wait ( jobHandle, VixApi.VIX_PROPERTY_NONE );
        if ( _jobResult != VixApi.VIX_OK )
        {
            IntPtr errorTextPtr = VixApi.Vix_GetErrorText ( _jobResult, ( string? ) null );
            string errorMessage = Marshal.PtrToStringAnsi ( errorTextPtr ) ?? "Unknown error";
            VixApi.Vix_FreeBuffer ( errorTextPtr );
            return new CommandResult { success = false, error = $"Output file not found or check failed: {errorMessage}" };
        }

        // 3. Скопировать файл вывода на хост
        jobHandle = VixApi.VixVM_CopyFileFromGuestToHost (
                        vmHandle,
                        guestCommandOutputPath,
                        hostCommandOutputPath,
                        0, // Options
                        VixApi.VIX_INVALID_HANDLE,
                        JobCallback,
                        IntPtr.Zero );
        VixApi.VixJob_Wait ( jobHandle, VixApi.VIX_PROPERTY_NONE );
        if ( _jobResult != VixApi.VIX_OK )
        {
            IntPtr errorTextPtr = VixApi.Vix_GetErrorText ( _jobResult, ( string? ) null );
            string errorMessage = Marshal.PtrToStringAnsi ( errorTextPtr ) ?? "Unknown error";
            VixApi.Vix_FreeBuffer ( errorTextPtr );
            return new CommandResult { success = false, error = $"Failed to copy command output file: {errorMessage}" };
        }

        // 4. Прочитать вывод
        try
        {
            string output = System.IO.File.ReadAllText ( hostCommandOutputPath, System.Text.Encoding.UTF8 );
            commandOutput = output;
        }
        catch ( Exception ex )
        {
            return new CommandResult { success = false, error = $"Reading command output file on host failed: {ex.Message}" };
        }

        // 5. Удалить временные файлы (гость и хост)
        jobHandle = VixApi.VixVM_DeleteFileInGuest (
                        vmHandle,
                        guestCommandOutputPath,
                        JobCallback,
                        IntPtr.Zero );
        VixApi.VixJob_Wait ( jobHandle, VixApi.VIX_PROPERTY_NONE );
        try
        {
            if ( System.IO.File.Exists ( hostCommandOutputPath ) )
            { System.IO.File.Delete ( hostCommandOutputPath ); }
        }
        catch { /* ignore */ }

        return new CommandResult { success = true, output = commandOutput };
    }
}

}