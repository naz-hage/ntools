# Function to check if the repository exists in the target project
function CheckRepositoryExists {
    param (
        [string]$organization,
        [string]$project,
        [string]$repositoryName,
        [string]$personalAccessToken
    )

    # Base64 encode the PAT for authentication
    $base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$personalAccessToken"))

    try {
        # Get the list of repositories in the target project
        $reposUrl = "$organization/$project/_apis/git/repositories?api-version=7.0"
        $reposResponse = Invoke-RestMethod -Uri $reposUrl -Headers @{ Authorization = "Basic $base64AuthInfo" } -ErrorAction Stop

        # Check if the repository exists
        $repository = $reposResponse.value | Where-Object { $_.name -eq $repositoryName }
        if ($repository) {
            Write-Host "Repository '$repositoryName' already exists in project '$project'." -ForegroundColor Yellow
            return $true
        } else {
            Write-Host "Repository '$repositoryName' does not exist in project '$project'." -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Error "Failed to check if the repository exists: $($_.Exception.Message)"
        return $false
    }
}

# Function to create a repository in Azure DevOps
function CreateRepository {
    param (
        [string]$organization,
        [string]$project,
        [string]$repositoryName,
        [string]$personalAccessToken
    )

    # Base64 encode the PAT for authentication
    $base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$personalAccessToken"))

    try {
        # Construct the API URL
        $createRepoUrl = "$organization/$project/_apis/git/repositories?api-version=7.0"

        # Define the request body
        $body = @{
            name = $repositoryName
        } | ConvertTo-Json -Depth 10

        # Make the API call to create the repository
        $response = Invoke-RestMethod -Uri $createRepoUrl -Method Post -Headers @{ Authorization = "Basic $base64AuthInfo" } -Body $body -ContentType "application/json" -ErrorAction Stop

        # Add a delay after repository creation
        Write-Host "Waiting for the repository to be fully available..."
        Start-Sleep -Seconds 10

        # check if the repository was created successfully
        $checkResponse = CheckRepositoryExists -organization $organization -project $project -repositoryName $repositoryName -personalAccessToken $personalAccessToken
        if (-not $checkResponse) {
            throw "Repository '$repositoryName' was not created successfully."
        }

        Write-Host "Repository '$repositoryName' successfully created in project '$project'." -ForegroundColor Green
        return $response
    } catch {
        Write-Error "Failed to create the repository: $($_.Exception.Message)"
        Write-Host "DEBUG: Full Error Details: $($_ | ConvertTo-Json -Depth 10)" -ForegroundColor Red
        return $null
    }
}

# Function to delete a repository in Azure DevOps
function DeleteRepository {
    param (
        [string]$organization,
        [string]$project,
        [string]$repositoryName,
        [string]$personalAccessToken
    )

    # Base64 encode the PAT for authentication
    $base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(":$personalAccessToken"))

    try {
        # Get the list of repositories to find the repository ID
        $reposUrl = "$organization/$project/_apis/git/repositories?api-version=7.0"
        Write-Host "DEBUG: Repos URL: $reposUrl" -ForegroundColor Cyan
        $reposResponse = Invoke-RestMethod -Uri $reposUrl -Headers @{ Authorization = "Basic $base64AuthInfo" } -ErrorAction Stop

        # Find the repository by name
        $repository = $reposResponse.value | Where-Object { $_.name -eq $repositoryName }
        if (-not $repository) {
            Write-Error "Repository '$repositoryName' does not exist in project '$project'."
            return $false
        }

        $repositoryId = $repository.id

        # Output the repository ID for confirmation
        Write-Host "Repository ID for '$repositoryName': $repositoryId" -ForegroundColor Cyan

        # Construct the API URL for deletion
        # https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repositoryId}?api-version=7.0
        
        # Correctly construct the API URL for deletion You can either remove the braces entirely 
        # or use subexpression $() if you need complex expressions.

        $deleteRepoUrl = "$organization/$project/_apis/git/repositories/$($repositoryId)?api-version=7.0"
        Write-Host "DEBUG: Delete Repo URL: $deleteRepoUrl" -ForegroundColor Cyan

        # Ask for confirmation before proceeding
        $confirmation = Read-Host "Are you sure you want to delete the repository '$repositoryName' in project '$project'? Type 'yes' to confirm"
        if ($confirmation -ne "yes") {
            Write-Host "Deletion canceled by the user." -ForegroundColor Yellow
            return $false
        }

        # Make the API call to delete the repository
        $response = Invoke-RestMethod -Uri $deleteRepoUrl -Method Delete -Headers @{
            Authorization = "Basic $base64AuthInfo"
            Accept = "application/json; api-version=7.0"
        } -ErrorAction Stop

        Write-Host "Repository '$repositoryName' successfully deleted from project '$project'." -ForegroundColor Green
        return $true
    } catch {
        Write-Error "Failed to delete the repository: $($_.Exception.Message)"
        Write-Host "DEBUG: Full Error Details: $($_ | ConvertTo-Json -Depth 20)" -ForegroundColor Red
        return $false
    }
}

Export-ModuleMember -Function *