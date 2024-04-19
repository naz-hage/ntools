- **c:\source\ntools\nbuild.targets Targets**

| **Target Name** | **Description** |
| --- | --- |
| ARTIFACTS           | Setup the ARTIFACTS folders for binaries and test results - override |
| CLEAN_ARTIFACTS     | Delete the ARTIFACTS folder after PACKAGE target is completed |
| TEST_GIT            | Temporary Target to test the Git Task |
| LOCAL               | Build local staging without incrementing the version |
| FILE_VERSIONS       | Test for FileVersion task and powershell file-version.ps1 |
| NBUILD_DOWNLOAD     | Download Nbuild specified in the NbuildTargetVersion |
| NBUILD_INSTALL      | Install Nbuild specified in the NbuildTargetVersion |
| DEV_SETUP           | Setup Development Environment |
| MKDOCS              | Build docs locally for testing |
| NBUILD_DOWNLOAD     | Download Nbuild specified in the NbuildTargetVersion |
