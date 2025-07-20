
param(
    [string]$projectName = "nbTests",
    [string]$solutionDir = "C:\source\ntools"
)

$solutionDir = "C:\source\ntools"
$projectPath = "$solutionDir\$projectName\$projectName.csproj"

# Check if project file exists
if (-not (Test-Path $projectPath)) {
    Write-Error "Project file does not exist: $projectPath"
    Write-Host "Available test projects:"
    Get-ChildItem -Path $solutionDir -Filter "*Tests.csproj" -Recurse | ForEach-Object {
        Write-Host "  - $($_.Directory.Name)"
    }
    exit 1
}

Write-Host "Running tests for project: $projectPath"

# Run tests and collect coverage
dotnet test $projectPath -c Release --collect:"XPlat Code Coverage"

# Check if reportgenerator is installed, if not install it
if (-not (Get-Command reportgenerator -ErrorAction SilentlyContinue)) {
    Write-Host "reportgenerator not found. Installing as a global tool..."
    dotnet tool install -g reportgenerator
    $env:PATH += ";$([System.Environment]::GetFolderPath('UserProfile'))\.dotnet\tools"
}

# Generate coverage report using the actual project's TestResults
reportgenerator -reports:"$solutionDir\$projectName\TestResults\**\coverage.cobertura.xml" -targetdir:"$solutionDir\TestResults\CoverageReport"

Write-Host "$projectName executed and code coverage report generated in TestResults\CoverageReport."

# Launch the HTML coverage report
$reportPath = "$solutionDir\TestResults\CoverageReport\index.html"
if (Test-Path $reportPath) {
    Write-Host "Opening coverage report: $reportPath"
    Start-Process $reportPath
} else {
    Write-Warning "Coverage report not found at: $reportPath"
}