# lf: File and Folder Listing Utility

`lf` is a command-line tool for listing files and folders in a directory tree, with flexible filtering options. It is built using `System.CommandLine` and is part of the ntools suite.

## Features

- **List files** by extension, recursively.
- **List folders** containing specified names, recursively.
- Simple, fast, and scriptable.

---

## Commands

### 1. `files`

Lists files with specified extensions in a directory (recursively).

**Options:**
- `-d`, `--directoryPath`  
  Directory path to search in (default: current directory)
- `-e`, `--extensions`  
  Comma-separated file extensions (default: `.yml,.yaml`)

**Example:**
```sh
lf files -d C:\Projects -e .cs,.md
```
This command will list all `.cs` and `.md` files in the `C:\Projects` directory and its subdirectories.

---

### 2. `folders`

Lists folders containing specified names in a directory (recursively).

**Options:**
- `-d`, `--directoryPath`  
  Directory path to search in (default: current directory)
- `-n`, `--name`  
  Comma-separated list of folder names to search for

**Example:**
```sh
lf folders -d C:\Projects -n bin,obj
```
This command will list all folders named `bin` and `obj` in the `C:\Projects` directory and its subdirectories.
