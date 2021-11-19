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

$MinStoreWPAVersion = New-Object -TypeName System.Version -ArgumentList "10.0.22500.0"
$WPAPreviewStoreLink = "https://www.microsoft.com/en-us/p/windows-performance-analyzer-preview/9n58qrw40dfw"

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

$wpaPreviewStorePkg = Get-AppPackage -Name Microsoft.WindowsPerformanceAnalyzerPreview
if (-not $wpaPreviewStorePkg -or $wpaPreviewStorePkg.Status -ne "Ok")
{
    Write-Error -Category NotInstalled -Message "REQUIRED PREREQUISITE Store Windows Performance Analyzer (Preview) is not installed. Please install it from the Store. Launching $WPAPreviewStoreLink"
    Start-Process "$WPAPreviewStoreLink"
    Pause
    Exit
}

$v = New-Object -TypeName System.Version -ArgumentList $wpaPreviewStorePkg.Version
# Is MinStoreWPAVersion same, later, or earlier than current WPA version?
$WpaVersionComparison = $MinStoreWPAVersion.CompareTo($v);
switch ($WpaVersionComparison )
{
    # MinStoreWPAVersion the same as current WPA
    0 { break }
    # MinStoreWPAVersion later than current WPA
    1 
    {
        Write-Error -Category NotInstalled  -Message "Current WPA version is $v. Need minimum of WPA $MinStoreWPAVersion. Redirecting to Store WPA so that you can update...";
        Start-Process "$WPAPreviewStoreLink"
        Pause
        Exit
    }
    # MinStoreWPAVersion earlier than current WPA. That's ok
    -1 { break }
}

$wpaProcess = "$env:LOCALAPPDATA\Microsoft\WindowsApps\wpa.exe"

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

