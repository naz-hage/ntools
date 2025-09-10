<# DEPRECATED: Merged into ntools-scripts.psm1. This file is kept temporarily and will be removed. #>

function Test-MicrosoftPowerShellSecurityModuleLoaded {
    try {
        Import-Module Microsoft.PowerShell.Security -ErrorAction Stop
        return $true
    } catch {
        Write-Error "Failed to import Microsoft.PowerShell.Security module: $($_.Exception.Message)"
        return $false
    }
}

function Test-CertificateStore {
    try {
        if (-not (Get-PSDrive -PSProvider Certificate -ErrorAction SilentlyContinue)) {
            Write-Error "Certificate PSProvider not available on this system."
            return $false
        }
        return $true
    } catch {
        Write-Error "Failed to access the Certificate PSProvider: $($_.Exception.Message)"
        return $false
    }
}

function New-SelfSignedCodeCertificate {
    param(
        [Parameter(Mandatory=$true)][string]$DnsName,
        [Parameter(Mandatory=$false)][string]$CertSubject = '',
        [Parameter(Mandatory=$false)][string]$CertStoreLocation = "Cert:\CurrentUser\My",
        [Parameter(Mandatory=$false)][int]$NotAfterYears = 5
    )

    try {
        if ([string]::IsNullOrWhiteSpace($CertSubject)) {
            $cert = New-SelfSignedCertificate -DnsName $DnsName -Type CodeSigningCert -CertStoreLocation $CertStoreLocation -NotAfter (Get-Date).AddYears($NotAfterYears)
        } else {
            # include subject if provided for compatibility with legacy scripts
            $cert = New-SelfSignedCertificate -DnsName $DnsName -Subject $CertSubject -Type CodeSigningCert -CertStoreLocation $CertStoreLocation -NotAfter (Get-Date).AddYears($NotAfterYears)
        }
        return $cert
    } catch {
        throw "Failed to create self-signed certificate: $($_.Exception.Message)"
    }
}

function Export-CertificateToPfx {
    param(
        [Parameter(Mandatory=$true)][System.Security.Cryptography.X509Certificates.X509Certificate2]$Certificate,
        [Parameter(Mandatory=$true)][string]$FilePath,
        [Parameter(Mandatory=$true)][System.Security.SecureString]$Password
    )

    try {
        Export-PfxCertificate -Cert $Certificate -FilePath $FilePath -Password $Password -Force
        return $FilePath
    } catch {
        throw "Failed to export certificate to PFX: $($_.Exception.Message)"
    }
}

function Export-CertificateToCer {
    param(
        [Parameter(Mandatory=$true)][System.Security.Cryptography.X509Certificates.X509Certificate2]$Certificate,
        [Parameter(Mandatory=$true)][string]$FilePath
    )

    try {
        Export-Certificate -Cert $Certificate -FilePath $FilePath -Force | Out-Null
        return $FilePath
    } catch {
        throw "Failed to export certificate to CER: $($_.Exception.Message)"
    }
}

function Import-CertificateToRoot {
    param(
        [Parameter(Mandatory=$true)][string]$CerFilePath
    )

    if (-not (Test-IsAdministrator)) {
        throw "Importing to LocalMachine Trusted Root requires Administrator rights."
    }

    try {
        Import-Certificate -FilePath $CerFilePath -CertStoreLocation "Cert:\LocalMachine\Root" | Out-Null
        return $true
    } catch {
        throw "Failed to import certificate to Trusted Root: $($_.Exception.Message)"
    }
}

function Import-CertificateToCurrentUser {
    param(
        [Parameter(Mandatory=$true)][string]$PfxFilePath,
        [Parameter(Mandatory=$true)][System.Security.SecureString]$Password
    )
    try {
        $cert = Import-PfxCertificate -FilePath $PfxFilePath -CertStoreLocation "Cert:\CurrentUser\My" -Password $Password -Exportable
        return $cert
    } catch {
        throw "Failed to import PFX to CurrentUser store: $($_.Exception.Message)"
    }
}

function Set-ScriptSignature {
    param(
        [Parameter(Mandatory=$true)][string]$ScriptPath,
        [Parameter(Mandatory=$true)][System.Security.Cryptography.X509Certificates.X509Certificate2]$Certificate
    )

    if (-not (Test-Path $ScriptPath)) {
        throw "Script path not found: $ScriptPath"
    }

    try {
        $sig = Set-AuthenticodeSignature -FilePath $ScriptPath -Certificate $Certificate
        return $sig
    } catch {
        throw "Failed to sign script $ScriptPath: $($_.Exception.Message)"
    }
}

function Get-ScriptSignature {
    param(
        [Parameter(Mandatory=$true)][string]$ScriptPath
    )
    try {
        $signature = Get-AuthenticodeSignature -FilePath $ScriptPath
        return $signature
    } catch {
        throw "Failed to verify signature for $ScriptPath: $($_.Exception.Message)"
    }
}

