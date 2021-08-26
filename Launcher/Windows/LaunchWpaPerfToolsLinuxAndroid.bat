:: Copyright (c) Microsoft Corporation.
:: Licensed under the MIT License.

@ECHO OFF

IF "%~1" == "" (
  %windir%\System32\WindowsPowerShell\v1.0\powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0\LaunchWpaPerfToolsLinuxAndroid.ps1"
) ELSE (
 IF "%~2" == "" (
    %windir%\System32\WindowsPowerShell\v1.0\powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0\LaunchWpaPerfToolsLinuxAndroid.ps1" -InputFile "%~1"
 ) ELSE (
    %windir%\System32\WindowsPowerShell\v1.0\powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0\LaunchWpaPerfToolsLinuxAndroid.ps1" -InputFile "%~1" -WpaProfile "%~2"
 )
)