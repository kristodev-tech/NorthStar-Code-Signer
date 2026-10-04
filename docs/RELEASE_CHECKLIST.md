# Release validation

Complete these steps on a patched Windows PC before publishing a production release.

- Run `01_build_app.bat`; compilation must exit zero.
- Run `02_run_checks.bat`; inspect `build/Windows_Self_Test.json`, including `windows_signing_executed: true`.
- Inspect all screenshots and the live UI on Windows at 100%, 125%, 150% and 200% scaling. Check scrolling, navigation, long paths, minimum window size and keyboard tab order. Mono screenshots are not proof of Windows DPI behavior.
- Confirm first-launch license decline closes without accepting, acceptance persists, and the license remains accessible in Guide.
- Create a certificate in a new private directory. Confirm existing PFXs cannot be overwritten and password fields clear after use.
- Confirm unsigned, signed, untrusted and tampered files show appropriate results. Check report/checksum exports against `Get-FileHash -Algorithm SHA256`.
- Test signed-copy and in-place modes using disposable builds, including backup restoration, wrong password, read-only/locked input, malformed EXE and existing output.
- Verify no new signing certificates remain in Personal after normal PFX signing; creation removes its temporary certificate. Never add test certificates to root trust.
- Test a real compatible timestamp endpoint with your provider. Confirm timestamp presence; test an unreachable/noncompatible endpoint and confirm source/output safety. Provider timestamping is not included in the offline self-tests.
- Build the optional installer with Inno Setup 6. Test install, MIT acceptance, shortcuts, app startup and uninstall on a clean Windows account with .NET Framework 4.8. Backups intentionally remain after uninstall.
- Review GitHub Actions logs, source license/branding and the final archive for secrets. `.gitignore` does not filter drag-and-drop uploads or ZIP contents.
- Sign final app, package it, sign setup, verify and export checksums. Never rebuild the app after signing without signing again.
- Publish the source at repository root and binaries as release assets. Explain that the shipped signer itself is unsigned unless you sign it with an appropriate certificate.

Record Windows version, DPI, provider timestamp result and installer result with your release notes. Do not claim those checks passed based only on a successful cross-platform compile.
