# Copyright (c) Microsoft Corporation.

#
# This script gets all of the DLL and EXE files that need to be signed.
# The list of paths is stored in the 'signingbinaries' build variable
# to be used by the signing task later in the pipeline.
#

[CmdletBinding()]
param (
    [Parameter(Mandatory = $True)]
    [String]
    $SigningRoot,

    [Parameter(Mandatory = $True)]
    [String[]]
    $Projects
)

$ErrorActionPreference = "Stop"

function Set-BuildVariable {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $True)]
        [String]
        $Name,

        [String]
        $Value
    )

    Write-Host "Setting '${Name}' build variable to '${Value}'"
    Write-Host "##vso[task.setvariable variable=$Name;]$Value"
}

function Get-UnsignedBinaries {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $True)]
        [String]
        $Path
    )

    return Get-ChildItem `
        -Path $Path `
        -File `
        -Recurse | ForEach-Object {
            $fileName = [System.IO.Path]::GetFileName($_.FullName)
            $ext = [System.IO.Path]::GetExtension($_.FullName)

            if (($ext -eq ".dll") -or ($ext -eq ".exe")) {

                if ((-not ($fileName -like 'perf*')) -and
                    (-not ($fileName -like 'System*'))) {

                    $signature = Get-AuthenticodeSignature $_.FullName
                    if (-not ($signature.Status -eq 'Valid')) {
                        $_.FullName
                    }
                }
            }
        }
}

Write-Host "Examining '${Projects}' for unsigned binaries"

$allToSign = $Projects | ForEach-Object {
    @(Get-UnsignedBinaries -Path $(Join-Path $(Join-Path ${SigningRoot} $_) "bin"))
} | Sort-Object | Get-Unique

Push-Location $SigningRoot
try {
    $allToSign = @($allToSign | % { (Resolve-Path -Relative -Path $_).TrimStart(".\") })
} finally {
    Pop-Location
}

Set-BuildVariable -Name BuildOutput.BinariesToSign -Value ([string]::Join(",", $allToSign))
