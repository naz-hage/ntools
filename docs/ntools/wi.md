# wi: Azure DevOps Work Item CLI Utility

`wi` is a command-line tool for creating Product Backlog Items (PBIs) and child tasks in Azure DevOps from a services file. It is built using `System.CommandLine` and is part of the ntools suite.

## Features

- **Create PBIs** for multiple services from a text file
- **Create child tasks** automatically with the same title as the PBI
- **Azure DevOps integration** via REST API
- **Batch processing** for multiple services
- **Environment-based configuration** for organization and project settings

---

## Prerequisites

### Environment Variables

The following environment variables must be set:

- **`PAT`** (Required): Personal Access Token for Azure DevOps authentication
- **`AZURE_DEVOPS_ORGANIZATION`** (Optional): Azure DevOps organization URL  
  Default: `https://dev.azure.com/nazh`
- **`AZURE_DEVOPS_PROJECT`** (Optional): Azure DevOps project name  
  Default: `Proto`

### Personal Access Token Setup

1. Go to Azure DevOps → User Settings → Personal Access Tokens
2. Create a new token with **Work Items (Read & Write)** permissions
3. Set the `PAT` environment variable:
   ```powershell
   $env:PAT = "your-personal-access-token"
   ```

---

## Usage

### Create PBIs for Multiple Services

Creates PBIs for each service listed in a file, with automatic child task creation:

```sh
wi --services services.txt --parentId 12345
```

**Options:**
- `-s`, `--services` (Required): Path to services.txt file containing service names (one per line)
- `-p`, `--parentId` (Required): Parent work item ID to link the PBIs to

### Create Child Task for Specific PBI

Creates a child task for an existing PBI:

```sh
wi --services services.txt --parentId 12345 --childTaskOfPbiId 67890
```

**Additional Option:**
- `-c`, `--childTaskOfPbiId`: PBI ID to create a child task for (skips PBI creation)

---

## Services File Format

The services file should contain one service name per line:

**Example `services.txt`:**
```
ServiceA
ServiceB
ServiceC
UserManagementService
PaymentProcessingService
```

---

## Work Item Creation Details

### PBI Creation
- **Title Format**: `{ServiceName}: update pipeline to perform SCA`
- **Type**: Product Backlog Item
- **Parent**: Links to the specified parent work item ID

### Child Task Creation
- **Title**: Same as the parent PBI
- **Type**: Task
- **Parent**: Links to the created PBI

---

## Examples

### Basic Usage
```sh
# Set required environment variables
$env:PAT = "your-pat-token"
$env:AZURE_DEVOPS_ORGANIZATION = "https://dev.azure.com/yourorg"
$env:AZURE_DEVOPS_PROJECT = "YourProject"

# Create PBIs for all services in the file
wi --services my-services.txt --parentId 54321
```

### Create Child Task Only
```sh
# Create a child task for an existing PBI
wi --services services.txt --parentId 54321 --childTaskOfPbiId 98765
```

---

## Output

The tool provides console output showing:
- Organization and project being used
- Success/failure status for each PBI creation
- Created work item IDs

**Example Output:**
```
Organization: https://dev.azure.com/yourorg
Project: YourProject
Created PBI for ServiceA: ID 12346
Created child task for PBI 12346
Created PBI for ServiceB: ID 12347
Created child task for PBI 12347
```

---

## Error Handling

Common issues and solutions:

- **"PAT environment variable is not set"**: Set the `PAT` environment variable with a valid Azure DevOps Personal Access Token
- **"Failed to read services file"**: Ensure the services file path is correct and accessible
- **"Failed to create PBI"**: Check PAT permissions and Azure DevOps connectivity
- **Authentication errors**: Verify PAT is valid and has Work Items read/write permissions
