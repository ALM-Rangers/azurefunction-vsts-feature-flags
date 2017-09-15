#region variables
$ResourceGroupName = 'demo-d-01-rg'
$HostingPlanName = 'demo-d-01-hp'
$AppServiceName = 'demo-d-01-as'
#endregion

Describe -Name 'Resource Group Resource' -Tags 'ARM' -Fixture {
    It -name 'Passed Resource Group existence' -test {
        Find-AzureRmResource -ResourceGroupNameContains $ResourceGroupName | Should Not Be $null
    }

    It -name 'Passed Hosting Plan existence' -test {
        Find-AzureRmResource -ResourceNameEquals $HostingPlanName | Should Not Be $null
    }

    It -name 'Passed App Service existence' -test {
        Find-AzureRmResource -ResourceNameEquals $AppServiceName | Should Not Be $null
    }
}