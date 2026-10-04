# Changes

## 2.0.1

- Fix self-signed signing incorrectly rejected as a verification failure on Windows PowerShell 5.1.
- Read the native WinVerifyTrust result and accept the exact untrusted-root code (0x800B0109) as a signed, untrusted result. Do not accept arbitrary UnknownError statuses or rely on localized message text.
- Preserve the raw PowerShell status and native Windows trust code in the report.
- Add regression checks for permitted native results and rejection of digest mismatch, explicit distrust, expiry, revocation, absent signatures, partial chains and other failures.
- Extend Windows integration tests to confirm self-signed output is committed and tampered output rejected by native verification.

## 2.0.0

- New NorthStar interface with four task screens, readable guidance, scrollable forms and DPI manifest.
- Separate verification path, signed-copy default, in-place confirmation and captured checksum/report exports.
- Required optional timestamping: a missing requested timestamp prevents publishing staged output.
- Stronger PE/path/PFX validation; reject ambiguous signing certificates and unacceptable signing verification statuses.
- PFX signing loads certificate objects without store imports; creation still cleans up its temporary Personal-store certificate.
- Password fields clear after use; a created certificate no longer carries its password into the signing form.
- Source-change detection before in-place replacement and locked verification snapshot.
- Embedded MIT license with first-launch acceptance; free-use GitHub package, ignore rules and security/contributor guidance.
- Consistent build/run/check scripts, Windows CI and per-user Inno installer with branding and license.

## 1.0.0

Initial certificate creation, PFX EXE signing, verification and backup workflow.
