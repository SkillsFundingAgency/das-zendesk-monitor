[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SourcePath
)

try {
    $TestResults = Get-ChildItem -Path $SourcePath -Filter *.trx

    if ($TestResults) {
        Write-Host "Tests executed successfully."
    }
    else {
        throw "No .trx file found. Indicates failed test execution."
    }
}
catch {
    throw "$_"
}


