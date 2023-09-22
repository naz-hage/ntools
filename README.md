# dev-ops
Collection of useful tools which automates various tasks on Windows client.

- **nBackup** - wrapper around robocopy which read a json file that contains a list of folders
      with source and destination.
nBackup command line options:
 ```
 nBackup.exe [-src value] [-dest value] [-opt value] [-input value] [-verbose value] [-performbackup value]
  ```
  - `src`           : Source Folder (string, default=)
  - `dest`          : Destination folder (string, default=)
  - `opt`           : Backup Options (string, default=/s /XD .git /XD .vs /XD TestResults /XF *.exe /XF *.dll /XF *.pdb /e)
  - `input`         : input backup file which specifies source, destination and backup options. See [backup.json](./nBackup/Data/backup.json) for a sample input backup json file. (string, default=)
  - `verbose`       : Values: true | false.  Default is false (true or false, default=False)
  - `performbackup` : Values: true | false.  false displays options and does not perform backup (default=True)


if `input` option is speciefied, the `src`, `dest`, and `opt` options are ignored.

if `input` option is not specified, the `src`, `dest`, and `opt` options are required.


    
