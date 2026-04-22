// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sdo.Models;

namespace Sdo.Utilities
{
    /// <summary>
    /// Parses markdown files for rich content creation (work items, PRs).
    /// Supports YAML frontmatter, tables, code blocks, and flexible metadata extraction.
    /// </summary>
    public class MarkdownParser
    {
        /// <summary>
        /// Parse result containing detailed error information.
        /// </summary>
        public class ParseError
        {
            public int LineNumber { get; set; }
            public string Message { get; set; } = string.Empty;
            public string LineContent { get; set; } = string.Empty;
        }

        /// <summary>
        /// Parse result containing parsed content and any errors encountered.
        /// </summary>
        public class MarkdownParseResult
        {
            public bool Success { get; set; } = true;
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public List<string> AcceptanceCriteria { get; set; } = new List<string>();
            public List<string> CodeBlocks { get; set; } = new List<string>();
            public List<ParseError> Errors { get; set; } = new List<ParseError>();
            public List<ParseError> Warnings { get; set; } = new List<ParseError>();
        }

        /// <summary>
        /// Parse a markdown file with comprehensive error handling and feature support.
        /// </summary>
        /// <param name="filePath">Path to the markdown file.</param>
        /// <param name="verbose">Enable verbose error reporting.</param>
        /// <returns>Parse result with content and any errors.</returns>
        public static MarkdownParseResult ParseFile(string filePath, bool verbose = false)
        {
            var result = new MarkdownParseResult();

            try
            {
                if (!File.Exists(filePath))
                {
                    result.Success = false;
                    result.Errors.Add(new ParseError 
                    { 
                        Message = $"File not found: {filePath}" 
                    });
                    return result;
                }

                if (new FileInfo(filePath).Length == 0)
                {
                    result.Success = false;
                    result.Errors.Add(new ParseError 
                    { 
                        Message = "Markdown file is empty" 
                    });
                    return result;
                }

                var lines = File.ReadAllLines(filePath);
                ParseMarkdownContent(lines, result, verbose);

                // Validate required fields
                if (string.IsNullOrWhiteSpace(result.Title))
                {
                    result.Success = false;
                    result.Errors.Add(new ParseError 
                    { 
                        LineNumber = 1, 
                        Message = "Title is required. Use '# Title' or YAML frontmatter." 
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add(new ParseError 
                { 
                    Message = $"Failed to parse file: {ex.Message}" 
                });
                if (verbose)
                {
                    result.Errors.Add(new ParseError 
                    { 
                        Message = $"Stack trace: {ex.StackTrace}" 
                    });
                }
                return result;
            }
        }

        /// <summary>
        /// Parse markdown content from lines array.
        /// </summary>
        private static void ParseMarkdownContent(string[] lines, MarkdownParseResult result, bool verbose)
        {
            int idx = 0;

            // Parse YAML frontmatter
            idx = ParseYamlFrontmatter(lines, result, idx);

            // Parse title
            idx = ParseTitle(lines, result, idx);

            // Parse metadata (Key: Value format in level-2 headers)
            idx = ParseMetadata(lines, result, idx);

            // Parse content (description, code blocks, acceptance criteria)
            ParseContent(lines, result, idx);
        }

        /// <summary>
        /// Parse YAML frontmatter if present.
        /// </summary>
        private static int ParseYamlFrontmatter(string[] lines, MarkdownParseResult result, int startIdx)
        {
            if (startIdx >= lines.Length) return startIdx;

            if (lines[startIdx].Trim() != "---") return startIdx;

            int idx = startIdx + 1;
            while (idx < lines.Length)
            {
                var line = lines[idx];
                if (line.Trim() == "---")
                {
                    return idx + 1;
                }

                // Parse YAML key-value pair
                var colonIdx = line.IndexOf(':');
                if (colonIdx > 0)
                {
                    var key = line.Substring(0, colonIdx).Trim();
                    var value = line.Substring(colonIdx + 1).Trim().Trim('"', '\'');
                    result.Metadata[NormalizeKey(key)] = value;
                }

                idx++;
            }

            result.Warnings.Add(new ParseError 
            { 
                LineNumber = startIdx + 1, 
                Message = "YAML frontmatter not closed (missing closing ---)." 
            });

            return idx;
        }

        /// <summary>
        /// Parse title from H1 header only (strict requirement).
        /// </summary>
        private static int ParseTitle(string[] lines, MarkdownParseResult result, int startIdx)
        {
            for (int i = startIdx; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // H1 header is required
                if (line.StartsWith("# "))
                {
                    result.Title = line.Substring(2).Trim();
                    return i + 1;
                }

                // Malformed H1 - warn and use as title
                if (line.StartsWith("#") && !line.StartsWith("##"))
                {
                    result.Warnings.Add(new ParseError 
                    { 
                        LineNumber = i + 1, 
                        LineContent = line,
                        Message = "Malformed header (missing space after #). Used as title anyway." 
                    });
                    result.Title = line.Substring(1).Trim();
                    return i + 1;
                }

                // Stop at any non-header content if no title found yet
                // Don't use regular content as fallback title
                break;
            }

            return startIdx;
        }

        /// <summary>
        /// Parse metadata from level-2 headers in "Key: Value" format.
        /// </summary>
        private static int ParseMetadata(string[] lines, MarkdownParseResult result, int startIdx)
        {
            int idx = startIdx;
            while (idx < lines.Length)
            {
                var line = lines[idx].Trim();

                // Skip empty lines
                if (string.IsNullOrEmpty(line))
                {
                    idx++;
                    continue;
                }

                // Check for level-2 header with key:value format
                if (line.StartsWith("## "))
                {
                    var content = line.Substring(3).Trim();
                    if (content.Contains(":"))
                    {
                        var colonIdx = content.IndexOf(':');
                        var key = content.Substring(0, colonIdx).Trim();
                        var value = content.Substring(colonIdx + 1).Trim().Trim('"', '\'');
                        result.Metadata[NormalizeKey(key)] = value;
                        idx++;
                        continue;
                    }
                }

                // Stop at any non-metadata header or content
                break;
            }

            return idx;
        }

        /// <summary>
        /// Parse content including description, code blocks, and acceptance criteria.
        /// </summary>
        private static void ParseContent(string[] lines, MarkdownParseResult result, int startIdx)
        {
            var descLines = new List<string>();
            int idx = startIdx;
            bool inCodeBlock = false;
            string codeBlockLanguage = string.Empty;
            var currentCodeBlock = new List<string>();

            while (idx < lines.Length)
            {
                var line = lines[idx];
                var trimmed = line.Trim();

                // Handle code blocks
                if (trimmed.StartsWith("```"))
                {
                    if (!inCodeBlock)
                    {
                        inCodeBlock = true;
                        codeBlockLanguage = trimmed.Substring(3).Trim().ToLower();
                        currentCodeBlock.Clear();
                    }
                    else
                    {
                        inCodeBlock = false;
                        if (currentCodeBlock.Count > 0)
                        {
                            result.CodeBlocks.Add(string.Join(Environment.NewLine, currentCodeBlock));
                        }
                        currentCodeBlock.Clear();
                    }
                    idx++;
                    continue;
                }

                if (inCodeBlock)
                {
                    currentCodeBlock.Add(line);
                    idx++;
                    continue;
                }

                // Check for acceptance criteria section
                if (trimmed.StartsWith("##") && trimmed.ToLower().Contains("acceptance"))
                {
                    // Add collected description
                    if (descLines.Count > 0)
                    {
                        result.Description = string.Join(Environment.NewLine, descLines).Trim();
                        descLines.Clear();
                    }

                    // Parse acceptance criteria starting from next line
                    idx = ParseAcceptanceCriteria(lines, result, idx + 1);
                    
                    // Continue parsing remaining content
                    continue;
                }

                // Skip metadata headers (level-2 headers with colons)
                if (trimmed.StartsWith("##") && trimmed.Contains(":"))
                {
                    idx++;
                    continue;
                }

                // Collect description lines (non-header content)
                if (!trimmed.StartsWith("#") || trimmed.Equals("##"))
                {
                    descLines.Add(line);
                }

                idx++;
            }

            // Use collected description if not already set
            if (string.IsNullOrWhiteSpace(result.Description) && descLines.Count > 0)
            {
                result.Description = string.Join(Environment.NewLine, descLines).Trim();
            }

            // Close unclosed code block
            if (inCodeBlock && currentCodeBlock.Count > 0)
            {
                result.Warnings.Add(new ParseError 
                { 
                    Message = "Unclosed code block at end of file." 
                });
                result.CodeBlocks.Add(string.Join(Environment.NewLine, currentCodeBlock));
            }
        }

        /// <summary>
        /// Parse acceptance criteria list items.
        /// </summary>
        private static int ParseAcceptanceCriteria(string[] lines, MarkdownParseResult result, int startIdx)
        {
            int idx = startIdx;
            while (idx < lines.Length)
            {
                var line = lines[idx].Trim();

                if (string.IsNullOrEmpty(line))
                {
                    idx++;
                    continue;
                }

                // List item with checkbox or marker
                if (line.StartsWith("- [") || line.StartsWith("- ") || line.StartsWith("* [") || line.StartsWith("* "))
                {
                    var normalized = NormalizeAcceptanceCriterion(line);
                    if (!string.IsNullOrEmpty(normalized))
                    {
                        result.AcceptanceCriteria.Add(normalized);
                    }
                    idx++;
                    continue;
                }

                // Next section
                if (line.StartsWith("## "))
                {
                    break;
                }

                idx++;
            }

            return idx;
        }

        /// <summary>
        /// Normalize acceptance criterion by removing markers and checkboxes.
        /// </summary>
        private static string NormalizeAcceptanceCriterion(string line)
        {
            var normalized = line;

            // Remove list markers
            if (normalized.StartsWith("- ")) normalized = normalized.Substring(2);
            else if (normalized.StartsWith("* ")) normalized = normalized.Substring(2);

            // Remove checkbox
            if (normalized.StartsWith("[") && normalized.Length > 2 && normalized[2] == ']')
            {
                normalized = normalized.Substring(3);
            }

            return normalized.Trim();
        }

        /// <summary>
        /// Normalize dictionary key (lowercase, underscores).
        /// </summary>
        private static string NormalizeKey(string key)
        {
            return key.ToLowerInvariant().Replace(" ", "_");
        }
    }
}
