using Xunit;
using Sdo;

namespace SdoTests;

/// <summary>
/// Tests for the Sdo CLI Program class
/// </summary>
public class ProgramTests
{
    [Fact]
    public void Main_WithNoArgs_ReturnsNonZero()
    {
        // Act - No arguments should return error (1) because a command is required
        var result = Program.Main();

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public void Main_WithHelpOption_ReturnsZero()
    {
        // Act
        var result = Program.Main("--help");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Main_WithVersionOption_ReturnsZero()
    {
        // Act
        var result = Program.Main("--version");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Main_WithInvalidOption_ReturnsNonZero()
    {
        // Act
        var result = Program.Main("--invalid-option");

        // Assert
        Assert.NotEqual(0, result);
    }
}