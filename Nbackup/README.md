- Nbackup command line options:
```
 Nbackup.exe [-src value] [-dest value] [-opt value] [-input value] [-verbose value] [-performbackup value]

 
      - `i`       : input backup file which specifies source, destination and backup options. See [backup.json](./nBackup/Data/backup.json) for a sample input backup json file. (string, default=)
      - `v`       : Verbose Values: true | false.  Default is false (true or false, default=False)
      - `performbackup` : Values: true | false.  false displays options and does not perform backup (default=True)

            - if `input` option is specified, the `src`, `dest`, and `opt` options are ignored.
            - if `input` option is not specified, the `src`, `dest`, and `opt` options are required.

```