@REM This batch file must be executed before the build process starts only once!
@rem so that RedColor and ColorMessage and other NbuildTasks are available in the build process.
@echo off
@rem Since nbuild.exe is in use, prebuild.bat is used to deploy ntools locally

if "%1"=="" (
    rem deploy the latest version of ntools
    echo getting the latest version of ntools
    ngit -c tag | findstr /R "Tag [0-9]*\.[0-9]*\.[0-9]*" > temp.txt
    rem the tag is in the 6th position
    for /f "tokens=6" %%A in (temp.txt) do set "tag=%%A"
    @REM echo latest_tag: %latest_tag%
    @REM set tag=%latest_tag%
    echo tag: %tag%
    del temp.txt
) else (
    set tag=%1
)

set DeploymentFolder=C:\Program Files\Nbuild
set NtoolsArtifactsFolder=C:\Artifacts\ntools\Release\%tag%.zip
"C:\Program Files\7-Zip\7z.exe" x %NtoolsArtifactsFolder% -o"C:\Program Files\Nbuild" -y
:end
