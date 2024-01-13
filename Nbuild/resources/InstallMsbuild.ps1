# Define the URL and the output file
$url = "https://aka.ms/vs/17/release/vs_BuildTools.exe"
$output = "C:\NToolsDownloads\BuildTools_Full.exe"

# Download the file
Invoke-WebRequest -Uri $url -OutFile $output

# Install the build tools
Start-Process -FilePath $output -ArgumentList "--add","Microsoft.VisualStudio.Workload.MSBuildTools","--quiet" -Wait