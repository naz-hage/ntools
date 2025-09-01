<#
.SYNOPSIS
Scans the repository for likely hardcoded secrets, UNC paths, and risky commands.

.DESCRIPTION
Searches text files under the repository root (excluding common binary folders) for a set of regex patterns.
Generates a JSON report and prints a summary to stdout. Optionally fails with exit code 1 when high-confidence issues are found.

.PARAMETER FailOnHighConfidence
If set, the script exits with code 1 when any High severity matches are found.

.PARAMETER Output
Path to write JSON report.

.PARAMETER Allowlist
Path to allowlist file (one substring per line) that will suppress matches containing any allowlist entry.

.EXAMPLE
.
    .\dev-scripts\audit-hardcoded.ps1 -FailOnHighConfidence

#>
[CmdletBinding()]
param(
    [switch]$FailOnHighConfidence,
    [string]$Output = "dev-scripts/audit-report.json",
    [string]$Allowlist = "dev-scripts/audit-allowlist.txt"
)

Write-Host "Starting repo audit for hardcoded literals..."

$root = Resolve-Path -Path "." | Select-Object -ExpandProperty Path

# File globs to skip
$excludeDirs = @('.git', 'bin', 'obj', 'Debug', 'Release', 'node_modules',
                'dist', 'coverage', 'TestResults', 'temp_test', 'site', 
                'pwsh', 'docs', 'HomeStandardTests', '.temp', '.venv', 'dev-scripts',
                'devops-scripts', 'homecliTests', 'CliClassTests', 'homecli-e2e-test')

# File names or substrings to skip entirely (useful to ignore specific files)
$excludeFiles = @('nbuild.targets', 'audit-allowlist.txt')
$excludeFiles += @('nbuild.log', 'targets.md', 'Home.sln')

# Skip files by noisy extensions (treat these as non-scan targets)
$skipExtensions = @('.sln', '.log', '.md', '.csproj', '.fsproj', '.vbproj', '.vcxproj', '.user', '.suo', '.db', '.db-journal',
                   '.png', '.jpg', '.jpeg', '.gif', '.ico', '.bmp', '.tiff', '.ttf', '.woff', '.woff2', '.eot', '.otf', '.psd', 
                   '.dat', '.zip', '.exe', '.dll', '.pdb', '.bin', '.bat')

# Optionally load additional file-exclude entries from dev-scripts/audit-exclude.txt
$excludeFilePath = Join-Path -Path $root -ChildPath 'dev-scripts/audit-exclude.txt'
if (Test-Path $excludeFilePath) {
    try {
        $extra = Get-Content $excludeFilePath | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' }
        if ($extra) { $excludeFiles += $extra }
        Write-Host "Loaded $($extra.Count) file-exclude entries from $excludeFilePath"
    } catch {
        Write-Warning ("Failed to read exclude file {0}: {1}" -f $excludeFilePath, $_.Exception.Message)
    }
}

# Define patterns to look for
$patterns = @(
    # Match UNC paths like \\server\share (require two leading backslashes)
        @{ Name = 'UNC Path'; Regex = '(?<!\S)\\\\[^\\\s]+\\[^\\\s]+'; Severity = 'High'; Description = 'UNC network path (\\server\\path)'; },
    @{ Name = 'Net Use'; Regex = '(?i)\bnet\s+use\b'; Severity = 'High'; Description = 'net use command mapping network drives'; },
    @{ Name = 'Common Test Code 12345678'; Regex = '\b12345678\b'; Severity = 'High'; Description = 'Specific known test code found in this repo'; },
    # Password-like assignment: match a variable/keyword followed by a colon/equals and a non-whitespace token of length >=4
    @{ Name = 'Password Assignment'; Regex = '(?i)\b(password|pass|pwd|code)\b\s*[:=]\s*([^\s]{4,})'; Severity = 'Medium'; Description = 'Possible password-like assignment'; },
    @{ Name = 'Long Numeric Literal'; Regex = '(?<![A-Za-z0-9])\d{6,12}(?![A-Za-z0-9])'; Severity = 'Low'; Description = 'Long numeric literal (may be a code)'; }
)

# Load allowlist
$allowlistEntries = @()
if (Test-Path $Allowlist) {
    try {
        $allowlistEntries = Get-Content $Allowlist | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' }
        Write-Host "Loaded $(($allowlistEntries).Count) allowlist entries from $Allowlist"
    } catch {
        Write-Warning "Failed to read allowlist $($Allowlist): $($_)"
    }
}

function Test-IsExcludedFile($filePath) {
    foreach ($d in $excludeDirs) {
        if ($filePath -like "*\$d\*") { return $true }
    }
    foreach ($f in $excludeFiles) {
        if ($f -and ($filePath -like "*$f*" -or [IO.Path]::GetFileName($filePath) -ieq $f)) { return $true }
    }
    # Skip very large files (likely binary blobs) - threshold 1MB
    try {
        $fi = Get-Item -LiteralPath $filePath -ErrorAction SilentlyContinue
        if ($fi -and $fi.Length -gt 1MB) { return $true }
    } catch {
        # ignore
    }

    # Quick binary sniff: skip files that contain a NUL byte in the first 4KB
    try {
        $fs = [System.IO.File]::OpenRead($filePath)
        $buf = New-Object byte[] 4096
        $read = $fs.Read($buf, 0, $buf.Length)
        $fs.Close()
        for ($bi = 0; $bi -lt $read; $bi++) {
            if ($buf[$bi] -eq 0) { return $true }
        }
    } catch {
        # ignore errors and continue; unreadable files will be skipped later
    }
    # Skip by extension if configured
    $fileExt = [IO.Path]::GetExtension($filePath)
    if ($fileExt -and ($skipExtensions -contains $fileExt.ToLower())) { return $true }
    # skip binary-like extensions
    $ext = [IO.Path]::GetExtension($filePath)
    $binaryExts = '.png','.jpg','.jpeg','.gif','.zip','.exe','.dll','.pdb','.bin','.so','.o','.class','.ico','.bmp','.tiff','.ttf','.woff','.woff2','.eot','.otf','.psd','.dat'
    if ($ext -and ($binaryExts -contains $ext.ToLower())) { return $true }
    return $false
}

