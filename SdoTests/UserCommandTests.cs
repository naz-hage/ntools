// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.

using System;
using System.CommandLine;
using Xunit;
using Sdo.Commands;

namespace SdoTests;

public class UserCommandTests
{
    private readonly Option<bool> _verboseOption;

    public UserCommandTests()
    {
        _verboseOption = new Option<bool>("--verbose");
        _verboseOption.Description = "Enable verbose output";
    }

    [Fact]
    public void Constructor_CreatesCommandWithCorrectNameAndDescription()
    {
        var cmd = new UserCommand(_verboseOption);
        Assert.Equal("user", cmd.Name);
        Assert.Equal("User management commands for GitHub and Azure DevOps", cmd.Description);
    }

    [Fact]
    public void Constructor_AddsExpectedSubcommands()
    {
        var cmd = new UserCommand(_verboseOption);
        Assert.NotNull(cmd.Subcommands);
        Assert.Contains(cmd.Subcommands, s => s.Name == "show");
        Assert.Contains(cmd.Subcommands, s => s.Name == "list");
        Assert.Contains(cmd.Subcommands, s => s.Name == "search");
        Assert.Contains(cmd.Subcommands, s => s.Name == "permissions");
    }

    [Fact]
    public void ShowSubcommand_HasLoginOption()
    {
        var cmd = new UserCommand(_verboseOption);
        var show = Assert.Single(cmd.Subcommands, s => s.Name == "show");
        var opt = Assert.Single(show.Options, o => o.Name == "--login");
        Assert.Equal("User login or id", opt.Description);
    }

    [Fact]
    public void ListSubcommand_HasTopOption()
    {
        var cmd = new UserCommand(_verboseOption);
        var list = Assert.Single(cmd.Subcommands, s => s.Name == "list");
        var opt = Assert.Single(list.Options, o => o.Name == "--top");
        Assert.Equal("Limit results", opt.Description);
    }

    [Fact]
    public void SearchSubcommand_RequiresQuery()
    {
        var cmd = new UserCommand(_verboseOption);
        var args = new[] { "search" };
        var result = cmd.Parse(args).Invoke();
        Assert.NotEqual(0, result);
    }

    [Fact]
    public void PermissionsSubcommand_HasUserOption()
    {
        var cmd = new UserCommand(_verboseOption);
        var perms = Assert.Single(cmd.Subcommands, s => s.Name == "permissions");
        var opt = Assert.Single(perms.Options, o => o.Name == "--user");
        Assert.Equal("User login or id", opt.Description);
    }

    [Fact]
    public void Subcommands_HaveGlobalVerboseOption()
    {
        var cmd = new UserCommand(_verboseOption);
        foreach (var sub in cmd.Subcommands)
        {
            Assert.Contains(sub.Options, o => o.Name == "--verbose");
        }
    }
}
