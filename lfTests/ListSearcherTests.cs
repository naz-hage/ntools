[TestClass]
public class ListSearcherTests
{
    private string _testRoot = string.Empty; // Initialize to an empty string to satisfy the non-nullable requirement.

    [TestInitialize]
    public void Setup()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testRoot);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testRoot))
            Directory.Delete(_testRoot, true);
    }

    [TestMethod]
    public void ListFoldersContaining_FindsFoldersByName()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_testRoot, "bin"));
        Directory.CreateDirectory(Path.Combine(_testRoot, "obj"));
        Directory.CreateDirectory(Path.Combine(_testRoot, "notmatch"));

        using var sw = new StringWriter();
        Console.SetOut(sw);

        // Act
        ListSearcher.ListFoldersContaining(_testRoot, new[] { "bin", "obj" });

        // Assert
        var output = sw.ToString();
        StringAssert.Contains(output, "Found 1 folders containing 'bin':");
        StringAssert.Contains(output, "bin");
        StringAssert.Contains(output, "Found 1 folders containing 'obj':");
        StringAssert.Contains(output, "obj");
    }

    [TestMethod]
    public void ListFoldersContaining_NoFoldersFound_PrintsMessage()
    {
        // Arrange
        using var sw = new StringWriter();
        Console.SetOut(sw);

        // Act
        ListSearcher.ListFoldersContaining(_testRoot, new[] { "doesnotexist" });

        // Assert
        var output = sw.ToString();
        StringAssert.Contains(output, "No folders found containing the name 'doesnotexist'.");
    }

    [TestMethod]
    public void ListFiles_FindsFilesByExtension()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testRoot, "a.yaml"), "test");
        File.WriteAllText(Path.Combine(_testRoot, "b.yml"), "test");
        File.WriteAllText(Path.Combine(_testRoot, "c.txt"), "test");

        using var sw = new StringWriter();
        Console.SetOut(sw);

        // Act
        ListSearcher.ListFiles(_testRoot, new[] { ".yml", ".yaml" });

        // Assert
        var output = sw.ToString();
        StringAssert.Contains(output, "Found 1 files with .yml extension:");
        StringAssert.Contains(output, "b.yml");
        StringAssert.Contains(output, "Found 1 files with .yaml extension:");
        StringAssert.Contains(output, "a.yaml");
        StringAssert.DoesNotMatch(output, new System.Text.RegularExpressions.Regex("c.txt"));
    }

    [TestMethod]
    public void ListFiles_NoFilesFound_PrintsMessage()
    {
        // Arrange
        using var sw = new StringWriter();
        Console.SetOut(sw);

        // Act
        ListSearcher.ListFiles(_testRoot, new[] { ".doesnotexist" });

        // Assert
        var output = sw.ToString();
        StringAssert.Contains(output, "No files found with .doesnotexist extension");
    }

    [TestMethod]
    public void ListFoldersContaining_HandlesException()
    {
        // Arrange
        var invalidPath = Path.Combine(_testRoot, "notarealdir");
        using var sw = new StringWriter();
        Console.SetOut(sw);

        // Act
        ListSearcher.ListFoldersContaining(invalidPath, new[] { "bin" });

        // Assert
        var output = sw.ToString();
        StringAssert.Contains(output, "An error occurred while searching for folders:");
    }

    [TestMethod]
    public void ListFoldersContaining_EmptyInputArray_NoOutputOrError()
    {
        // Arrange
        using var sw = new StringWriter();
        Console.SetOut(sw);

        // Act
        ListSearcher.ListFoldersContaining(_testRoot, Array.Empty<string>());

        // Assert
        var output = sw.ToString();
        Assert.AreEqual(string.Empty, output);
    }

    [TestMethod]
    public void ListFiles_EmptyInputArray_NoOutputOrError()
    {
        // Arrange
        using var sw = new StringWriter();
        Console.SetOut(sw);

        // Act
        ListSearcher.ListFiles(_testRoot, Array.Empty<string>());

        // Assert
        var output = sw.ToString();
        Assert.AreEqual(string.Empty, output);
    }

    [TestMethod]
    public void ListFoldersContaining_CaseInsensitiveMatch()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_testRoot, "TestFolder"));

        using var sw = new StringWriter();
        Console.SetOut(sw);

        // Act
        ListSearcher.ListFoldersContaining(_testRoot, new[] { "testfolder" });

        // Assert
        var output = sw.ToString();
        StringAssert.Contains(output, "Found 1 folders containing 'testfolder':");
        StringAssert.Contains(output, "TestFolder");
    }

    [TestMethod]
    public void ListFiles_CaseInsensitiveExtension()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testRoot, "file.YML"), "test");

        using var sw = new StringWriter();
        Console.SetOut(sw);

        // Act
        ListSearcher.ListFiles(_testRoot, new[] { ".yml" });

        // Assert
        var output = sw.ToString();
        // Directory.GetFiles is case-insensitive on Windows, but not on Linux. This test is for Windows.
        StringAssert.Contains(output, "Found 1 files with .yml extension:");
        StringAssert.Contains(output, "file.YML");
    }

    [TestMethod]
    public void ListFoldersContaining_SpecialCharactersAndSpaces()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_testRoot, "my folder (1)"));

        using var sw = new StringWriter();
        Console.SetOut(sw);

        // Act
        ListSearcher.ListFoldersContaining(_testRoot, new[] { "my folder (1)" });

        // Assert
        var output = sw.ToString();
        StringAssert.Contains(output, "Found 1 folders containing 'my folder (1)':");
        StringAssert.Contains(output, "my folder (1)");
    }

    [TestMethod]
    public void ListFiles_SpecialCharactersAndSpaces()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testRoot, "file (1).yaml"), "test");

        using var sw = new StringWriter();
        Console.SetOut(sw);

        // Act
        ListSearcher.ListFiles(_testRoot, new[] { ".yaml" });

        // Assert
        var output = sw.ToString();
        StringAssert.Contains(output, "Found 1 files with .yaml extension:");
        StringAssert.Contains(output, "file (1).yaml");
    }

    [TestMethod]
    public void ListFoldersContaining_NestedDirectories()
    {
        // Arrange
        var nested = Directory.CreateDirectory(Path.Combine(_testRoot, "parent", "bin"));
        using var sw = new StringWriter();
        Console.SetOut(sw);

        // Act
        ListSearcher.ListFoldersContaining(_testRoot, new[] { "bin" });

        // Assert
        var output = sw.ToString();
        StringAssert.Contains(output, "Found 1 folders containing 'bin':");
        StringAssert.Contains(output, "bin");
    }

    [TestMethod]
    public void ListFiles_NestedDirectories()
    {
        // Arrange
        var nestedDir = Directory.CreateDirectory(Path.Combine(_testRoot, "parent", "sub"));
        File.WriteAllText(Path.Combine(nestedDir.FullName, "deep.yaml"), "test");

        using var sw = new StringWriter();
        Console.SetOut(sw);

        // Act
        ListSearcher.ListFiles(_testRoot, new[] { ".yaml" });

        // Assert
        var output = sw.ToString();
        StringAssert.Contains(output, "Found 1 files with .yaml extension:");
        StringAssert.Contains(output, "deep.yaml");
    }

    [TestMethod]
    public void ListFoldersContaining_MultipleMatches()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_testRoot, "bin1"));
        Directory.CreateDirectory(Path.Combine(_testRoot, "bin2"));

        using var sw = new StringWriter();
        Console.SetOut(sw);

        // Act
        ListSearcher.ListFoldersContaining(_testRoot, new[] { "bin" });

        // Assert
        var output = sw.ToString();
        StringAssert.Contains(output, "Found 2 folders containing 'bin':");
        StringAssert.Contains(output, "bin1");
        StringAssert.Contains(output, "bin2");
    }

    [TestMethod]
    public void ListFiles_MultipleMatches()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_testRoot, "a.yaml"), "test");
        File.WriteAllText(Path.Combine(_testRoot, "b.yaml"), "test");

        using var sw = new StringWriter();
        Console.SetOut(sw);

        // Act
        ListSearcher.ListFiles(_testRoot, new[] { ".yaml" });

        // Assert
        var output = sw.ToString();
        StringAssert.Contains(output, "Found 2 files with .yaml extension:");
        StringAssert.Contains(output, "a.yaml");
        StringAssert.Contains(output, "b.yaml");
    }
}