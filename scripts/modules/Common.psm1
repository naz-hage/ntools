<#
.SYNOPSIS
    Common utility functions used across NTools PowerShell scripts.

.DESCRIPTION
    This module provides shared functionality for console output formatting, 
    file operations, error handling, and other common tasks used throughout 
    the NTools PowerShell scripts.

.FUNCTIONS
    | Function Name               | Description                                                   |
    |-----------------------------|---------------------------------------------------------------|
    | Write-Info                  | Writes informational messages with consistent formatting.     |
    | Write-Success               | Writes success messages with consistent formatting.           |
    | Write-Warning               | Writes warning messages with consistent formatting.           |
    | Write-Error                 | Writes error messages with consistent formatting.             |
    | Write-OutputMessage         | General output message function with prefix and color support.|
    | Test-PathSafely             | Tests if a path exists with error handling.                   |
    | Get-ScriptDirectory         | Gets the directory where the calling script is located.       |
    | Format-ElapsedTime          | Formats elapsed time for display.                             |

.EXAMPLE
    Import-Module .\scripts\modules\Common.psm1
    Write-Info "Starting process..."
    Write-Success "Process completed successfully"
    Write-Warning "Minor issue detected"
    Write-Error "Critical error occurred"

.NOTES
    This module is designed to be imported by other NTools scripts to provide
    consistent output formatting and common functionality.
#>

# Global counters for tracking messages (used by scripts that need them)
$script:InfoCount = 0
$script:SuccessCount = 0
$script:WarningCount = 0
$script:ErrorCount = 0

function Write-Info {
    <#
    .SYNOPSIS
        Writes an informational message with consistent formatting.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )
    Write-Host "[INFO] $Message" -ForegroundColor Cyan
    $script:InfoCount++
}

function Write-Success {
    <#
    .SYNOPSIS
        Writes a success message with consistent formatting.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
    $script:SuccessCount++
}

function Write-Warning {
    <#
    .SYNOPSIS
        Writes a warning message with consistent formatting.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
    $script:WarningCount++
}

function Write-Error {
    <#
    .SYNOPSIS
        Writes an error message with consistent formatting.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )
    Write-Host "[ERROR] $Message" -ForegroundColor Red
    $script:ErrorCount++
}

function Write-OutputMessage {
    <#
    .SYNOPSIS
        General output message function with prefix and color support.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Prefix,
        
        [Parameter(Mandatory = $true)]
        [string]$Message,
        
        [Parameter(Mandatory = $false)]
        [System.ConsoleColor]$ForegroundColor = [System.ConsoleColor]::White,
        
        [Parameter(Mandatory = $false)]
        [switch]$NoNewline
    )
    
    $formattedMessage = "[$Prefix] $Message"
    if ($NoNewline) {
        Write-Host $formattedMessage -ForegroundColor $ForegroundColor -NoNewline
    } else {
        Write-Host $formattedMessage -ForegroundColor $ForegroundColor
    }
}

function Test-PathSafely {
    <#
    .SYNOPSIS
        Tests if a path exists with error handling.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )
    
    try {
        return Test-Path -Path $Path -ErrorAction Stop
    }
    catch {
        Write-Warning "Could not test path '$Path': $($_.Exception.Message)"
        return $false
    }
}

function Get-ScriptDirectory {
    <#
    .SYNOPSIS
        Gets the directory where the calling script is located.
    #>
    try {
        return Split-Path -Parent $MyInvocation.ScriptName
    }
    catch {
        # Fallback to current directory if script name is not available
        return Get-Location
    }
}

function Format-ElapsedTime {
    <#
    .SYNOPSIS
        Formats elapsed time for display.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [TimeSpan]$TimeSpan
    )
    
    if ($TimeSpan.TotalDays -ge 1) {
        return "{0:N0} days, {1:N0} hours, {2:N0} minutes" -f $TimeSpan.Days, $TimeSpan.Hours, $TimeSpan.Minutes
    }
    elseif ($TimeSpan.TotalHours -ge 1) {
        return "{0:N0} hours, {1:N0} minutes, {2:N0} seconds" -f $TimeSpan.Hours, $TimeSpan.Minutes, $TimeSpan.Seconds
    }
    elseif ($TimeSpan.TotalMinutes -ge 1) {
        return "{0:N0} minutes, {1:N0} seconds" -f $TimeSpan.Minutes, $TimeSpan.Seconds
    }
    else {
        return "{0:N2} seconds" -f $TimeSpan.TotalSeconds
    }
}

function Get-MessageCounts {
    <#
    .SYNOPSIS
        Gets the current message counts.
    #>
    return @{
        Info = $script:InfoCount
        Success = $script:SuccessCount
        Warning = $script:WarningCount
        Error = $script:ErrorCount
    }
}

function Reset-MessageCounts {
    <#
    .SYNOPSIS
        Resets all message counters to zero.
    #>
    $script:InfoCount = 0
    $script:SuccessCount = 0
    $script:WarningCount = 0
    $script:ErrorCount = 0
}

# Export functions
Export-ModuleMember -Function Write-Info, Write-Success, Write-Warning, Write-Error, Write-OutputMessage, Test-PathSafely, Get-ScriptDirectory, Format-ElapsedTime, Get-MessageCounts, Reset-MessageCounts