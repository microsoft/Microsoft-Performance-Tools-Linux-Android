:: Copyright (c) Microsoft Corporation.
:: Licensed under the MIT License.

@ECHO OFF
SETLOCAL ENABLEDELAYEDEXPANSION

IF "%~1" == "-?" (
    CALL :_SHOW_HELP
    EXIT /B 0
)

IF "%~1" == "-h" (
    CALL :_SHOW_HELP
    EXIT /B 0
)

IF "%~1" == "-help" (
    CALL :_SHOW_HELP
    EXIT /B 0
)

SET CONFIG=%~1

REM Grab the second argument to the last argument to pass along to the driver
SET PARAMETERS=%*
CALL SET PARAMETERS=%%PARAMETERS:*%1=%%

IF NOT EXIST bin\%CONFIG%\netcoreapp2.2 (
    MKDIR bin\%CONFIG%\netcoreapp2.2
)

PUSHD bin\%CONFIG%\netcoreapp2.2

@ECHO ON

dotnet run ^
    --project ..\..\.. ^
    -c %CONFIG% ^
    -- ^
     %PARAMETERS%

@ECHO OFF

POPD
EXIT /B 0

:_SHOW_HELP
    ECHO usage: run_driver ^<CONFIG^> ^<DRIVER PARAMETERS^>
    ECHO use 'run_driver ^<CONFIG^> -?' for driver help
    ECHO.
    GOTO :EOF
