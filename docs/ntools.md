## Tool version automation

Tool versions in this table are automatically updated from the single `apps.json` file (located at `go/apps.json`) using the `UpdateVersionsInDocs` MSBuild task (C#). To update the table, run:

```
nb update_doc_versions
```

This will extract all tool/version pairs from the `NbuildAppList` entries in `go/apps.json` and update the documentation table accordingly. The `go/apps.json` file serves as the **single source of truth** for all developer tools managed by ntools. No PowerShell script is needed or maintained for this process.
The [Windows dev environment](https://learn.microsoft.com/en-us/windows/dev-environment/) has good information on how to setup a Windows dev environment.

- The table below list the latest dev tools used in Ntools.

| Tool                                                                                                       | Version     | Last Checked on |
| :--------------------------------------------------------------------------------------------------------- | :---------- | :-------------- |
| [Argo CD](https://github.com/argoproj/argo-cd/releases/) | 3.2.0      | 24-May-26      |
| [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli-windows?pivots=msi)             | 2.78.0     | 24-May-26      |
| [Burp Suite](https://portswigger.net/burp/communitydownload)                                              | 2021.11.2   | 01-Oct-23       |
| [Dotnet Runtime](https://dotnet.microsoft.com/en-us/download/dotnet) | 9.0.2      | 24-May-26      |
| [Dotnet8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet)                                         | 8.0.408     | 02-May-25       |
| [Dotnet9 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet)                                     | 9.0.203     | 02-May-25       |
| [Draw.io](https://app.diagrams.net/)                                                                      | N/A         | 01-Oct-23       |
| [Git for Windows](https://git-scm.com/downloads) | 2.51.1     | 24-May-26      |
| [Install Docker Desktop on Windows](https://docs.docker.com/docker-for-windows/install/)                  | 4.38.0.0   | 07-Sep-25      |
| [kubernetes](https://github.com/kubernetes/kubernetes/releases)                                           | 1.34.1     | 24-May-26      |
| [minikube](https://github.com/kubernetes/minikube/releases/) | 1.37.0     | 24-May-26      |
| [MongoDB Community Server](https://www.mongodb.com/try/download/community)                                | 8.2.1      | 24-May-26      |
| [Node.js](https://nodejs.org/en/download/) | 22.21.0    | 24-May-26      |
| [Ntools](https://github.com/naz-hage/ntools/releases)                                                     | 1.47.0     | 24-May-26      |
| [GitHub CLI](https://github.com/cli/cli/releases) | 2.82.1     | 24-May-26      |
| [NuGet](https://www.nuget.org/downloads) | 6.12.1     | 24-May-26      |
| [pnpm](https://pnpm.io/) | 10.19.0    | 24-May-26      |
| [Postman Get Started for Free](https://www.postman.com/downloads/)                                        | v11.36.0    | 10-Mar-25       |
| [PowerShell](https://github.com/PowerShell/PowerShell/releases) | 7.5.4      | 24-May-26      |
| [Python](https://www.python.org/downloads/) | 3.14.0     | 24-May-26      |
| [SysInternals](https://learn.microsoft.com/en-us/sysinternals/) | 2.90.0.0   | 24-May-26      |
| [Terraform](https://releases.hashicorp.com/terraform) | 1.13.4     | 24-May-26      |
| [Terraform Lint](https://github.com/terraform-linters/tflint/releases) | 0.55.1     | 24-May-26      |
| [Visual Studio 2022 Community Edition](https://visualstudio.microsoft.com/vs/community/)                  | 17.11.3    | 07-Sep-25      |
| [Visual Studio Code](https://code.visualstudio.com/download) | 1.105.1    | 24-May-26      |
| [Windows Terminal](https://www.microsoft.com/en-us/p/windows-terminal/9n0dx20hk701)                       | 1.21.10351.0| 10-Mar-25       |
