# dev-ops
Colletion of useful tools which helps productivity and maintenance.
nBackup - wrapper around robocopy which read a json file that contains a list of folders
      with source and destination.
nBackup command line options:

 nBackup.exe [-src value] [-dest value] [-opt value] [-input value] [-verbose value] [-performbackup value]
  - src           : Source Folder (string, default=)
  - dest          : Destination folder (string, default=)
  - opt           : Backup Options (string, default=/s /XD .git /XD .vs /XD TestResults /XF *.exe /XF *.dll /XF *.pdb /e)
  - input         : input backup file which specifies source, destination and backup options (string, default=)
  - verbose       : Values: true | false.  Default is false (true or false, default=False)
  - performbackup : Values: true | false. default is true (true or false, default=True)
