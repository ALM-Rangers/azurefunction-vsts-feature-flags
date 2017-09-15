Param(
  [string]$myResourceGroup,
  [string]$Site,
  [string]$slot,
  [string]$RollUp_ExtensionCertificate,
  [string]$LDSdkKey,
  [string]$LDAPIKey,
  [string]$AppInsightKey
)

$webApp = Get-AzureRMWebAppSlot -ResourceGroupName $myResourceGroup -Name $Site -Slot $slot

$appSettingList = $webApp.SiteConfig.AppSettings

$hash = @{}
ForEach ($kvp in $appSettingList) {
	$hash[$kvp.Name] = $kvp.Value
}
$hash['RollUpBoard_ExtensionCertificate'] = $RollUp_ExtensionCertificate
Write-Host "Update RollUpBoard_ExtensionCertificate : "$RollUp_ExtensionCertificate

$hash['LaunchDarkly_SDK_Key'] = $LDSdkKey
Write-Host "Update LaunchDarkly_SDK_Key : "$LDSdkKey

$hash['LaunchDarkly_API_Key'] = $LDAPIKey
Write-Host "Update LaunchDarkly_API_Key : "$LDAPIKey

$hash['APPINSIGHTS_INSTRUMENTATIONKEY'] = $AppInsightKey
Write-Host "Update APPINSIGHTS_INSTRUMENTATIONKEY : "$AppInsightKey

Set-AzureRMWebAppSlot -ResourceGroupName $myResourceGroup -Name $Site -AppSettings $hash -Slot $slot