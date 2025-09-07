param (
    [Parameter(Mandatory=$true)]
    [string]$dnsName,

    [Parameter(Mandatory=$true)]
    [string]$location,

    [Parameter(Mandatory=$true)]
    [string]$certSubject
)

# Ensure the Microsoft.PowerShell.Security module is loaded
try {
    Import-Module Microsoft.PowerShell.Security -ErrorAction Stop
} catch {
    Write-Error "Failed to import Microsoft.PowerShell.Security module: $_"
    exit
}

# Check if the certificate store exists
try {
    if (-not (Get-PSDrive -PSProvider Certificate)) {
        Write-Error "Certificate store not found. Ensure the Certificate PSProvider is available."
        exit
    }
} catch {
    Write-Error "Failed to access the Certificate PSProvider: $_"
    exit
}

# $location = "\\192.168.1.138\WDSync\Users\Naz\.pfx"
# $dnsName = "ntools-naz-hage"

# Step 1: Create a self-signed certificate
try {
    $cert = New-SelfSignedCertificate -DnsName $dnsName -Type CodeSigningCert -CertStoreLocation "Cert:\CurrentUser\My"
} catch {
    Write-Error "Failed to create self-signed certificate: $_"
    exit
}

# Step 2: Export the certificate to a .pfx file
try {
    $certPassword = ConvertTo-SecureString -String $env:VTAPIKEY -Force -AsPlainText
    Export-PfxCertificate -Cert "Cert:\CurrentUser\My\$($cert.Thumbprint)" -FilePath "$location\certificate.pfx" -Password $certPassword
} catch {
    Write-Error "Failed to export certificate: $_"
    exit
}

# Step 3: Import the certificate
try {
    $cert = Import-PfxCertificate -FilePath "$location\certificate.pfx" -CertStoreLocation "Cert:\CurrentUser\My" -Password $certPassword
} catch {
    Write-Error "Failed to import certificate: $_"
    exit
}

# Step 4: Sign the PowerShell script
try {
    Set-AuthenticodeSignature -FilePath "C:\source\ntools\dev-setup\ff.ps1" -Certificate $cert
} catch {
    Write-Error "Failed to sign the script: $_"
    exit
}

# Step 5: Verify the signature
try {
    $signature = Get-AuthenticodeSignature -FilePath "C:\source\ntools\dev-setup\ff.ps1"
    Write-Output $signature
} catch {
    Write-Error "Failed to verify the script signature: $_"
    exit
}
# $location = "\\192.168.1.138\WDSync\Users\Naz\.pfx"
# # Step 1: Create a self-signed certificate
# # - The self-signed certificate, along with its private key, is created and stored in the `CurrentUser` certificate store under the `My` (Personal) store.
# $cert = New-SelfSignedCertificate -DnsName "ntools-naz-hage" -Type CodeSigningCert -CertStoreLocation "Cert:\CurrentUser\My"

# # Step 2: Export the certificate to a .pfx file
# # - The certificate, along with its private key, is exported to a `.pfx` file located at `$location\certificate.pfx`. The private key is protected by the password you provide.
# $certPassword = ConvertTo-SecureString -String $env:VTAPIKEY -Force -AsPlainText
# Export-PfxCertificate -Cert "Cert:\CurrentUser\My\$($cert.Thumbprint)" -FilePath "$location\certificate.pfx" -Password $certPassword

# # Step 3: Import the certificate
# # - The `.pfx` file, which includes the private key, is imported back into the `CurrentUser` certificate store under the `My` (Personal) store.
# $cert = Import-PfxCertificate -FilePath "$location\certificate.pfx" -CertStoreLocation "Cert:\CurrentUser\My" -Password $certPassword

# # Step 4: Sign the PowerShell script
# # - The script is signed using the certificate, which includes the private key stored in the `CurrentUser\My` certificate store.
# Set-AuthenticodeSignature -FilePath "C:\source\ntools\dev-setup\ff.ps1" -Certificate $cert

# # Step 5: Verify the signature
# # - The signature of the script is verified to ensure that it was signed by the certificate.
# Get-AuthenticodeSignature -FilePath "C:\source\ntools\dev-setup\ff.ps1"

# ### Summary
# - **Initial Creation**: The private key is initially stored in the `CurrentUser\My` certificate store when the self-signed certificate is created.
# - **Export**: The private key is exported along with the certificate to a `.pfx` file.
# - **Import**: The private key is imported back into the `CurrentUser\My` certificate store from the `.pfx` file.
# - **Usage**: The private key is used to sign the script from the `CurrentUser\My` certificate store.

# ### Security Considerations
# - **Protect the `.pfx` File**: Ensure that the `.pfx` file is stored securely and that the password used to protect it is strong and kept confidential.
# - **Access Control**: Limit access to the certificate store and the `.pfx` file to authorized users only.
# - **Production Use**: For production environments, use a certificate issued by a trusted Certificate Authority (CA) rather than a self-signed certificate.

