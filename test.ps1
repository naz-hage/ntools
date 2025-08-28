# Set the branch name you want to cherry-pick from
$branch = "149-add-support-for-downloadinstall-from-json-manifests-that-reference-private-github-release-assets"

# Find all .csproj files recursively
Get-ChildItem -Path . -Filter *.csproj -Recurse | ForEach-Object {
    $relativePath = $_.FullName.Replace((Get-Location).Path + "\", ".\")
    # Check if the file differs from the target branch
    $diff = git diff $branch -- $relativePath
    if ($diff) {
        Write-Host "Updating $relativePath from $branch"
        git checkout $branch -- $relativePath
    } else {
        Write-Host "No changes for $relativePath"
    }
}