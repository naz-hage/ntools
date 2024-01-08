@REM This batch file must be executed before the build process starts only once!
@rem so that RedColor and ColorMessage and other NbuildTasks are available in the build process.
@echo off
set SRC=release\netstandard2.0
set DST=%programfiles%\nbuild\netstandard2.0
set FILES=NbuildTasks.dll launcher.dll
for %%F in (%FILES%) do (
    xcopy "%SRC%\%%F" "%DST%\" /d /y
)

set SRC=nbuild\resources
set DST=%programfiles%\nbuild
set FILES=common.targets nuget.targets nbuild.targets 
for %%F in (%FILES%) do (
    xcopy "%SRC%\%%F" "%DST%\" /d /y
)

#rem Since nbuild.exe is in use, prebuild.bat is used to deploy ntools locally
"C:\Program Files\BuildTools\7-Zip\7z.exe" x C:\Artifacts\ntools\Release\%1.zip -o"C:\Program Files\Nbuild" -y