#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Builds a signed MSIX package for CopilotCostTracker (x64, Windows).
    Run once; subsequent runs reuse the existing certificate.

.NOTES
    Must be run as Administrator (needed to install the certificate to
    Trusted Root and Trusted People so the MSIX installs without warnings).
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ProjectRoot = Split-Path $PSScriptRoot -Parent
$DistDir     = $PSScriptRoot
$CertSubject = "CN=CopilotCostTracker"
$PfxPath     = Join-Path $DistDir "CopilotCostTracker.pfx"
$CerPath     = Join-Path $DistDir "CopilotCostTracker.cer"
$PfxPassword = "CopilotCostTracker2025!"   # used only locally to protect the PFX

# ── 1. Certificate ───────────────────────────────────────────────────────────
Write-Host "`n[1/4] Checking certificate..." -ForegroundColor Cyan

$cert = Get-ChildItem Cert:\CurrentUser\My |
        Where-Object { $_.Subject -eq $CertSubject } |
        Sort-Object NotAfter -Descending |
        Select-Object -First 1

if (-not $cert) {
    Write-Host "     Creating new self-signed certificate..." -ForegroundColor Yellow
    $cert = New-SelfSignedCertificate `
        -Type Custom `
        -Subject $CertSubject `
        -KeyUsage DigitalSignature `
        -FriendlyName "CopilotCostTracker Code Signing" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
    Write-Host "     Certificate created: $($cert.Thumbprint)" -ForegroundColor Green
} else {
    Write-Host "     Reusing existing certificate: $($cert.Thumbprint)" -ForegroundColor Green
}

# Export PFX (for dotnet publish)
$securePassword = ConvertTo-SecureString -String $PfxPassword -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath $PfxPath -Password $securePassword | Out-Null
Write-Host "     PFX exported: $PfxPath"

# Export CER (public key, for distribution / manual trust installation)
Export-Certificate -Cert $cert -FilePath $CerPath -Type CERT | Out-Null
Write-Host "     CER exported: $CerPath"

# ── 2. Install certificate so MSIX installs silently ────────────────────────
Write-Host "`n[2/4] Installing certificate into Trusted People & Trusted Root..." -ForegroundColor Cyan

$stores = @("Cert:\LocalMachine\TrustedPeople", "Cert:\LocalMachine\Root")
foreach ($store in $stores) {
    $existing = Get-ChildItem $store -ErrorAction SilentlyContinue |
                Where-Object { $_.Thumbprint -eq $cert.Thumbprint }
    if (-not $existing) {
        Import-Certificate -FilePath $CerPath -CertStoreLocation $store | Out-Null
        Write-Host "     Installed into $store" -ForegroundColor Green
    } else {
        Write-Host "     Already present in $store"
    }
}

# ── 3. Build MSIX ────────────────────────────────────────────────────────────
Write-Host "`n[3/4] Building MSIX (Release, x64)..." -ForegroundColor Cyan

$MsixOutDir = Join-Path $DistDir "msix-output"
if (Test-Path $MsixOutDir) { Remove-Item $MsixOutDir -Recurse -Force }
New-Item $MsixOutDir -ItemType Directory | Out-Null

$publishArgs = @(
    "publish"
    $ProjectRoot
    "-f", "net10.0-windows10.0.19041.0"
    "-c", "Release"
    "-r", "win-x64"
    "--self-contained", "true"
    "-p:WindowsPackageType=MSIX"
    "-p:WindowsAppSDKSelfContained=true"
    "-p:Platform=x64"
    "-p:AppxPackageDir=$MsixOutDir\"
    "-p:PackageCertificateKeyFile=$PfxPath"
    "-p:PackageCertificatePassword=$PfxPassword"
    "-p:PackageCertificateThumbprint="   # let it use the PFX directly
    "--nologo"
)

Write-Host "     dotnet $($publishArgs -join ' ')" -ForegroundColor DarkGray
& dotnet @publishArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

# ── 4. Copy MSIX to dist/ ────────────────────────────────────────────────────
Write-Host "`n[4/4] Copying artefacts to dist/..." -ForegroundColor Cyan

$msixFiles = Get-ChildItem $MsixOutDir -Recurse -Filter "*.msix"
if (-not $msixFiles) {
    # Some SDK versions produce .msixbundle instead
    $msixFiles = Get-ChildItem $MsixOutDir -Recurse -Include "*.msix","*.msixbundle"
}

foreach ($f in $msixFiles) {
    $dest = Join-Path $DistDir $f.Name
    Copy-Item $f.FullName $dest -Force
    Write-Host "     $($f.Name) -> $dest" -ForegroundColor Green
}

Write-Host "`n✅  Done! Files in $DistDir :" -ForegroundColor Green
Get-ChildItem $DistDir | Where-Object { -not $_.PSIsContainer } |
    Format-Table Name, @{L='Size';E={"{0:N0} KB" -f ($_.Length/1KB)}} -AutoSize

Write-Host @"

To install on this machine:
  Double-click  dist\*.msix  (certificate is already trusted)

To install on another machine:
  1. Copy both  CopilotCostTracker.cer  and  *.msix  to the target machine
  2. Install the .cer into 'Local Machine > Trusted People' (as Administrator)
  3. Double-click the .msix
"@ -ForegroundColor Cyan
