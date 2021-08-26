# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

<#
    .Synopsis
      Launches WPA UI with the Microsoft-Performance-Tools-Linux-Android plugins
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
    $localLinuxPerfWpaAddins = Join-Path -Path $LinuxPerfToolsPluginFolder -ChildPath "MicrosoftPerfToolkitAddins"
}

Write-Host "Using plugins folder as:" $localLinuxPerfWpaAddins

if ($LinuxPerfToolsPluginFolder -and -not (Test-Path -Path $localLinuxPerfWpaAddins))
{
    Write-Host "Please download the latest release from https://github.com/microsoft/Microsoft-Performance-Tools-Linux-Android/releases"
    Start-Process "https://github.com/microsoft/Microsoft-Performance-Tools-Linux-Android/releases"
    Pause
    Exit
}

$wpaProcess = "wpa.exe"

if (-not (Test-Path -Path $wpaProcess))
{
    Write-Host "Please download the latest Store Windows Performance Analyzer (Preview)"
    Start-Process "https://www.microsoft.com/en-us/p/windows-performance-analyzer-preview/9n58qrw40dfw"
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
    $startInfo.Arguments = "-addsearchdir `"$localLinuxPerfWpaAddins`""
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

