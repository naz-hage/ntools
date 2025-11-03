@echo off
set "PYTHONPATH=%~dp0;%PYTHONPATH%"
python.exe -m sdo_package.cli %*