**ntools** have two predefined build types: `staging` and `production`. The `staging` build type is deploy code to a staging environment used for debugging and testing, while the `production` build type is used for production deployment. The `staging` build type includes debugging symbols and is not optimized, while the `production` build type is optimized for performance and does not include debugging symbols.

##
### Staging
The `staging` build type use the following command:

```powershell
nb staging
```
It includes the following steps:
- Clean the project
- Restore the project
- Build the project
- Test the project
- Publish the project to the staging environment
- Run various tests on the staging environment

### Production
The `production` build type use the following command:

```powershell
nb production
```
It includes the following steps:
- Clean the project
- Restore the project
- Build the project
- Test the project
- Publish the project to the production environment
- Run smoke tests on the production environment
- This build is available for download from the GitHub release page

Your project can have additional build types which you can add to your `nbuild.targets` fille, 
