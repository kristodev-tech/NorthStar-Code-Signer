# NorthStar Code Signer 2.0.1 — User guide

## 1. Open the application

Extract the package, then open `NorthStarCodeSigner.exe`. If downloading source only, double-click `01_build_app.bat`. The Windows .NET Framework compiler builds the app without extra Python tools. Accept the included MIT license on first launch. The same license permits free use, modification and redistribution, including commercial use, with its notice retained.

The window resizes and each form scrolls when space is limited. Use Tab to move between controls. All password fields are masked. The app blocks closing while an operation is running so a normal close cannot interrupt signing.

## 2. Create a development certificate

Open **Create certificate**. Enter your publisher name, select a NEW `.pfx` filename in an existing private folder, and enter/confirm a password of at least eight characters. Click **Create certificate**. This creates an RSA 3072/SHA-256 self-signed code-signing certificate valid for three years and fills its path on the signing screen. You must re-enter its password; it is not retained for you.

Choose a strong unique password and keep a secure backup of both the PFX and password. Do not store the PFX inside your project, installer output or GitHub folder. Existing certificates are never overwritten. If the certificate is lost, this app cannot reconstruct it.

This is a development certificate. It proves that a file carries a signature from that key; it does not independently prove your business identity to customers. The app does not make Windows trust it automatically.

## 3. Sign an EXE

On **Sign application**, choose your PFX and enter its password. Choose the finished application EXE or setup EXE. The app suggests a `_signed.exe` copy beside the original. Select a different output name if that file already exists.

**Copy mode** is the default. It keeps the original untouched and creates a new signed file. With copy mode disabled, confirm in-place signing. The app saves the original in the backups folder before replacing it. The previous embedded signature is replaced; signatures are not appended.

Click **Sign application**. The password is cleared after submitting. The controls pause while the worker signs and verifies a staging file. Only an accepted signature result is moved into place. If the password is wrong, the PFX is unsuitable, the requested timestamp fails or Windows rejects the operation, the original EXE is not replaced.

Normal operations need no administrator approval. Targets under Program Files, read-only folders, locked running applications or security policy restrictions can prevent signing. Copy the build output into a writable folder rather than routinely elevating the signer.

## 4. Optional timestamp

Enable **Require a timestamp from my provider**, then enter the provider's `http://` Authenticode-compatible timestamp URL. The Windows PowerShell API used here supports legacy Authenticode timestamping. It is not an HTTPS/RFC 3161 client. Use your provider's workflow for unsupported timestamp services or keys.

The endpoint is contacted during signing. The result must contain a timestamp certificate; otherwise the staged file is discarded. Do not assume that checking the box means a timestamp was successfully applied. Read **Timestamp present** in the final report.

Without a timestamp, validation can fail after the signing certificate expires. A timestamp does not make a self-signed certificate publicly trusted.

## 5. Verify and understand results

On **Verify file**, browse to any EXE or use the last checked file, then click **Verify file**. No password is needed. The result area shows Windows status, signer name/thumbprint, expiry, timestamp certificate, and the complete-file SHA-256 checksum.

| Result | Meaning |
| --- | --- |
| `Valid` | Windows accepts the signature on this PC now; public trust and SmartScreen reputation are separate |
| `NotTrusted` | Windows does not accept the certificate chain; expected for many self-signed certificates |
| `NotSigned` | Windows found no recognized signature |
| `HashMismatch` | Signed content no longer matches the signature; investigate the change |
| Other status | Read Windows detail and investigate before distribution |

Verification evaluates your PC's trust stores and policy. It is not a malware scan or universal trust guarantee. Windows may contact chain/revocation services. A checksum identifies the exact bytes checked at that moment and must be regenerated after any change.

## 6. Save results

**Copy report** copies the latest successful report to the clipboard. **Save report** writes it to a `.txt` file. **Save SHA-256** writes `<filename>.sha256.txt` in the conventional checksum format. This checksum is the one captured during signing/verification; if the file changed afterward, verify it again first.

You may include a final installer checksum beside the installer in your release. The checksum helps users compare downloads; it does not replace a trusted signature. Do not include the PFX or password. Reports contain local paths, so review them before publishing.

## 7. Correct release order

1. Build the application EXE.
2. Sign it, preserving the filename used by your installer script or pointing that script to the signed copy.
3. Build your installer containing that signed EXE.
4. Sign the setup EXE.
5. Verify both final artifacts and export their checksums.
6. Publish the installer and public documentation. Keep private signing materials elsewhere.

For this utility, `03_build_installer.bat` uses the existing `NorthStarCodeSigner.exe`. It builds only when no EXE exists, so your signature is preserved. Running the build script again replaces the EXE and removes that previously added signature.

## 8. Backups and recovery

Open **Guide / About → Open backups**. Originals are named with a unique prefix under `%LOCALAPPDATA%\NorthStar Code Signer\Backups`. Match the filename and operation report before restoring one. Keep the signed file separately, close anything using the target, and copy the recognized backup back to the original path. Backup cleanup is manual; the app does not delete your recovery files automatically.

## 9. Troubleshooting

- **Wrong password:** enter the exact PFX password again. The app cannot recover it.
- **Output already exists:** select a new filename or move the existing file yourself.
- **Invalid PFX/key:** use a PFX with exactly one code-signing certificate and its private key. A public `.cer` is not sufficient.
- **Expired certificate:** obtain/create a currently valid certificate. The app will not sign with expired or not-yet-valid certificates.
- **Timestamp failed:** check the provider's compatible URL/network. No output is committed when a required timestamp is missing.
- **PowerShell blocked:** follow your administrator's policy; do not disable organizational security controls to run the app.
- **Signing timed out:** check Windows/network policy. The app leaves the original EXE alone, but a forced worker interruption may leave temporary certificate material from creation in the current user's Personal store.
- **Installer/build problem:** read the compiler output, confirm .NET Framework 4.8, and install Inno Setup 6 only if building the installer.

Run `02_run_checks.bat` before your first public release. It creates temporary test certificates/files, exercises signing and backup safety, and writes reports/screenshots under `build`. Timestamp-provider checks and real Windows DPI/layout inspection are separate steps in the release checklist.
