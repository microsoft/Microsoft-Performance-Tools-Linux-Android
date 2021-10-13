# Copyright (c) Microsoft Corporation.

#
# This script creates our NuGet packages with the appropriate symbols.
#

[CmdletBinding()]
param (
    [Parameter(Mandatory = $True)]
    [String[]]
    $Projects,

    [Parameter(Mandatory = $True)]
    [String]
    $OutputDirectory,


    [Parameter(Mandatory = $False)]
    [String]
    $Verbosity = "normal"
)

$ErrorActionPreference = "Stop"

foreach ($project in $Projects) {
    & dotnet pack `
        --no-build `
        --no-restore `
        --configuration Release `
        --verbosity $Verbosity `
        --output $OutputDirectory `
        $project
}
