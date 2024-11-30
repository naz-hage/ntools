# This script is used to sign and trust a PowerShell script using a self-signed certificate.
# The script creates a self-signed certificate, exports it to a .pfx file, exports it to a .cer file, 
# imports it to Trusted Root Certification Authorities, imports it back to CurrentUser store, 
# signs the script, and verifies the signature.
# Usage: .\sign-trust.ps1 -dnsName "ntools" -location "C:\source\ntools\DevSetup" -certSubject "CN=ntools"
param (
    [Parameter(Mandatory=$true)]
    [string]$dnsName,

    [Parameter(Mandatory=$true)]
    [string]$location,

    [Parameter(Mandatory=$true)]
    [string]$certSubject,

    [Parameter(Mandatory=$false)]
    [string]$certPasswordPlain = $env:VTAPIKEY
)

# Ensure the Microsoft.PowerShell.Security module is loaded
function Ensure-MicrosoftPowerShellSecurityModuleLoaded {
    try {
        Import-Module Microsoft.PowerShell.Security -ErrorAction Stop
    } catch {
        Write-Error "Failed to import Microsoft.PowerShell.Security module: $_"
        exit
    }
}

# Check if the certificate store exists
function Check-CertificateStore {
    try {
        if (-not (Get-PSDrive -PSProvider Certificate)) {
            Write-Error "Certificate store not found. Ensure the Certificate PSProvider is available."
            exit
        }
    } catch {
        Write-Error "Failed to access the Certificate PSProvider: $_"
        exit
    }
}

# Create a self-signed certificate
function Create-SelfSignedCertificate {
    param (
        [Parameter(Mandatory=$true)]
        [string]$dnsName
    )

    try {
        $cert = New-SelfSignedCertificate -DnsName $dnsName -Type CodeSigningCert -CertStoreLocation "Cert:\CurrentUser\My"
        return $cert
    } catch {
        Write-Error "Failed to create self-signed certificate: $_"
        exit
    }
}

# Export the certificate to a .pfx file
function Export-CertificateToPfx {
    param (
        [Parameter(Mandatory=$true)]
        [System.Security.Cryptography.X509Certificates.X509Certificate2]$certificate,
        
        [Parameter(Mandatory=$true)]
        [string]$filePath,
        
        [Parameter(Mandatory=$true)]
        [System.Security.SecureString]$password
    )

    try {
        Export-PfxCertificate -Cert $certificate -FilePath $filePath -Password $password
    } catch {
        Write-Error "Failed to export certificate to .pfx file: $_"
        exit
    }
}

# Export the certificate to a .cer file
function Export-CertificateToCer {
    param (
        [Parameter(Mandatory=$true)]
        [System.Security.Cryptography.X509Certificates.X509Certificate2]$certificate,
        
        [Parameter(Mandatory=$true)]
        [string]$filePath
    )

    try {
        Export-Certificate -Cert $certificate -FilePath $filePath
    } catch {
        Write-Error "Failed to export certificate to .cer file: $_"
        exit
    }
}

# Import the certificate to Trusted Root Certification Authorities
function Import-CertificateToRoot {
    param (
        [Parameter(Mandatory=$true)]
        [string]$filePath
    )

    try {
        Import-Certificate -FilePath $filePath -CertStoreLocation "Cert:\LocalMachine\Root"
    } catch {
        Write-Error "Failed to import certificate to Trusted Root Certification Authorities: $_"
        exit
    }
}

# Import the certificate back to CurrentUser store
function Import-CertificateToCurrentUser {
    param (
        [Parameter(Mandatory=$true)]
        [string]$filePath,
        
        [Parameter(Mandatory=$true)]
        [System.Security.SecureString]$password
    )

    try {
        $cert = Import-PfxCertificate -FilePath $filePath -CertStoreLocation "Cert:\CurrentUser\My" -Password $password
        return $cert
    } catch {
        Write-Error "Failed to import certificate: $_"
        exit
    }
}

# Sign the PowerShell script
function Sign-Script {
    param (
        [Parameter(Mandatory=$true)]
        [string]$scriptPath,
        
        [Parameter(Mandatory=$true)]
        [System.Security.Cryptography.X509Certificates.X509Certificate2]$certificate
    )

    try {
        Set-AuthenticodeSignature -FilePath $scriptPath -Certificate $certificate
    } catch {
        Write-Error "Failed to sign the script: $_"
        exit
    }
}

# Verify the signature
function Verify-Signature {
    param (
        [Parameter(Mandatory=$true)]
        [string]$scriptPath
    )

    try {
        $signature = Get-AuthenticodeSignature -FilePath $scriptPath
        Write-Output $signature
    } catch {
        Write-Error "Failed to verify the script signature: $_"
        exit
    }
}

Ensure-MicrosoftPowerShellSecurityModuleLoaded
Check-CertificateStore

$certPassword = ConvertTo-SecureString -String $certPasswordPlain -Force -AsPlainText

# Step 1: Create a self-signed certificate
$cert = Create-SelfSignedCertificate -dnsName $dnsName

# Step 2: Export the certificate to a .pfx file
Export-CertificateToPfx -certificate $cert -filePath "$location\certificate.pfx" -password $certPassword

# Step 3: Export the certificate to a .cer file
Export-CertificateToCer -certificate $cert -filePath "$location\certificate.cer"

# Step 4: Import the certificate to Trusted Root Certification Authorities
Import-CertificateToRoot -filePath "$location\certificate.cer"

# Step 5: Import the certificate back to CurrentUser store
$cert = Import-CertificateToCurrentUser -filePath "$location\certificate.pfx" -password $certPassword

# Step 6: Sign the PowerShell script
Sign-Script -scriptPath "C:\source\ntools\DevSetup\ff.ps1" -certificate $cert

# Step 7: Verify the signature
Verify-Signature -scriptPath "C:\source\ntools\DevSetup\ff.ps1"