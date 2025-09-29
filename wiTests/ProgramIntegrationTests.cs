using System.CommandLine;
using System.CommandLine.Parsing;

[TestClass]
public class ProgramIntegrationTests
{
    private string _tempServicesFile = string.Empty;
    private const string TestOrganization = "https://dev.azure.com/testorg";
    private const string TestProject = "TestProject";
    private const string TestPat = "test-pat-token";

    [TestInitialize]
    public void Setup()
    {
        // Create a temporary services file for testing
        _tempServicesFile = Path.GetTempFileName();
        File.WriteAllLines(_tempServicesFile, new[] { "service1", "service2", "service3" });

        // Set environment variables
        Environment.SetEnvironmentVariable("PAT", TestPat);
        Environment.SetEnvironmentVariable("AZURE_DEVOPS_ORGANIZATION", TestOrganization);
        Environment.SetEnvironmentVariable("AZURE_DEVOPS_PROJECT", TestProject);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (File.Exists(_tempServicesFile))
        {
            File.Delete(_tempServicesFile);
        }

        // Clean up environment variables
        Environment.SetEnvironmentVariable("PAT", null);
        Environment.SetEnvironmentVariable("AZURE_DEVOPS_ORGANIZATION", null);
        Environment.SetEnvironmentVariable("AZURE_DEVOPS_PROJECT", null);
    }

    [TestMethod]
    public void ServicesFileOption_WhenProvided_ShouldParseCorrectly()
    {
        // Arrange
        var option = new Option<string>(
            name: "--services",
            parseArgument: result => result.Tokens.Count > 0 ? result.Tokens[0].Value : throw new ArgumentException("A value is required for --services"),
            description: "Path to services.txt file",
            isDefault: false
        )
        { IsRequired = true };

        var command = new Command("test");
        command.AddOption(option);

        // Act
        var parseResult = command.Parse(new[] { "--services", _tempServicesFile });

        // Assert
        Assert.IsFalse(parseResult.Errors.Any());
        Assert.AreEqual(_tempServicesFile, parseResult.GetValueForOption(option));
    }

    [TestMethod]
    public void ServicesFileOption_WhenMissing_ShouldHaveErrors()
    {
        // Arrange
        var option = new Option<string>(
            name: "--services",
            parseArgument: result => result.Tokens.Count > 0 ? result.Tokens[0].Value : throw new ArgumentException("A value is required for --services"),
            description: "Path to services.txt file",
            isDefault: false
        )
        { IsRequired = true };

        var command = new Command("test");
        command.AddOption(option);

        // Act
        var parseResult = command.Parse(new string[] { });

        // Assert
        Assert.IsTrue(parseResult.Errors.Any());
        Assert.IsTrue(parseResult.Errors.Any(e => e.Message.Contains("services")));
    }

    [TestMethod]
    public void ParentIdOption_WhenValidInteger_ShouldParseCorrectly()
    {
        // Arrange
        var option = new Option<int>(
            name: "--parentId",
            parseArgument: result =>
            {
                if (result.Tokens.Count > 0)
                {
                    var tokenValue = result.Tokens[0].Value;
                    if (int.TryParse(tokenValue, out var value))
                    {
                        return value;
                    }
                    throw new ArgumentException("A valid integer is required for --parentId");
                }
                throw new ArgumentException("A valid integer is required for --parentId");
            },
            description: "Parent work item ID",
            isDefault: false
        )
        { IsRequired = true };

        var command = new Command("test");
        command.AddOption(option);

        // Act
        var parseResult = command.Parse(new[] { "--parentId", "12345" });

        // Assert
        Assert.IsFalse(parseResult.Errors.Any());
        Assert.AreEqual(12345, parseResult.GetValueForOption(option));
    }

