REM Copyright (c) Microsoft Corporation.
REM Licensed under the MIT License.
@ECHO OFF

REM Launches WPA with Perfetto plugin
REM Requires WPA be part of PATH. Requires latest version of Microsoft WPA (version 10.7.3.2)
REM Requires that WPA is launched from same location as trace_processor_shell.exe

REM Change directory to location of this batch file
cd /d "%~dp0"

REM Launch WPA, specifying it to load this Perfetto plugin directory
wpa -addsearchdir %cd%