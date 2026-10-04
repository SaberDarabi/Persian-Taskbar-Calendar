using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;

public sealed class PersianDateWidget : Form
{
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern IntPtr FindWindow(string className, string title);
    [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);
    [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr hwnd);
    [DllImport("user32.dll")] static extern bool ScreenToClient(IntPtr hwnd, ref Point point);
    [DllImport("user32.dll")] static extern IntPtr GetParent(IntPtr hwnd);
    [DllImport("user32.dll", SetLastError=true)] static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint key, byte alpha, uint flags);
    [DllImport("user32.dll")] static extern IntPtr GetWindowDpiAwarenessContext(IntPtr hwnd);
    [DllImport("user32.dll")] static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
    [StructLayout(LayoutKind.Sequential)] struct NativeRect { public int Left, Top, Right, Bottom; }
    public readonly IntPtr Taskbar;
    public event EventHandler ExitRequested;
    public static void MatchTaskbarDpi(IntPtr taskbar) {
        try {
            IntPtr context = GetWindowDpiAwarenessContext(taskbar);
            if (context != IntPtr.Zero) SetThreadDpiAwarenessContext(context);
        } catch (EntryPointNotFoundException) { }
    }
    Rectangle TaskbarBounds {
        get {
            NativeRect r;
            if (GetWindowRect(Taskbar, out r)) return Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
            return Rectangle.Empty;
        }
    }
    Point ScreenLocation {
        get {
            NativeRect r;
            if (IsHandleCreated && GetWindowRect(Handle, out r)) return new Point(r.Left, r.Top);
            return Location;
        }
    }
    void MoveOnTaskbar(Point position) {
        Rectangle bar = TaskbarBounds;
        if (bar.IsEmpty) return;
        // Keep the widget inside its actual taskbar parent, including while dragging.
        position.X = Math.Max(bar.Left, Math.Min(position.X, bar.Right - Width));
        position.Y = Math.Max(bar.Top, Math.Min(position.Y, bar.Bottom - Height));
        if (!ScreenToClient(Taskbar, ref position)) return;
        Location = position;
    }
    void RaiseOverlay() {
        if (IsDisposed || !IsHandleCreated || !IsWindow(Taskbar)) return;
        // HWND_TOP orders siblings inside the taskbar; no global topmost contest.
        SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, 0x0053);
    }
    protected override void OnHandleCreated(EventArgs e) {
        base.OnHandleCreated(e);
        if (GetParent(Handle) != Taskbar)
            throw new InvalidOperationException("The widget could not attach to the Windows taskbar.");
        if (!SetLayeredWindowAttributes(Handle, 0x00FF00FF, 255, 1))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
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
        get {
            CreateParams cp = base.CreateParams;
            cp.Style = (cp.Style & ~unchecked((int)0x80000000)) | 0x40000000;
            cp.ExStyle = (cp.ExStyle & ~0x00040008) | 0x08080080;
            cp.Parent = Taskbar;
            return cp;
        }
    }
    public PersianDateWidget(IntPtr taskbar)
    {
        Taskbar = taskbar;
        TopLevel = false;
        Text = "Persian Date Compact v9";
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        Font = new Font("Tahoma", 10.5f, FontStyle.Regular);
        BackColor = Color.Magenta;
        ForeColor = Color.Black;
        DoubleBuffered = true;
        Padding = Padding.Empty;
        var menu = new ContextMenuStrip();
        menu.Items.Add("Move to bottom-right", null, delegate { customPosition = false; DockRight(); SavePosition(); });
        menu.Items.Add("Black / white text", null, delegate {
            ForeColor = ForeColor == Color.Black ? Color.White : Color.Black;
            Invalidate();
        });
        menu.Items.Add("Version 9 - attached to taskbar").Enabled = false;
        menu.Items.Add("Exit", null, delegate {
            if (ExitRequested != null) ExitRequested(this, EventArgs.Empty);
        });
        ContextMenuStrip = menu;
        UpdateDate();
        LoadPosition();
        MouseDown += delegate(object s, MouseEventArgs e) {
            if (e.Button != MouseButtons.Left) return;
            dragging = true; mouseOrigin = Cursor.Position; windowOrigin = ScreenLocation; Capture = true;
        };
        MouseMove += delegate {
            if (!dragging) return;
            Point now = Cursor.Position;
            MoveOnTaskbar(new Point(windowOrigin.X + now.X - mouseOrigin.X, windowOrigin.Y + now.Y - mouseOrigin.Y));
            RaiseOverlay();
        };
        MouseUp += delegate(object s, MouseEventArgs e) {
            if (!dragging) return;
            dragging = false; Capture = false; customPosition = true; ClampPosition(); SavePosition(); RaiseOverlay();
        };
        timer.Interval = 250;
        timer.Tick += delegate { UpdateDate(); if (!dragging) { if (!customPosition) DockRight(); else ClampPosition(); } RaiseOverlay(); };
        timer.Start();
        Disposed += delegate { timer.Stop(); timer.Dispose(); tip.Dispose(); menu.Dispose(); };
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
        Rectangle bar = TaskbarBounds;
        if (bar.IsEmpty) return;
        MoveOnTaskbar(new Point(bar.Right - Width - 380, bar.Top + Math.Max(0, (bar.Height - Height) / 2)));
    }
    void ClampPosition()
    {
        MoveOnTaskbar(ScreenLocation);
    }
    void LoadPosition()
    {
        DockRight();
        try {
            string[] values = File.ReadAllText(config).Split(',');
            int x, y;
            if (values.Length == 2 && Int32.TryParse(values[0], out x) && Int32.TryParse(values[1], out y)) {
                MoveOnTaskbar(new Point(x,y)); customPosition = true;
            }
        } catch (IOException) {} catch (UnauthorizedAccessException) {}
    }
    void SavePosition()
    {
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(config));
            if (customPosition) {
                Point position = ScreenLocation;
                File.WriteAllText(config, position.X.ToString(CultureInfo.InvariantCulture) + "," + position.Y.ToString(CultureInfo.InvariantCulture));
            }
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
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                using (var context = new TaskbarWidgetContext()) Application.Run(context);
            } finally { mutex.ReleaseMutex(); }
        }
    }
}

