# NTools Development Todo List

## PATH Management Refactoring

### Problem
Current PATH-related tests are extremely slow (25s-1m41s per test) because they directly modify the Windows registry via `Environment.SetEnvironmentVariable("PATH", value, EnvironmentVariableTarget.User)`. This makes the test suite unusable and poses risks of corrupting the system PATH.

### Current Issues
- Tests call `PathManager.SetUserPath()` which writes to `HKCU\Environment\PATH`
- Each registry write requires system broadcast to all processes
- Tests take minutes to complete instead of seconds
- Risk of accidentally modifying production system PATH

### Required Changes

#### 1. Extract PathManager and centralize PATH writes
- Create a new `PathManager` (or similar) to centralize all Environment.Get/Set PATH logic
- Replace direct SetEnvironmentVariable calls in `Command.cs` to use it
- Add unit tests for its behaviour (preserve order, dedupe, handle null)

#### 2. Refactor tests to use PATH test helper
- Update `NbuildTests` to use a save/restore PATH helper (or mock PathManager)
- Rather than directly calling `Environment.SetEnvironmentVariable`
- Ensure tests run without requiring machine-level changes

#### 3. Update PowerShell install script docs
- Document that `dev-setup/install.psm1` modifies machine PATH
- Add an optional switch or safer merge behavior if desired

### Expected Benefits
- Test execution time: minutes → seconds
- Safer testing (no registry modifications)
- Centralized PATH management logic
- Better test isolation and reliability

### Implementation Notes
- Consider using dependency injection to allow test-specific PATH implementations
- Maintain backward compatibility with existing production code
- Add comprehensive test coverage for the new PATH management layer