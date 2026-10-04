# Architecture and maintenance

NorthStar Code Signer remains a small .NET Framework 4.8 Windows application.

| Location | Responsibility |
| --- | --- |
| `src/Program.cs` | Branded WinForms UI, license acceptance, background operations, report export |
| `src/SigningService.cs` | Input/PE validation, worker lifecycle, staging, checksums and safe commit |
| `src/NativeTrust.cs` | Native WinVerifyTrust interop and exact signing-result policy |
| `src/SelfTest.cs` | Validation checks and Windows signing integration tests |
| `powershell/worker.ps1` | Certificate creation/loading, SHA-256 Authenticode signing, Windows verification |
| `assets/` | Original NorthStar icon and asInvoker/DPI manifest |
| `installer/` | Per-user Inno Setup packaging |
| `.github/workflows/` | Windows build and signing integration CI |

The worker is embedded at compile time and extracted into a random temporary directory for each operation. Requests are JSON via standard input; passwords are not command-line arguments. Standard output/error are drained asynchronously to prevent pipe deadlock. Operations run on a background task and post results to the UI thread. Worker timeout is 120 seconds.

Certificate creation uses Windows PKI commands in the current user's Personal store, exports a PFX into a temporary file, and attempts to remove the created certificate/private key. Signing loads a PFX collection with UserKeySet without adding it to a certificate store or requesting persistent keys. Certificate instances are disposed after use. Abnormal worker termination can interrupt cleanup; no promise of forensic memory erasure is made.

An EXE must have an MZ header and a valid PE signature offset. Signing occurs on a sibling staging EXE and validates the resulting signer and acceptable Windows status. A requested timestamp must be present. New copy paths cannot overwrite existing files. In-place signing makes an original backup and compares hashes before atomic replacement; callers should not modify a target concurrently. Verification holds a shared read lock while checking the signature and hashing the file so reports describe one snapshot.

Public trust is separate from signature presence. The worker loads the native helper from the same executable assembly and calls WinVerifyTrust for the embedded PE signature. Only native success (zero) or CERT_E_UNTRUSTEDROOT (0x800B0109) permits commit. Untrusted roots are normalized to `NotTrusted`; reports retain the raw PowerShell status and native code. This avoids treating PowerShell UnknownError as a generic success. Other native failures stop the commit. Verification reports all Windows statuses for diagnosis.

Build scripts use the Windows .NET Framework compiler, embedded resources, optimized output, warning level 4 and warnings as errors. Keep version constants aligned in Program, SelfTest, manifest, installer, docs and release metadata. Never embed a PFX in resources or commit signing secrets. Use the Windows CI report and local release checklist for validation after changes.
