# Version and Tag

Throughout this document, the terms "version" and "tag" are used interchangeably. The version applies to binaries, product, or repository tagging. The rules for SDO versioning are as follows:

1. The version is a string in the format of `X.Y.Z`, where `X`, `Y`, and `Z` are integers.
2. The version is incremented as follows:
    - `X` is the major number:
      - Incremented for breaking changes.
    - `Y` is the minor number:
      - Incremented for new features or bug fixes.
      - Incremented when [production](buildtypes.md#prod) Build Type is deployed.
      - Incrementing `Y` resets `Z` to 0.
    - `Z` is the build number:
        - Incremented when [staging](buildtypes.md#stage) Build Type is deployed.

Tags in the [GitHubRelease](./github-release.md) are used to:

- Identify specific versions of the repository.
- Associate release assets with a particular version.
- Generate release notes based on commits since the last tag.

## Version Automation

Tool versions in documentation are automatically updated using the MSBuild task (`UpdateVersionsInDocs`) via the `sdo update_doc_versions` command. This extracts all tool/version pairs from the `NbuildAppList` entries in `dev-setup/sdo.yaml` and `dev-setup/apps.yaml`, then updates the documentation table accordingly.

### PowerShell Module Integration (v3.0.0+)

- **Module**: `scripts/module-package/sdo-scripts.psm1`
- **Function**: `Get-VersionFromYaml`
- **Purpose**: Consolidated version management within the SDO scripts module

```powershell
# Import the module
Import-Module "./scripts/module-package/sdo-scripts.psm1" -Force

# Get version from the SDO YAML manifest
$version = Get-VersionFromYaml -YamlPath "./dev-setup/sdo.yaml"
```

For complete automation details, see [Version Automation Guide](version-automation-guide.md).