function Set-CodeSigningTrust {
    <#
    Creates a code-signing certificate, exports it, optionally trusts it at the machine level,
    imports to CurrentUser, signs the supplied scripts and verifies signatures.

    Parameters:
      -DnsName (string) : DNS name used for the self-signed cert (common name)
      -Location (string) : Directory to write .pfx/.cer files (will be created)
      -CertSubject (string) : Certificate subject (optional; default CN=<DnsName>)
      -CertPasswordPlain (string) : Plain text password for the PFX (optional; generated if empty)
      -ScriptPaths (string[]) : Scripts to sign (optional)
      -TrustMachine (switch) : If set, import CER into LocalMachine\Root (requires Admin)
      -NotAfterYears (int) : Certificate validity in years (default 5)
      -Force (switch) : Overwrite existing files if present
    #>
    param(
        [Parameter(Mandatory=$true)][string]$DnsName,
        [Parameter(Mandatory=$false)][string]$Location = "$(Join-Path -Path (Get-Location) -ChildPath 'certs')",
        [Parameter(Mandatory=$false)][string]$CertSubject = '',
        [Parameter(Mandatory=$false)][string]$CertPasswordPlain = '',
        [Parameter(Mandatory=$false)][string[]]$ScriptPaths = @(),
        [Parameter(Mandatory=$false)][switch]$TrustMachine,
        [Parameter(Mandatory=$false)][int]$NotAfterYears = 5,
        [Parameter(Mandatory=$false)][switch]$Force
    )

    if (-not (Test-MicrosoftPowerShellSecurityModuleLoaded)) { throw "Required PowerShell Security module unavailable" }
    if (-not (Test-CertificateStore)) { throw "Certificate store unavailable" }

    if (-not $CertSubject) { $CertSubject = "CN=$DnsName" }

    # ensure location
    if (-not (Test-Path $Location)) { New-Item -ItemType Directory -Force -Path $Location | Out-Null }

    # password: prefer env:VTAPIKEY if present, otherwise use supplied plain or generate one
    if ([string]::IsNullOrWhiteSpace($CertPasswordPlain) -and -not [string]::IsNullOrWhiteSpace($env:VTAPIKEY)) {
        $CertPasswordPlain = $env:VTAPIKEY
        Write-Host "Using VTAPIKEY environment variable as PFX password (converted to SecureString)."
    }

    if ([string]::IsNullOrWhiteSpace($CertPasswordPlain)) {
        # generate a random password if not supplied
        $bytes = New-Object byte[] 16; (New-Object System.Security.Cryptography.RNGCryptoServiceProvider).GetBytes($bytes)
        $CertPasswordPlain = [Convert]::ToBase64String($bytes)
        Write-Host "Generated random password for PFX (kept only in memory)."
    }

    $securePassword = ConvertTo-SecureString -String $CertPasswordPlain -Force -AsPlainText

    # create cert (pass CertSubject for compatibility)
    $cert = New-SelfSignedCodeCertificate -DnsName $DnsName -CertSubject $CertSubject -CertStoreLocation "Cert:\CurrentUser\My" -NotAfterYears $NotAfterYears

    # prepare paths
    $pfxPath = Join-Path $Location "$DnsName.pfx"
    $cerPath = Join-Path $Location "$DnsName.cer"

    if ((Test-Path $pfxPath -PathType Leaf -ErrorAction SilentlyContinue -ErrorVariable ev) -and (-not $Force)) {
        throw "PFX file already exists at $pfxPath. Use -Force to overwrite."
    }

    # export
    Export-CertificateToPfx -Certificate $cert -FilePath $pfxPath -Password $securePassword | Out-Null
    Export-CertificateToCer -Certificate $cert -FilePath $cerPath | Out-Null

    # optionally trust in LocalMachine - requires admin
    if ($TrustMachine) {
        if (-not (Test-IsAdministrator)) { throw "TrustMachine requested but caller is not Administrator." }
        Import-CertificateToRoot -CerFilePath $cerPath | Out-Null
        Write-Host "Imported $cerPath to LocalMachine\Root"
    }

    # import to current user
    $imported = Import-CertificateToCurrentUser -PfxFilePath $pfxPath -Password $securePassword

    # if no ScriptPaths were provided, default to legacy ff.ps1 location for compatibility
    if (($ScriptPaths -eq $null) -or ($ScriptPaths.Count -eq 0)) {
        $legacy = 'C:\source\ntools\dev-setup\ff.ps1'
        if (Test-Path $legacy) { $ScriptPaths = @($legacy) }
    }

    # sign provided scripts
    $signed = @()
    foreach ($script in $ScriptPaths) {
        try {
            $full = (Resolve-Path -Path $script).Path
            $sig = Set-ScriptSignature -ScriptPath $full -Certificate $imported
            $signed += @{ Script = $full; Status = $sig.Status; Signature = $sig }
            Write-Host "Signed script: $full (Status: $($sig.Status))"
        } catch {
            Write-Warning "Failed to sign $script: $($_.Exception.Message)"
            $signed += @{ Script = $script; Status = 'Error'; Error = $_.Exception.Message }
        }
    }

    return @{ Certificate = $cert; Pfx = $pfxPath; Cer = $cerPath; Imported = $imported; Signed = $signed }
}

# Export public functions
Export-ModuleMember -Function Test-IsAdministrator, Test-MicrosoftPowerShellSecurityModuleLoaded, Test-CertificateStore, New-SelfSignedCodeCertificate, Export-CertificateToPfx, Export-CertificateToCer, Import-CertificateToRoot, Import-CertificateToCurrentUser, Set-ScriptSignature, Get-ScriptSignature, Set-CodeSigningTrust
