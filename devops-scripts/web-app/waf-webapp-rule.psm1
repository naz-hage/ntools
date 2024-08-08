# Description: This script module contains functions to manage access restrictions for a web app in Azure App Service.
function Add-WafWebAppRule {
    param (
        [Parameter(Mandatory=$true)]
        [string]$resourceGroupName,

        [Parameter(Mandatory=$true)]
        [string]$appServiceName,

        [Parameter(Mandatory=$true)]
        [string]$slotName,

        [Parameter(Mandatory=$true)]
        [string]$ruleName,

        [Parameter(Mandatory=$true)]
        [string]$agentIp
    )

    # Add an access restriction rule to the app service
    try {
        $result = az webapp config access-restriction add `
        --resource-group $resourceGroupName `
        --name $appServiceName `
        --rule-name $ruleName `
        --action Allow `
        --ip-address $agentIp/32 `
        --priority 100 `
        --slot $slotName
        if ($result) {
            Write-Host "Access restriction rule '$ruleName' added successfully."
        } else {
            Write-Error "Failed to add access restriction rule '$ruleName'."
            exit 1
        }
    } catch {
        Write-Error "An error occurred while adding the access restriction rule: $_"
    }
}

function Remove-WafWebAppRule {
    param (
        [Parameter(Mandatory=$true)]
        [string]$resourceGroupName,

        [Parameter(Mandatory=$true)]
        [string]$appServiceName,

        [Parameter(Mandatory=$true)]
        [string]$slotName,

        [Parameter(Mandatory=$true)]
        [string]$ruleName
    )

    try {
        $result = az webapp config access-restriction remove `
        --resource-group $resourceGroupName `
        --name $appServiceName `
        --rule-name $ruleName `
        --slot $slotName
        if ($result) {
            Write-Host "Access restriction rule '$ruleName' removed successfully."
        } else {
            Write-Error "Failed to remove access restriction rule '$ruleName'."
            exit 1
        }
    } catch {
        Write-Error "An error occurred while removing the access restriction rule: $_"
        exit 1
    }
}

# List the access restrictions
function Show-WafWebAppRule {
    param (
        [Parameter(Mandatory=$true)]
        [string]$resourceGroupName,

        [Parameter(Mandatory=$true)]
        [string]$appServiceName,

        [Parameter(Mandatory=$true)]
        [string]$slotName,

        [Parameter(Mandatory=$true)]
        [string]$ruleName
    )

    try {
        # List the access restrictions
        $output = az webapp config access-restriction show --resource-group $resourceGroupName --name $appServiceName --slot $slotName
    
        # Convert the JSON output to a PowerShell object
        $accessRestrictions = $output | ConvertFrom-Json
    
        # Filter the access restrictions by rule name
        $filteredRestrictions = $accessRestrictions.ipSecurityRestrictions | Where-Object { $_.name -eq $ruleName }
    
        # Display the filtered access restrictions
        $filteredRestrictions | ConvertTo-Json -Depth 10
    
    }
    catch {
        Write-Error "An error occurred while showing the access restriction rule: $_"
        exit 1
    }
}

Export-ModuleMember -Function Add-WafWebAppRule, Remove-WafWebAppRule, Show-WafWebAppRule