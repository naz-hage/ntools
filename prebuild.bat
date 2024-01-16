@REM This batch file must be executed before the build process starts only once!
@rem so that RedColor and ColorMessage and other NbuildTasks are available in the build process.
@echo off
@rem Since nbuild.exe is in use, prebuild.bat is used to deploy ntools locally
set DeploymentFolder=C:\Program Files\Nbuild
set ArtifactsFolder=C:\Artifacts\ntools\Release\%1.zip
"C:\Program Files\7-Zip\7z.exe" x C:\Artifacts\ntools\Release\%1.zip -o"C:\Program Files\Nbuild" -y
goto skip
set SRC=release\netstandard2.0
set DST=%programfiles%\nbuild\netstandard2.0
set FILES=NbuildTasks.dll launcher.dll
for %%F in (%FILES%) do (
    xcopy "%SRC%\%%F" "%DST%\" /d /y
)
:skip
set SRC=%DevDrive%\%MainDir%\ntools\nbuild\resources
set DST=%programfiles%\nbuild
set FILES=common.targets node.targets mongodb.targets ngit.targets apps-versions.targets git.targets dotnet.targets code.targets
for %%F in (%FILES%) do (
    xcopy "%SRC%\%%F" "%DST%\" /d /y
)
:end
