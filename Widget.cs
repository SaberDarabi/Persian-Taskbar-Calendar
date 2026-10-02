using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;

public sealed class PersianDateWidget : Form
{
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern IntPtr FindWindow(string className, string title);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);
    [StructLayout(LayoutKind.Sequential)] struct NativeRect { public int Left, Top, Right, Bottom; }
    void RaiseOverlay() {
        if (IsHandleCreated && Visible) SetWindowPos(Handle, new IntPtr(-1), 0, 0, 0, 0, 0x0013);
    }
    protected override void WndProc(ref Message m) {
        if (m.Msg == 0x0021) { m.Result = new IntPtr(3); return; }
        base.WndProc(ref m);
    }
    readonly PersianCalendar calendar = new PersianCalendar();
    readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
    readonly ToolTip tip = new ToolTip();
    readonly string config = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PersianDateCompact", "position-v2.txt");
    string dateText = "";
    bool dragging, customPosition;
    Point mouseOrigin, windowOrigin;
    protected override bool ShowWithoutActivation { get { return true; } }
    protected override CreateParams CreateParams {
        get { CreateParams cp = base.CreateParams; cp.ExStyle |= 0x08000000; return cp; }
    }
    public PersianDateWidget()
    {
        Text = "Persian Date Compact";
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        Font = new Font("Tahoma", 10.5f, FontStyle.Regular);
        BackColor = Color.Magenta;
        TransparencyKey = Color.Magenta;
        ForeColor = Color.Black;
        DoubleBuffered = true;
        Padding = Padding.Empty;
        var menu = new ContextMenuStrip();
        menu.Items.Add("Move to bottom-right", null, delegate { customPosition = false; DockRight(); SavePosition(); });
        menu.Items.Add("Black / white text", null, delegate {
            ForeColor = ForeColor == Color.Black ? Color.White : Color.Black;
            Invalidate();
        });
        menu.Items.Add("Current font", null, delegate {
            MessageBox.Show("Current font: " + Font.Name + "\nTahoma is used for a formal, readable Persian date.", "Widget font");
        });
        menu.Items.Add("Exit", null, delegate { Close(); });
        ContextMenuStrip = menu;
        UpdateDate();
        LoadPosition();
        MouseDown += delegate(object s, MouseEventArgs e) {
            if (e.Button != MouseButtons.Left) return;
            dragging = true; mouseOrigin = Cursor.Position; windowOrigin = Location; Capture = true;
        };
        MouseMove += delegate {
            if (!dragging) return;
            Point now = Cursor.Position;
            Location = new Point(windowOrigin.X + now.X - mouseOrigin.X, windowOrigin.Y + now.Y - mouseOrigin.Y);
            RaiseOverlay();
        };
        MouseUp += delegate(object s, MouseEventArgs e) {
            if (!dragging) return;
            dragging = false; Capture = false; customPosition = true; ClampPosition(); SavePosition(); RaiseOverlay();
        };
        timer.Interval = 250;
        timer.Tick += delegate { UpdateDate(); if (!dragging) { if (!customPosition) DockRight(); else ClampPosition(); } RaiseOverlay(); };
        timer.Start();
        FormClosed += delegate { timer.Stop(); timer.Dispose(); tip.Dispose(); menu.Dispose(); };
    }
    void UpdateDate()
    {
        DateTime now = DateTime.Now;
        string[] days = new string[] { "\u06cc\u06a9\u0634\u0646\u0628\u0647", "\u062f\u0648\u0634\u0646\u0628\u0647", "\u0633\u0647\u200c\u0634\u0646\u0628\u0647", "\u0686\u0647\u0627\u0631\u0634\u0646\u0628\u0647", "\u067e\u0646\u062c\u0634\u0646\u0628\u0647", "\u062c\u0645\u0639\u0647", "\u0634\u0646\u0628\u0647" };
        string[] months = new string[] { "\u0641\u0631\u0648\u0631\u062f\u06cc\u0646", "\u0627\u0631\u062f\u06cc\u0628\u0647\u0634\u062a", "\u062e\u0631\u062f\u0627\u062f", "\u062a\u06cc\u0631", "\u0645\u0631\u062f\u0627\u062f", "\u0634\u0647\u0631\u06cc\u0648\u0631", "\u0645\u0647\u0631", "\u0622\u0628\u0627\u0646", "\u0622\u0630\u0631", "\u062f\u06cc", "\u0628\u0647\u0645\u0646", "\u0627\u0633\u0641\u0646\u062f" };
        string next = String.Format(CultureInfo.InvariantCulture, "{0}\u060c {1} {2} {3}", days[(int)now.DayOfWeek], calendar.GetDayOfMonth(now), months[calendar.GetMonth(now)-1], calendar.GetYear(now));
        if (next == dateText) return;
        dateText = next;
        Size measured = TextRenderer.MeasureText(dateText, Font, Size.Empty, TextFormatFlags.NoPadding);
        ClientSize = new Size(measured.Width + 16, measured.Height + 8);
        tip.SetToolTip(this, "Persian: " + dateText + "\nGregorian: " + now.ToString("dddd, dd MMMM yyyy", CultureInfo.InvariantCulture) + "\nDrag to move. Right-click for options.");
        Invalidate();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        // Binary glyph edges avoid a colored halo against the transparency key.
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
        using (var brush = new SolidBrush(ForeColor))
        using (var format = new StringFormat(StringFormatFlags.DirectionRightToLeft | StringFormatFlags.NoWrap)) {
            format.Alignment = StringAlignment.Center;
            format.LineAlignment = StringAlignment.Center;
            e.Graphics.DrawString(dateText, Font, brush, new RectangleF(0, 0, ClientSize.Width, ClientSize.Height), format);
        }
    }
    void DockRight()
    {
        Rectangle screen = Screen.PrimaryScreen.Bounds;
        NativeRect bar;
        IntPtr handle = FindWindow("Shell_TrayWnd", null);
        if (handle != IntPtr.Zero && GetWindowRect(handle, out bar) && bar.Bottom - bar.Top < 150 && bar.Right - bar.Left > 300) {
            // Leave space for the Windows clock and tray icons; dragging remains available.
            int x = Math.Max(bar.Left + 4, bar.Right - Width - 380);
            Location = new Point(x, bar.Top + Math.Max(0, (bar.Bottom - bar.Top - Height) / 2));
        } else {
            Rectangle r = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(r.Right - Width - 4, r.Bottom - Height - 4);
        }
    }
    void ClampPosition()
    {
        Rectangle r = Screen.FromRectangle(Bounds).Bounds;
        Location = new Point(Math.Max(r.Left, Math.Min(Left, r.Right - Width)), Math.Max(r.Top, Math.Min(Top, r.Bottom - Height)));
    }
    void LoadPosition()
    {
        DockRight();
        try {
            string[] values = File.ReadAllText(config).Split(',');
            int x, y;
            if (values.Length == 2 && Int32.TryParse(values[0], out x) && Int32.TryParse(values[1], out y)) {
                Location = new Point(x,y); customPosition = true; ClampPosition();
            }
        } catch (IOException) {} catch (UnauthorizedAccessException) {}
    }
    void SavePosition()
    {
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(config));
            if (customPosition) File.WriteAllText(config, Left.ToString(CultureInfo.InvariantCulture) + "," + Top.ToString(CultureInfo.InvariantCulture));
            else if (File.Exists(config)) File.Delete(config);
        } catch (IOException) {} catch (UnauthorizedAccessException) {}
    }
    [STAThread]
    public static void Main() { Run(); }
    public static void Run()
    {
        bool created;
        using (var mutex = new Mutex(true, "Local\\PersianDateCompactWidgetV1", out created)) {
            if (!created) return;
            try {
                SetProcessDPIAware();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                using (var form = new PersianDateWidget()) Application.Run(form);
            } finally { mutex.ReleaseMutex(); }
        }
    }
}
