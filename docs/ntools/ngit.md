`Ngit` is simple wrapper for `Git` tool that perform simple commands such as get tag and set tag.

### Usage
```batch
 Ngit.exe command [-url value] [-tag value] [-buildtype value] [-v value]
  - command   : Specifies the git command to execute.
         tag             -> Get the current tag
         settag          -> Set specified tag in -tag option
         autotag         -> Set next tag based on the build type: STAGE vs. PROD
         setautotag      -> Set next tag based on the build type: STAGE vs. PROD
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
