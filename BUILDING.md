# Building an MSIX Installer

This guide explains how to produce a signed MSIX package for distribution.

## Prerequisites

- Windows 10 / 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) with the MAUI workload:
  ```powershell
  dotnet workload install maui-windows
  ```
- PowerShell 5.1 or later (included in Windows)

---

## Step 1 — Create a Self-Signed Certificate

The package publisher in `Platforms/Windows/Package.appxmanifest` is `CN=CopilotCostTracker`.  
The certificate subject **must match** this value exactly.

```powershell
$cert = New-SelfSignedCertificate `
    -Type Custom `
    -Subject "CN=CopilotCostTracker" `
    -KeyUsage DigitalSignature `
    -FriendlyName "CopilotCostTracker MSIX Signing" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")

# Note the thumbprint — you will need it in Step 3
Write-Host "Thumbprint: $($cert.Thumbprint)"
```

Export the public certificate so recipients can trust the package:

```powershell
Export-Certificate `
    -Cert $cert `
    -FilePath "dist\CopilotCostTracker.cer" `
    -Type CERT
```

> The `.cer` file contains only the public key — it is safe to commit to source control.  
> Never commit `.pfx` files (they contain the private key).

---

## Step 2 — Build the MSIX

Run `dotnet publish` with `WindowsPackageType=MSIX` and the certificate thumbprint:

```powershell
dotnet publish `
    -f net10.0-windows10.0.19041.0 `
    -p:Configuration=Release `
    -p:WindowsPackageType=MSIX `
    -p:AppxPackageSigningEnabled=true `
    -p:PackageCertificateThumbprint=<YOUR_THUMBPRINT> `
    -p:Platform=x64 `
    --nologo
```

Replace `<YOUR_THUMBPRINT>` with the value printed in Step 1.

The MSIX is written to:

```
AppPackages\CopilotCostTracker_1.0.0.0_Test\CopilotCostTracker_1.0.0.0_x64.msix
```

---

## Step 3 — Copy Artifacts to `dist/`

```powershell
New-Item -ItemType Directory -Force -Path dist | Out-Null

Copy-Item `
    "AppPackages\CopilotCostTracker_1.0.0.0_Test\CopilotCostTracker_1.0.0.0_x64.msix" `
    -Destination dist\ -Force
```

After this step `dist/` contains:

| File | Description |
|---|---|
| `CopilotCostTracker_1.0.0.0_x64.msix` | Signed installer |
| `CopilotCostTracker.cer` | Public certificate for trust installation |

---

## Step 4 — Install on the Target Machine

Because the package is self-signed, the certificate must be trusted on the target machine **before** installing the MSIX.

1. Copy both files to the target machine.
2. **Trust the certificate** (run PowerShell as Administrator):
   ```powershell
   Import-Certificate `
       -FilePath "CopilotCostTracker.cer" `
       -CertStoreLocation "Cert:\LocalMachine\Root"
   ```
3. Double-click `CopilotCostTracker_1.0.0.0_x64.msix` to launch the installer.

---

## Updating the Version

Version numbers are controlled in `CopilotCostTracker.csproj`:

```xml
<ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>
<ApplicationVersion>1</ApplicationVersion>
```

And in `Platforms/Windows/Package.appxmanifest`:

```xml
<Identity Name="CopilotCostTracker" Publisher="CN=CopilotCostTracker" Version="1.0.0.0" />
```

Both values must be kept in sync before building a new release.

---

## Troubleshooting

| Problem | Solution |
|---|---|
| `NETSDK1083` — RuntimeIdentifier not recognized | Do **not** pass `-p:RuntimeIdentifier=win10-x64`; use `-p:Platform=x64` instead |
| `mspdbcmf.exe` warning | Harmless — only affects symbol package generation, not the MSIX itself |
| Install blocked — "App not trusted" | Trust the `.cer` as described in Step 4 |
| Thumbprint not found | Re-run Step 1; check `Get-ChildItem Cert:\CurrentUser\My` |
