## Nbackup
`Nbackup` is a tool that leverages `robocopy` to backup a list of files and folders from a source to a destination. It is designed to be reliable and efficient, ensuring that your data is safe.

### Usage

```
 Nbackup.exe [-i value] [-e value] [-v value] [-performbackup value]
  - i             : input json file which specifies source, destination and backup options. (string, default=)
  - e             : Extract input json example file to current directory. (string, default=)
  - v             : Verbose level (true or false, default=False)
  - performbackup :  Set to false to verify json file without backup (true or false, default=True)
```
