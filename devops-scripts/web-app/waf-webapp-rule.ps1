# This Script Module contains functions to manage access restrictions for a web app in Azure App Service. 
# The Add-WafWebAppRule function adds an access restriction rule to the app service, 
# while the Remove-WafWebAppRule function removes an access restriction rule from the app service. 
# The functions use the `az webapp config access-restriction add` and `az webapp config access-restriction remove` commands 
# to interact with the Azure CLI and perform the necessary operations.
[CmdletBinding()]
param (
    [Parameter(Mandatory=$true)]
    [string]$command,

    [Parameter(Mandatory=$true)]
    [string]$resourceGroupName,

    [Parameter(Mandatory=$true)]
    [string]$appServiceName,

    [Parameter(Mandatory=$true)]
    [string]$slotName,

    [Parameter(Mandatory=$true)]
    [string]$ruleName,

     # Optional parameter but only required for the 'add' command
    [Parameter(Mandatory=$false, ParameterSetName='add')]
    [string]$agentIp
)

Import-Module -Name ".\waf-webapp-rule.psm1" -Force

switch ($command) {
    "add" {
        # Validate the agent IP
        if ([string]::IsNullOrEmpty($agentIp)) {
            Write-Error "Agent IP is required."
            return
        }
        # Add a WAF rule to the web app
        Add-WafWebAppRule -resourceGroupName $resourceGroupName -appServiceName $appServiceName -slotName $slotName -ruleName $ruleName -agentIp $agentIp

        # Show the WAF rule
        Show-WafWebAppRule -resourceGroupName $resourceGroupName -appServiceName $appServiceName -slotName $slotName -ruleName $ruleName
    }
    "remove" {
        # Remove the WAF rule
        Remove-WafWebAppRule -resourceGroupName $resourceGroupName -appServiceName $appServiceName -slotName $slotName -ruleName $ruleName

        # Show the WAF rule
        Show-WafWebAppRule -resourceGroupName $resourceGroupName -appServiceName $appServiceName -slotName $slotName -ruleName $ruleName
    }
    "show" {
        # Show the WAF rule
        Show-WafWebAppRule -resourceGroupName $resourceGroupName -appServiceName $appServiceName -slotName $slotName -ruleName $ruleName
    }
    default {
        Write-Error "Invalid command. Please use 'add' or 'show'."
    }
}
