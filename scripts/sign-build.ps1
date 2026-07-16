param (
    [string]$ExePath = "artifacts\MineHarbor.exe"
)

if (-Not (Test-Path $ExePath)) {
    Write-Error "Executable not found at $ExePath"
    exit 1
}

$certFile = "MineHarbor-CodeSign.pfx"
$certPassword = ConvertTo-SecureString -String "mineharbor123" -Force -AsPlainText

if (-Not (Test-Path $certFile)) {
    Write-Host "No certificate found. Generating a new Self-Signed Code Signing Certificate..."
    $cert = New-SelfSignedCertificate -Subject "CN=MineHarbor Developer" -Type CodeSigningCert -CertStoreLocation "Cert:\CurrentUser\My"
    Export-PfxCertificate -Cert $cert -FilePath $certFile -Password $certPassword | Out-Null
    Write-Host "Certificate generated and saved to $certFile"
} else {
    Write-Host "Using existing certificate: $certFile"
}

$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($certFile, "mineharbor123")

Write-Host "Signing $ExePath ..."
# We will use DigiCert timestamp server
$signResult = Set-AuthenticodeSignature -FilePath $ExePath -Certificate $cert -TimestampServer "http://timestamp.digicert.com"

if ($signResult.Status -eq "Valid") {
    Write-Host "Successfully signed $($ExePath)." -ForegroundColor Green
} else {
    Write-Warning "Signing failed or has warnings. Status: $($signResult.Status) - $($signResult.StatusMessage)"
}
