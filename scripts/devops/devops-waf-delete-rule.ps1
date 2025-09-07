[CmdletBinding()]
param (
  [Parameter(Mandatory=$true)]
  [string]$resourceGroupName,

  [Parameter(Mandatory=$true)]
  [string]$wafPolicyName,

  [Parameter(Mandatory=$true)]
  [string]$customRuleName
)

Write-Host "ResourceGroupName: $env:resourceGroupName"
Write-Host "WafPolicyName: $env:wafPolicyName"
Write-Host "CustomRuleName: $env:customRuleName"


try {
  # check for required parameters
  if (-not $resourceGroupName) {
    throw "Resource group name is required."
  }
  Write-Host "Resource group name: $resourceGroupName"

  if (-not $wafPolicyName) {
    throw "WAF policy name is required."
  }
  Write-Host "WAF policy name: $wafPolicyName"

  if (-not $customRuleName) {
    throw "Custom rule name is required."
  }
  Write-Host "Custom rule name: $customRuleName"

  # remove a match-condition from the rule
  $result = az network front-door waf-policy rule delete `
    --name $customRuleName `
    --policy-name $wafPolicyName `
    --resource-group $resourceGroupName

  if ($result) {
    Write-Host "Custom rule '$customRuleName' deleted successfully."
  }
  else {
    throw "Failed to delete custom rule '$customRuleName'."
  }
}
catch {
  Write-Error "An error occurred while deleting the custom rule: $_"
  exit 1
}
