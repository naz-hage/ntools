# Git Repository Tools Configuration
# This file allows you to customize the behavior of git-repo-tools.ps1

# Global settings
$GlobalConfig = @{
    # Default exclude patterns for all repositories
    DefaultExcludePatterns = @(
        "*.log",
        "*.tmp",
        "node_modules/*",
        "bin/*",
        "obj/*",
        ".vs/*",
        "Debug/*",
        "Release/*",
        "TestResults/*",
        "*.user",
        "*.suo"
    )
    
    # File size thresholds for analysis (in bytes)
    LargeFileThreshold = 10MB
    EmptyFileThreshold = 0
    
    # Display preferences
    MaxFilesToShow = 100
    ShowTimestamps = $true
    ShowFileSizes = $true
    UseColors = $true
}

# Repository-specific configurations
$RepoConfigs = @{
    # .NET Projects
    "dotnet" = @{
        ExcludePatterns = @(
            "bin/*", "obj/*", "Debug/*", "Release/*",
            "*.user", "*.suo", ".vs/*", "TestResults/*",
            "packages/*", "*.nupkg"
        )
        FileExtensions = @(".cs", ".csproj", ".sln", ".config")
        BuildCommand = "dotnet build"
        TestCommand = "dotnet test"
    }
    
    # Node.js Projects
    "nodejs" = @{
        ExcludePatterns = @(
            "node_modules/*", "dist/*", "build/*",
            "*.log", ".npm/*", "coverage/*"
        )
        FileExtensions = @(".js", ".ts", ".json", ".md")
        BuildCommand = "npm run build"
        TestCommand = "npm test"
    }
    
    # Python Projects
    "python" = @{
        ExcludePatterns = @(
            "__pycache__/*", "*.pyc", "*.pyo",
            ".pytest_cache/*", "venv/*", ".venv/*",
            "dist/*", "build/*", "*.egg-info/*"
        )
        FileExtensions = @(".py", ".pyw", ".pyx", ".requirements.txt")
        BuildCommand = "python setup.py build"
        TestCommand = "pytest"
    }
    
    # Docker Projects
    "docker" = @{
        ExcludePatterns = @(
            ".dockerignore", "*.log"
        )
        FileExtensions = @("Dockerfile", "docker-compose.yml", ".dockerignore")
        BuildCommand = "docker build ."
        TestCommand = "docker-compose up -d"
    }
}

# Stash recovery strategies
$StashStrategies = @{
    # Safe strategy - always check for conflicts
    "safe" = @{
        CheckConflicts = $true
        AutoCommitBeforeApply = $true
        BackupCurrentChanges = $true
    }
    
    # Fast strategy - minimal checks
    "fast" = @{
        CheckConflicts = $false
        AutoCommitBeforeApply = $false
        BackupCurrentChanges = $false
    }
    
    # Interactive strategy - prompt for everything
    "interactive" = @{
        CheckConflicts = $true
        PromptForEachFile = $true
        ShowDiffBeforeApply = $true
    }
}

# Cleanup strategies
$CleanupStrategies = @{
    # Conservative - only obvious empty files
    "conservative" = @{
        RemoveEmptyFiles = $true
        RemoveEmptyDirectories = $false
        RemoveLogFiles = $false
        MaxFileAge = 0  # Don't consider age
    }
    
    # Aggressive - clean everything possible
    "aggressive" = @{
        RemoveEmptyFiles = $true
        RemoveEmptyDirectories = $true
        RemoveLogFiles = $true
        RemoveTempFiles = $true
        MaxFileAge = 7  # Remove files older than 7 days
    }
    
    # Custom - user defined
    "custom" = @{
        RemoveEmptyFiles = $true
        RemoveEmptyDirectories = $true
        RemoveLogFiles = $false
        CustomPatterns = @("*.bak", "*.orig", "*~")
    }
}

# Function to detect repository type
function Get-RepositoryType {
    param([string]$Path = ".")
    
    $indicators = @{
        "dotnet" = @("*.sln", "*.csproj", "*.fsproj", "*.vbproj")
        "nodejs" = @("package.json", "node_modules")
        "python" = @("requirements.txt", "setup.py", "pyproject.toml", "Pipfile")
        "docker" = @("Dockerfile", "docker-compose.yml")
        "go" = @("go.mod", "go.sum")
        "rust" = @("Cargo.toml", "Cargo.lock")
        "java" = @("pom.xml", "build.gradle", "*.java")
    }
    
    foreach ($type in $indicators.Keys) {
        foreach ($pattern in $indicators[$type]) {
            if (Get-ChildItem -Path $Path -Filter $pattern -Recurse | Select-Object -First 1) {
                return $type
            }
        }
    }
    
    return "generic"
}

# Function to get configuration for current repository
function Get-RepoConfig {
    param([string]$RepoType = $null)
    
    if (-not $RepoType) {
        $RepoType = Get-RepositoryType
    }
    
    $config = $GlobalConfig.Clone()
    
    if ($RepoConfigs.ContainsKey($RepoType)) {
        $repoSpecific = $RepoConfigs[$RepoType]
        foreach ($key in $repoSpecific.Keys) {
            $config[$key] = $repoSpecific[$key]
        }
    }
    
    return $config
}

# Export functions and variables for use by main script
Export-ModuleMember -Variable GlobalConfig, RepoConfigs, StashStrategies, CleanupStrategies
Export-ModuleMember -Function Get-RepositoryType, Get-RepoConfig
