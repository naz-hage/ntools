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
set FILES=common.targets
for %%F in (%FILES%) do (
    xcopy "%SRC%\%%F" "%DST%\" /d /y
)
