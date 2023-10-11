# ntools
Collection of useful tools which automates various tasks on Windows client.

- **nBackup** - A .NET executable which relies on robocopy to backup a list of folders from source and destination.
nBackup command line options:
 
```
 nBackup.exe [-src value] [-dest value] [-opt value] [-input value] [-verbose value] [-performbackup value]

 
      - `src`           : Source Folder (string, default=)
      - `dest`          : Destination folder (string, default=)
      - `opt`           : Backup Options (string, default=/s /XD .git /XD .vs /XD TestResults /XF *.exe /XF *.dll /XF *.pdb /e)
      - `input`         : input backup file which specifies source, destination and backup options. See [backup.json](./nBackup/Data/backup.json) for a sample input backup json file. (string, default=)
      - `verbose`       : Values: true | false.  Default is false (true or false, default=False)
      - `performbackup` : Values: true | false.  false displays options and does not perform backup (default=True)

            - if `input` option is specified, the `src`, `dest`, and `opt` options are ignored.
            - if `input` option is not specified, the `src`, `dest`, and `opt` options are required.

```

- **launcher** - A .NET class library which exposes the launcher class and methods to launch a process and wait for it to complete. The launcher class is used by nBackup to launch robocopy and wait for it to complete.

- example usage:

using Launcher;

```c#
var result = Launcher.Launcher.Start(new()
                  {
                  WorkingDir = Directory.GetCurrentDirectory(),
                  Arguments = "/?",
                  FileName = "robocopy",
                  RedirectStandardOutput = true
                  }
            );
console.writeline(result.Output);
foreach (var line in result.Output)
{
      console.writeline(line);
}
```

