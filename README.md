# .NET Bindings for VMware VIX API and MCP Integration

This repository contains .NET bindings for the VMware VIX (Virtual Infrastructure eXtension) API and a Model Context Protocol (MCP) server implementation for VM management operations.

## Projects

### 🔧 VixBindings
A .NET 9.0 library providing comprehensive C# bindings for the VMware VIX API, enabling programmatic control of VMware virtual machines.

### 🚀 McpProcessToolSample  
A .NET 9.0 console application implementing an MCP server that exposes VMware VIX operations as tools for AI assistants and automation workflows.

## Features

### VixBindings Library
- **Complete VIX API Coverage**: Full P/Invoke declarations for VMware VIX API
- **VM Lifecycle Management**: Power on/off, suspend, reset operations
- **Guest OS Operations**: File transfer, command execution, process management
- **Snapshot Management**: Create, revert, and manage VM snapshots
- **Shared Folders**: Configure and manage host-guest shared directories
- **Error Handling**: Comprehensive error checking and reporting
- **Helper Utilities**: Simplified command execution and file operations

### MCP Server Implementation
- **Session Management**: Persistent VM connections for multiple operations
- **Guest Command Execution**: Run commands in guest OS and retrieve output
- **Process Monitoring**: List and manage processes in guest VMs
- **Connection Testing**: Validate VM connectivity and guest access
- **MCP Protocol**: Standard Model Context Protocol for AI integration

## Prerequisites

### System Requirements
- Windows 10/11 (x64)
- .NET 9.0 Runtime
- VMware Workstation Pro/Player or VMware vSphere
- VMware VIX API (Vix64AllProductsDyn.dll)

### VMware Setup
1. Install VMware Workstation Pro/Player or connect to vSphere
2. Ensure target VMs have VMware Tools installed
3. Configure guest OS with appropriate user accounts
4. Place `Vix64AllProductsDyn.dll` in the application directory

## Quick Start

### Building the Projects

```bash
# Clone the repository
git clone <repository-url>
cd dotnet-bindings

# Build VixBindings library
cd VixBindings
dotnet build

# Build MCP server
cd ../McpProcessToolSample
dotnet build
```

### Running the MCP Server

```bash
cd McpProcessToolSample
dotnet run
```

The MCP server will start with stdio transport, ready to receive MCP protocol messages.

### Using VixBindings Library

```csharp
using VixBindings;

// Basic VM connection example
var hostHandle = VixApi.VixHost_Connect(/* connection parameters */);
var vmHandle = VixApi.VixHost_OpenVM(hostHandle, "path/to/vm.vmx", /*...*/);

// Wait for VMware Tools and login
VixApi.VixVM_WaitForToolsInGuest(vmHandle, 600, /*...*/);
VixApi.VixVM_LoginInGuest(vmHandle, "username", "password", /*...*/);

// Execute command in guest
var result = VixApiHelper.RunGuestCommandAndGetOutput(
    vmHandle, 
    "ps aux", 
    "/tmp/output.txt", 
    "C:\\temp\\output.txt"
);
```

## MCP Tools Available

### `ConnectAndLogin`
Establishes a persistent session with a VM and guest OS.

**Parameters:**
- `hostName`: VMware host address
- `hostPort`: Connection port
- `hostUserName`: Host authentication username  
- `hostPassword`: Host authentication password
- `vmxFilePath`: Path to VM configuration file
- `guestUserName`: Guest OS username
- `guestPassword`: Guest OS password

### `ExecCommandInGuestSession`
Executes commands in the guest OS using an active session.

**Parameters:**
- `command`: Command to execute
- `guestTempPath`: Temporary directory in guest (default: `/tmp`)
- `hostTempPath`: Temporary directory on host (default: `C:\\temp`)

### `ListAllProcessesInGuest`
Lists all running processes in the guest VM.

**Parameters:**
- `guestTempPath`: Temporary directory in guest
- `hostTempPath`: Temporary directory on host

### `CheckGuestConnection`
Tests connectivity and authentication without maintaining a session.

## Configuration

### Project Dependencies

**VixBindings.csproj:**
```xml
<PackageReference Include="System.Text.Encoding.CodePages" Version="9.0.6" />
```

**McpProcessToolSample.csproj:**
```xml
<PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.6" />
<PackageReference Include="ModelContextProtocol" Version="0.3.0-preview.1" />
<ProjectReference Include="..\VixBindings\VixBindings.csproj" />
```

### Runtime Requirements
- Ensure `Vix64AllProductsDyn.dll` is accessible to the application
- Configure appropriate temporary directories for file operations
- Set up guest OS user accounts with necessary permissions

## Error Handling

The library provides comprehensive error handling through:
- VIX error code translation to human-readable messages
- Structured error responses in MCP tools
- Timeout handling for long-running operations
- Resource cleanup and handle management

## Security Considerations

- Store VM credentials securely (avoid hardcoding)
- Use least-privilege accounts for guest operations
- Validate and sanitize command inputs
- Monitor temporary file cleanup
- Consider network security for remote VM connections

## Troubleshooting

### Common Issues

**"VIX_E_TOOLS_NOT_RUNNING"**
- Ensure VMware Tools are installed and running in guest
- Check guest OS power state

**"VIX_E_AUTHENTICATION_FAIL"**  
- Verify guest credentials are correct
- Check guest OS user account permissions
- Ensure interactive login is allowed

**"File not found" errors**
- Verify VIX DLL is in the correct location
- Check temporary directory permissions
- Validate VM file paths

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is provided as-is for educational and development purposes. Please ensure compliance with VMware licensing terms when using VIX API.

## Related Documentation

- [VMware VIX API Documentation](https://docs.vmware.com/en/VMware-Workstation-Pro/index.html)
- [Model Context Protocol Specification](https://modelcontextprotocol.io/)
- [.NET 9.0 Documentation](https://docs.microsoft.com/en-us/dotnet/)

## Support

For issues and questions:
1. Check the troubleshooting section above
2. Review VMware VIX API documentation
3. Create an issue in this repository
