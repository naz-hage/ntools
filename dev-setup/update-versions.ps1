# Script to automatically update ntools.md with versions from JSON files
param(
    [string]$DevSetupPath = ".",
    [string]$DocsPath = "..\docs\ntools\ntools.md"
)

# Function to extract version from JSON file
function Get-VersionFromJson {
    param([string]$JsonPath)
    
    try {
        $json = Get-Content $JsonPath | ConvertFrom-Json
        $appInfo = $json.NbuildAppList[0]
        return @{
            Name = $appInfo.Name
            Version = $appInfo.Version
            Found = $true
        }
    }
    catch {
        # Log a warning for the user if parsing fails (for visibility)
        Write-Warning "Failed to parse $($JsonPath): $($_)"
        # Return a hashtable indicating failure so the calling code can skip this file
        return @{ Found = $false }
    }
}

# Function to update markdown table
function Update-MarkdownTable {
    param(
        [string]$MarkdownPath,
        [hashtable]$VersionMap
    )
    
    $content = Get-Content $MarkdownPath
    $today = Get-Date -Format "dd-MMM-yy"
    
    for ($i = 0; $i -lt $content.Length; $i++) {
        $line = $content[$i]
        
        # Check if this is a table row with a tool
        if ($line -match '^\| \[([^\]]+)\]') {
            $toolName = $matches[1]
            
            # Find matching version in our map
            foreach ($key in $VersionMap.Keys) {
                $versionInfo = $VersionMap[$key]
                
                # Match tool names (flexible matching)
                if ($toolName -like "*$($versionInfo.Name)*" -or 
                    $versionInfo.Name -like "*$toolName*" -or
                    ($toolName -eq "Node.js" -and $versionInfo.Name -eq "Node.js") -or
                    ($toolName -eq "Git for Windows" -and $versionInfo.Name -eq "Git for Windows") -or
                    ($toolName -eq "PowerShell" -and $versionInfo.Name -eq "Powershell") -or
                    ($toolName -eq "Visual Studio Code" -and $versionInfo.Name -eq "Visual Studio Code") -or
                    ($toolName -eq "Python" -and $versionInfo.Name -eq "Python") -or
                    ($toolName -eq "NuGet" -and $versionInfo.Name -eq "Nuget") -or
                    ($toolName -eq "Terraform" -and $versionInfo.Name -eq "Terraform") -or
                    ($toolName -eq "Terraform Lint" -and $versionInfo.Name -eq "terraform lint") -or
                    ($toolName -eq "kubernetes" -and $versionInfo.Name -eq "kubectl") -or
                    ($toolName -eq "minikube" -and $versionInfo.Name -eq "minikube") -or
                    ($toolName -eq "Azure CLI" -and $versionInfo.Name -eq "AzureCLI") -or
                    ($toolName -eq "MongoDB Community Server" -and $versionInfo.Name -eq "MongoDB") -or
                    ($toolName -eq "pnpm" -and $versionInfo.Name -eq "pnpm") -or
                    ($toolName -eq "Ntools" -and $versionInfo.Name -eq "Ntools")) {
                    
                    # Update the line with new version and date
                    if ($line -match '(\| \[[^\]]+\]\([^)]+\)\s+\| )([^|]+)(\| )([^|]+)(\|.*)') {
                        $newLine = $matches[1] + $versionInfo.Version.PadRight(11) + $matches[3] + $today.PadRight(15) + $matches[5]
                        $content[$i] = $newLine
                        Write-Host "Updated $toolName: $($versionInfo.Version)" -ForegroundColor Green
                        break
                    }
                }
            }
        }
    }
    
    # Write updated content back to file
    $content | Set-Content $MarkdownPath -Encoding UTF8
}

# Main execution
Write-Host "Starting version update process..." -ForegroundColor Cyan

# Get all JSON files
$jsonFiles = Get-ChildItem -Path $DevSetupPath -Filter "*.json"
$versionMap = @{}

# Extract versions from JSON files
foreach ($file in $jsonFiles) {
    $versionInfo = Get-VersionFromJson $file.FullName
    if ($versionInfo.Found) {
        $versionMap[$file.Name] = $versionInfo
        Write-Host "Found $($versionInfo.Name): $($versionInfo.Version)" -ForegroundColor Yellow
    }
}

# Update markdown file
if (Test-Path $DocsPath) {
    Update-MarkdownTable -MarkdownPath $DocsPath -VersionMap $versionMap
    Write-Host "Updated $DocsPath successfully!" -ForegroundColor Green
} else {
    Write-Error "Markdown file not found: $DocsPath"
}

Write-Host "Version update process completed!" -ForegroundColor Cyan
