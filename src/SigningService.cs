using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace NorthStarCodeSigner {
public static class SigningService {
    
    public static string Text(IDictionary<string, object> value, string key) {
        object found; return value.TryGetValue(key, out found) && found != null ? found.ToString() : "";
    }
    public static bool Flag(IDictionary<string, object> value, string key) {
        object found; return value.TryGetValue(key, out found) && found is bool && (bool)found;
    }
    public static string CleanPath(string value) {
        if (String.IsNullOrWhiteSpace(value)) throw new ArgumentException("Choose a file path first.");
        return Path.GetFullPath(value.Trim().Trim('"'));
    }
    static Dictionary<string, object> Invoke(string operation, Dictionary<string, object> data) {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            throw new InvalidOperationException("Certificate creation and signing require Windows.");
        string directory = Path.Combine(Path.GetTempPath(), "NorthStarSigner_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try {
            string script = Path.Combine(directory, "worker.ps1");
            using (Stream source = Assembly.GetExecutingAssembly().GetManifestResourceStream("worker.ps1")) {
                if (source == null) throw new InvalidOperationException("The signing helper is missing.");
                using (FileStream output = File.Create(script)) source.CopyTo(output);
            }
            string shell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                                        @"System32\WindowsPowerShell\v1.0\powershell.exe");
            if (!File.Exists(shell)) throw new FileNotFoundException("Windows PowerShell was not found.");
            data["operation"] = operation;
            data["assembly"] = Assembly.GetExecutingAssembly().Location;
            ProcessStartInfo start = new ProcessStartInfo(shell,
                "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"" + script + "\"");
            start.UseShellExecute = false; start.CreateNoWindow = true;
            start.RedirectStandardInput = true; start.RedirectStandardOutput = true; start.RedirectStandardError = true;
            start.StandardOutputEncoding = Encoding.UTF8; start.StandardErrorEncoding = Encoding.UTF8;
            using (Process process = Process.Start(start)) {
                Task<string> stdout = process.StandardOutput.ReadToEndAsync();
                Task<string> stderr = process.StandardError.ReadToEndAsync();
                byte[] request = Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(data));
                process.StandardInput.BaseStream.Write(request, 0, request.Length);
                process.StandardInput.BaseStream.Flush(); process.StandardInput.Close();
                Array.Clear(request, 0, request.Length);
                if (!process.WaitForExit(120000)) {
                    process.Kill(); process.WaitForExit();
                    throw new TimeoutException("Windows signing timed out. The original EXE was not replaced.");
                }
                Task.WaitAll(stdout, stderr);
                Dictionary<string, object> result;
                try { result = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(stdout.Result.Trim().TrimStart('\uFEFF')); }
                catch { throw new InvalidOperationException("Windows returned an unexpected result. Check Windows PowerShell and the PKI module."); }
                if (process.ExitCode != 0 || !Flag(result, "ok")) {
                    string message = Text(result, "error"); string password = Text(data, "password");
                    if (password.Length > 0) message = message.Replace(password, "[password removed]");
                    throw new InvalidOperationException(message.Length > 0 ? message : "Windows signing failed.");
                }
                return result;
            }
        } finally { try { Directory.Delete(directory, true); } catch { } }
    }
    public static string NewFile(string path, string extension) {
        string full = CleanPath(path);
        if (!String.Equals(Path.GetExtension(full), extension, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Choose a " + extension + " filename.");
        if (File.Exists(full) || Directory.Exists(full)) throw new IOException("That output already exists. Choose a new filename.");
        if (!Directory.Exists(Path.GetDirectoryName(full))) throw new DirectoryNotFoundException("Choose an existing output folder.");
        return full;
    }
    public static string CheckExe(string path) {
        string full = CleanPath(path);
        if (!File.Exists(full) || !String.Equals(Path.GetExtension(full), ".exe", StringComparison.OrdinalIgnoreCase))
            throw new FileNotFoundException("Choose an existing Windows EXE or setup EXE.");
        using (FileStream stream = File.OpenRead(full)) {
            if (stream.Length < 64 || stream.ReadByte() != 'M' || stream.ReadByte() != 'Z') throw new ArgumentException("This is not a Windows executable.");
            stream.Position = 60;
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true)) {
                int offset = reader.ReadInt32();
                if (offset < 64 || offset > stream.Length - 24) throw new ArgumentException("The executable has an invalid PE header.");
                stream.Position = offset;
                if (reader.ReadUInt32() != 0x00004550) throw new ArgumentException("The executable has no valid PE signature.");
            }
        }
        return full;
    }
    public static string Hash(string path) {
        using (SHA256 sha = SHA256.Create()) using (FileStream stream = File.OpenRead(path))
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }
    public static string TimestampUrl(string value) {
        if (String.IsNullOrWhiteSpace(value)) return "";
        Uri uri;
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out uri) || uri.Scheme != "http" ||
            uri.UserInfo.Length > 0 || uri.Fragment.Length > 0 || String.IsNullOrWhiteSpace(uri.Host))
            throw new ArgumentException("Use your provider's http:// Authenticode timestamp URL. This backend does not support HTTPS or RFC 3161-only endpoints.");
        return uri.AbsoluteUri;
    }
    public static string Report(Dictionary<string, object> result, string operation) {
        var report = new StringBuilder("NorthStar Code Signer 2.0.1\r\nOperation: " + operation + "\r\nUTC: " + DateTime.UtcNow.ToString("o") + "\r\n");
        string[,] fields = {{"path","File"},{"subject","Publisher"},{"thumbprint","Certificate thumbprint"},{"expires","Certificate expires"},
            {"status","Signature result"},{"windows_status","PowerShell status"},{"native_code","Windows trust code"},{"trusted","Valid on this PC"},{"timestamped","Timestamp present"},{"timestamp_subject","Timestamp authority"},
            {"sha256","SHA-256"},{"backup","Original backup"},{"message","Windows detail"}};
        for (int i = 0; i < fields.GetLength(0); i++) if (result.ContainsKey(fields[i,0])) report.Append(fields[i,1] + ": " + Text(result, fields[i,0]) + "\r\n");
        report.Append("\r\nSelf-signed certificates do not establish public trust. Trust results apply to this PC at the time of checking.\r\n");
        return report.ToString();
    }
    public static Dictionary<string, object> Create(string publisher, string password, string path) {
        publisher = publisher.Trim();
        if (publisher.Length < 1 || publisher.Length > 100 || publisher.IndexOfAny(new char[]{',','=','\r','\n'}) >= 0)
            throw new ArgumentException("Use a publisher name of 1–100 characters without commas, equals signs or line breaks.");
        foreach (char c in publisher) if (Char.IsControl(c)) throw new ArgumentException("Publisher names cannot contain control characters.");
        if (String.IsNullOrEmpty(password) || password.Length < 8) throw new ArgumentException("Use at least 8 characters for the new PFX password.");
        string output = NewFile(path, ".pfx");
        string temp = Path.Combine(Path.GetTempPath(), "NorthStarPfx_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try {
            string staging = Path.Combine(temp, "certificate.pfx");
            var result = Invoke("create", new Dictionary<string, object>{{"publisher", publisher}, {"password", password}, {"pfx", staging}});
            File.Copy(staging, output, false); result["path"] = output; return result;
        } finally { try { Directory.Delete(temp, true); } catch { } }
    }
    public static Dictionary<string, object> Sign(string path, string pfx, string password, string copyPath, string backupRoot, string timestampServer = "") {
        string input = CheckExe(path); pfx = CleanPath(pfx);
        if (!File.Exists(pfx) || !String.Equals(Path.GetExtension(pfx), ".pfx", StringComparison.OrdinalIgnoreCase))
            throw new FileNotFoundException("Choose an existing PFX certificate.");
        if (String.IsNullOrEmpty(password)) throw new ArgumentException("Enter the PFX password.");
        timestampServer = TimestampUrl(timestampServer);
        bool copy = !String.IsNullOrWhiteSpace(copyPath);
        string output = copy ? NewFile(copyPath, ".exe") : input;
        string staging = Path.Combine(Path.GetDirectoryName(output), ".northstar_" + Guid.NewGuid().ToString("N") + ".exe");
        try {
            File.Copy(input, staging, false);
            string originalHash = Hash(staging);
            var result = Invoke("sign", new Dictionary<string, object>{{"target", staging}, {"pfx", pfx}, {"password", password}, {"timestamp", timestampServer}});
            if (!Flag(result, "signature_present")) throw new InvalidOperationException("Windows did not confirm a signature.");
            result["sha256"] = Hash(staging);
            if (copy) File.Move(staging, output);
            else {
                if (String.IsNullOrEmpty(backupRoot)) backupRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NorthStar Code Signer", "Backups");
                Directory.CreateDirectory(backupRoot);
                string backup = Path.Combine(backupRoot, Guid.NewGuid().ToString("N") + "_" + Path.GetFileName(input));
                File.Copy(input, backup, false);
                if (Hash(backup) != originalHash || Hash(input) != originalHash)
                    throw new IOException("The original EXE changed while signing. It was not replaced. Please rebuild or close the other program and try again.");
                result["backup"] = backup;
                File.Replace(staging, input, null);
            }
            result["path"] = output; return result;
        } finally { try { if (File.Exists(staging)) File.Delete(staging); } catch { } }
    }
    public static Dictionary<string, object> Verify(string path) {
        string full = CheckExe(path);
        using (var guard = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read)) {
            var result = Invoke("verify", new Dictionary<string, object>{{"target", full}});
            result["path"] = full; result["sha256"] = Hash(full); return result;
        }
    }
}
}
