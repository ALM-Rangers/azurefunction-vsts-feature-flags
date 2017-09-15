<#
    Invoke Pester Test from VSTS Release Task
#>

Param([Parameter(Mandatory = $true,
        ValueFromPipelineByPropertyName = $true,
        Position = 0)]
    $TestScript)

#region Install Pester
Write-Host "Installing Pester from PSGallery"
Install-PackageProvider -Name NuGet -Force -Scope CurrentUser
Install-Module -Name Pester -Force -Scope CurrentUser
#endregion

#region call Pester script
Write-Host "Calling Pester test script"
#Invoke-Pester -Script $TestScript -PassThru
$result = Invoke-Pester -Script $TestScript -PassThru
if ($result.failedCount -ne 0) { 
    Write-Error "Pester returned errors"
}
#endregion