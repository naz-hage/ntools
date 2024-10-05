**ntools** have two predefined build types: `stage` and `prod`. The `stage` build type is deploy code to a stage environment used for debugging and testing, while the `prod` build type is used for prod deployment. The `stage` build type includes debugging symbols and is not optimized, while the `production` build type is optimized for performance and does not include debugging symbols.

##
### stage
The `stage` build type use the following command:

```powershell
nb stage
```
It includes the following steps:

- Clean the project
- Restore the project
- Build the project
- Test the project
- Publish the project to the stage environment
- Run various tests on the stage environment
- The version is set according to the rules in [versioning](versioning.md)

### Production
The `prod` build type use the following command:

```powershell
nb prod
```
It includes the following steps:

- Clean the project
- Restore the project
- Build the project
- Test the project
- Publish the project to the production environment
- Run smoke tests on the production environment
- This build is available for download from the GitHub release page
- The version is set according to the rules in [versioning](versioning.md)

Your project can have additional build types which you can add to your `nbuild.targets` fille, 
