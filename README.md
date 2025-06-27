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
1. **Download and install VMware VIX API SDK** from VMware's official website
2. **Accept the VMware SDK Agreement** during SDK installation
3. Install VMware Workstation Pro/Player or connect to vSphere
4. Ensure target VMs have VMware Tools installed
5. Configure guest OS with appropriate user accounts
6. Copy `Vix64AllProductsDyn.dll` from the VIX SDK to the application directory

> **Note**: The VIX DLL is not included in this repository and must be obtained from VMware's official SDK distribution.

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

### Configuring in Cursor IDE

To integrate the MCP server with Cursor IDE, add the following configuration to your MCP settings file (`~/.cursor/mcp.json` or `C:\Users\{Username}\.cursor\mcp.json`):

```json
{
  "mcpServers": {
    "mcpWinAuditServer": {
      "command": "cmd",
      "args": [
        "/c",
        "C:\\path\\to\\your\\McpProcessToolSample.exe"
      ]
    }
  }
}
```

**Configuration Steps:**
1. Build the McpProcessToolSample project: `dotnet build -c Release`
2. Note the output path of `McpProcessToolSample.exe`
3. Update the path in the MCP configuration file
4. Restart Cursor IDE to load the MCP server
5. The VMware VIX tools will be available in Cursor's AI assistant

> **Note**: Ensure the VIX DLL (`Vix64AllProductsDyn.dll`) is in the same directory as the executable or in your system PATH.

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

## Cursor IDE Integration

Once configured in Cursor IDE, you can use natural language to interact with VMware VMs:

**Example prompts:**
- *"Connect to my Ubuntu VM and list all running processes"*
- *"Execute 'df -h' command in the guest OS to check disk usage"*
- *"Check if I can connect to the VM with these credentials"*
- *"Run a system update command in the guest VM"*

The AI assistant will automatically use the appropriate MCP tools based on your requests, handling VM connections, command execution, and result parsing.

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
- Obtain `Vix64AllProductsDyn.dll` from the official VMware VIX SDK and place it in the application directory
- Configure appropriate temporary directories for file operations  
- Set up guest OS user accounts with necessary permissions
- Ensure compliance with VMware SDK Agreement terms

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

### Project Code
The .NET bindings and MCP server implementation in this repository are provided as-is for educational and development purposes.

### VMware VIX API License
**IMPORTANT**: This project uses the VMware VIX API, which is subject to the VMware Software Developer Kit (SDK) Agreement. Key points:

- The VIX API SDK is provided "AS IS" without warranties
- You may use it to create software that communicates with VMware products
- Redistributable Code must be distributed in object code form only
- You cannot reverse engineer or create derivative works of the Redistributable Code
- VMware retains all intellectual property rights to the SDK
- No VMware support is provided for SDK usage
- You cannot represent your software as certified by VMware
- VMware trademarks cannot be used without permission

### Compliance Requirements
Before using this code:
1. **Download the official VIX API SDK** from VMware's website
2. **Review and accept** the complete VMware SDK Agreement
3. **Ensure you have** the proper `Vix64AllProductsDyn.dll` from the official SDK
4. **Comply with all** redistribution and usage restrictions
5. **Do not redistribute** the VMware SDK components separately

### Disclaimer
This repository does not include VMware's proprietary VIX API components. Users must obtain these directly from VMware and comply with VMware's licensing terms. The authors of this repository are not responsible for any licensing violations or improper use of VMware's intellectual property.

## Related Documentation

- [VMware VIX API Documentation](https://docs.vmware.com/en/VMware-Workstation-Pro/index.html)
- [Model Context Protocol Specification](https://modelcontextprotocol.io/)
- [.NET 9.0 Documentation](https://docs.microsoft.com/en-us/dotnet/)

## Support

For issues and questions:
1. Check the troubleshooting section above
2. Review VMware VIX API documentation
3. Create an issue in this repository
