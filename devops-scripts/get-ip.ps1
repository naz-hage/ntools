# Description: This script retrieves the public IP address of the agent machine and sets it as a variable in the pipeline.
# Usage: .\get-ip.ps1
# in the pipeline, you can access the IP address using $(agentIp)
# here is an example how to use it in the pipeline:
# - task: PowerShell@2
#   inputs:
#     targetType: 'inline'
#     script: |
#       Write-Host "Agent IP: $(agentIp)"
#       # do something with the agent IP
#   displayName: 'Use Agent IP'
#

# Reference: https://docs.microsoft.com/en-us/azure/devops/pipelines/scripts/logging-commands?view=azure-devops&tabs=powershell

$maxAttempts = 5
$count = 0
$agentIp = $null

while ($count -lt $maxAttempts -and $null -eq $agentIp) {
  $count++
  try {
    $agentIp = (Invoke-RestMethod http://ipinfo.io/json).ip
  } catch {
    Write-Host "Failed to retrieve IP address. Retrying $count ..."
    Start-Sleep -Seconds 5
  }
}

if ($null -eq $agentIp) {
  throw "IP Not Found"
}

Write-Output "IP Address: $agentIp"
Write-Host "##vso[task.setvariable variable=agentIp]$agentIp"
