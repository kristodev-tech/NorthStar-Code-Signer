$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Console]::InputEncoding = New-Object System.Text.UTF8Encoding($false)
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
$created = $null
$loaded = $null
$exitCode = 0
try {
    $request = [Console]::In.ReadToEnd() | ConvertFrom-Json
    if ($request.operation -eq 'create') {
        $password = ConvertTo-SecureString ([string]$request.password) -AsPlainText -Force
        $created = New-SelfSignedCertificate -Type CodeSigningCert `
            -Subject ('CN=' + [string]$request.publisher) `
            -FriendlyName 'NorthStar Code Signer' -CertStoreLocation 'Cert:\CurrentUser\My' `
            -KeyAlgorithm RSA -KeyLength 3072 -HashAlgorithm SHA256 `
            -KeyExportPolicy Exportable -NotAfter (Get-Date).AddYears(3)
        Export-PfxCertificate -Cert $created -FilePath ([string]$request.pfx) -Password $password | Out-Null
        $result = @{ok=$true; subject=$created.Subject; thumbprint=$created.Thumbprint; expires=$created.NotAfter.ToString('o')}
    } elseif ($request.operation -eq 'sign' -or $request.operation -eq 'verify') {
        if ($request.operation -eq 'sign') {
            # Load without adding certificates to a store or marking keys persistent.
            $loaded = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2Collection
            $loaded.Import([string]$request.pfx, [string]$request.password, [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::UserKeySet)
            $candidates = @($loaded | Where-Object {
                $eku = $_.Extensions | Where-Object { $_.Oid.Value -eq '2.5.29.37' }
                $_.HasPrivateKey -and ($eku.EnhancedKeyUsages.Value -contains '1.3.6.1.5.5.7.3.3')
            })
            if ($candidates.Count -ne 1) { throw 'The PFX must contain exactly one code-signing certificate with a private key.' }
            $cert = $candidates[0]
            if ((Get-Date) -lt $cert.NotBefore -or (Get-Date) -gt $cert.NotAfter) { throw 'The signing certificate is not currently valid.' }
            $parameters = @{LiteralPath=[string]$request.target; Certificate=$cert; HashAlgorithm='SHA256'; IncludeChain='NotRoot'; WarningAction='SilentlyContinue'}
            if ($request.timestamp) { $parameters.TimestampServer = [string]$request.timestamp }
            $null = Set-AuthenticodeSignature @parameters
        }
        $signature = Get-AuthenticodeSignature -LiteralPath ([string]$request.target)
        $null = [System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes([string]$request.assembly))
        $nativeResult = [NorthStarCodeSigner.NativeTrust]::Verify([string]$request.target)
        $nativeCode = [NorthStarCodeSigner.NativeTrust]::Code($nativeResult)
        $displayStatus = $signature.Status.ToString()
        if ($signature.SignerCertificate -and $nativeResult -eq [NorthStarCodeSigner.NativeTrust]::UntrustedRoot) {
            $displayStatus = 'NotTrusted'
        }
        if ($request.operation -eq 'sign') {
            if (-not $signature.SignerCertificate -or $signature.SignerCertificate.Thumbprint -ne $cert.Thumbprint) { throw 'Windows did not apply the requested certificate.' }
            # Accept only native success or the exact untrusted-root condition.
            # PowerShell 5.1 may classify CERT_E_UNTRUSTEDROOT as UnknownError.
            # Never accept arbitrary UnknownError or localized message matches.
            if (-not [NorthStarCodeSigner.NativeTrust]::AcceptableForSigning($nativeResult)) {
                throw ('Signature verification failed (' + $nativeCode + '): ' + $signature.StatusMessage)
            }
            if ($request.timestamp -and -not $signature.TimeStamperCertificate) { throw 'The requested timestamp was not applied. The original file was not replaced. Check the provider endpoint.' }
        }
        $subject = ''; $thumbprint = ''; $expires = ''; $timestampSubject = ''
        if ($signature.SignerCertificate) { $subject=$signature.SignerCertificate.Subject; $thumbprint=$signature.SignerCertificate.Thumbprint; $expires=$signature.SignerCertificate.NotAfter.ToString('o') }
        if ($signature.TimeStamperCertificate) { $timestampSubject=$signature.TimeStamperCertificate.Subject }
        $result = @{ok=$true; status=$displayStatus; windows_status=$signature.Status.ToString(); native_code=$nativeCode; message=$signature.StatusMessage; subject=$subject; thumbprint=$thumbprint; expires=$expires; signature_present=($null -ne $signature.SignerCertificate); trusted=($nativeResult -eq 0 -and $signature.Status.ToString() -eq 'Valid'); timestamped=($null -ne $signature.TimeStamperCertificate); timestamp_subject=$timestampSubject}
    } else { throw 'Unknown operation.' }
} catch {
    $result = @{ok=$false; error=$_.Exception.Message}
    $exitCode = 1
} finally {
    if ($created) { Remove-Item ('Cert:\CurrentUser\My\' + $created.Thumbprint) -DeleteKey -ErrorAction SilentlyContinue; $created.Dispose() }
    if ($loaded) { foreach ($certificate in $loaded) { $certificate.Dispose() } }
    if ($password) { $password.Dispose() }
    if ($request -and $request.PSObject.Properties['password']) { $request.password = $null }
}
[Console]::Out.WriteLine(($result | ConvertTo-Json -Compress -Depth 5))
exit $exitCode
