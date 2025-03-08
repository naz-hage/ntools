// Package main provides a utility to scan a directory for JSON files,
// parse them, and combine their contents into a single JSON file.
package main

import (
    "encoding/json"
    "fmt"
    "os"
    "path/filepath"
)

// GetJSONFiles returns a slice of all .json files in the specified directory,
// excluding any files named "apps.json".
//
// folderPath: The path to the directory to scan for JSON files.
//
// Returns a slice of file paths to .json files and an error if any.
func GetJSONFiles(folderPath string) ([]string, error) {
    var result []string

    // Check if the directory exists
    info, err := os.Stat(folderPath)
    if err != nil {
        return nil, fmt.Errorf("error accessing directory: %w", err)
    }
    if !info.IsDir() {
        return nil, fmt.Errorf("path is not a directory: %s", folderPath)
    }

    // Walk through the directory
    err = filepath.Walk(folderPath, func(path string, info os.FileInfo, err error) error {
        if err != nil {
            return err
        }

        // Skip directories
        if info.IsDir() {
            return nil
        }

        // Check for .json extension and exclude apps.json
        if filepath.Ext(path) == ".json" && filepath.Base(path) != "apps.json" {
            result = append(result, path)
        }

        return nil
    })

    if err != nil {
        return nil, fmt.Errorf("error walking through directory: %w", err)
    }

    return result, nil
}

// main is the entry point for the application. It scans the current directory
// for JSON files, parses them, and combines their contents into a single JSON file.
func main() {
    files, err := GetJSONFiles(".")
    if err != nil {
        fmt.Printf("Error: %v\n", err)
        return
    }

    count := len(files)
    fmt.Printf("%d JSON files found (excluding apps.json)\n", count)
    for _, file := range files {
        fmt.Println(file)
    }

    // Initialize the combined structure
    type App struct {
        Name            string `json:"Name"`
        Version         string `json:"Version"`
        AppFileName     string `json:"AppFileName"`
        WebDownloadFile string `json:"WebDownloadFile"`
        DownloadedFile  string `json:"DownloadedFile"`
        InstallCommand  string `json:"InstallCommand"`
        InstallArgs     string `json:"InstallArgs"`
        InstallPath     string `json:"InstallPath"`
        UninstallCommand string `json:"UninstallCommand"`
        UninstallArgs   string `json:"UninstallArgs"`
        StoredHash      string `json:"StoredHash,omitempty"`
    }

    type FileContent struct {
        Version       string `json:"Version"`
        NbuildAppList []App  `json:"NbuildAppList"`
    }

    type CombinedJSON struct {
        Version       string `json:"Version"`
        NbuildAppList []App  `json:"NbuildAppList"`
    }

    combined := CombinedJSON{
        Version:       "1.2.0",
        NbuildAppList: []App{},
    }

    // Read and combine the content of each JSON file
    for _, file := range files {
        content, err := os.ReadFile(file)
        if err != nil {
            fmt.Printf("Error reading file %s: %v\n", file, err)
            continue
        }

        // Parse the content as FileContent
        var fileContent FileContent
        if err := json.Unmarshal(content, &fileContent); err != nil {
            fmt.Printf("Error parsing JSON file %s: %v\n", file, err)
            continue
        }

        // Append the apps to the combined list
        combined.NbuildAppList = append(combined.NbuildAppList, fileContent.NbuildAppList...)
    }

    // Write the combined content to apps.json
    combinedContent, err := json.MarshalIndent(combined, "", "  ")
    if err != nil {
        fmt.Printf("Error marshaling combined JSON: %v\n", err)
        return
    }

    if err := os.WriteFile("apps.json", combinedContent, 0644); err != nil {
        fmt.Printf("Error writing to apps.json: %v\n", err)
        return
    }

    fmt.Println("Combined JSON written to apps.json")
}