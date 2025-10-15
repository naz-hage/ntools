## Tool version automation

Tool versions in this table are automatically updated from the JSON install definitions in the `dev-setup` folder using the `UpdateVersionsInDocs` MSBuild task (C#). To update the table, run:

```
nb update_doc_versions
```

This will extract all tool/version pairs from every `NbuildAppList` entry in every `*.json` file in `dev-setup` and update the documentation table accordingly. No PowerShell script is needed or maintained for this process.
The [Windows dev environment](https://learn.microsoft.com/en-us/windows/dev-environment/) has good information on how to setup a Windows dev environment.

- The table below list the latest dev tools used in Ntools.

| Tool                                                                                                       | Version     | Last Checked on |
| :--------------------------------------------------------------------------------------------------------- | :---------- | :-------------- |
| 2.14.11 | 06-Oct-25 | 07-Sep-25      |
| [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli-windows?pivots=msi)             | 2.70.0     | 17-Sep-25      |
| [Burp Suite](https://portswigger.net/burp/communitydownload)                                              | 2021.11.2   | 01-Oct-23       |
| 9.0.2 | 06-Oct-25 | 07-Sep-25      |
| [Dotnet8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet)                                         | 8.0.408     | 02-May-25       |
| [Dotnet9 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet)                                     | 9.0.203     | 02-May-25       |
| [Draw.io](https://app.diagrams.net/)                                                                      | N/A         | 01-Oct-23       |
| 2.51.0 | 06-Oct-25 | 17-Sep-25      |
| [Install Docker Desktop on Windows](https://docs.docker.com/docker-for-windows/install/)                  | 4.38.0.0   | 07-Sep-25      |
| [kubernetes](https://github.com/kubernetes/kubernetes/releases)                                           | 1.33.0     | 17-Sep-25      |
| 1.35.0 | 06-Oct-25 | 17-Sep-25      |
| [MongoDB Community Server](https://www.mongodb.com/try/download/community)                                | 8.0.5      | 17-Sep-25      |
| 22.12.0 | 06-Oct-25 | 17-Sep-25      |
| 1.32.0 | 06-Oct-25 | 17-Sep-25      |
| 2.81.0 | 06-Oct-25 | 07-Sep-25      |
| 6.12.1 | 06-Oct-25 | 17-Sep-25      |
| 10.14.0 | 06-Oct-25 | 17-Sep-25      |
| [Postman Get Started for Free](https://www.postman.com/downloads/)                                        | v11.36.0    | 10-Mar-25       |
| 7.5.3 | 06-Oct-25 | 17-Sep-25      |
| 3.13.7 | 06-Oct-25 | 17-Sep-25      |
| 2.90.0.0 | 06-Oct-25 | 07-Sep-25      |
| 1.11.1 | 06-Oct-25 | 17-Sep-25      |
| 0.55.1 | 06-Oct-25 | 17-Sep-25      |
| [Visual Studio 2022 Community Edition](https://visualstudio.microsoft.com/vs/community/)                  | 17.11.3    | 07-Sep-25      |
| 1.100.1 | 06-Oct-25 | 17-Sep-25      |
| [Windows Terminal](https://www.microsoft.com/en-us/p/windows-terminal/9n0dx20hk701)                       | 1.21.10351.0| 10-Mar-25       |
