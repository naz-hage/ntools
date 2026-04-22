// Copyright (c) 2020-2026 naz-hage. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Sdo.Utilities
{
    /// <summary>
    /// Manages SDO configuration from .sdo.yaml files.
    /// Supports environment variable interpolation, defaults, and CLI overrides.
    /// </summary>
    public class ConfigurationManager
    {
        private const string DefaultConfigFileName = "sdo-config.yaml";
        private readonly Dictionary<string, string> _config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _errors = new List<string>();
        private bool _isLoaded = false;
        private string? _loadedConfigPath = null;

        /// <summary>
        /// Get the path to the loaded configuration file (if any).
        /// </summary>
        public string? LoadedConfigPath => _loadedConfigPath;

        /// <summary>
        /// Load configuration from file or use defaults.
        /// </summary>
        /// <param name="configPath">Optional path to config file. If null, searches for .sdo.yaml in current/parent dirs.</param>
        /// <returns>True if configuration loaded successfully.</returns>
        public bool Load(string? configPath = null)
        {
            _config.Clear();
            _errors.Clear();

            // Try to find config file
            string? foundPath = null;

            if (!string.IsNullOrEmpty(configPath))
            {
                if (File.Exists(configPath))
                {
                    foundPath = configPath;
                }
                else
                {
                    _errors.Add($"Configuration file not found: {configPath}");
                    return false;
                }
            }
            else
            {
                // Search for .sdo.yaml in current directory and up to 3 parent directories
                foundPath = FindConfigFile();
            }

            if (foundPath != null)
            {
                return LoadFromFile(foundPath);
            }

            // No config file found, use defaults
            _isLoaded = true;
            return true;
        }

        /// <summary>
        /// Get a configuration value with optional default.
        /// Supports environment variable interpolation (${VAR_NAME}).
        /// Accepts both dot and colon notation (e.g., "commands.wi.list.area_path" or "commands:wi:list:area_path").
        /// </summary>
        public string? GetValue(string key, string? defaultValue = null)
        {
            if (!_isLoaded)
            {
                Load();
            }

            // Normalize key: convert colons to dots for consistency
            var normalizedKey = key.Replace(':', '.').ToLower();
            
            if (_config.TryGetValue(normalizedKey, out var value))
            {
                return InterpolateEnvironmentVariables(value);
            }

            return InterpolateEnvironmentVariables(defaultValue);
        }

        /// <summary>
        /// Get a configuration value as an integer.
        /// </summary>
        public int GetInt(string key, int defaultValue = 0)
        {
            var value = GetValue(key);
            if (int.TryParse(value, out var result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Get a configuration value as a boolean.
        /// </summary>
        public bool GetBool(string key, bool defaultValue = false)
        {
            var value = GetValue(key);
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }

            return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("1", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Set a configuration value (e.g., from CLI override).
        /// Accepts both dot and colon notation.
        /// </summary>
        public void SetValue(string key, string value)
        {
            // Normalize key: convert colons to dots for consistency
            var normalizedKey = key.Replace(':', '.').ToLower();
            _config[normalizedKey] = value;
        }

        /// <summary>
        /// Get all configuration keys.
        /// </summary>
        public IEnumerable<string> GetKeys()
        {
            return _config.Keys;
        }

        /// <summary>
        /// Get all errors encountered during loading.
        /// </summary>
        public IEnumerable<string> GetErrors()
        {
            return _errors;
        }

        /// <summary>
        /// Check if configuration is valid.
        /// </summary>
        public bool IsValid()
        {
            return _errors.Count == 0 && _isLoaded;
        }

        /// <summary>
        /// Search for sdo-config.yaml in:
        /// 1. Current directory
        /// 2. Current directory's .temp folder
        /// 3. Parent directory's .temp folder
        /// 4. Up to 3 parent directories
        /// </summary>
        private static string? FindConfigFile()
        {
            var currentDir = Directory.GetCurrentDirectory();

            // Check current directory
            var configPath = Path.Combine(currentDir, DefaultConfigFileName);
            if (File.Exists(configPath))
            {
                return configPath;
            }

            // Check current directory's .temp folder
            var tempConfigPath = Path.Combine(currentDir, ".temp", DefaultConfigFileName);
            if (File.Exists(tempConfigPath))
            {
                return tempConfigPath;
            }

            // Check parent directory's .temp folder
            var parentDir = Directory.GetParent(currentDir);
            if (parentDir != null)
            {
                tempConfigPath = Path.Combine(parentDir.FullName, ".temp", DefaultConfigFileName);
                if (File.Exists(tempConfigPath))
                {
                    return tempConfigPath;
                }
            }

            // Check up to 3 parent directories
            currentDir = Directory.GetCurrentDirectory();
            for (int i = 0; i < 3; i++)
            {
                configPath = Path.Combine(currentDir, DefaultConfigFileName);
                if (File.Exists(configPath))
                {
                    return configPath;
                }

                parentDir = Directory.GetParent(currentDir);
                if (parentDir == null) break;

                currentDir = parentDir.FullName;
            }

            return null;
        }

        /// <summary>
        /// Load configuration from YAML file using YamlDotNet for nested structure support.
        /// Flattens nested keys into dot-notation (e.g., commands.wi.list.area_path).
        /// </summary>
        private bool LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _errors.Add($"Configuration file not found: {filePath}");
                    return false;
                }

                _loadedConfigPath = Path.GetFullPath(filePath);  // Store absolute path
                var yaml = File.ReadAllText(filePath);
                
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                object? parsedYaml;
                try
                {
                    parsedYaml = deserializer.Deserialize(yaml);
                }
                catch (Exception ex)
                {
                    _errors.Add($"YAML parsing error: {ex.Message}");
                    _isLoaded = true;
                    return false;
                }

                if (parsedYaml is Dictionary<object, object> rootDict)
                {
                    FlattenDictionary(rootDict, "", _config);
                }

                _isLoaded = true;
                return true;
            }
            catch (Exception ex)
            {
                _errors.Add($"Failed to load configuration: {ex.Message}");
                _isLoaded = true;
                return false;
            }
        }

        /// <summary>
        /// Recursively flatten a nested dictionary into dot-notation keys.
        /// Example: {commands: {wi: {list: {area_path: "value"}}}} → "commands.wi.list.area_path" = "value"
        /// </summary>
        private static void FlattenDictionary(Dictionary<object, object> dict, string prefix, Dictionary<string, string> flatConfig)
        {
            foreach (var kvp in dict)
            {
                var key = kvp.Key.ToString() ?? "";
                var fullKey = string.IsNullOrEmpty(prefix) ? key : $"{prefix}.{key}";

                if (kvp.Value is Dictionary<object, object> nestedDict)
                {
                    // Recursively flatten nested dictionaries
                    FlattenDictionary(nestedDict, fullKey, flatConfig);
                }
                else if (kvp.Value is List<object> list)
                {
                    // Handle lists by joining with comma
                    var listValue = string.Join(",", list.Select(x => x?.ToString() ?? ""));
                    flatConfig[fullKey.ToLower()] = listValue;
                }
                else if (kvp.Value != null)
                {
                    // Convert value to string, remove inline comments
                    var valueStr = kvp.Value.ToString() ?? "";
                    
                    // Remove inline comments (# for YAML comments)
                    var hashIdx = valueStr.IndexOf('#');
                    if (hashIdx >= 0)
                    {
                        valueStr = valueStr.Substring(0, hashIdx).Trim();
                    }
                    
                    valueStr = valueStr.Trim('"', '\'');
                    flatConfig[fullKey.ToLower()] = valueStr;
                }
            }
        }

        /// <summary>
        /// Replace environment variable references (${VAR_NAME}) with actual values.
        /// </summary>
        private static string? InterpolateEnvironmentVariables(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            // Find all ${VAR_NAME} patterns
            var pattern = @"\$\{([^}]+)\}";
            return Regex.Replace(value, pattern, match =>
            {
                var varName = match.Groups[1].Value;
                var envValue = Environment.GetEnvironmentVariable(varName);
                return envValue ?? match.Value;
            });
        }

        /// <summary>
        /// Export configuration to JSON for debugging.
        /// </summary>
        public string ExportAsJson()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(_config, options);
        }

        /// <summary>
        /// Get configuration as dictionary (non-sensitive values only).
        /// </summary>
        public Dictionary<string, string> GetAllValues(bool includeSensitive = false)
        {
            var result = new Dictionary<string, string>(_config);

            // Remove sensitive values if requested
            if (!includeSensitive)
            {
                var sensitiveKeys = new[] { "token", "key", "password", "secret", "pat" };
                var keysToRemove = result.Keys
                    .Where(k => sensitiveKeys.Any(s => k.ToLower().Contains(s)))
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    result[key] = "***REDACTED***";
                }
            }

            return result;
        }
    }
}
