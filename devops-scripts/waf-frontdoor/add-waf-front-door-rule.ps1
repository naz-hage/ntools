[CmdletBinding()]
param (
    [Parameter(Mandatory=$true)]
    [string]$resourceGroupName,

    [Parameter(Mandatory=$true)]
    [string]$wafPolicyName,

    [Parameter(Mandatory=$true)]
    [string]$customRuleName,

    [Parameter(Mandatory=$true)]
    [string]$agentIp
)

Write-Host "Received parameters:"
Write-Host "ResourceGroupName: $resourceGroupName"
Write-Host "WafPolicyName: $wafPolicyName"
Write-Host "CustomRuleName: $customRuleName"
Write-Host "AgentIp: $agentIp"

try {
    # Create the custom rule
    $result = az network front-door waf-policy rule create `
        --action Allow `
        --name $customRuleName `
        --policy-name $wafPolicyName `
        --resource-group $resourceGroupName `
        --priority 20 `
        --rule-type MatchRule `
        --defer
    if ($result) {
        Write-Host "Custom rule '$customRuleName' added successfully."
    } else {
        Write-Error "Failed to add custom rule '$customRuleName'."
        exit 1
    }
    # Add match condition to the custom rule
    $result = az network front-door waf-policy rule match-condition add `
        --match-variable SocketAddr `
        --operator IPMatch `
        --values "$agentIp" `
        --negate false `
        --name $customRuleName `
        --resource-group $resourceGroupName `
        --policy-name $wafPolicyName
    if ($result) {
        Write-Host "Match condition added to custom rule '$customRuleName' successfully."
    } else {
        Write-Error "Failed to add match condition to custom rule '$customRuleName'."
        exit 1
    }
} catch {
    Write-Error "An error occurred: $_"
    exit 1
}
