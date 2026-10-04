using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Web.Script.Serialization;

namespace NorthStarCodeSigner {
public static class SelfTest {
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Rejected(Action action) {
        bool rejected = false; try { action(); } catch { rejected = true; }
        Require(rejected, "Invalid request was not rejected.");
    }
    static string PersonalStore() {
        var prints = new List<string>();
        using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser)) {
            store.Open(OpenFlags.ReadOnly);
            foreach (var certificate in store.Certificates) { prints.Add(certificate.Thumbprint); certificate.Dispose(); }
        }
        prints.Sort(StringComparer.Ordinal); return String.Join(",", prints);
    }
    static string Hash(string file) {
        using (SHA256 sha = SHA256.Create()) using (FileStream stream = File.OpenRead(file))
            return Convert.ToBase64String(sha.ComputeHash(stream));
    }
    public static int Run(string path) {
        var checks = new List<string>();
        var report = new Dictionary<string, object>(); report["version"] = "2.0.1";
        report["platform"] = Environment.OSVersion.ToString();
        string temp = Path.Combine(Path.GetTempPath(), "NorthStarSignerTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        int exit = 0;
        try {
            using (var script = Assembly.GetExecutingAssembly().GetManifestResourceStream("worker.ps1")) Require(script != null, "Missing embedded signing worker.");
            using (var icon = Assembly.GetExecutingAssembly().GetManifestResourceStream("northstar.ico")) Require(icon != null, "Missing embedded icon.");
            using (var license = Assembly.GetExecutingAssembly().GetManifestResourceStream("LICENSE")) Require(license != null, "Missing embedded license.");
            checks.Add("embedded resources");
            Require(NativeTrust.AcceptableForSigning(0), "Valid trust result rejected.");
            Require(NativeTrust.AcceptableForSigning(unchecked((int)0x800B0109)), "Untrusted root rejected.");
            foreach (uint failure in new uint[] {0x80096010, 0x800B0111, 0x800B010C, 0x800B0101, 0x800B0100, 0x800B010A, 0x80004005})
                Require(!NativeTrust.AcceptableForSigning(unchecked((int)failure)), "Invalid trust result accepted.");
            Require(NativeTrust.Code(NativeTrust.UntrustedRoot) == "0x800B0109", "Native code formatting failed.");
            checks.Add("native trust policy: untrusted root accepted; tampering, explicit distrust, revocation, expiry, unsigned, partial chain and unknown failures rejected");
            Rejected(delegate { SigningService.CleanPath("  "); });
            Rejected(delegate { SigningService.TimestampUrl("https://example.com/"); });
            Rejected(delegate { SigningService.TimestampUrl("http://user:secret@example.com/"); });
            Require(SigningService.TimestampUrl("") == "", "Optional timestamp rejected.");
            Require(SigningService.TimestampUrl("http://example.com/") == "http://example.com/", "Valid timestamp rejected.");
            checks.Add("empty paths and incompatible timestamp endpoints rejected");
            string mz = Path.Combine(temp, "mz-only.exe"); File.WriteAllBytes(mz, new byte[]{77, 90});
            Rejected(delegate { SigningService.CheckExe(mz); }); checks.Add("truncated MZ executable rejected");
            string hashFile = Path.Combine(temp, "hash.txt"); File.WriteAllText(hashFile, "abc");
            Require(SigningService.Hash(hashFile) == "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", "SHA-256 differs from known vector.");
            checks.Add("SHA-256 known vector");
            string bad = Path.Combine(temp, "bad.exe"); File.WriteAllText(bad, "not an EXE");
            Rejected(delegate { SigningService.CheckExe(bad); }); checks.Add("invalid executable rejected");
            Rejected(delegate { SigningService.NewFile(bad, ".exe"); }); checks.Add("existing output protected");
            Rejected(delegate { SigningService.Create("", "abcdefgh", Path.Combine(temp, "empty.pfx")); }); checks.Add("empty publisher rejected");
            Rejected(delegate { SigningService.Create("Test", "short", Path.Combine(temp, "short.pfx")); }); checks.Add("short creation password rejected");
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                string personalBefore = PersonalStore();
                string secret = "Test'$[Password]123\u00e9";
                string pfx = Path.Combine(temp, "test.pfx");
                var certificate = SigningService.Create("NorthStar Test", secret, pfx); Require(File.Exists(pfx), "PFX missing.");
                checks.Add("Windows certificate creation with punctuation/Unicode password");
                string original = Path.Combine(temp, "original.exe"); File.Copy(Assembly.GetExecutingAssembly().Location, original);
                string hash = Hash(original); string copy = Path.Combine(temp, "[signed]'$ app.exe");
                var signed = SigningService.Sign(original, pfx, secret, copy, Path.Combine(temp, "backups"));
                Require(SigningService.Flag(signed, "signature_present"), "Signature missing.");
                Require(Hash(original) == hash, "Copy signing changed the source.");
                var verify = SigningService.Verify(copy);
                Require(SigningService.Text(verify, "thumbprint") == SigningService.Text(certificate, "thumbprint"), "Wrong signer.");
                Require(SigningService.Text(signed, "native_code") == "0x800B0109", "Expected native untrusted-root status for the isolated self-signed certificate.");
                Require(SigningService.Text(signed, "status") == "NotTrusted", "Untrusted-root status not normalized.");
                checks.Add("Windows signed-copy creation, literal filename handling and untrusted-root regression");
                string before = Hash(original);
                Rejected(delegate { SigningService.Sign(original, pfx, "wrong-password", null, Path.Combine(temp, "backups")); });
                Require(Hash(original) == before, "Wrong password changed the EXE."); checks.Add("wrong password preserves original");
                var inplace = SigningService.Sign(original, pfx, secret, null, Path.Combine(temp, "backups"));
                Require(File.Exists(SigningService.Text(inplace, "backup")), "Backup missing.");
                Require(Hash(SigningService.Text(inplace, "backup")) == before, "Backup differs from original.");
                Require(SigningService.Flag(SigningService.Verify(original), "signature_present"), "In-place signature missing.");
                checks.Add("Windows in-place signing and intact backup");
                Require(SigningService.Text(inplace, "sha256") == SigningService.Hash(original), "Signed output checksum mismatch.");
                Rejected(delegate { SigningService.Sign(original, pfx, secret, copy, null); });
                checks.Add("signed output checksum and existing copy protection");
                byte[] bytes = File.ReadAllBytes(copy); bytes[bytes.Length / 2] ^= 1; File.WriteAllBytes(copy, bytes);
                Require(SigningService.Text(SigningService.Verify(copy), "status") == "HashMismatch", "Tampering was not detected as a hash mismatch.");
                Require(!NativeTrust.AcceptableForSigning(NativeTrust.Verify(copy)), "Native verification accepted tampered bytes.");
                checks.Add("tampered executable rejected by PowerShell and native verification");
                Require(PersonalStore() == personalBefore, "Personal certificate store changed after operations.");
                checks.Add("Personal certificate store unchanged after normal operations");
                report["windows_signing_executed"] = true;
            } else { report["windows_signing_executed"] = false; }
            report["passed"] = true;
        } catch (Exception error) { report["passed"] = false; report["error"] = error.Message; exit = 1; }
        finally {
            report["checks"] = checks;
            File.WriteAllText(Path.GetFullPath(path), new JavaScriptSerializer().Serialize(report));
            try { Directory.Delete(temp, true); } catch { }
        }
        return exit;
    }
}
}
