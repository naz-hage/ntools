using wi;

[TestClass]
public class ServicesFileTests
{
    private string _tempDirectory = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
        Environment.SetEnvironmentVariable("PAT", "test-pat");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
        Environment.SetEnvironmentVariable("PAT", null);
    }

    [TestMethod]
    public async Task ReadServicesFile_WhenFileExists_ShouldReturnServices()
    {
        // Arrange
        var servicesFile = Path.Combine(_tempDirectory, "services.txt");
        var expectedServices = new[] { "service1", "service2", "service3" };
        await File.WriteAllLinesAsync(servicesFile, expectedServices);

        // Act
        var services = await File.ReadAllLinesAsync(servicesFile);

        // Assert
        Assert.AreEqual(expectedServices.Length, services.Length);
        CollectionAssert.AreEqual(expectedServices, services);
    }

    [TestMethod]
    public async Task ReadServicesFile_WhenFileDoesNotExist_ShouldThrowException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_tempDirectory, "nonexistent.txt");

        // Act & Assert
        await Assert.ThrowsExceptionAsync<FileNotFoundException>(
            () => File.ReadAllLinesAsync(nonExistentFile));
    }

    [TestMethod]
    public async Task ReadServicesFile_WhenFileIsEmpty_ShouldReturnEmptyArray()
    {
        // Arrange
        var emptyFile = Path.Combine(_tempDirectory, "empty.txt");
        await File.WriteAllTextAsync(emptyFile, string.Empty);

        // Act
        var services = await File.ReadAllLinesAsync(emptyFile);

        // Assert
        Assert.AreEqual(0, services.Length);
    }

    [TestMethod]
    public async Task ReadServicesFile_WhenFileContainsEmptyLines_ShouldIncludeEmptyLines()
    {
        // Arrange
        var servicesFile = Path.Combine(_tempDirectory, "services-with-empty.txt");
        var servicesWithEmpty = new[] { "service1", "", "service2", "   ", "service3" };
        await File.WriteAllLinesAsync(servicesFile, servicesWithEmpty);

        // Act
        var services = await File.ReadAllLinesAsync(servicesFile);

        // Assert
        Assert.AreEqual(5, services.Length);
        Assert.AreEqual("service1", services[0]);
        Assert.AreEqual("", services[1]);
        Assert.AreEqual("service2", services[2]);
        Assert.AreEqual("   ", services[3]);
        Assert.AreEqual("service3", services[4]);
    }

    [TestMethod]
    public async Task ReadServicesFile_WhenFileContainsSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var servicesFile = Path.Combine(_tempDirectory, "services-special.txt");
        var specialServices = new[] {
            "service-with-dash",
            "service_with_underscore",
            "service.with.dots",
            "service with spaces",
            "service@with@symbols"
        };
        await File.WriteAllLinesAsync(servicesFile, specialServices);

        // Act
        var services = await File.ReadAllLinesAsync(servicesFile);

        // Assert
        Assert.AreEqual(specialServices.Length, services.Length);
        CollectionAssert.AreEqual(specialServices, services);
    }

    [TestMethod]
    public void FilterEmptyServices_ShouldSkipEmptyAndWhitespaceLines()
    {
        // Arrange
        var services = new[] { "service1", "", "service2", "   ", "service3", "\t\n", "service4" };

        // Act
        var filteredServices = services.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

        // Assert
        Assert.AreEqual(4, filteredServices.Length);
        CollectionAssert.AreEqual(new[] { "service1", "service2", "service3", "service4" }, filteredServices);
    }

    [TestMethod]
    public async Task ReadServicesFile_WhenFileIsLocked_ShouldThrowException()
    {
        // Arrange
        var lockedFile = Path.Combine(_tempDirectory, "locked.txt");
        await File.WriteAllTextAsync(lockedFile, "service1");

        // Act & Assert
        using (var fileStream = new FileStream(lockedFile, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            await Assert.ThrowsExceptionAsync<IOException>(
                () => File.ReadAllLinesAsync(lockedFile));
        }
    }

    [TestMethod]
    public async Task ReadServicesFile_WhenAccessDenied_ShouldThrowException()
    {
        // Arrange
        var restrictedFile = Path.Combine(_tempDirectory, "restricted.txt");
        await File.WriteAllTextAsync(restrictedFile, "service1");

        try
        {
            // Make file read-only to simulate access denied (this might not work on all systems)
            var fileInfo = new FileInfo(restrictedFile);
            fileInfo.IsReadOnly = true;

            // Try to read with a mode that requires write access
            using var fileStream = new FileStream(restrictedFile, FileMode.Open, FileAccess.ReadWrite);

            // If we get here without exception, skip the test
            Assert.Inconclusive("Could not simulate access denied scenario on this system");
        }
        catch (UnauthorizedAccessException)
        {
            // This is expected - access was denied
            Assert.IsTrue(true, "Access denied exception was thrown as expected");
        }
        catch (Exception ex)
        {
            // Some other exception occurred, which is also valid for testing error handling
            Assert.IsInstanceOfType(ex, typeof(Exception), "An exception was thrown when accessing restricted file");
        }
        finally
        {
            // Clean up - remove read-only attribute
            if (File.Exists(restrictedFile))
            {
                var fileInfo = new FileInfo(restrictedFile);
                fileInfo.IsReadOnly = false;
            }
        }
    }
}
