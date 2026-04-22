// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Sdo.Utilities;

namespace SdoTests
{
    /// <summary>
    /// Unit tests for ConfigurationManager.
    /// Tests YAML parsing, environment variable interpolation, and configuration management.
    /// </summary>
    public class ConfigurationManagerTests
    {
        private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), "ConfigurationManagerTests");

        public ConfigurationManagerTests()
        {
            Directory.CreateDirectory(_testDirectory);
        }

        private string CreateConfigFile(string content)
        {
            var filePath = Path.Combine(_testDirectory, $".sdo_{Guid.NewGuid()}.yaml");
            File.WriteAllText(filePath, content);
            return filePath;
        }

        private void CleanupFile(string filePath)
        {
            if (File.Exists(filePath)) File.Delete(filePath);
        }

        [Fact]
        public void Load_WithValidYamlFile_LoadsConfiguration()
        {
            // Arrange
            var content = @"# SDO Configuration
platform: github
organization: my-org
project: my-project
verbose: true
batch_size: 10";

            var filePath = CreateConfigFile(content);
            var config = new ConfigurationManager();

            try
            {
                // Act
                var result = config.Load(filePath);

                // Assert
                Assert.True(result);
                Assert.Equal("github", config.GetValue("platform"));
                Assert.Equal("my-org", config.GetValue("organization"));
                Assert.Equal("10", config.GetValue("batch_size"));
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void Load_WithSections_ParsesSectionedConfiguration()
        {
            // Arrange - Using YAML nested structure instead of INI [section] format
            var content = @"github:
  token: ghp_xxxxxxxxxxxx
  owner: my-org
azure_devops:
  pat: ${AZURE_DEVOPS_PAT}
  organization: my-org";

            var filePath = CreateConfigFile(content);
            var config = new ConfigurationManager();

            try
            {
                // Act
                var result = config.Load(filePath);

                // Assert
                Assert.True(result);
                Assert.Equal("ghp_xxxxxxxxxxxx", config.GetValue("github:token"));
                Assert.Equal("my-org", config.GetValue("azure_devops:organization"));
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void GetValue_WithEnvironmentVariable_InterpolatesCorrectly()
        {
            // Arrange
            Environment.SetEnvironmentVariable("TEST_VAR", "test_value");
            var content = @"config_value: ${TEST_VAR}
other_value: static";

            var filePath = CreateConfigFile(content);
            var config = new ConfigurationManager();

            try
            {
                // Act
                config.Load(filePath);
                var value = config.GetValue("config_value");

                // Assert
                Assert.Equal("test_value", value);
            }
            finally
            {
                CleanupFile(filePath);
                Environment.SetEnvironmentVariable("TEST_VAR", null);
            }
        }

        [Fact]
        public void GetInt_WithValidInteger_ReturnsInt()
        {
            // Arrange
            var content = "batch_size: 25";
            var filePath = CreateConfigFile(content);
            var config = new ConfigurationManager();

            try
            {
                // Act
                config.Load(filePath);
                var value = config.GetInt("batch_size");

                // Assert
                Assert.Equal(25, value);
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void GetInt_WithInvalidValue_ReturnsDefault()
        {
            // Arrange
            var content = "batch_size: not_a_number";
            var filePath = CreateConfigFile(content);
            var config = new ConfigurationManager();

            try
            {
                // Act
                config.Load(filePath);
                var value = config.GetInt("batch_size", 10);

                // Assert
                Assert.Equal(10, value);
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void GetBool_WithTrueVariations_ReturnsTrue()
        {
            // Arrange
            var content = @"option_a: true
option_b: yes
option_c: 1";

            var filePath = CreateConfigFile(content);
            var config = new ConfigurationManager();

            try
            {
                // Act
                config.Load(filePath);

                // Assert
                Assert.True(config.GetBool("option_a"));
                Assert.True(config.GetBool("option_b"));
                Assert.True(config.GetBool("option_c"));
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void GetBool_WithFalseVariations_ReturnsFalse()
        {
            // Arrange
            var content = @"option_a: false
option_b: no
option_c: 0";

            var filePath = CreateConfigFile(content);
            var config = new ConfigurationManager();

            try
            {
                // Act
                config.Load(filePath);

                // Assert
                Assert.False(config.GetBool("option_a"));
                Assert.False(config.GetBool("option_b"));
                Assert.False(config.GetBool("option_c"));
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void SetValue_OverridesConfigurationValue()
        {
            // Arrange
            var content = "platform: github";
            var filePath = CreateConfigFile(content);
            var config = new ConfigurationManager();

            try
            {
                // Act
                config.Load(filePath);
                config.SetValue("platform", "azure_devops");
                var value = config.GetValue("platform");

                // Assert
                Assert.Equal("azure_devops", value);
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void GetKeys_ReturnsAllConfigurationKeys()
        {
            // Arrange
            var content = @"platform: github
organization: my-org
project: my-project";

            var filePath = CreateConfigFile(content);
            var config = new ConfigurationManager();

            try
            {
                // Act
                config.Load(filePath);
                var keys = config.GetKeys();

                // Assert
                Assert.Contains("platform", keys);
                Assert.Contains("organization", keys);
                Assert.Contains("project", keys);
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void Load_WithComments_IgnoresCommentLines()
        {
            // Arrange
            var content = @"# This is a comment
platform: github  # inline comment ignored
# organization: commented_out
project: my-project";

            var filePath = CreateConfigFile(content);
            var config = new ConfigurationManager();

            try
            {
                // Act
                config.Load(filePath);

                // Assert
                Assert.Equal("github", config.GetValue("platform"));
                Assert.Null(config.GetValue("organization"));
                Assert.Equal("my-project", config.GetValue("project"));
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void Load_WithQuotedValues_StripsQuotes()
        {
            // Arrange
            var content = @"value_single: 'single quoted'
value_double: ""double quoted""
value_unquoted: unquoted";

            var filePath = CreateConfigFile(content);
            var config = new ConfigurationManager();

            try
            {
                // Act
                config.Load(filePath);

                // Assert
                Assert.Equal("single quoted", config.GetValue("value_single"));
                Assert.Equal("double quoted", config.GetValue("value_double"));
                Assert.Equal("unquoted", config.GetValue("value_unquoted"));
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void Load_WithNonexistentFile_ReturnsFalse()
        {
            // Arrange
            var config = new ConfigurationManager();

            // Act
            var result = config.Load("/nonexistent/path/.sdo.yaml");

            // Assert
            Assert.False(result);
            Assert.NotEmpty(config.GetErrors());
        }

        [Fact]
        public void GetValue_WithMissingKey_ReturnsDefault()
        {
            // Arrange
            var content = "platform: github";
            var filePath = CreateConfigFile(content);
            var config = new ConfigurationManager();

            try
            {
                // Act
                config.Load(filePath);
                var value = config.GetValue("missing_key", "default_value");

                // Assert
                Assert.Equal("default_value", value);
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void GetAllValues_WithSensitiveKeys_RedactsValues()
        {
            // Arrange
            var content = @"platform: github
github_token: ghp_xxxxxxxxxxxx
azure_pat: dev.azure.com_token";

            var filePath = CreateConfigFile(content);
            var config = new ConfigurationManager();

            try
            {
                // Act
                config.Load(filePath);
                var allValues = config.GetAllValues(includeSensitive: false);

                // Assert
                Assert.Equal("github", allValues["platform"]);
                Assert.Equal("***REDACTED***", allValues["github_token"]);
                Assert.Equal("***REDACTED***", allValues["azure_pat"]);
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void IsValid_WithLoadedConfiguration_ReturnsTrue()
        {
            // Arrange
            var content = "platform: github";
            var filePath = CreateConfigFile(content);
            var config = new ConfigurationManager();

            try
            {
                // Act
                config.Load(filePath);

                // Assert
                Assert.True(config.IsValid());
                Assert.Empty(config.GetErrors());
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void ExportAsJson_ExportsConfigurationAsJson()
        {
            // Arrange
            var content = @"platform: github
organization: my-org";

            var filePath = CreateConfigFile(content);
            var config = new ConfigurationManager();

            try
            {
                // Act
                config.Load(filePath);
                var json = config.ExportAsJson();

                // Assert
                Assert.Contains("platform", json);
                Assert.Contains("github", json);
                Assert.Contains("organization", json);
                Assert.Contains("my-org", json);
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void Load_WithoutFile_UsesDefaults()
        {
            // Arrange
            var config = new ConfigurationManager();

            // Act
            var result = config.Load();

            // Assert
            Assert.True(result); // Should succeed with no errors even if file not found
            Assert.True(config.IsValid());
        }

        #region Nested YAML and Config Defaults Tests

        [Fact]
        public void Load_WithNestedYaml_FlattensTooDotNotation()
        {
            // Arrange
            var content = @"commands:
  wi:
    list:
      project: Proto
      area_path: Proto\Warriors
      state: To Do,In Progress
      top: 50";

            var filePath = CreateConfigFile(content);
            var config = new ConfigurationManager();

            try
            {
                // Act
                config.Load(filePath);

                // Assert
                Assert.Equal("Proto", config.GetValue("commands:wi:list:project"));
                Assert.Equal("Proto\\Warriors", config.GetValue("commands:wi:list:area_path"));
                Assert.Equal("To Do,In Progress", config.GetValue("commands:wi:list:state"));
                Assert.Equal("50", config.GetValue("commands:wi:list:top"));
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void LoadedConfigPath_AfterLoad_StoresAbsolutePath()
        {
            // Arrange
            var content = "project: TestProject";
            var filePath = CreateConfigFile(content);
            var config = new ConfigurationManager();

            try
            {
                // Act
                config.Load(filePath);

                // Assert
                Assert.NotNull(config.LoadedConfigPath);
                Assert.Equal(Path.GetFullPath(filePath), config.LoadedConfigPath);
                Assert.True(Path.IsPathRooted(config.LoadedConfigPath));
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void LoadedConfigPath_WithoutLoad_ReturnsNull()
        {
            // Arrange
            var config = new ConfigurationManager();

            // Act - don't call Load()

            // Assert
            Assert.Null(config.LoadedConfigPath);
        }

        [Fact]
        public void Load_WithCustomPath_StoresLoadedPath()
        {
            // Arrange
            var content = "setting: value";
            var filePath = CreateConfigFile(content);
            var config = new ConfigurationManager();

            try
            {
                // Act
                config.Load(filePath);

                // Assert
                Assert.NotNull(config.LoadedConfigPath);
                Assert.Contains("ConfigurationManagerTests", config.LoadedConfigPath);
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void GetValue_WithDotNotation_RetrievesNestedValues()
        {
            // Arrange
            var content = @"azure_devops:
  organization: myorg
  project: myproject
commands:
  wi:
    list:
      state: To Do,In Progress";

            var filePath = CreateConfigFile(content);
            var config = new ConfigurationManager();

            try
            {
                // Act
                config.Load(filePath);

                // Assert
                Assert.Equal("myorg", config.GetValue("azure_devops.organization"));
                Assert.Equal("myproject", config.GetValue("azure_devops.project"));
                Assert.Equal("To Do,In Progress", config.GetValue("commands.wi.list.state"));
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void GetValue_WithColonNotation_RetrievesNestedValues()
        {
            // Arrange
            var content = @"azure_devops:
  organization: myorg
commands:
  wi:
    list:
      state: To Do,In Progress";

            var filePath = CreateConfigFile(content);
            var config = new ConfigurationManager();

            try
            {
                // Act
                config.Load(filePath);

                // Assert
                Assert.Equal("myorg", config.GetValue("azure_devops:organization"));
                Assert.Equal("To Do,In Progress", config.GetValue("commands:wi:list:state"));
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void GetInt_WithNestedYamlValue_ParsesInteger()
        {
            // Arrange
            var content = @"commands:
  wi:
    list:
      top: 50";

            var filePath = CreateConfigFile(content);
            var config = new ConfigurationManager();

            try
            {
                // Act
                config.Load(filePath);
                var top = config.GetInt("commands:wi:list:top");

                // Assert
                Assert.Equal(50, top);
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void Load_WithComplexNestedStructure_HandlesMultipleLevels()
        {
            // Arrange
            var content = @"project:
  name: TestProject
  description: Test Description
azure_devops:
  organization: testorg
  project: TestProject
  area_path: TestProject\Team
  default_iteration: TestProject\Sprint01
commands:
  wi:
    list:
      project: TestProject
      area_path: TestProject\Team
      state: To Do,In Progress
      type: PBI,Task
      top: 100";

            var filePath = CreateConfigFile(content);
            var config = new ConfigurationManager();

            try
            {
                // Act
                config.Load(filePath);

                // Assert
                // Verify all nested values are accessible
                Assert.Equal("TestProject", config.GetValue("project:name"));
                Assert.Equal("testorg", config.GetValue("azure_devops:organization"));
                Assert.Equal("TestProject\\Team", config.GetValue("azure_devops:area_path"));
                Assert.Equal("To Do,In Progress", config.GetValue("commands:wi:list:state"));
                Assert.Equal("100", config.GetValue("commands:wi:list:top"));
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void Load_WithTempFolderStructure_FindsConfigInTempFolder()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), $"ConfigTempTest_{Guid.NewGuid()}");
            var tempFolderDir = Path.Combine(tempDir, ".temp");
            Directory.CreateDirectory(tempFolderDir);

            var configPath = Path.Combine(tempFolderDir, "sdo-config.yaml");
            var content = @"commands:
  wi:
    list:
      project: TestProject
      state: To Do,In Progress";
            File.WriteAllText(configPath, content);

            var config = new ConfigurationManager();
            var originalCwd = Directory.GetCurrentDirectory();

            try
            {
                // Act
                Directory.SetCurrentDirectory(tempDir);
                var result = config.Load(); // Should find config in .temp subfolder

                // Assert
                Assert.True(result);
                Assert.NotNull(config.LoadedConfigPath);
                Assert.Contains(".temp", config.LoadedConfigPath);
                Assert.Equal("TestProject", config.GetValue("commands:wi:list:project"));
                Assert.Equal("To Do,In Progress", config.GetValue("commands:wi:list:state"));
            }
            finally
            {
                Directory.SetCurrentDirectory(originalCwd);
                Directory.Delete(tempDir, recursive: true);
            }
        }

        [Fact]
        public void Load_SearchOrder_CurrentDirectoryFirst()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), $"ConfigSearchTest_{Guid.NewGuid()}");
            var tempFolderDir = Path.Combine(tempDir, ".temp");
            Directory.CreateDirectory(tempFolderDir);

            // Create config in both current dir and .temp folder
            var currentDirConfig = Path.Combine(tempDir, "sdo-config.yaml");
            var tempFolderConfig = Path.Combine(tempFolderDir, "sdo-config.yaml");

            File.WriteAllText(currentDirConfig, "location: current_directory");
            File.WriteAllText(tempFolderConfig, "location: temp_folder");

            var config = new ConfigurationManager();
            var originalCwd = Directory.GetCurrentDirectory();

            try
            {
                // Act
                Directory.SetCurrentDirectory(tempDir);
                config.Load(); // Should prefer current directory

                // Assert
                Assert.Equal("current_directory", config.GetValue("location"));
            }
            finally
            {
                Directory.SetCurrentDirectory(originalCwd);
                Directory.Delete(tempDir, recursive: true);
            }
        }

        #endregion
    }
}