    [TestMethod]
    public void ParentIdOption_WhenInvalidInteger_ShouldHaveErrors()
    {
        // Arrange
        var option = new Option<int>(
            name: "--parentId",
            parseArgument: result =>
            {
                if (result.Tokens.Count > 0)
                {
                    var tokenValue = result.Tokens[0].Value;
                    if (int.TryParse(tokenValue, out var value))
                    {
                        return value;
                    }
                    throw new ArgumentException("A valid integer is required for --parentId");
                }
                throw new ArgumentException("A valid integer is required for --parentId");
            },
            description: "Parent work item ID",
            isDefault: false
        )
        { IsRequired = true };

        var command = new Command("test");
        command.AddOption(option);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() =>
        {
            var parseResult = command.Parse(new[] { "--parentId", "not-a-number" });
            var value = parseResult.GetValueForOption(option); // This triggers validation
        });
    }

    [TestMethod]
    public void ParentIdOption_WhenMissing_ShouldHaveErrors()
    {
        // Arrange
        var option = new Option<int>(
            name: "--parentId",
            parseArgument: result =>
            {
                if (result.Tokens.Count > 0)
                {
                    var tokenValue = result.Tokens[0].Value;
                    if (int.TryParse(tokenValue, out var value))
                    {
                        return value;
                    }
                    throw new ArgumentException("A valid integer is required for --parentId");
                }
                throw new ArgumentException("A valid integer is required for --parentId");
            },
            description: "Parent work item ID",
            isDefault: false
        )
        { IsRequired = true };

        var command = new Command("test");
        command.AddOption(option);

        // Act
        var parseResult = command.Parse(new string[] { });

        // Assert
        Assert.IsTrue(parseResult.Errors.Any());
        Assert.IsTrue(parseResult.Errors.Any(e => e.Message.Contains("parentId") || e.Message.Contains("Required")));
    }

    [TestMethod]
    public void ChildTaskPbiIdOption_WhenProvided_ShouldParseCorrectly()
    {
        // Arrange
        var option = new Option<int?>(
            name: "--childTaskOfPbiId",
            description: "If set, creates a child task with the same title as the PBI with this ID"
        );

        var command = new Command("test");
        command.AddOption(option);

        // Act
        var parseResult = command.Parse(new[] { "--childTaskOfPbiId", "67890" });

        // Assert
        Assert.IsFalse(parseResult.Errors.Any());
        Assert.AreEqual(67890, parseResult.GetValueForOption(option));
    }

    [TestMethod]
    public void ChildTaskPbiIdOption_WhenNotProvided_ShouldBeNull()
    {
        // Arrange
        var option = new Option<int?>(
            name: "--childTaskOfPbiId",
            description: "If set, creates a child task with the same title as the PBI with this ID"
        );

        var command = new Command("test");
        command.AddOption(option);

        // Act
        var parseResult = command.Parse(new string[] { });

        // Assert
        Assert.IsFalse(parseResult.Errors.Any());
        Assert.IsNull(parseResult.GetValueForOption(option));
    }

    [TestMethod]
    public void AllOptions_WhenProvidedTogether_ShouldParseCorrectly()
    {
        // Arrange
        var servicesOption = new Option<string>(
            name: "--services",
            parseArgument: result => result.Tokens.Count > 0 ? result.Tokens[0].Value : throw new ArgumentException("A value is required for --services"),
            description: "Path to services.txt file",
            isDefault: false
        )
        { IsRequired = true };

        var parentIdOption = new Option<int>(
            name: "--parentId",
            parseArgument: result =>
            {
                if (result.Tokens.Count > 0)
                {
                    var tokenValue = result.Tokens[0].Value;
                    if (int.TryParse(tokenValue, out var value))
                    {
                        return value;
                    }
                    throw new ArgumentException("A valid integer is required for --parentId");
                }
                throw new ArgumentException("A valid integer is required for --parentId");
            },
            description: "Parent work item ID",
            isDefault: false
        )
        { IsRequired = true };

        var childTaskOption = new Option<int?>(
            name: "--childTaskOfPbiId",
            description: "If set, creates a child task with the same title as the PBI with this ID"
        );

        var command = new Command("test");
        command.AddOption(servicesOption);
        command.AddOption(parentIdOption);
        command.AddOption(childTaskOption);

        // Act
        var parseResult = command.Parse(new[] {
            "--services", _tempServicesFile,
            "--parentId", "12345",
            "--childTaskOfPbiId", "67890"
        });

        // Assert
        Assert.IsFalse(parseResult.Errors.Any());
        Assert.AreEqual(_tempServicesFile, parseResult.GetValueForOption(servicesOption));
        Assert.AreEqual(12345, parseResult.GetValueForOption(parentIdOption));
        Assert.AreEqual(67890, parseResult.GetValueForOption(childTaskOption));
    }

    [TestMethod]
    public void ShortAliases_ShouldWorkCorrectly()
    {
        // Arrange
        var servicesOption = new Option<string>(
            name: "--services",
            parseArgument: result => result.Tokens.Count > 0 ? result.Tokens[0].Value : throw new ArgumentException("A value is required for --services"),
            description: "Path to services.txt file",
            isDefault: false
        )
        { IsRequired = true };
        servicesOption.AddAlias("-s");

        var parentIdOption = new Option<int>(
            name: "--parentId",
            parseArgument: result =>
            {
                if (result.Tokens.Count > 0)
                {
                    var tokenValue = result.Tokens[0].Value;
                    if (int.TryParse(tokenValue, out var value))
                    {
                        return value;
                    }
                    throw new ArgumentException("A valid integer is required for --parentId");
                }
                throw new ArgumentException("A valid integer is required for --parentId");
            },
            description: "Parent work item ID",
            isDefault: false
        )
        { IsRequired = true };
        parentIdOption.AddAlias("-p");

        var childTaskOption = new Option<int?>(
            name: "--childTaskOfPbiId",
            description: "If set, creates a child task with the same title as the PBI with this ID"
        );
        childTaskOption.AddAlias("-c");

        var command = new Command("test");
        command.AddOption(servicesOption);
        command.AddOption(parentIdOption);
        command.AddOption(childTaskOption);

        // Act
        var parseResult = command.Parse(new[] {
            "-s", _tempServicesFile,
            "-p", "12345",
            "-c", "67890"
        });

        // Assert
        Assert.IsFalse(parseResult.Errors.Any());
        Assert.AreEqual(_tempServicesFile, parseResult.GetValueForOption(servicesOption));
        Assert.AreEqual(12345, parseResult.GetValueForOption(parentIdOption));
        Assert.AreEqual(67890, parseResult.GetValueForOption(childTaskOption));
    }
}
