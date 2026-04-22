// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sdo.Utilities
{
    /// <summary>
    /// Framework for processing bulk operations with error recovery and progress tracking.
    /// Supports batch create/update/delete operations with configurable batch sizes and retry logic.
    /// </summary>
    public class BulkOperationProcessor
    {
        /// <summary>
        /// Configuration for bulk operation processing.
        /// </summary>
        public class BulkOperationConfig
        {
            /// <summary>
            /// Maximum number of items to process in a single batch.
            /// </summary>
            public int BatchSize { get; set; } = 10;

            /// <summary>
            /// Maximum number of retry attempts for failed items.
            /// </summary>
            public int MaxRetries { get; set; } = 3;

            /// <summary>
            /// Delay in milliseconds between retries.
            /// </summary>
            public int RetryDelayMs { get; set; } = 1000;

            /// <summary>
            /// Whether to stop on first error or continue processing.
            /// </summary>
            public bool StopOnError { get; set; } = false;

            /// <summary>
            /// Whether to generate a summary report at the end.
            /// </summary>
            public bool GenerateReport { get; set; } = true;

            /// <summary>
            /// Path to write the operation report (optional).
            /// </summary>
            public string? ReportPath { get; set; }
        }

        /// <summary>
        /// Result for a single operation in a bulk process.
        /// </summary>
        public class OperationResult
        {
            public int ItemIndex { get; set; }
            public string ItemIdentifier { get; set; } = string.Empty;
            public bool Success { get; set; }
            public string? Message { get; set; }
            public string? ErrorMessage { get; set; }
            public int RetryCount { get; set; }
            public TimeSpan Duration { get; set; }
        }

        /// <summary>
        /// Summary of bulk operation processing.
        /// </summary>
        public class BulkOperationSummary
        {
            public int TotalItems { get; set; }
            public int SuccessfulItems { get; set; }
            public int FailedItems { get; set; }
            public int SkippedItems { get; set; }
            public TimeSpan TotalDuration { get; set; }
            public List<OperationResult> Results { get; set; } = new List<OperationResult>();
            public List<string> Warnings { get; set; } = new List<string>();

            public double SuccessRate => TotalItems > 0 ? (SuccessfulItems * 100.0) / TotalItems : 0;
        }

        /// <summary>
        /// Process a batch of items asynchronously.
        /// </summary>
        /// <typeparam name="T">Type of item to process.</typeparam>
        /// <param name="items">Collection of items to process.</param>
        /// <param name="operationFunc">Async function to process each item. Returns (success, message).</param>
        /// <param name="identifierFunc">Function to extract identifier for an item (for logging).</param>
        /// <param name="config">Configuration for bulk processing.</param>
        /// <returns>Summary of the bulk operation.</returns>
        public static async Task<BulkOperationSummary> ProcessBulkAsync<T>(
            IEnumerable<T> items,
            Func<T, Task<(bool success, string? message)>> operationFunc,
            Func<T, string> identifierFunc,
            BulkOperationConfig? config = null)
        {
            config ??= new BulkOperationConfig();
            var summary = new BulkOperationSummary();
            var results = new List<OperationResult>();
            var itemList = items.ToList();

            summary.TotalItems = itemList.Count;
            var startTime = DateTime.UtcNow;

            try
            {
                int itemIndex = 0;
                foreach (var item in itemList)
                {
                    var operationStartTime = DateTime.UtcNow;
                    var result = new OperationResult
                    {
                        ItemIndex = itemIndex,
                        ItemIdentifier = identifierFunc(item)
                    };

                    try
                    {
                        int retryCount = 0;
                        (bool success, string? message) opResult = (false, null);

                        while (retryCount <= config.MaxRetries)
                        {
                            try
                            {
                                opResult = await operationFunc(item);

                                if (opResult.success)
                                {
                                    break;
                                }

                                if (retryCount < config.MaxRetries)
                                {
                                    retryCount++;
                                    await Task.Delay(config.RetryDelayMs);
                                }
                                else
                                {
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                if (retryCount < config.MaxRetries)
                                {
                                    retryCount++;
                                    await Task.Delay(config.RetryDelayMs);
                                }
                                else
                                {
                                    opResult = (false, ex.Message);
                                    break;
                                }
                            }
                        }

                        result.Success = opResult.success;
                        result.Message = opResult.message;
                        result.RetryCount = retryCount;

                        if (result.Success)
                        {
                            summary.SuccessfulItems++;
                        }
                        else
                        {
                            summary.FailedItems++;
                            result.ErrorMessage = opResult.message ?? "Operation failed";

                            if (config.StopOnError)
                            {
                                summary.SkippedItems = itemList.Count - itemIndex - 1;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Success = false;
                        result.ErrorMessage = ex.Message;
                        summary.FailedItems++;

                        if (config.StopOnError)
                        {
                            summary.SkippedItems = itemList.Count - itemIndex - 1;
                            break;
                        }
                    }

                    result.Duration = DateTime.UtcNow - operationStartTime;
                    results.Add(result);
                    itemIndex++;
                }

                summary.Results = results;
                summary.TotalDuration = DateTime.UtcNow - startTime;

                // Generate report if configured
                if (config.GenerateReport)
                {
                    GenerateReport(summary, config.ReportPath);
                }

                return summary;
            }
            catch (Exception ex)
            {
                summary.Warnings.Add($"Bulk operation encountered error: {ex.Message}");
                summary.TotalDuration = DateTime.UtcNow - startTime;
                return summary;
            }
        }

        /// <summary>
        /// Generate a report of the bulk operation results.
        /// </summary>
        private static void GenerateReport(BulkOperationSummary summary, string? reportPath)
        {
            var reportLines = new List<string>
            {
                "=== Bulk Operation Report ===",
                $"Total Items: {summary.TotalItems}",
                $"Successful: {summary.SuccessfulItems} ({summary.SuccessRate:F1}%)",
                $"Failed: {summary.FailedItems}",
                $"Skipped: {summary.SkippedItems}",
                $"Total Duration: {summary.TotalDuration.TotalSeconds:F2}s",
                "",
                "=== Detailed Results ===",
            };

            foreach (var result in summary.Results)
            {
                var status = result.Success ? "✓" : "✗";
                reportLines.Add($"{status} [{result.ItemIndex}] {result.ItemIdentifier}");

                if (!result.Success && result.ErrorMessage != null)
                {
                    reportLines.Add($"  Error: {result.ErrorMessage}");
                }

                if (result.RetryCount > 0)
                {
                    reportLines.Add($"  Retries: {result.RetryCount}");
                }

                reportLines.Add($"  Duration: {result.Duration.TotalMilliseconds:F0}ms");
            }

            if (summary.Warnings.Count > 0)
            {
                reportLines.Add("");
                reportLines.Add("=== Warnings ===");
                reportLines.AddRange(summary.Warnings);
            }

            var report = string.Join(Environment.NewLine, reportLines);

            // Output to console
            Console.WriteLine(report);

            // Write to file if path provided
            if (!string.IsNullOrEmpty(reportPath))
            {
                try
                {
                    File.WriteAllText(reportPath, report);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to write report to {reportPath}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Read items from a CSV or JSON file for bulk processing.
        /// </summary>
        /// <param name="filePath">Path to the file containing items.</param>
        /// <returns>List of dictionaries representing items.</returns>
        public static List<Dictionary<string, string>> ReadItemsFromFile(string filePath)
        {
            var items = new List<Dictionary<string, string>>();

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var extension = Path.GetExtension(filePath).ToLower();

            if (extension == ".csv")
            {
                items = ParseCsvFile(filePath);
            }
            else if (extension == ".json")
            {
                items = ParseJsonFile(filePath);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported file format: {extension}");
            }

            return items;
        }

        /// <summary>
        /// Parse CSV file into list of dictionaries.
        /// </summary>
        private static List<Dictionary<string, string>> ParseCsvFile(string filePath)
        {
            var items = new List<Dictionary<string, string>>();
            var lines = File.ReadAllLines(filePath);

            if (lines.Length < 2)
            {
                return items;
            }

            var headers = ParseCsvLine(lines[0]);

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                var values = ParseCsvLine(lines[i]);
                var item = new Dictionary<string, string>();

                for (int j = 0; j < headers.Count && j < values.Count; j++)
                {
                    item[headers[j]] = values[j];
                }

                items.Add(item);
            }

            return items;
        }

        /// <summary>
        /// Parse a CSV line handling quoted values.
        /// </summary>
        private static List<string> ParseCsvLine(string line)
        {
            var values = new List<string>();
            var current = new System.Text.StringBuilder();
            bool inQuotes = false;

            foreach (var ch in line)
            {
                if (ch == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (ch == ',' && !inQuotes)
                {
                    values.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(ch);
                }
            }

            values.Add(current.ToString().Trim());
            return values;
        }

        /// <summary>
        /// Parse JSON file into list of dictionaries.
        /// </summary>
        private static List<Dictionary<string, string>> ParseJsonFile(string filePath)
        {
            var items = new List<Dictionary<string, string>>();

            try
            {
                var json = File.ReadAllText(filePath);
                var jArray = System.Text.Json.JsonDocument.Parse(json);

                if (jArray.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var element in jArray.RootElement.EnumerateArray())
                    {
                        var item = new Dictionary<string, string>();

                        foreach (var property in element.EnumerateObject())
                        {
                            item[property.Name] = property.Value.GetString() ?? string.Empty;
                        }

                        items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to parse JSON file: {ex.Message}");
            }

            return items;
        }
    }
}
