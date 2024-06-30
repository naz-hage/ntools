`Ngit` is simple wrapper for `Git` tool that perform simple commands such as get tag and set tag.

### Usage
```batch
Ngit.exe [-c value] [-url value] [-tag value] [-buildtype value] [-v value]
- c         : git Command, value= [tag | settag| autotag| setautotag| deletetag | branch | clone]
    tag         -> Get the current tag
    autotag     -> Set next tag based on the build type: STAGING vs.PRODUCTION
    pushtag     -> push specified tag in -tag option to remote repo
    settag      -> Set specified tag in -tag option
    deletetag   -> Delete specified tag in -tag option
    branch      -> Get the current branch
    clone       -> Clone specified Git repo in the -url option (string, default=)
- url       : Git repo path (string, default=)
- tag       : Tag used for -c settag and -c deletetag (string, default=)
- buildtype : Build type used for -c autotag and -c setautotag Values: STAGING | PRODUCTION (string, default=)
- v         : verbose. value = [true | false] (true or false, default=False)
```
