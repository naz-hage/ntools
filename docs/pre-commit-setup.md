# Pre-commit Setup Instructions

## Method 1: Using Pre-commit Framework (Recommended)

### Installation
```powershell
# Install pre-commit (requires Python)
pip install pre-commit

# Install the git hook scripts
pre-commit install

# Install commit-msg hook
pre-commit install --hook-type commit-msg

# Test the hooks
pre-commit run --all-files
```

### Usage
```powershell
# Normal git workflow - hooks run automatically
git add .
git commit -m "Your commit message"

# Skip hooks (emergency use only)
git commit -m "Emergency fix" --no-verify

# Run hooks manually
pre-commit run

# Update hook versions
pre-commit autoupdate
```

## Method 2: Manual Git Hooks

### Windows Setup
```powershell
# Make the hook executable (Git Bash or WSL)
chmod +x .git/hooks/pre-commit

# Or copy the hook file
copy dev-setup\pre-commit-hook.sh .git\hooks\pre-commit
```

### Test the Hook
```powershell
# Test by modifying a JSON file
echo '{"test": "change"}' > dev-setup/test.json
git add dev-setup/test.json
git commit -m "Test commit"
# Hook should run and update documentation
```

## Customization Options

### Skip Specific Hooks
```powershell
# Skip specific hooks
SKIP=trailing-whitespace git commit -m "Skip whitespace check"

# Skip pre-commit framework entirely
git commit -m "Emergency commit" --no-verify
```

### Hook Configuration
Edit `.pre-commit-config.yaml` to:
- Add new hooks
- Modify existing hook behavior
- Set file patterns
- Configure stages (commit, push, etc.)

### Environment Variables
```powershell
# Debug mode
export PRE_COMMIT_DEBUG=1

# Colored output
export PRE_COMMIT_COLOR=always
```