$findings = @()

# Get candidate files
$files = Get-ChildItem -Path $root -Recurse -File -ErrorAction SilentlyContinue | Where-Object { -not (Test-IsExcludedFile $_.FullName) }

Write-Host "Scanning $($files.Count) files..."

foreach ($file in $files) {
    try {
        $lines = Get-Content -LiteralPath $file.FullName -ErrorAction Stop
    } catch {
        # skip unreadable files
        continue
    }

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        foreach ($p in $patterns) {
            $regex = $p.Regex
            $rgxMatches = [regex]::Matches($line, $regex)
            if ($rgxMatches.Count -gt 0) {
                foreach ($m in $rgxMatches) {
                    $matchedText = $m.Value
                    # apply allowlist filter
                    $ignored = $false
                    foreach ($allow in $allowlistEntries) {
                        if ($allow -and ($file.FullName -like "*$allow*" -or $matchedText -like "*$allow*")) { $ignored = $true; break }
                    }
                    if ($ignored) { continue }

                    # Post-match heuristics to reduce false positives
                    # - ignore MSBuild variables like $(...)
                    # - ignore matches containing quotes or control characters
                    # - ignore excessively long matches
                    # - ignore regex-like matches (character classes, escaped dots, alternation)
                    # - ignore when the surrounding line appears to be a regex literal or a Regex constructor
                    if ($matchedText -match '\$\(') { continue }
                    if ($matchedText -match '"') { continue }
                    if ($matchedText -match "'") { continue }
                    if ($matchedText -match '[\x00-\x08\x0B\x0C\x0E-\x1F]') { continue }
                    if ($matchedText.Length -gt 200) { continue }
                    if ($matchedText -match '[\[\]\(\)\|]' -or $matchedText -match '\\.') { continue }
                    if ($line -match '@"' -or $line -match 'new\s+Regex\(' -or $line -match 'Regex\(') { continue }

                    $finding = [PSCustomObject]@{
                        File = (Resolve-Path -LiteralPath $file.FullName).Path
                        LineNumber = $i + 1
                        Line = $line.Trim()
                        Pattern = $p.Name
                        Match = $matchedText
                        Severity = $p.Severity
                        Description = $p.Description
                    }
                    $findings += $finding
                }
            }
        }
    }
}

# Summarize
$summary = $findings | Group-Object -Property Severity | ForEach-Object {
    [PSCustomObject]@{ Severity = $_.Name; Count = $_.Count }
}

Write-Host "\nAudit summary:"
if ($summary) {
    $summary | Sort-Object {switch ($_.Severity) { 'High' {0 } 'Medium' {1} 'Low' {2} default {3}} } | ForEach-Object {
        Write-Host "  $($_.Severity): $($_.Count)"
    }
} else {
    Write-Host "  No findings"
}

# Write JSON report
try {
    $report = [PSCustomObject]@{
        Root = $root
        ScanDate = (Get-Date).ToString('o')
        Findings = $findings
        Summary = $summary
    }
    $json = $report | ConvertTo-Json -Depth 5

    # Determine output path: if caller passed an absolute path, use it as-is; otherwise treat it relative to repo root
    if ([string]::IsNullOrWhiteSpace($Output)) {
        $outPath = Join-Path -Path $root -ChildPath 'dev-scripts/audit-report.json'
    } elseif ([IO.Path]::IsPathRooted($Output)) {
        $outPath = $Output
    } else {
        $outPath = Join-Path -Path $root -ChildPath $Output
    }

    # Normalize to full path and ensure parent directory exists
    try {
        $outPath = [IO.Path]::GetFullPath($outPath)
    } catch {
        # fallback to the original value if GetFullPath fails for some reason
    }
    $outDir = Split-Path -Path $outPath -Parent
    if ($outDir -and -not (Test-Path $outDir)) {
        New-Item -ItemType Directory -Path $outDir -Force | Out-Null
    }

    $json | Set-Content -LiteralPath $outPath -Encoding UTF8
    Write-Host "\nReport written to: $outPath"
} catch {
    Write-Warning "Failed to write report: $_"
}

# Print top 20 findings
if ($findings.Count -gt 0) {
    Write-Host "\nTop findings (first 50):"
    $findings | Sort-Object -Property @{Expression={switch ($_.Severity) {'High' {0} 'Medium' {1} 'Low' {2} default {3}}}} | Select-Object -First 50 | ForEach-Object {
        Write-Host "[$($_.Severity)] $($_.File):$($_.LineNumber) -> $($_.Pattern) -> $($_.Match)"
    }
}

# Exit code behaviour
$highCount = ($findings | Where-Object { $_.Severity -eq 'High' }).Count
if ($FailOnHighConfidence -and $highCount -gt 0) {
    Write-Host "\nHigh-confidence issues found: $highCount. Exiting with code 1." -ForegroundColor Red
    exit 1
} else {
    Write-Host "\nScan complete. High-confidence issues: $highCount"
    exit 0
}
