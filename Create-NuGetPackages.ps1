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

    [Parameter(Mandatory = $True)]
    [String]
    $PublicRelease = "False",
    #$PublicRelease,

    [Parameter(Mandatory = $False)]
    [String]
    $Verbosity = "normal"
    #$Verbosity = "minimal"
)

$ErrorActionPreference = "Stop"

foreach ($project in $Projects) {
    & dotnet pack `
        --no-build `
        --no-restore `
        --configuration Release `
        --verbosity $Verbosity `
        --output $OutputDirectory `
        -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg `
        -p:PublicRelease=$PublicRelease `
        $project
}
