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
| [Argo CD](https://github.com/argoproj/argo-cd/releases/)                                                  | 2.14.11      | 04-May-25       |
| [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli-windows?pivots=msi)             | 2.70.0     | 06-Jul-25      |
| [Burp Suite](https://portswigger.net/burp/communitydownload)                                              | 2021.11.2   | 01-Oct-23       |
| [Dotnet Runtime](https://dotnet.microsoft.com/en-us/download/dotnet)                                      | 9.0.2       | 10-Mar-25       |
| [Dotnet8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet)                                         | 8.0.408     | 02-May-25       |
| [Dotnet9 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet)                                     | 9.0.203     | 02-May-25       |
| [Draw.io](https://app.diagrams.net/)                                                                      | N/A         | 01-Oct-23       |
| [Git for Windows](https://git-scm.com/downloads)                                                          | 2.50.0     | 06-Jul-25      |
| [Install Docker Desktop on Windows](https://docs.docker.com/docker-for-windows/install/)                  | 4.38.0.0    | 03-Mar-25       |
| [kubernetes](https://github.com/kubernetes/kubernetes/releases)                                           | 1.33.0     | 06-Jul-25      |
| [minikube](https://github.com/kubernetes/minikube/releases/)                                              | 1.35.0     | 06-Jul-25      |
| [MongoDB Community Server](https://www.mongodb.com/try/download/community)                                | 8.0.5      | 06-Jul-25      |
| [Node.js](https://nodejs.org/en/download/)                                                                | 22.12.0    | 06-Jul-25      |
| [Ntools](https://github.com/naz-hage/ntools/releases)                                                     | 1.22.0     | 06-Jul-25      |
| [GitHub CLI](https://github.com/cli/cli/releases)                                                         | 2.74.2     | 06-Jul-25      |
| [NuGet](https://www.nuget.org/downloads)                                                                  | 6.12.1     | 06-Jul-25      |
| [pnpm](https://pnpm.io/)                                                                                  | 9.1.2      | 06-Jul-25      |
| [Postman Get Started for Free](https://www.postman.com/downloads/)                                        | v11.36.0    | 10-Mar-25       |
| [PowerShell](https://github.com/PowerShell/PowerShell/releases)                                           | 7.5.2      | 06-Jul-25      |
| [Python](https://www.python.org/downloads/)                                                               | 3.13.3     | 06-Jul-25      |
| [SysInternals](https://learn.microsoft.com/en-us/sysinternals/)                                           | 2.90.0      | 22-Jun-24       |
| [Terraform](https://releases.hashicorp.com/terraform)                                                     | 1.11.1     | 06-Jul-25      |
| [Terraform Lint](https://github.com/terraform-linters/tflint/releases)                                    | 0.55.1     | 06-Jul-25      |
| [Visual Studio 2022 Community Edition](https://visualstudio.microsoft.com/vs/community/)                  | 17.11.3     | 14-Sep-24       |
| [Visual Studio Code](https://code.visualstudio.com/download)                                              | 1.100.1    | 06-Jul-25      |
| [Windows Terminal](https://www.microsoft.com/en-us/p/windows-terminal/9n0dx20hk701)                       | 1.21.10351.0| 10-Mar-25       |
