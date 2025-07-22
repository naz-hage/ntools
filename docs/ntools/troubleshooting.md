**Summary of Issue:**  
- Tests for the `list` command crash because `Environment.Exit()` is used in some command handlers (like `install`), which terminates the process immediately. This prevents the test framework from capturing the return code or output.
- The `list` command correctly uses `Environment.ExitCode = exitCode;`, allowing the process to return control to the test runner and enabling proper result checking.

**Root Cause:**  
- Using `Environment.Exit()` in command handlers (e.g., for `install` and `uninstall`) causes the process to exit before the test can assert results.
- The test framework expects the process to complete and return a code, not exit abruptly.

**Resolution in Program.cs:**  
- Replace all instances of `Environment.Exit(result.Code);` in command handlers with `Environment.ExitCode = result.Code;` and allow the handler to return.
- This change ensures the process does not terminate prematurely, and tests can capture the exit code and output as expected.

**Example Fix:**
```csharp
installCommand.SetHandler((string json, bool verbose) => {
    try
    {
        var result = Nbuild.Command.Install(json, verbose);
        Environment.ExitCode = result.Code; // Instead of Environment.Exit(result.Code);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Environment.ExitCode = -1; // Instead of Environment.Exit(-1);
    }
}, jsonOption, verboseOption);
```
- Apply similar changes to other command handlers that use `Environment.Exit()`.

**Summary:**  
Tests crash because `Environment.Exit()` terminates the process before the test can check results. Use `Environment.ExitCode = ...` instead, as done in the `list` command, to allow proper test execution and result checking. Update all command handlers in Program.cs accordingly.