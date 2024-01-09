Usage:
 ```batch
 Ng.exe [-git value] [-org value] [-url value] [-branch value] [-tag value] [-buildtype value] [-v value]
  - git       : git Command, value= [gettag | settag| autotag| autoversion| deletetag | getbranch | setbranch| createbranch]
         gettag          -> Get tag of a branch for a given project
         settag          -> Set specied tag of a branch for a given project
         autotag                 -> Set next tag based of branch and project on STAGING vs.PRODUCTION build (commit to remote repo)
         autoversion     -> Equivalent to `autotag` cmd (Does not commit to remote repo)
         deletetag       -> Delete specified tag of a branch for a given Project
         getbranch       -> Get the current branch for a given project
         setbranch       -> Set/checkout specified branch for a given project
         createbranch    -> Create specified branch for a given project
         clone           -> Clone a Project (string, default=)
  - org       : Organization Name (string, default=)
  - url       : GitHub Url Name (string, default=)
  - branch    : Branch Name (string, default=)
  - tag       : Tag Name (string, default=)
  - buildtype : Values: STAGING | PRODUCTION (string, default=)
  - v         : verbose. value = [true | false] (true or false, default=False)
 ```