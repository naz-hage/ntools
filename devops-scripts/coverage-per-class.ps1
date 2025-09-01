param(
    [string]$CovPath,
    [string]$OutPath
)

if ([string]::IsNullOrWhiteSpace($CovPath)) {
    $CovPath = Join-Path (Get-Location) 'coverage.cobertura.xml'
}

if (!(Test-Path $CovPath)) {
    Write-Error "coverage.cobertura.xml not found. Run 'nb COVERAGE' first."
    exit 2
}


try {
    [xml]$xml = Get-Content $CovPath
} catch {
    Write-Error "Failed to read XML file: $CovPath - $($_.Exception.Message)"
    exit 3
}

# Exclude packages that look like test projects (case-insensitive 'test' or 'e2e')
$packages = $xml.coverage.packages.package | Where-Object { $_.name -notmatch '(?i)test|e2e' }
if ($null -eq $packages -or $packages.Count -eq 0) {
    Write-Output "No non-test package coverage data found in: $CovPath"
    exit 0
}

$items = @()
foreach ($pkg in $packages) {
    $classes = $pkg.classes.class
    if ($null -eq $classes) { continue }
    foreach ($c in $classes) {
        $name = $c.GetAttribute('name')
        $rate = $c.GetAttribute('line-rate')
        $pct = 0.0
        if ($rate -ne $null -and $rate -ne '') { $pct = [double]$rate * 100.0 }
        $items += [pscustomobject]@{ Name = $name; Pct = $pct }
    }
}

$items | Sort-Object -Property Pct -Descending | ForEach-Object { Write-Output (("{0,6:N2}%  {1}" -f $_.Pct, $_.Name)) }

# If an output path was provided, write a Markdown artifact (table) for CI consumption
if (-not [string]::IsNullOrWhiteSpace($OutPath)) {
    try {
        $outDir = Split-Path -Parent $OutPath
        if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }

        $md = @()
        $md += "# Per-class Code Coverage"
        $md += "Generated: $(Get-Date -Format o)"
        $md += ""
        $md += "| Coverage | Class |"
        $md += "|---:|---|"

        $items | Sort-Object -Property Pct -Descending | ForEach-Object {
            $pct = ("{0,6:N2}%" -f $_.Pct)
            $name = $_.Name
            $md += "| $pct | $name |"
        }

        $md -join "`n" | Out-File -FilePath $OutPath -Encoding UTF8
        Write-Output "Wrote per-class coverage artifact: $OutPath"
    } catch {
        Write-Warning "Failed to write artifact to $OutPath - $($_.Exception.Message)"
    }
}

exit 0
