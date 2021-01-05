# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

<#
    .Synopsis
      Launches WPA UI with the Microsoft-Performance-Tools-Linux plugins
#>

Param( 
    [parameter(Mandatory=$false)]
    [alias("i")]
    $InputFile,
    [parameter(Mandatory=$false)]
    [alias("p")]
    $WpaProfile,
    [parameter(Mandatory=$false)]
    [alias("l")]
    $LinuxPerfToolsPluginFolder
    )

Write-Host "Please see https://aka.ms/linuxperftools for help" 

if (-not $LinuxPerfToolsPluginFolder -or -not (Test-Path -Path $LinuxPerfToolsPluginFolder -ErrorAction Ignore | Out-Null))
{
    $scriptPath = Get-Item (Split-Path $MyInvocation.MyCommand.Path -Parent)
    $LinuxPerfToolsPluginFolder = $scriptPath.Parent.Parent.FullName
}
Write-Host "Using root folder as:" $LinuxPerfToolsPluginFolder 

if ($LinuxPerfToolsPluginFolder)
{
    $localLinuxPerfWpaAddins = Join-Path -Path $LinuxPerfToolsPluginFolder -ChildPath "MicrosoftPerfToolkitAddins\LTTngDataExtensions" # TODO - Remove LTTngDataExtensions path to include all plugins - Bug only 1 extension is supported at a time in current ADK DROP"
}

Write-Host "Using plugins folder as:" $localLinuxPerfWpaAddins

if ($LinuxPerfToolsPluginFolder -and -not (Test-Path -Path $localLinuxPerfWpaAddins))
{
    Write-Host "Please download the latest release from https://github.com/microsoft/Microsoft-Performance-Tools-Linux/releases"
    Start-Process "https://github.com/microsoft/Microsoft-Performance-Tools-Linux/releases"
    Pause
    Exit
}

$wpaProcess = "C:\Program Files (x86)\Windows Kits\10\Windows Performance Toolkit\wpa.exe"

if (-not (Test-Path -Path $wpaProcess))
{
    Write-Host "Please download the latest ADK Preview and install Windows Performance Toolkit"
    Start-Process "https://www.microsoft.com/en-us/software-download/windowsinsiderpreviewADK"
    Pause
    Exit
}

$startInfo = New-Object System.Diagnostics.ProcessStartInfo
$startInfo.FileName = $wpaProcess

if ($InputFile)
{
    $paramFile = Get-Item $InputFile -ErrorAction Continue
    if ($paramFile.Exists)
    {
        if ($WpaProfile)
        {
            $startInfo.Arguments = "-i `"$InputFile`" -profile `"$WpaProfile`" -addsearchdir `"$localLinuxPerfWpaAddins`""
        }
        else
        {
            $startInfo.Arguments = "-i `"$InputFile`" -addsearchdir `"$localLinuxPerfWpaAddins`""
        }
    }
}
else
{
    # TODO - Add back in for just launch - Current ADK DROP has bug to not allow launch w/o -i
    #$startInfo.Arguments = "-addsearchdir `"$localLinuxPerfWpaAddins`""
    Write-Error "Current ADK DROP has bug to not allow launch WPA UI just configured with plugins (use -i and -addsearchdir)"
    Pause
    Exit
}

Write-Host "Launching" $wpaProcess $startInfo.Arguments

$startInfo.RedirectStandardOutput = $true
$startInfo.UseShellExecute = $false
$startInfo.CreateNoWindow = $false

$process = New-Object System.Diagnostics.Process
$process.StartInfo = $startInfo
$process.Start()

sleep 1
if ($process.HasExited)
{
    Write-Host "Process StdOut:" 
    Write-Host $process.StandardOutput.ReadToEnd()
}

