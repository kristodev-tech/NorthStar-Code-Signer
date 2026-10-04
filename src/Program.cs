using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

[assembly: AssemblyTitle("NorthStar Code Signer")]
[assembly: AssemblyCompany("NorthStar Software Works")]
[assembly: AssemblyProduct("NorthStar Code Signer")]
[assembly: AssemblyVersion("2.0.1.0")]
[assembly: AssemblyFileVersion("2.0.1.0")]
[assembly: AssemblyCopyright("Copyright 2026 NorthStar Software Works")]

namespace NorthStarCodeSigner {
public class SignerForm : Form {
    readonly Color ink = Color.FromArgb(25, 39, 61), accent = Color.FromArgb(31, 91, 164);
    readonly TabControl tabs = new TabControl();
    readonly TextBox publisher = new TextBox(), newPfx = new TextBox(), newPassword = new TextBox(), confirm = new TextBox();
    readonly TextBox pfx = new TextBox(), password = new TextBox(), target = new TextBox(), copyPath = new TextBox();
    readonly TextBox timestamp = new TextBox(), verifyPath = new TextBox(), details = new TextBox();
    readonly CheckBox copyMode = new CheckBox(), useTimestamp = new CheckBox();
    readonly Label status = new Label(); readonly ProgressBar progress = new ProgressBar();
    Button taskButton;
    readonly FlowLayoutPanel reportActions = new FlowLayoutPanel();
    bool busy; string lastFile = ""; string report = "";
    public SignerForm() {
        Text = "NorthStar Code Signer  |  2.0.1"; ClientSize = new Size(960, 850); MinimumSize = new Size(720, 600);
        StartPosition = FormStartPosition.CenterScreen; AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 10F); ForeColor = ink; BackColor = Color.FromArgb(241, 244, 249);
        using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("northstar.ico")) {
            if (stream != null) Icon = new Icon(stream, 48, 48);
        }
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), ColumnCount = 1, RowCount = 5 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 94));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 5));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 155)); Controls.Add(layout);
        var header = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = ink, Padding = new Padding(16), ColumnCount = 2, RowCount = 1 };
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 62)); header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.Controls.Add(new PictureBox { Image = Icon.ToBitmap(), SizeMode = PictureBoxSizeMode.Zoom, Dock = DockStyle.Fill }, 0, 0);
        var headingText = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        headingText.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        headingText.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); headingText.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        headingText.Controls.Add(new Label { Text = "NorthStar Code Signer", Dock = DockStyle.Fill, ForeColor = Color.White,
            Font = new Font("Segoe UI", 17F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        headingText.Controls.Add(new Label { Text = "NORTHSTAR SOFTWARE WORKS  /  Sign. Verify. Release.", Dock = DockStyle.Fill, ForeColor = Color.FromArgb(191, 209, 232),
            Font = new Font("Segoe UI", 9F), TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        header.Controls.Add(headingText, 1, 0);
        layout.Controls.Add(header, 0, 0);
        tabs.Dock = DockStyle.Fill; tabs.Padding = new Point(16, 10); layout.Controls.Add(tabs, 0, 1);
        var signing = Page("Sign application"); var create = Page("Create certificate"); var verify = Page("Verify file"); var help = Page("Guide / About");
        Title(signing, "Sign your Windows release", "Choose a code-signing PFX and an EXE. Processing runs in the background.");
        Field(signing, "Certificate (.pfx)", pfx, delegate { OpenPath(pfx, "PFX certificate|*.pfx"); });
        password.UseSystemPasswordChar = true;
        Field(signing, "Certificate password", password, null);
        Field(signing, "Application or installer (.exe)", target, delegate { OpenPath(target, "Windows executable|*.exe"); });
        target.TextChanged += delegate { SuggestCopy(); };
        copyMode.Text = "Create a signed copy (keep the original unchanged)"; copyMode.AutoSize = true; copyMode.Checked = true;
        Add(signing, copyMode);
        Field(signing, "New signed-copy filename", copyPath, delegate { if (copyMode.Checked) SavePath(copyPath, "Windows executable|*.exe", "exe"); });
        copyMode.CheckedChanged += delegate { copyPath.Enabled = copyMode.Checked; };
        useTimestamp.Text = "Require a timestamp from my provider"; useTimestamp.AutoSize = true; Add(signing, useTimestamp);
        Field(signing, "Authenticode timestamp URL (http://)", timestamp, null); timestamp.Enabled = false;
        useTimestamp.CheckedChanged += delegate { timestamp.Enabled = useTimestamp.Checked; };
        Note(signing, "Timestamping requires an online, legacy Authenticode-compatible endpoint. If requested and missing, the output is not committed. For RFC 3161, hardware tokens or cloud keys, use your provider's tools.");
        Note(signing, "Self-signed certificates do not establish customer trust. In-place signing saves a backup and replaces the existing signature.");
        Actions(signing, "Clear password", delegate { password.Clear(); }, null, null);
        Title(create, "Create a reusable certificate", "For development and private distribution. RSA 3072 / SHA-256 / 3-year validity.");
        publisher.Text = "NorthStar Software Works";
        Field(create, "Publisher name", publisher, null);
        Field(create, "New certificate filename (.pfx)", newPfx, delegate { SavePath(newPfx, "PFX certificate|*.pfx", "pfx"); });
        newPassword.UseSystemPasswordChar = true; confirm.UseSystemPasswordChar = true;
        Field(create, "Password (minimum 8 characters)", newPassword, null); Field(create, "Confirm password", confirm, null);
        Note(create, "Store your certificate securely outside your repository and release folders. Never share the PFX or password. The app will not install this certificate into Windows trust stores.");
        
        Title(verify, "Inspect a signed or unsigned file", "Verification is independent of signing. No certificate password is needed.");
        Field(verify, "File to verify (.exe)", verifyPath, delegate { OpenPath(verifyPath, "Windows executable|*.exe"); });
        Note(verify, "Windows trust depends on this computer's certificate stores and policies. SHA-256 describes the exact file bytes. Verify again after any change to a release.");
        Actions(verify, "Use last checked file", delegate { verifyPath.Text = lastFile; }, null, null);
        Title(help, "NorthStar Code Signer 2.0.1", "Free and open source under the MIT License. No account or subscription.");
        Note(help, "RELEASE WORKFLOW\n1. Build your app EXE.\n2. Sign the app EXE.\n3. Build the installer containing that signed app.\n4. Sign the completed setup EXE.\n5. Verify the final files and export their SHA-256 checksums.");
        Note(help, "TRUST & RECOVERY\nSelf-signed signatures will usually show NotTrusted. This is different from an unsigned or modified file. A valid signature on this PC does not guarantee public trust or remove SmartScreen warnings. Backups are stored in your local AppData folder. Restore only a backup you recognize; retain a copy of the signed release first.");
        Note(help, "PRIVACY\nPasswords are masked, cleared after use, and sent through standard input to the worker. They are not stored in reports or settings. Managed strings cannot be reliably erased from memory. Timestamping and Windows chain/revocation checks may contact external services.");
        Actions(help, "Open backups", delegate { Directory.CreateDirectory(BackupRoot); Process.Start(BackupRoot); }, "View license", delegate { ShowLicense(false); });
        var footer = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(0, 7, 0, 7) };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        status.AutoSize = true; status.Anchor = AnchorStyles.Left; status.Text = "Ready  /  Choose a file or create a certificate.";
        footer.Controls.Add(status, 0, 0);
        taskButton = Button("Sign application", delegate { if (tabs.SelectedIndex == 0) Sign(); else if (tabs.SelectedIndex == 1) CreateCertificate(); else if (tabs.SelectedIndex == 2) Verify(); else ShowLicense(false); }, true);
        footer.Controls.Add(taskButton, 1, 0);
        footer.SizeChanged += delegate { status.MaximumSize = new Size(Math.Max(140, footer.Width - taskButton.Width - 24), 0); };
        tabs.SelectedIndexChanged += delegate { taskButton.Text = new[] { "Sign application", "Create certificate", "Verify file", "View license" }[tabs.SelectedIndex]; };
        layout.Controls.Add(footer, 0, 2); progress.Dock = DockStyle.Fill; layout.Controls.Add(progress, 0, 3);
        var resultPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(0, 10, 0, 0) };
        resultPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); resultPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        reportActions.AutoSize = true; reportActions.Dock = DockStyle.Top;
        reportActions.Controls.Add(Button("Copy report", delegate { if (report.Length > 0) Clipboard.SetText(report); }, false));
        reportActions.Controls.Add(Button("Save report", delegate { Export(false); }, false));
        reportActions.Controls.Add(Button("Save SHA-256", delegate { Export(true); }, false));
        reportActions.Enabled = false; resultPanel.Controls.Add(reportActions, 0, 0);
        details.Dock = DockStyle.Fill; details.Multiline = true; details.ReadOnly = true; details.ScrollBars = ScrollBars.Vertical;
        details.Text = "Signing and verification results appear here."; details.BackColor = Color.White; details.Font = new Font("Consolas", 9F); resultPanel.Controls.Add(details, 0, 1);
        layout.Controls.Add(resultPanel, 0, 4);
        FormClosing += delegate(object sender, FormClosingEventArgs e) { if (busy) { e.Cancel = true; status.Text = "Finishing the operation. Please wait before closing."; } };
    }
    static string BackupRoot { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NorthStar Code Signer", "Backups"); } }
    void SuggestCopy() {
        try { if (!String.IsNullOrWhiteSpace(target.Text)) { string path = SigningService.CleanPath(target.Text); copyPath.Text = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "_signed.exe"); } } catch { }
    }
    TableLayoutPanel Page(string caption) {
        var page = new TabPage(caption) { AutoScroll = true, Padding = new Padding(20), BackColor = Color.White };
        tabs.TabPages.Add(page);
        var table = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1 };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); page.Controls.Add(table);
        table.SizeChanged += delegate { page.AutoScrollMinSize = new Size(0, table.Height + page.Padding.Vertical); };
        page.SizeChanged += delegate { page.AutoScrollMinSize = new Size(0, table.Height + page.Padding.Vertical); };
        return table;
    }
    void Add(TableLayoutPanel table, Control control) {
        int row = table.RowCount++; table.RowStyles.Add(new RowStyle(SizeType.AutoSize)); control.Margin = new Padding(0, 3, 0, 7); table.Controls.Add(control, 0, row);
    }
    void Title(TableLayoutPanel table, string title, string description) {
        Add(table, new Label { Text = title, AutoSize = true, Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = ink }); Note(table, description);
    }
    void Note(TableLayoutPanel table, string caption) {
        var label = new Label { Text = caption, AutoSize = true, ForeColor = Color.FromArgb(79, 94, 115), MaximumSize = new Size(800, 0) };
        Add(table, label); table.SizeChanged += delegate { label.MaximumSize = new Size(Math.Max(180, table.Width - 12), 0); };
    }
    void Field(TableLayoutPanel table, string caption, TextBox box, Action browse) {
        var label = new Label { Text = caption, AutoSize = true }; Add(table, label);
        var row = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = browse == null ? 1 : 2 };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); if (browse != null) row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        box.Dock = DockStyle.Fill; box.Margin = new Padding(0, 4, 8, 4); box.AccessibleName = caption; label.TabIndex = 0; box.TabIndex = 1; row.Controls.Add(box, 0, 0);
        if (browse != null) row.Controls.Add(Button("Browse...", browse, false), 1, 0); Add(table, row);
    }
    Button Button(string caption, Action action, bool primary) {
        var button = new Button { Text = caption, AutoSize = true, Padding = new Padding(10, 5, 10, 5), FlatStyle = FlatStyle.Flat,
            BackColor = primary ? accent : Color.FromArgb(240, 244, 249), ForeColor = primary ? Color.White : ink, Cursor = Cursors.Hand };
        button.FlatAppearance.BorderSize = 0; button.Click += delegate { try { action(); } catch (Exception e) { ShowError(e.Message); } }; return button;
    }
    void Actions(TableLayoutPanel table, string caption, Action action, string second, Action secondAction) {
        var row = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, WrapContents = true };
        row.Controls.Add(Button(caption, action, true)); if (secondAction != null) row.Controls.Add(Button(second, secondAction, false)); Add(table, row);
    }
    void OpenPath(TextBox box, string filter) { using (var dialog = new OpenFileDialog { Filter = filter }) if (dialog.ShowDialog(this) == DialogResult.OK) box.Text = dialog.FileName; }
    void SavePath(TextBox box, string filter, string extension) { using (var dialog = new SaveFileDialog { Filter = filter, DefaultExt = extension, OverwritePrompt = false }) if (dialog.ShowDialog(this) == DialogResult.OK) box.Text = dialog.FileName; }
    void CreateCertificate() {
        if (newPassword.Text != confirm.Text) throw new ArgumentException("Passwords do not match.");
        string name = publisher.Text, secret = newPassword.Text, path = newPfx.Text; newPassword.Clear(); confirm.Clear();
        Run("create", delegate { return SigningService.Create(name, secret, path); });
    }
    void Sign() {
        if (!copyMode.Checked && MessageBox.Show(this, "Replace the selected EXE's signature? An original-file backup will be saved first.", "Sign in place", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        if (copyMode.Checked && String.IsNullOrWhiteSpace(copyPath.Text)) throw new ArgumentException("Choose a new signed-copy filename.");
        string file = target.Text, cert = pfx.Text, secret = password.Text, output = copyMode.Checked ? copyPath.Text : null;
        string server = useTimestamp.Checked ? timestamp.Text : "";
        if (useTimestamp.Checked && String.IsNullOrWhiteSpace(server)) throw new ArgumentException("Enter your provider's timestamp URL.");
        password.Clear(); Run("sign", delegate { return SigningService.Sign(file, cert, secret, output, null, server); });
    }
    void Verify() { string file = verifyPath.Text; Run("verify", delegate { return SigningService.Verify(file); }); }
    void ShowError(string error) { status.Text = "Operation did not complete."; status.ForeColor = Color.Firebrick; MessageBox.Show(this, error, "NorthStar Code Signer", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    void Run(string operation, Func<Dictionary<string, object>> work) {
        if (busy) return; busy = true; tabs.Enabled = false; taskButton.Enabled = false; reportActions.Enabled = false; report = ""; details.Clear(); lastFile = "";
        progress.Style = ProgressBarStyle.Marquee; status.ForeColor = ink; status.Text = "Working  /  Please wait. File operations may take up to two minutes.";
        Task.Run(delegate {
            Dictionary<string, object> result = null; string error = null;
            try { result = work(); } catch (Exception e) { error = e.Message; }
            BeginInvoke((Action)delegate {
                busy = false; tabs.Enabled = true; taskButton.Enabled = true; progress.Style = ProgressBarStyle.Blocks;
                if (error != null) { details.Text = error; ShowError(error); return; }
                if (operation == "create") {
                    pfx.Text = SigningService.Text(result, "path"); tabs.SelectedIndex = 0;
                    status.Text = "Certificate created  /  Enter its password to sign an application.";
                } else {
                    lastFile = SigningService.Text(result, "path"); verifyPath.Text = lastFile;
                    bool trusted = SigningService.Flag(result, "trusted");
                    status.Text = (operation == "sign" ? "Signed  /  " : "Verification  /  ") + SigningService.Text(result, "status") + (trusted ? " / Valid on this PC" : " / Windows trust not confirmed");
                    status.ForeColor = trusted ? Color.FromArgb(22, 112, 73) : Color.FromArgb(151, 91, 9);
                }
                report = SigningService.Report(result, operation); details.Text = report; reportActions.Enabled = true;
            });
        });
    }
    void Export(bool checksum) {
        if (report.Length == 0) return;
        if (checksum && lastFile.Length == 0) throw new InvalidOperationException("Sign or verify an EXE first.");
        using (var dialog = new SaveFileDialog { Filter = "Text file|*.txt", DefaultExt = "txt", FileName = checksum ? Path.GetFileName(lastFile) + ".sha256.txt" : "NorthStar_Signing_Report.txt" }) {
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            string output = SigningService.CleanPath(dialog.FileName);
            if (!String.Equals(Path.GetExtension(output), ".txt", StringComparison.OrdinalIgnoreCase)) throw new IOException("Reports must use a .txt filename.");
            if (String.Equals(output, lastFile, StringComparison.OrdinalIgnoreCase) || String.Equals(output, SigningService.CleanPath(pfx.Text.Length > 0 ? pfx.Text : "unused.pfx"), StringComparison.OrdinalIgnoreCase))
                throw new IOException("Choose a report filename different from the EXE and certificate.");
            // Export the hash captured at verification, rather than silently hashing changed bytes.
            string content = checksum ? lastHash + "  " + Path.GetFileName(lastFile) + "\r\n" : report;
            File.WriteAllText(output, content, new System.Text.UTF8Encoding(false));
        }
    }
    string lastHash { get { foreach (string line in report.Split('\n')) if (line.StartsWith("SHA-256: ")) return line.Substring(9).Trim(); throw new InvalidOperationException("No verified checksum is available."); } }
    public bool ShowLicense(bool requireAcceptance) {
        using (var dialog = new Form { Text = "NorthStar Code Signer - MIT License", Size = new Size(720, 540), MinimumSize = new Size(500, 350), StartPosition = FormStartPosition.CenterParent }) {
            var text = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F) };
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LICENSE")) using (var reader = new StreamReader(stream)) text.Text = reader.ReadToEnd();
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.RightToLeft };
            var accept = new Button { Text = requireAcceptance ? "I accept" : "Close", AutoSize = true, DialogResult = DialogResult.OK };
            buttons.Controls.Add(accept); if (requireAcceptance) buttons.Controls.Add(new Button { Text = "Decline", AutoSize = true, DialogResult = DialogResult.Cancel });
            dialog.Controls.Add(text); dialog.Controls.Add(buttons); dialog.AcceptButton = accept; return dialog.ShowDialog(this) == DialogResult.OK;
        }
    }
    public void CaptureScreenshot(string path) { Show(); PerformLayout(); Refresh(); Application.DoEvents(); using (var bitmap = new Bitmap(ClientSize.Width, ClientSize.Height)) { using (var graphics = Graphics.FromImage(bitmap)) graphics.CopyFromScreen(PointToScreen(Point.Empty), Point.Empty, ClientSize); bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png); } }
    public void CaptureAll(string folder) {
        Directory.CreateDirectory(folder);
        foreach (Size size in new[] { new Size(960, 850), new Size(720, 600) }) {
            Size = size;
            for (int i = 0; i < tabs.TabCount; i++) { tabs.SelectedIndex = i; tabs.SelectedTab.AutoScrollPosition = Point.Empty;
                CaptureScreenshot(Path.Combine(folder, "page_" + i + "_" + size.Width + ".png"));
                taskButton.Focus();
                tabs.SelectedTab.AutoScrollPosition = new Point(0, 10000);
                CaptureScreenshot(Path.Combine(folder, "page_" + i + "_" + size.Width + "_bottom.png"));
                var page = tabs.SelectedTab;
                using (var writer = new StreamWriter(Path.Combine(folder, "layout_" + i + "_" + size.Width + ".txt"))) {
                    writer.WriteLine("Scroll: " + page.AutoScrollPosition + " Max: " + page.VerticalScroll.Maximum + " Page: " + page.ClientSize);
                    foreach (Control panel in page.Controls) { writer.WriteLine("Panel " + panel.Bounds + " Preferred: " + panel.PreferredSize); foreach (Control child in panel.Controls) writer.WriteLine(child.GetType().Name + " " + child.Text + " " + child.Bounds); }
                } }
        }
    }
}
public static class Program {
    [STAThread] public static int Main(string[] args) {
        if (args.Length == 2 && args[0] == "--self-test") return SelfTest.Run(args[1]);
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        try {
            using (var form = new SignerForm()) {
                if (args.Length == 2 && args[0] == "--ui-smoke-test") { form.CaptureScreenshot(args[1]); return 0; }
                if (args.Length == 2 && args[0] == "--ui-check-all") { form.CaptureAll(args[1]); return 0; }
                string settings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NorthStar Code Signer", "license-2.0.accepted");
                if (!File.Exists(settings)) { if (!form.ShowLicense(true)) return 0; Directory.CreateDirectory(Path.GetDirectoryName(settings)); File.WriteAllText(settings, DateTime.UtcNow.ToString("o")); }
                Application.Run(form);
            }
            return 0;
        } catch (Exception error) {
            if (args.Length > 0 && args[0].StartsWith("--ui-")) Console.Error.WriteLine(error.ToString());
            else MessageBox.Show(error.Message, "NorthStar Code Signer");
            return 1;
        }
    }
}
}
