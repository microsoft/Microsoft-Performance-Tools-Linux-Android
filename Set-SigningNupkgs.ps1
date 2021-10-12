# Copyright (c) Microsoft Corporation.

#
# This script gets all of the Nupkg files that need to be signed.
# The list of paths is stored in the 'signingnupkgs' build variable
# to be used by the signing task later in the pipeline.
#

[CmdletBinding()]
param (
    [Parameter(Mandatory = $True)]
    [String]
    $SearchDirectory
)

function Set-BuildVariable
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$True)]
        [String]
        $Name,

        [String]
        $Value
    )

    Write-Output "Setting '${Name}' build variable to '${Value}'"
    Write-Output "##vso[task.setvariable variable=$Name;]$Value"
}

$nupkgs = @(Get-ChildItem `
    -Path  ${SearchDirectory} `
    -File `
    -Filter *.nupkg `
    -Recurse | ForEach-Object {
        $_.FullName
    })

Push-Location $SearchDirectory
try {
    $nupkgs = @( $nupkgs | % { (Resolve-Path -Path $_ -Relative).TrimStart(".\") })
} finally {
    Pop-Location
}

Set-BuildVariable -Name "BuildOutput.NuGetPackages" -Value ([string]::Join(",", $nupkgs))
