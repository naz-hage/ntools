`Ngit` is a simple wrapper for the `Git` tool that performs basic commands such as getting and setting tags.

### Usage
```batch
Ngit.exe command [-url value] [-tag value] [-buildtype value] [-v value]
  - command   : Specifies the git command to execute.
         tag             -> Get the current tag
         settag          -> Set specified tag in -tag option
         autotag         -> Set next tag based on the build type: STAGE | PROD
         setautotag      -> Set next tag based on the build type and push to remote repo
         deletetag       -> Delete specified tag in -tag option
         branch          -> Get the current branch
         clone           -> Clone specified Git repo in the -url option
         ----
 (one of tag,setTag,autoTag,setAutoTag,deleteTag,branch,clone,pushTag, required)
  - url       : Specifies the Git repository URL. (string, default=)
  - tag       : Specifies the tag used for settag and deletetag commands. (string, default=)
  - buildtype : Specifies the build type used for autotag and setautotag commands. Possible values: STAGE, PROD. (string, default=)
  - v         : Specifies whether to print additional information. (true or false, default=False)
```

### Examples

#### Example 1: Get the Current Tag
To retrieve the current Git tag:
```batch
Ngit.exe tag -v true
```

#### Example 2: Set a Specific Tag
To set a specific tag (e.g., `1.0.0`):
```batch
Ngit.exe settag -tag 1.0.0 -v true
```

#### Example 3: Auto-Tag Based on Build Type
To set the next tag based on the build type `STAGE`:
```batch
Ngit.exe autotag -buildtype STAGE -v true
```

#### Example 4: Clone a Repository
To clone a Git repository from a specific URL:
```batch
Ngit.exe clone -url https://github.com/example/repo.git -v true
```

#### Example 5: Delete a Tag
To delete a specific tag (e.g., `1.0.0`):
```batch
Ngit.exe deletetag -tag 1.0.0 -v true
```

These examples demonstrate how to use `Ngit` for common Git operations such as managing tags, branches, and repositories.
