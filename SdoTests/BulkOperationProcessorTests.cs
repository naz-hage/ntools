// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Sdo.Utilities;

namespace SdoTests
{
    /// <summary>
    /// Unit tests for BulkOperationProcessor.
    /// Tests batch processing, error handling, retry logic, and reporting.
    /// </summary>
    public class BulkOperationProcessorTests
    {
        private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), "BulkOperationTests");

        public BulkOperationProcessorTests()
        {
            Directory.CreateDirectory(_testDirectory);
        }

        [Fact]
        public async Task ProcessBulkAsync_WithSuccessfulItems_CompletesSuccessfully()
        {
            // Arrange
            var items = new[] { "item1", "item2", "item3" };
            var config = new BulkOperationProcessor.BulkOperationConfig { BatchSize = 2 };

            // Act
            var summary = await BulkOperationProcessor.ProcessBulkAsync(
                items,
                async (item) => await Task.FromResult((true, (string?)null)),
                item => item,
                config);

            // Assert
            Assert.Equal(3, summary.TotalItems);
            Assert.Equal(3, summary.SuccessfulItems);
            Assert.Equal(0, summary.FailedItems);
            Assert.Equal(0, summary.SkippedItems);
        }

        [Fact]
        public async Task ProcessBulkAsync_WithFailedItems_RecordsFailures()
        {
            // Arrange
            var items = new[] { "success1", "fail1", "success2" };
            var config = new BulkOperationProcessor.BulkOperationConfig { MaxRetries = 1 };

            // Act
            var summary = await BulkOperationProcessor.ProcessBulkAsync(
                items,
                async (item) =>
                {
                    var success = !item.StartsWith("fail");
                    return await Task.FromResult((success, success ? (string?)null : "Failed"));
                },
                item => item,
                config);

            // Assert
            Assert.Equal(3, summary.TotalItems);
            Assert.Equal(2, summary.SuccessfulItems);
            Assert.Equal(1, summary.FailedItems);
        }

        [Fact]
        public async Task ProcessBulkAsync_WithStopOnError_StopsProcessing()
        {
            // Arrange
            var items = new[] { "item1", "fail1", "item3", "item4" };
            var config = new BulkOperationProcessor.BulkOperationConfig 
            { 
                StopOnError = true, 
                MaxRetries = 0 
            };

            // Act
            var summary = await BulkOperationProcessor.ProcessBulkAsync(
                items,
                async (item) =>
                {
                    var success = !item.StartsWith("fail");
                    return await Task.FromResult((success, success ? (string?)null : "Failed"));
                },
                item => item,
                config);

            // Assert
            Assert.Equal(4, summary.TotalItems);
            Assert.Equal(1, summary.SuccessfulItems);
            Assert.Equal(1, summary.FailedItems);
            Assert.Equal(2, summary.SkippedItems); // item3 and item4 skipped
        }

        [Fact]
        public async Task ProcessBulkAsync_WithRetries_RetriesFailedItems()
        {
            // Arrange
            var items = new[] { "item1" };
            int attemptCount = 0;
            var config = new BulkOperationProcessor.BulkOperationConfig 
            { 
                MaxRetries = 2, 
                RetryDelayMs = 1 
            };

            // Act
            var summary = await BulkOperationProcessor.ProcessBulkAsync(
                items,
                async (item) =>
                {
                    attemptCount++;
                    // Fail first attempt, succeed on second
                    var success = attemptCount > 1;
                    return await Task.FromResult((success, success ? (string?)null : "Failed"));
                },
                item => item,
                config);

            // Assert
            Assert.Equal(1, summary.TotalItems);
            Assert.Equal(1, summary.SuccessfulItems);
            Assert.Equal(0, summary.FailedItems);
            Assert.Equal(1, summary.Results[0].RetryCount);
        }

        [Fact]
        public async Task ProcessBulkAsync_WithAsyncOperation_HandlesAsync()
        {
            // Arrange
            var items = new[] { 1, 2, 3 };
            var config = new BulkOperationProcessor.BulkOperationConfig();

            // Act
            var summary = await BulkOperationProcessor.ProcessBulkAsync(
                items,
                async (item) =>
                {
                    await Task.Delay(10); // Simulate async work
                    return (true, (string?)null);
                },
                item => item.ToString(),
                config);

            // Assert
            Assert.Equal(3, summary.TotalItems);
            Assert.Equal(3, summary.SuccessfulItems);
        }

        [Fact]
        public async Task ProcessBulkAsync_GeneratesReport_WritesToConsole()
        {
            // Arrange
            var items = new[] { "item1", "item2" };
            var reportPath = Path.Combine(_testDirectory, $"report_{Guid.NewGuid()}.txt");
            var config = new BulkOperationProcessor.BulkOperationConfig 
            { 
                GenerateReport = true, 
                ReportPath = reportPath 
            };

            // Act
            var summary = await BulkOperationProcessor.ProcessBulkAsync(
                items,
                async (item) => await Task.FromResult((true, (string?)null)),
                item => item,
                config);

            // Assert
            Assert.Equal(2, summary.TotalItems);
            Assert.Equal(2, summary.SuccessfulItems);
            Assert.True(File.Exists(reportPath), "Report file should be created");

            var reportContent = File.ReadAllText(reportPath);
            Assert.Contains("Bulk Operation Report", reportContent);
            Assert.Contains("Total Items: 2", reportContent);

            // Cleanup
            File.Delete(reportPath);
        }

        [Fact]
        public void ReadItemsFromFile_WithCsvFile_ParsesCorrectly()
        {
            // Arrange
            var csvPath = Path.Combine(_testDirectory, $"test_{Guid.NewGuid()}.csv");
            var csvContent = @"Name,Description,Type
PBI-001,Feature 1,PBI
PBI-002,Feature 2,PBI
Bug-001,Bug Fix,Bug";

            File.WriteAllText(csvPath, csvContent);

            try
            {
                // Act
                var items = BulkOperationProcessor.ReadItemsFromFile(csvPath);

                // Assert
                Assert.Equal(3, items.Count);
                Assert.Equal("PBI-001", items[0]["Name"]);
                Assert.Equal("Feature 1", items[0]["Description"]);
                Assert.Equal("Bug", items[2]["Type"]);
            }
            finally
            {
                File.Delete(csvPath);
            }
        }

        [Fact]
        public void ReadItemsFromFile_WithJsonFile_ParsesCorrectly()
        {
            // Arrange
            var jsonPath = Path.Combine(_testDirectory, $"test_{Guid.NewGuid()}.json");
            var jsonContent = @"[
  { ""Name"": ""PBI-001"", ""Description"": ""Feature 1"", ""Type"": ""PBI"" },
  { ""Name"": ""PBI-002"", ""Description"": ""Feature 2"", ""Type"": ""PBI"" },
  { ""Name"": ""Bug-001"", ""Description"": ""Bug Fix"", ""Type"": ""Bug"" }
]";

            File.WriteAllText(jsonPath, jsonContent);

            try
            {
                // Act
                var items = BulkOperationProcessor.ReadItemsFromFile(jsonPath);

                // Assert
                Assert.Equal(3, items.Count);
                Assert.Equal("PBI-001", items[0]["Name"]);
                Assert.Equal("Bug-001", items[2]["Name"]);
            }
            finally
            {
                File.Delete(jsonPath);
            }
        }

        [Fact]
        public void ReadItemsFromFile_WithInvalidFormat_ThrowsException()
        {
            // Arrange
            var invalidPath = Path.Combine(_testDirectory, "test.txt");
            File.WriteAllText(invalidPath, "some content");

            try
            {
                // Act & Assert
                Assert.Throws<InvalidOperationException>(
                    () => BulkOperationProcessor.ReadItemsFromFile(invalidPath));
            }
            finally
            {
                File.Delete(invalidPath);
            }
        }

        [Fact]
        public void ReadItemsFromFile_WithNonexistentFile_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<FileNotFoundException>(
                () => BulkOperationProcessor.ReadItemsFromFile("/nonexistent/file.csv"));
        }

        [Fact]
        public async Task ProcessBulkAsync_CalculatesSuccessRate_Correctly()
        {
            // Arrange
            var items = new[] { "s1", "f1", "s2", "f2", "s3" };
            var config = new BulkOperationProcessor.BulkOperationConfig { MaxRetries = 0 };

            // Act
            var summary = await BulkOperationProcessor.ProcessBulkAsync(
                items,
                async (item) =>
                {
                    var success = item.StartsWith("s");
                    return await Task.FromResult((success, (string?)null));
                },
                item => item,
                config);

            // Assert
            Assert.Equal(5, summary.TotalItems);
            Assert.Equal(3, summary.SuccessfulItems);
            Assert.Equal(2, summary.FailedItems);
            Assert.Equal(60, summary.SuccessRate); // 3/5 = 60%
        }

        [Fact]
        public async Task ProcessBulkAsync_TracksDuration_Correctly()
        {
            // Arrange
            var items = new[] { "item1", "item2" };
            var config = new BulkOperationProcessor.BulkOperationConfig();

            // Act
            var summary = await BulkOperationProcessor.ProcessBulkAsync(
                items,
                async (item) =>
                {
                    await Task.Delay(10);
                    return (true, (string?)null);
                },
                item => item,
                config);

            // Assert
            Assert.True(summary.TotalDuration.TotalMilliseconds >= 20, 
                "Total duration should account for processing time");
            Assert.True(summary.Results.All(r => r.Duration.TotalMilliseconds >= 10),
                "Individual durations should be tracked");
        }
    }
}