// The message loop outlives Explorer windows so a restarted taskbar can be reattached.
public sealed class TaskbarWidgetContext : ApplicationContext
{
    readonly System.Windows.Forms.Timer monitor = new System.Windows.Forms.Timer();
    PersianDateWidget widget;
    bool stopping;
    public TaskbarWidgetContext() {
        monitor.Interval = 1;
        monitor.Tick += delegate { monitor.Interval = 250; EnsureWidget(); };
        monitor.Start();
    }
    void EnsureWidget() {
        if (stopping) return;
        IntPtr taskbar = PersianDateWidget.FindWindow("Shell_TrayWnd", null);
        if (widget != null && !widget.IsDisposed && widget.IsHandleCreated &&
            widget.Taskbar == taskbar && PersianDateWidget.IsWindow(taskbar)) return;
        if (widget != null) { widget.Dispose(); widget = null; }
        if (taskbar == IntPtr.Zero) return;
        try {
            PersianDateWidget.MatchTaskbarDpi(taskbar);
            widget = new PersianDateWidget(taskbar);
            widget.ExitRequested += delegate { ExitThread(); };
            widget.Show();
        } catch (Exception ex) {
            // Explorer can disappear between discovery and window creation.
            if (!PersianDateWidget.IsWindow(taskbar)) return;
            stopping = true;
            monitor.Stop();
            MessageBox.Show("Version 9 could not attach to the taskbar: " + ex.Message,
                "Persian Date - send a screenshot");
            ExitThread();
        }
    }
    protected override void ExitThreadCore() {
        stopping = true;
        monitor.Stop();
        if (widget != null) { widget.Dispose(); widget = null; }
        base.ExitThreadCore();
    }
    protected override void Dispose(bool disposing) {
        if (disposing) {
            stopping = true;
            monitor.Dispose();
            if (widget != null) { widget.Dispose(); widget = null; }
        }
        base.Dispose(disposing);
    }
}
