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
    /// Unit tests for MarkdownParser class.
    /// Tests YAML frontmatter, titles, metadata, code blocks, and acceptance criteria parsing.
    /// </summary>
    public class MarkdownParserTests
    {
        private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), "MarkdownParserTests");

        public MarkdownParserTests()
        {
            Directory.CreateDirectory(_testDirectory);
        }

        private string CreateTestFile(string content)
        {
            var filePath = Path.Combine(_testDirectory, $"test_{Guid.NewGuid()}.md");
            File.WriteAllText(filePath, content);
            return filePath;
        }

        private void CleanupFile(string filePath)
        {
            if (File.Exists(filePath)) File.Delete(filePath);
        }

        [Fact]
        public void ParseFile_WithSimpleTitleAndDescription_ParsesCorrectly()
        {
            // Arrange
            var content = @"# Work Item Title

This is the description.";
            var filePath = CreateTestFile(content);

            try
            {
                // Act
                var result = MarkdownParser.ParseFile(filePath);

                // Assert
                Assert.True(result.Success);
                Assert.Equal("Work Item Title", result.Title);
                Assert.Equal("This is the description.", result.Description);
                Assert.Empty(result.Errors);
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void ParseFile_WithYamlFrontmatter_ParsesMetadata()
        {
            // Arrange
            var content = @"---
work_item_type: PBI
assignee: user@example.com
labels: ""feature, backlog""
---
# PBI Title

Description here.";
            var filePath = CreateTestFile(content);

            try
            {
                // Act
                var result = MarkdownParser.ParseFile(filePath);

                // Assert
                Assert.True(result.Success);
                Assert.Equal("PBI Title", result.Title);
                Assert.Equal("PBI", result.Metadata["work_item_type"]);
                Assert.Equal("user@example.com", result.Metadata["assignee"]);
                Assert.Equal("feature, backlog", result.Metadata["labels"]);
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void ParseFile_WithMetadataHeaders_ParsesMetadata()
        {
            // Arrange
            var content = @"# Work Item Title

## Work Item Type: PBI
## Priority: High
## Assignee: user@example.com

Description here.";
            var filePath = CreateTestFile(content);

            try
            {
                // Act
                var result = MarkdownParser.ParseFile(filePath);

                // Assert
                Assert.True(result.Success);
                Assert.Equal("Work Item Title", result.Title);
                Assert.Equal("PBI", result.Metadata["work_item_type"]);
                Assert.Equal("High", result.Metadata["priority"]);
                Assert.Equal("user@example.com", result.Metadata["assignee"]);
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void ParseFile_WithAcceptanceCriteria_ParsesCorrectly()
        {
            // Arrange
            var content = @"# Feature Title

This feature does X.

## Acceptance Criteria
- [ ] Criterion 1
- [x] Criterion 2 (completed)
- Criterion 3 (no checkbox)";
            var filePath = CreateTestFile(content);

            try
            {
                // Act
                var result = MarkdownParser.ParseFile(filePath);

                // Assert
                Assert.True(result.Success);
                Assert.Equal(3, result.AcceptanceCriteria.Count);
                Assert.Equal("Criterion 1", result.AcceptanceCriteria[0]);
                Assert.Equal("Criterion 2 (completed)", result.AcceptanceCriteria[1]);
                Assert.Equal("Criterion 3 (no checkbox)", result.AcceptanceCriteria[2]);
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void ParseFile_WithCodeBlocks_ExtractsCodeBlocks()
        {
            // Arrange
            var content = @"# Technical Task

## Steps
```bash
npm install
npm run build
```

## Expected Output
```json
{
  ""status"": ""success""
}
```";
            var filePath = CreateTestFile(content);

            try
            {
                // Act
                var result = MarkdownParser.ParseFile(filePath);

                // Assert
                Assert.True(result.Success);
                Assert.Equal(2, result.CodeBlocks.Count);
                Assert.Contains("npm install", result.CodeBlocks[0]);
                Assert.Contains("status", result.CodeBlocks[1]);
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void ParseFile_WithMalformedHeader_WarnsButParses()
        {
            // Arrange
            var content = @"#Malformed Title

Description here.";
            var filePath = CreateTestFile(content);

            try
            {
                // Act
                var result = MarkdownParser.ParseFile(filePath);

                // Assert
                Assert.True(result.Success);
                Assert.NotEmpty(result.Warnings);
                Assert.Contains("Malformed header", result.Warnings[0].Message);
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void ParseFile_WithMissingTitle_ReturnsError()
        {
            // Arrange
            var content = @"Just some description without a title.";
            var filePath = CreateTestFile(content);

            try
            {
                // Act
                var result = MarkdownParser.ParseFile(filePath);

                // Assert
                Assert.False(result.Success);
                Assert.NotEmpty(result.Errors);
                Assert.Contains("Title is required", result.Errors[0].Message);
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void ParseFile_WithNonexistentFile_ReturnsError()
        {
            // Act
            var result = MarkdownParser.ParseFile("/nonexistent/path/file.md");

            // Assert
            Assert.False(result.Success);
            Assert.NotEmpty(result.Errors);
            Assert.Contains("File not found", result.Errors[0].Message);
        }

        [Fact]
        public void ParseFile_WithEmptyFile_ReturnsError()
        {
            // Arrange
            var filePath = CreateTestFile("");

            try
            {
                // Act
                var result = MarkdownParser.ParseFile(filePath);

                // Assert
                Assert.False(result.Success);
                Assert.NotEmpty(result.Errors);
                Assert.Contains("empty", result.Errors[0].Message.ToLower());
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void ParseFile_WithComplexDocument_ParsesAllSections()
        {
            // Arrange
            var content = @"---
type: Epic
priority: High
---
# Epic: Authentication System

Implement comprehensive authentication.

## Labels: Security, Auth

### Overview
Complete authentication overhaul with OAuth2 support.

## Acceptance Criteria
- [ ] OAuth2 integration
- [ ] Token refresh mechanism
- [ ] Rate limiting

## Code Examples
```csharp
public void ConfigureAuth(IServiceCollection services)
{
    services.AddAuthentication();
}
```";
            var filePath = CreateTestFile(content);

            try
            {
                // Act
                var result = MarkdownParser.ParseFile(filePath);

                // Assert
                Assert.True(result.Success);
                Assert.Equal("Epic: Authentication System", result.Title);
                Assert.Equal("Epic", result.Metadata["type"]);
                Assert.Equal("High", result.Metadata["priority"]);
                Assert.Equal(3, result.AcceptanceCriteria.Count);
                Assert.NotEmpty(result.CodeBlocks);
                Assert.Contains("ConfigureAuth", result.CodeBlocks[0]);
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void ParseFile_WithUnclosedCodeBlock_WarnsButIncludesBlock()
        {
            // Arrange
            var content = @"# Task

## Implementation
```csharp
public void DoSomething()
{
    // Implementation here
}";
            var filePath = CreateTestFile(content);

            try
            {
                // Act
                var result = MarkdownParser.ParseFile(filePath);

                // Assert
                Assert.True(result.Success);
                Assert.NotEmpty(result.Warnings);
                Assert.Contains("Unclosed code block", result.Warnings[0].Message);
                Assert.NotEmpty(result.CodeBlocks);
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void ParseFile_WithMultipleMetadataFormats_MergesAll()
        {
            // Arrange
            var content = @"---
source: yaml
---
# Title

## Target: azdo
## Project: MyProject

Content here.";
            var filePath = CreateTestFile(content);

            try
            {
                // Act
                var result = MarkdownParser.ParseFile(filePath);

                // Assert
                Assert.True(result.Success);
                Assert.Equal("yaml", result.Metadata["source"]);
                Assert.Equal("azdo", result.Metadata["target"]);
                Assert.Equal("MyProject", result.Metadata["project"]);
            }
            finally
            {
                CleanupFile(filePath);
            }
        }

        [Fact]
        public void ParseFile_WithVerboseMode_IncludesDetailedErrors()
        {
            // Arrange
            var filePath = "/invalid/path/test.md";

            // Act
            var result = MarkdownParser.ParseFile(filePath, verbose: true);

            // Assert
            Assert.False(result.Success);
            Assert.NotEmpty(result.Errors);
            // Verbose mode may include stack traces
        }

        [Fact]
        public void ParseFile_WithSpecialCharactersInMetadata_ParsesCorrectly()
        {
            // Arrange
            var content = @"---
description: ""Complex value with: colons and quotes""
---
# Test Title

Content.";
            var filePath = CreateTestFile(content);

            try
            {
                // Act
                var result = MarkdownParser.ParseFile(filePath);

                // Assert
                Assert.True(result.Success);
                Assert.Contains("Complex value with: colons and quotes", result.Metadata["description"]);
            }
            finally
            {
                CleanupFile(filePath);
            }
        }
    }
}
