# 📸 Usage Examples

This document showcases real examples of the VMware VIX MCP server in action through Cursor IDE.

## Real-World Demonstrations

Here are actual screenshots demonstrating how seamlessly you can interact with VMware VMs through natural language in Cursor IDE, with the AI automatically selecting and executing the appropriate MCP tools.

### 1. Connecting to VM
![Connect to VM](images/Connect%20to%20VM.png)

**Example scenario:** Establishing connection to Ubuntu VM using natural language commands
- User simply describes what they want to do
- AI automatically calls the `ConnectAndLogin` MCP tool
- Connection established and ready for further operations

### 2. Checking System Uptime
![Check uptime](images/Check%20uptime.png)

**Example scenario:** Executing uptime command to see how long the VM has been running
- Natural language request: "check uptime"
- AI uses `ExecCommandInGuestSession` tool
- Returns formatted uptime information with load averages

### 3. Checking User Privileges
![Check Privs](images/Check%20Privs.png)

**Example scenario:** Using id command to verify current user permissions in the guest OS
- Request: "Check my privileges"
- AI executes `id` command through MCP tool
- Shows user ID, group membership, and privilege level

### 4. Monitoring Disk Usage
![Check disk](images/Check%20disk.png)

**Example scenario:** Running df -h command to check filesystem usage and available space
- Natural language: "Execute 'df -h' command in the guest OS to check disk usage"
- AI automatically formats and presents disk usage information
- Shows filesystem sizes, used space, and usage percentages

## Key Benefits Demonstrated

### 🤖 **AI-Driven Automation**
- No need to remember complex VIX API syntax
- Natural language commands automatically translated to proper tool calls
- Seamless integration with development workflow

### 🔄 **Session Management**
- Persistent connections maintained across multiple commands
- No need to reconnect for each operation
- Efficient resource usage

### 📊 **Rich Output Formatting**
- Command results properly formatted and presented
- Error handling with meaningful messages
- Structured data presentation

### 🛠️ **Development Integration**
- Works directly in Cursor IDE alongside your code
- No context switching between tools
- AI understands intent and selects appropriate operations

## Getting Started

To reproduce these examples:

1. **Set up the MCP server** following the main README instructions
2. **Configure Cursor IDE** with the MCP server
3. **Start with simple commands** like those shown above
4. **Experiment with natural language** - the AI is quite flexible in understanding intent

## More Examples

Want to see more examples? Try these natural language commands:

- *"List all running processes in the VM"*
- *"Check if I can connect to the VM with these credentials"*
- *"Show me the current working directory"*
- *"Check network interfaces in the guest OS"*
- *"Display system information"*

The AI will automatically select the appropriate MCP tools and execute the necessary commands to fulfill your requests. 
