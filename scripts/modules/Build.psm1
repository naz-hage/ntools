<#
.SYNOPSIS
    Build utility functions for NTools PowerShell scripts.

.DESCRIPTION
    This module provides common build-related functionality including project discovery,
    artifact verification, and build process helpers.

.FUNCTIONS
    | Function Name               | Description                                                   |
    |-----------------------------|---------------------------------------------------------------|
    | Get-ProjectFiles            | Gets .NET project files with filtering options.              |
    | Invoke-ProjectPublish       | Publishes a .NET project with consistent settings.           |
    | Test-ArtifactExists         | Tests if a build artifact exists.                            |
    | Get-ProjectVersion          | Gets the version from a project file.                        |
    | Invoke-BuildValidation      | Validates build artifacts and dependencies.                   |

.EXAMPLE
    Import-Module .\scripts\modules\Build.psm1
    $projects = Get-ProjectFiles -ExcludeTests
    foreach ($project in $projects) {
        Invoke-ProjectPublish -ProjectPath $project.FullName -OutputPath "C:\output"
    }

.NOTES
    This module is designed to provide consistent build patterns across all NTools build scripts.
#>

function Get-ProjectFiles {
    <#
    .SYNOPSIS
        Gets .NET project files with filtering options.
    #>
    param(
        [Parameter(Mandatory = $false)]
        [string]$SearchPath = $PSScriptRoot,
        
        [Parameter(Mandatory = $false)]
        [switch]$ExcludeTests,
        
        [Parameter(Mandatory = $false)]
        [string[]]$IncludePatterns = @("*.csproj"),
        
        [Parameter(Mandatory = $false)]
        [string[]]$ExcludePatterns = @()
    )
    
    $projects = Get-ChildItem -Path $SearchPath -Filter "*.csproj" -Recurse
    
    if ($ExcludeTests) {
        $projects = $projects | Where-Object { $_.FullName -notmatch '(?i)test' }
    }
    
    if ($ExcludePatterns.Count -gt 0) {
        foreach ($pattern in $ExcludePatterns) {
            $projects = $projects | Where-Object { $_.FullName -notmatch $pattern }
        }
    }
    
    return $projects
}

function Invoke-ProjectPublish {
    <#
    .SYNOPSIS
        Publishes a .NET project with consistent settings.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath,
        
        [Parameter(Mandatory = $true)]
        [string]$OutputPath,
        
        [Parameter(Mandatory = $false)]
        [string]$Configuration = "Release",
        
        [Parameter(Mandatory = $false)]
        [string]$ProductVersion = $null,
        
        [Parameter(Mandatory = $false)]
        [hashtable]$AdditionalProperties = @{}
    )
    
    if (-not (Test-Path $ProjectPath)) {
        throw "Project file not found: $ProjectPath"
    }
    
    # Ensure output directory exists
    if (-not (Test-Path $OutputPath)) {
        New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null
    }
    
    # Build dotnet publish command
    $publishArgs = @(
        "publish"
        $ProjectPath
        "-c", $Configuration
        "-o", $OutputPath
    )
    
    # Add version if specified
    if ($ProductVersion) {
        $publishArgs += "/p:Version=$ProductVersion"
    }
    
    # Add additional properties
    foreach ($property in $AdditionalProperties.GetEnumerator()) {
        $publishArgs += "/p:$($property.Key)=$($property.Value)"
    }
    
    Write-Host "Publishing $ProjectPath to $OutputPath" -ForegroundColor Cyan
    Write-Host "Command: dotnet $($publishArgs -join ' ')" -ForegroundColor Gray
    
    try {
        & dotnet @publishArgs
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet publish failed with exit code $LASTEXITCODE"
        }
        Write-Host "Successfully published $ProjectPath" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "Failed to publish $ProjectPath : $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

function Test-ArtifactExists {
    <#
    .SYNOPSIS
        Tests if a build artifact exists.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$ArtifactPath,
        
        [Parameter(Mandatory = $false)]
        [string]$ExpectedExtension = $null,
        
        [Parameter(Mandatory = $false)]
        [long]$MinimumSize = 0
    )
    
    $result = @{
        Exists = $false
        Size = 0
        Extension = ""
        Details = ""
    }
    
    if (-not (Test-Path $ArtifactPath)) {
        $result.Details = "Artifact not found: $ArtifactPath"
        return $result
    }
    
    try {
        $fileInfo = Get-Item $ArtifactPath
        $result.Exists = $true
        $result.Size = $fileInfo.Length
        $result.Extension = $fileInfo.Extension
        
        # Check extension if specified
        if ($ExpectedExtension -and $result.Extension -ne $ExpectedExtension) {
            $result.Details = "Expected extension '$ExpectedExtension' but found '$($result.Extension)'"
            return $result
        }
        
        # Check minimum size if specified
        if ($MinimumSize -gt 0 -and $result.Size -lt $MinimumSize) {
            $result.Details = "File size $($result.Size) bytes is below minimum $MinimumSize bytes"
            return $result
        }
        
        $result.Details = "Artifact valid: $($result.Size) bytes"
        return $result
    }
    catch {
        $result.Details = "Error checking artifact: $($_.Exception.Message)"
        return $result
    }
}

function Get-ProjectVersion {
    <#
    .SYNOPSIS
        Gets the version from a project file.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )
    
    if (-not (Test-Path $ProjectPath)) {
        throw "Project file not found: $ProjectPath"
    }
    
    try {
        [xml]$projectXml = Get-Content $ProjectPath
        
        # Look for Version in PropertyGroup
        $versionNode = $projectXml.SelectSingleNode("//Version")
        if ($versionNode) {
            return $versionNode.InnerText
        }
        
        # Look for AssemblyVersion
        $assemblyVersionNode = $projectXml.SelectSingleNode("//AssemblyVersion")
        if ($assemblyVersionNode) {
            return $assemblyVersionNode.InnerText
        }
        
        return $null
    }
    catch {
        throw "Error reading project version from '$ProjectPath': $($_.Exception.Message)"
    }
}

function Invoke-BuildValidation {
    <#
    .SYNOPSIS
        Validates build artifacts and dependencies.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$ArtifactsPath,
        
        [Parameter(Mandatory = $true)]
        [string[]]$ExpectedArtifacts,
        
        [Parameter(Mandatory = $false)]
        [hashtable]$ArtifactSizeChecks = @{}
    )
    
    $results = @{
        Valid = $true
        Details = @()
        MissingArtifacts = @()
        InvalidArtifacts = @()
    }
    
    foreach ($artifact in $ExpectedArtifacts) {
        $artifactPath = Join-Path $ArtifactsPath $artifact
        $minSize = if ($ArtifactSizeChecks.ContainsKey($artifact)) { $ArtifactSizeChecks[$artifact] } else { 0 }
        
        $artifactTest = Test-ArtifactExists -ArtifactPath $artifactPath -MinimumSize $minSize
        
        if (-not $artifactTest.Exists) {
            $results.Valid = $false
            $results.MissingArtifacts += $artifact
            $results.Details += "Missing: $artifact"
        }
        elseif ($artifactTest.Details -notmatch "valid") {
            $results.Valid = $false
            $results.InvalidArtifacts += $artifact
            $results.Details += "Invalid: $artifact - $($artifactTest.Details)"
        }
        else {
            $results.Details += "Valid: $artifact - $($artifactTest.Details)"
        }
    }
    
    return $results
}

# Export functions
Export-ModuleMember -Function Get-ProjectFiles, Invoke-ProjectPublish, Test-ArtifactExists, Get-ProjectVersion, Invoke-BuildValidation