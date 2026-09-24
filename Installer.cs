using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace FramePulseSetup
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            bool uninstall = false;
            bool silent = false;
            int i;
            for (i = 0; i < args.Length; i++)
            {
                string a = args[i];
                if (string.Equals(a, "/uninstall", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(a, "-uninstall", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(a, "/x", StringComparison.OrdinalIgnoreCase))
                    uninstall = true;
                if (string.Equals(a, "/silent", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(a, "/S", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(a, "-silent", StringComparison.OrdinalIgnoreCase))
                    silent = true;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (uninstall)
            {
                int code = Uninstall.Run(silent);
                Environment.Exit(code);
                return;
            }

            Application.Run(new SetupForm(silent));
        }
    }

    static class AppInfo
    {
        public const string Name = "FramePulse";
        public const string Version = "1.0.0";
        public const string Publisher = "R3G1ST";
        public const string ExeName = "FramePulse.exe";
        public const string AppId = "FramePulse";
        public const string Site = "https://github.com/R3G1ST/FramePulse";
        public const string UninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\FramePulse";

        public static string DefaultDir()
        {
            string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (string.IsNullOrEmpty(pf))
                pf = @"C:\Program Files";
            return Path.Combine(pf, Name);
        }

        public static string ConfigDir()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FpsOverlay");
        }

        public static string ConfigPath()
        {
            return Path.Combine(ConfigDir(), "config.ini");
        }

        public static string DesktopLnk()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), Name + ".lnk");
        }

        public static string StartMenuLnk()
        {
            string programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            return Path.Combine(programs, Name + ".lnk");
        }

        public static string StartupLnk()
        {
            string startup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            return Path.Combine(startup, Name + ".lnk");
        }
    }

    static class Sys
    {
        public static readonly Color Bg = Color.FromArgb(10, 12, 18);
        public static readonly Color Panel = Color.FromArgb(14, 17, 26);
        public static readonly Color Card = Color.FromArgb(20, 24, 36);
        public static readonly Color Card2 = Color.FromArgb(26, 31, 46);
        public static readonly Color Border = Color.FromArgb(40, 48, 66);
        public static readonly Color Text = Color.FromArgb(235, 240, 250);
        public static readonly Color Muted = Color.FromArgb(140, 152, 175);
        public static readonly Color Faint = Color.FromArgb(90, 100, 120);
        public static readonly Color Accent = Color.FromArgb(110, 200, 255);
        public static readonly Color Accent2 = Color.FromArgb(124, 110, 245);
        public static readonly Color Ok = Color.FromArgb(80, 210, 150);
        public static readonly Color Err = Color.FromArgb(255, 100, 110);

        public static void Log(TextBox box, string msg)
        {
            if (box.IsHandleCreated && !box.IsDisposed)
            {
                box.AppendText(msg + Environment.NewLine);
            }
        }
    }

    static class Res
    {
        public static byte[] Get(string name)
        {
            Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
            if (s == null) throw new InvalidOperationException("Missing resource: " + name);
            byte[] b = new byte[s.Length];
            int o = 0;
            while (o < b.Length)
            {
                int n = s.Read(b, o, b.Length - o);
                if (n <= 0) break;
                o += n;
            }
            s.Close();
            return b;
        }

        public static void Save(string name, string path)
        {
            File.WriteAllBytes(path, Get(name));
        }
    }

    static class Win
    {
        public static void Kill(string processName)
        {
            try
            {
                Process[] list = Process.GetProcessesByName(processName);
                for (int i = 0; i < list.Length; i++)
                {
                    try
                    {
                        list[i].Kill();
                        list[i].WaitForExit(4000);
                    }
                    catch { }
                    try { list[i].Dispose(); } catch { }
                }
            }
            catch { }
        }

        public static void KillAll()
        {
            Kill("FramePulse");
            Kill("FpsOverlay");
            Kill("PresentMon");
        }

        public static void MkDir(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        public static void Shortcut(string lnkPath, string target, string iconPath, string desc, string args)
        {
            Type t = Type.GetTypeFromProgID("WScript.Shell");
            if (t == null) return;
            object shell = Activator.CreateInstance(t);
            object lnk = t.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { lnkPath });
            Type lt = lnk.GetType();
            lt.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, lnk, new object[] { target });
            lt.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, lnk, new object[] { Path.GetDirectoryName(target) });
            if (!string.IsNullOrEmpty(iconPath))
                lt.InvokeMember("IconLocation", System.Reflection.BindingFlags.SetProperty, null, lnk, new object[] { iconPath });
            if (!string.IsNullOrEmpty(desc))
                lt.InvokeMember("Description", System.Reflection.BindingFlags.SetProperty, null, lnk, new object[] { desc });
            if (!string.IsNullOrEmpty(args))
                lt.InvokeMember("Arguments", System.Reflection.BindingFlags.SetProperty, null, lnk, new object[] { args });
            lt.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, lnk, null);
            Marshal.FinalReleaseComObject(lnk);
            Marshal.FinalReleaseComObject(shell);
        }

        public static void DeleteFile(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        public static void WriteReg(string name, string value)
        {
            try
            {
                RegistryKey k = Registry.LocalMachine.CreateSubKey(AppInfo.UninstallKey);
                if (k != null)
                {
                    k.SetValue(name, value);
                    k.Close();
                }
            }
            catch { }
        }

        public static void DeleteRegKey()
        {
            try { Registry.LocalMachine.DeleteSubKeyTree(AppInfo.UninstallKey, false); } catch { }
        }
    }

    static class PaintUtil
    {
        public static GraphicsPath RoundRect(int x, int y, int w, int h, int r)
        {
            GraphicsPath p = new GraphicsPath();
            if (r <= 0)
            {
                p.AddRectangle(new Rectangle(x, y, w, h));
                return p;
            }
            int d = r * 2;
            p.AddArc(x, y, d, d, 180, 90);
            p.AddArc(x + w - d, y, d, d, 270, 90);
            p.AddArc(x + w - d, y + h - d, d, d, 0, 90);
            p.AddArc(x, y + h - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void DrawLogo(Graphics g, int x, int y, int size)
        {
            int r = Math.Max(4, (int)(size * 0.22));
            GraphicsPath path = RoundRect(x, y, size, size, r);
            Rectangle rect = new Rectangle(x, y, size, size);
            LinearGradientBrush br = new LinearGradientBrush(rect,
                Color.FromArgb(86, 148, 255), Color.FromArgb(155, 110, 255), 45f);
            g.FillPath(br, path);
            using (Pen pen = new Pen(Color.FromArgb(70, 255, 255, 255), Math.Max(1f, size * 0.03f)))
                g.DrawPath(pen, path);
            br.Dispose();

            using (Font f = new Font("Segoe UI", size * 0.36f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (SolidBrush tb = new SolidBrush(Color.White))
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                g.DrawString("FP", f, tb, new RectangleF(x, y, size, size), sf);
            }
        }
    }

    class FlatButton : Control
    {
        public bool Primary;
        public bool Hover;

        public FlatButton()
        {
            Size = new Size(150, 42);
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); Hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); Hover = false; Invalidate(); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            GraphicsPath path = PaintUtil.RoundRect(r.X, r.Y, r.Width, r.Height, 10);

            if (!Enabled)
            {
                using (SolidBrush b = new SolidBrush(Sys.Card))
                    e.Graphics.FillPath(b, path);
                using (Pen p = new Pen(Sys.Border))
                    e.Graphics.DrawPath(p, path);
                using (SolidBrush b = new SolidBrush(Sys.Faint))
                using (StringFormat sf = Center())
                    e.Graphics.DrawString(Text, Font, b, r, sf);
            }
            else if (Primary)
            {
                Color c1 = Sys.Accent;
                Color c2 = Sys.Accent2;
                if (Hover) { c1 = Color.FromArgb(140, 215, 255); c2 = Color.FromArgb(150, 130, 255); }
                using (LinearGradientBrush b = new LinearGradientBrush(r, c1, c2, 45f))
                    e.Graphics.FillPath(b, path);
                using (SolidBrush b = new SolidBrush(Color.FromArgb(10, 8, 20)))
                using (StringFormat sf = Center())
                    e.Graphics.DrawString(Text, Font, b, r, sf);
            }
            else
            {
                using (SolidBrush b = new SolidBrush(Hover ? Sys.Card2 : Sys.Card))
                    e.Graphics.FillPath(b, path);
                using (Pen p = new Pen(Hover ? Sys.Accent : Sys.Border))
                    e.Graphics.DrawPath(p, path);
                using (SolidBrush b = new SolidBrush(Hover ? Sys.Accent : Sys.Text))
                using (StringFormat sf = Center())
                    e.Graphics.DrawString(Text, Font, b, r, sf);
            }
        }

        static StringFormat Center()
        {
            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Center;
            sf.LineAlignment = StringAlignment.Center;
            return sf;
        }
    }

    class CheckRow : Control
    {
        public bool Checked;
        public string Title;
        public string Hint;
        bool _hover;

        public CheckRow()
        {
            Height = 56;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hover = false; Invalidate(); }
        protected override void OnClick(EventArgs e) { base.OnClick(e); Checked = !Checked; Invalidate(); if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty); }

        public event EventHandler CheckedChanged;

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle cardR = new Rectangle(0, 0, Width - 1, Height - 1);
            GraphicsPath cardPath = PaintUtil.RoundRect(cardR.X, cardR.Y, cardR.Width, cardR.Height, 12);
            using (SolidBrush b = new SolidBrush(_hover ? Sys.Card2 : Sys.Card))
                e.Graphics.FillPath(b, cardPath);
            using (Pen p = new Pen(_hover ? Color.FromArgb(70, 110, 200, 255) : Sys.Border))
                e.Graphics.DrawPath(p, cardPath);

            Rectangle box = new Rectangle(16, (Height - 22) / 2, 22, 22);
            GraphicsPath path = PaintUtil.RoundRect(box.X, box.Y, box.Width, box.Height, 6);

            if (Checked)
            {
                using (LinearGradientBrush b = new LinearGradientBrush(box, Sys.Accent, Sys.Accent2, 45f))
                    e.Graphics.FillPath(b, path);
                using (Pen p = new Pen(Color.White, 2f))
                {
                    e.Graphics.DrawLines(p, new Point[] {
                        new Point(box.X + 5, box.Y + 11),
                        new Point(box.X + 9, box.Y + 16),
                        new Point(box.X + 17, box.Y + 7)
                    });
                }
            }
            else
            {
                using (SolidBrush b = new SolidBrush(Sys.Bg))
                    e.Graphics.FillPath(b, path);
                using (Pen p = new Pen(_hover ? Sys.Accent : Sys.Border))
                    e.Graphics.DrawPath(p, path);
            }

            RectangleF titleR = new RectangleF(52, 12, Width - 64, 20);
            RectangleF hintR = new RectangleF(52, 34, Width - 64, 18);
            using (SolidBrush b = new SolidBrush(Sys.Text))
                e.Graphics.DrawString(Title, new Font(Font.FontFamily, 10f, FontStyle.Bold), b, titleR);
            using (Font hf = new Font(Font.FontFamily, 8.5f, FontStyle.Regular))
            using (SolidBrush b = new SolidBrush(Sys.Faint))
                e.Graphics.DrawString(Hint, hf, b, hintR);
        }
    }

    class SidePanel : Control
    {
        public int Step;
        static readonly string[] Titles = { "Добро пожаловать", "Параметры", "Установка", "Готово" };

        public SidePanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new Rectangle(0, 0, Width, Height);
            using (LinearGradientBrush bg = new LinearGradientBrush(rect, Color.FromArgb(8, 10, 16), Color.FromArgb(14, 16, 28), 90f))
                e.Graphics.FillRectangle(bg, rect);

            PaintUtil.DrawLogo(e.Graphics, 36, 40, 72);

            using (Font f = new Font("Segoe UI", 15f, FontStyle.Bold))
            using (SolidBrush b = new SolidBrush(Sys.Text))
                e.Graphics.DrawString(AppInfo.Name, f, b, new PointF(36, 128));

            using (Font f = new Font("Segoe UI", 9f))
            using (SolidBrush b = new SolidBrush(Sys.Faint))
                e.Graphics.DrawString("v" + AppInfo.Version + " · " + AppInfo.Publisher, f, b, new PointF(36, 156));

            int y = 220;
            for (int i = 0; i < Titles.Length; i++)
            {
                bool active = i == Step;
                bool done = i < Step;
                RectangleF numR = new RectangleF(40, y, 28, 28);

                GraphicsPath np = PaintUtil.RoundRect((int)numR.X, (int)numR.Y, 28, 28, 14);
                if (active || done)
                {
                    using (LinearGradientBrush b = new LinearGradientBrush(numR, Sys.Accent, Sys.Accent2, 45f))
                        e.Graphics.FillPath(b, np);
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(10, 8, 20)))
                    using (StringFormat sf = new StringFormat())
                    {
                        sf.Alignment = StringAlignment.Center;
                        sf.LineAlignment = StringAlignment.Center;
                        e.Graphics.DrawString((i + 1).ToString(), new Font("Segoe UI", 9f, FontStyle.Bold), b, numR, sf);
                    }
                }
                else
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(22, 26, 40)))
                        e.Graphics.FillPath(b, np);
                    using (Pen p = new Pen(Sys.Border))
                        e.Graphics.DrawPath(p, np);
                    using (SolidBrush b = new SolidBrush(Sys.Faint))
                    using (StringFormat sf = new StringFormat())
                    {
                        sf.Alignment = StringAlignment.Center;
                        sf.LineAlignment = StringAlignment.Center;
                        e.Graphics.DrawString((i + 1).ToString(), Font, b, numR, sf);
                    }
                }

                using (Font f = new Font("Segoe UI", 10f, active ? FontStyle.Bold : FontStyle.Regular))
                using (SolidBrush b = new SolidBrush(active ? Sys.Text : (done ? Sys.Muted : Sys.Faint)))
                    e.Graphics.DrawString(Titles[i], f, b, new PointF(80, y + 5));

                if (i < Titles.Length - 1)
                {
                    using (Pen p = new Pen(done || active ? Color.FromArgb(60, 110, 200, 255) : Sys.Border, 2f))
                        e.Graphics.DrawLine(p, 54, y + 32, 54, y + 44);
                }
                y += 56;
            }

            using (Font f = new Font("Segoe UI", 8f))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(70, 80, 100)))
                e.Graphics.DrawString(AppInfo.Site.Replace("https://", ""), f, b, new PointF(36, Height - 40));
        }
    }

    class ProgressView : Control
    {
        public float Value;
        public string Status = "";

        public ProgressView()
        {
            Height = 8;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle track = new Rectangle(0, 0, Width, 8);
            GraphicsPath tp = PaintUtil.RoundRect(0, 0, Width, 8, 4);
            using (SolidBrush b = new SolidBrush(Sys.Card))
                e.Graphics.FillPath(b, tp);
            int w = (int)(Width * Math.Max(0f, Math.Min(1f, Value)));
            if (w > 8)
            {
                GraphicsPath fp = PaintUtil.RoundRect(0, 0, w, 8, 4);
                using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(0, 0, w, 8), Sys.Accent, Sys.Accent2, 0f))
                    e.Graphics.FillPath(b, fp);
            }
        }
    }

    class SetupForm : Form
    {
        readonly bool _silent;
        int _step;
        string _dir;
        bool _desktop = true;
        bool _startup = true;
        bool _config = true;
        bool _launch = true;

        SidePanel _side;
        Panel _page;
        Panel _footer;
        Panel _titleBar;
        Label _h1;
        TextBox _dirBox;
        CheckRow _ckDesk;
        CheckRow _ckStart;
        CheckRow _ckCfg;
        CheckRow _ckLaunch;
        ProgressView _prog;
        TextBox _log;
        Label _status;
        FlatButton _btnBack;
        FlatButton _btnNext;
        Label _doneTitle;
        Label _doneHint;

        [DllImport("user32.dll")]
        static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        const int WM_NCLBUTTONDOWN = 0xA1;
        const int HTCAPTION = 0x2;

        public SetupForm(bool silent)
        {
            _silent = silent;
            _dir = AppInfo.DefaultDir();

            Text = AppInfo.Name + " Setup";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(900, 560);
            BackColor = Sys.Bg;
            Font = new Font("Segoe UI", 10f);
            KeyPreview = true;
            MinimizeBox = false;
            MaximizeBox = false;

            BuildTitleBar();
            BuildBody();
            ShowStep(0);

            if (_silent)
            {
                BeginInstall(true);
            }
        }

        void BuildTitleBar()
        {
            _titleBar = new Panel();
            _titleBar.Dock = DockStyle.Top;
            _titleBar.Height = 40;
            _titleBar.BackColor = Sys.Panel;
            _titleBar.Paint += delegate(object s, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                PaintUtil.DrawLogo(e.Graphics, 12, 10, 20);
                using (Pen p = new Pen(Sys.Border))
                    e.Graphics.DrawLine(p, 0, 39, _titleBar.Width, 39);
                using (SolidBrush b = new SolidBrush(Sys.Text))
                using (Font f = new Font("Segoe UI", 9.5f, FontStyle.Bold))
                    e.Graphics.DrawString(AppInfo.Name + " Setup", f, b, new PointF(42, 11));
            };
            _titleBar.MouseDown += TitleBarMouseDown;

            FlatButton close = new FlatButton();
            close.Text = "✕";
            close.Size = new Size(46, 40);
            close.Location = new Point(900 - 46, 0);
            close.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            close.Primary = false;
            close.Click += delegate { Close(); };
            _titleBar.Controls.Add(close);
        }

        void TitleBarMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        void BuildBody()
        {
            _side = new SidePanel();
            _side.Dock = DockStyle.Left;
            _side.Width = 240;

            _page = new Panel();
            _page.Dock = DockStyle.Fill;
            _page.BackColor = Sys.Bg;
            _page.Padding = new Padding(32, 24, 32, 16);

            Panel body = new Panel();
            body.Dock = DockStyle.Fill;
            body.BackColor = Sys.Bg;
            body.Controls.Add(_page);
            body.Controls.Add(_side);

            _footer = new Panel();
            _footer.Dock = DockStyle.Bottom;
            _footer.Height = 72;
            _footer.BackColor = Sys.Panel;
            _footer.Paint += delegate(object s, PaintEventArgs e)
            {
                using (Pen p = new Pen(Sys.Border))
                    e.Graphics.DrawLine(p, 0, 0, _footer.Width, 0);
            };
            _footer.Resize += FooterLayout;

            _btnBack = new FlatButton();
            _btnBack.Text = "Назад";
            _btnBack.Size = new Size(120, 40);
            _btnBack.Click += BtnBackClick;

            _btnNext = new FlatButton();
            _btnNext.Text = "Далее";
            _btnNext.Primary = true;
            _btnNext.Size = new Size(170, 40);
            _btnNext.Click += BtnNextClick;

            _footer.Controls.Add(_btnBack);
            _footer.Controls.Add(_btnNext);

            Controls.Add(body);
            Controls.Add(_footer);
            Controls.Add(_titleBar);
            FooterLayout(_footer, EventArgs.Empty);
        }

        void FooterLayout(object sender, EventArgs e)
        {
            if (_footer == null || _btnBack == null || _btnNext == null) return;
            int pad = 24;
            int mid = Math.Max(0, (_footer.Height - _btnBack.Height) / 2);
            _btnBack.Location = new Point(pad, mid);
            _btnNext.Location = new Point(Math.Max(pad, _footer.Width - _btnNext.Width - pad), mid);
        }

        void ClearPage()
        {
            _page.Controls.Clear();
        }

        void ShowStep(int step)
        {
            _step = step;
            _side.Step = step;
            _side.Invalidate();
            ClearPage();

            if (step == 0) BuildWelcome();
            else if (step == 1) BuildOptions();
            else if (step == 2) BuildProgress();
            else BuildDone();

            _btnBack.Visible = step < 3;
            _btnNext.Visible = true;

            if (step == 0)
            {
                _btnBack.Enabled = false;
                _btnNext.Enabled = true;
                _btnNext.Text = "Далее";
            }
            else if (step == 1)
            {
                _btnBack.Enabled = true;
                _btnNext.Enabled = true;
                _btnNext.Text = "Установить";
            }
            else if (step == 2)
            {
                _btnBack.Enabled = false;
                _btnNext.Enabled = false;
                _btnNext.Text = "Установка…";
            }
            else
            {
                _btnBack.Visible = false;
                _btnNext.Enabled = true;
                _btnNext.Text = "Готово";
            }

            FooterLayout(_footer, EventArgs.Empty);
        }

        void BuildWelcome()
        {
            int cw = Math.Max(360, _page.ClientSize.Width - _page.Padding.Horizontal);

            Panel hero = new Panel();
            hero.Location = new Point(0, 0);
            hero.Size = new Size(cw, 96);
            hero.BackColor = Color.Transparent;
            hero.Paint += delegate(object o, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                PaintUtil.DrawLogo(e.Graphics, 0, 8, 56);

                using (Font f = new Font("Segoe UI", 20f, FontStyle.Bold))
                using (SolidBrush b = new SolidBrush(Sys.Text))
                    e.Graphics.DrawString("FramePulse", f, b, new PointF(72, 12));

                using (Font f = new Font("Segoe UI", 10.5f))
                using (SolidBrush b = new SolidBrush(Sys.Muted))
                    e.Graphics.DrawString("Игровой оверлей для Windows", f, b, new PointF(74, 46));

                using (Font f = new Font("Segoe UI", 9f))
                using (SolidBrush b = new SolidBrush(Sys.Faint))
                    e.Graphics.DrawString("v" + AppInfo.Version + "  ·  " + AppInfo.Publisher, f, b, new PointF(74, 68));
            };
            _page.Controls.Add(hero);

            Label lead = new Label();
            lead.Text = "FPS, время кадра, ЦП / ГП и температура — поверх игры.\nРусский интерфейс, мульти-монитор и живые настройки.";
            lead.Font = new Font("Segoe UI", 11f);
            lead.ForeColor = Sys.Muted;
            lead.AutoSize = false;
            lead.Size = new Size(cw, 52);
            lead.Location = new Point(0, 112);
            _page.Controls.Add(lead);

            string[][] feats = new string[][] {
                new string[] { "PresentMon", "Точный FPS и low 1% в реальном времени" },
                new string[] { "Система", "ЦП, ГП, температура, VRAM и ОЗУ" },
                new string[] { "Интерфейс", "Панель тянется по контенту, без обрезки" },
                new string[] { "Настройки", "Применяются сразу — без перезапуска" }
            };

            int cardW = (cw - 12) / 2;
            int cardH = 78;
            int x0 = 0, y0 = 184;
            for (int i = 0; i < feats.Length; i++)
            {
                int col = i % 2;
                int row = i / 2;
                Panel card = new Panel();
                card.Location = new Point(x0 + col * (cardW + 12), y0 + row * (cardH + 12));
                card.Size = new Size(cardW, cardH);
                card.BackColor = Sys.Card;
                string title = feats[i][0];
                string hint = feats[i][1];
                card.Paint += delegate(object o, PaintEventArgs e)
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    Rectangle r = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                    GraphicsPath path = PaintUtil.RoundRect(r.X, r.Y, r.Width, r.Height, 12);
                    using (SolidBrush b = new SolidBrush(Sys.Card))
                        e.Graphics.FillPath(b, path);
                    using (Pen p = new Pen(Sys.Border))
                        e.Graphics.DrawPath(p, path);

                    Rectangle bar = new Rectangle(0, 14, 3, card.Height - 28);
                    GraphicsPath bp = PaintUtil.RoundRect(bar.X, bar.Y, bar.Width, bar.Height, 2);
                    using (LinearGradientBrush b = new LinearGradientBrush(bar, Sys.Accent, Sys.Accent2, 90f))
                        e.Graphics.FillPath(b, bp);

                    using (Font tf = new Font("Segoe UI", 10f, FontStyle.Bold))
                    using (SolidBrush tb = new SolidBrush(Sys.Text))
                        e.Graphics.DrawString(title, tf, tb, new PointF(18, 16));

                    using (Font hf = new Font("Segoe UI", 9f))
                    using (SolidBrush hb = new SolidBrush(Sys.Faint))
                        e.Graphics.DrawString(hint, hf, hb, new RectangleF(18, 40, card.Width - 32, 30));
                };
                _page.Controls.Add(card);
            }

            Label note = new Label();
            note.Text = "Windows 10 / 11  ·  .NET Framework 4.x  ·  MIT License";
            note.Font = new Font("Segoe UI", 9f);
            note.ForeColor = Sys.Faint;
            note.AutoSize = true;
            note.Location = new Point(0, y0 + 2 * (cardH + 12) + 16);
            _page.Controls.Add(note);
        }

        void BuildOptions()
        {
            int cw = Math.Max(360, _page.ClientSize.Width - _page.Padding.Horizontal);

            Label h = new Label();
            h.Text = "Параметры установки";
            h.Font = new Font("Segoe UI", 18f, FontStyle.Bold);
            h.ForeColor = Sys.Text;
            h.AutoSize = true;
            h.Location = new Point(0, 0);
            _page.Controls.Add(h);

            Label sub = new Label();
            sub.Text = "Выберите папку и что настроить после установки.";
            sub.Font = new Font("Segoe UI", 10.5f);
            sub.ForeColor = Sys.Muted;
            sub.AutoSize = true;
            sub.Location = new Point(0, 36);
            _page.Controls.Add(sub);

            Label pathLbl = new Label();
            pathLbl.Text = "Папка установки";
            pathLbl.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            pathLbl.ForeColor = Sys.Faint;
            pathLbl.AutoSize = true;
            pathLbl.Location = new Point(0, 78);
            _page.Controls.Add(pathLbl);

            int boxW = Math.Max(280, cw - 120);
            _dirBox = new TextBox();
            _dirBox.Text = _dir;
            _dirBox.Font = new Font("Segoe UI", 10f);
            _dirBox.BackColor = Sys.Card;
            _dirBox.ForeColor = Sys.Text;
            _dirBox.BorderStyle = BorderStyle.FixedSingle;
            _dirBox.Location = new Point(0, 102);
            _dirBox.Size = new Size(boxW, 28);
            _page.Controls.Add(_dirBox);

            FlatButton browse = new FlatButton();
            browse.Text = "Обзор…";
            browse.Size = new Size(104, 32);
            browse.Location = new Point(boxW + 10, 100);
            browse.Click += delegate(object o, EventArgs e)
            {
                using (FolderBrowserDialog dlg = new FolderBrowserDialog())
                {
                    dlg.Description = "Папка установки FramePulse";
                    dlg.SelectedPath = _dirBox.Text;
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                        _dirBox.Text = dlg.SelectedPath;
                }
            };
            _page.Controls.Add(browse);

            Label opt = new Label();
            opt.Text = "Дополнительно";
            opt.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
            opt.ForeColor = Sys.Text;
            opt.AutoSize = true;
            opt.Location = new Point(0, 154);
            _page.Controls.Add(opt);

            _ckDesk = new CheckRow();
            _ckDesk.Title = "Ярлык на рабочем столе";
            _ckDesk.Hint = "Быстрый запуск в один клик";
            _ckDesk.Checked = _desktop;
            _ckDesk.Location = new Point(0, 190);
            _ckDesk.Width = cw;
            _ckDesk.Height = 60;
            _page.Controls.Add(_ckDesk);

            _ckStart = new CheckRow();
            _ckStart.Title = "Запускать вместе с Windows";
            _ckStart.Hint = "Оверлей будет готов при входе в систему";
            _ckStart.Checked = _startup;
            _ckStart.Location = new Point(0, 262);
            _ckStart.Width = cw;
            _ckStart.Height = 60;
            _page.Controls.Add(_ckStart);

            _ckCfg = new CheckRow();
            _ckCfg.Title = "Применить настройки по умолчанию";
            _ckCfg.Hint = "Текущие параметры оверлея (config.ini)";
            _ckCfg.Checked = _config;
            _ckCfg.Location = new Point(0, 334);
            _ckCfg.Width = cw;
            _ckCfg.Height = 60;
            _page.Controls.Add(_ckCfg);

            Label note = new Label();
            note.Text = "Требуются права администратора. Старая версия (если есть) будет остановлена и заменена.";
            note.Font = new Font("Segoe UI", 9f);
            note.ForeColor = Sys.Faint;
            note.AutoSize = false;
            note.Size = new Size(cw, 32);
            note.Location = new Point(0, 412);
            _page.Controls.Add(note);

            _dirBox.TextChanged += delegate { _dir = _dirBox.Text; };
            _ckDesk.CheckedChanged += delegate { _desktop = _ckDesk.Checked; };
            _ckStart.CheckedChanged += delegate { _startup = _ckStart.Checked; };
            _ckCfg.CheckedChanged += delegate { _config = _ckCfg.Checked; };
        }

        void BuildProgress()
        {
            Label t = new Label();
            t.Text = "Установка…";
            t.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
            t.ForeColor = Sys.Text;
            t.AutoSize = true;
            t.Location = new Point(0, 4);
            _page.Controls.Add(t);

            _status = new Label();
            _status.Text = "Подготовка";
            _status.Font = new Font("Segoe UI", 10f);
            _status.ForeColor = Sys.Muted;
            _status.AutoSize = true;
            _status.Location = new Point(0, 40);
            _page.Controls.Add(_status);

            _prog = new ProgressView();
            _prog.Location = new Point(0, 70);
            _prog.Size = new Size(540, 8);
            _page.Controls.Add(_prog);

            _log = new TextBox();
            _log.Multiline = true;
            _log.ReadOnly = true;
            _log.ScrollBars = ScrollBars.Vertical;
            _log.BorderStyle = BorderStyle.None;
            _log.BackColor = Sys.Card;
            _log.ForeColor = Sys.Muted;
            _log.Font = new Font("Consolas", 9.5f);
            _log.Location = new Point(0, 96);
            _log.Size = new Size(540, 300);
            _log.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _page.Controls.Add(_log);
        }

        void BuildDone()
        {
            Panel mark = new Panel();
            mark.Size = new Size(72, 72);
            mark.Location = new Point(0, 20);
            mark.Paint += delegate(object o, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                GraphicsPath p = PaintUtil.RoundRect(0, 0, 72, 72, 36);
                using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(0, 0, 72, 72), Sys.Ok, Color.FromArgb(40, 180, 120), 45f))
                    e.Graphics.FillPath(b, p);
                using (Pen pen = new Pen(Color.White, 4f))
                {
                    e.Graphics.DrawLines(pen, new Point[] {
                        new Point(18, 37), new Point(31, 50), new Point(55, 24)
                    });
                }
            };
            _page.Controls.Add(mark);

            _doneTitle = new Label();
            _doneTitle.Text = "FramePulse установлен";
            _doneTitle.Font = new Font("Segoe UI", 20f, FontStyle.Bold);
            _doneTitle.ForeColor = Sys.Text;
            _doneTitle.AutoSize = true;
            _doneTitle.Location = new Point(0, 110);
            _page.Controls.Add(_doneTitle);

            _doneHint = new Label();
            _doneHint.Text = "Панель появится поверх игры. Хоткеи:\nCtrl+Shift+F10 — скрыть/показать · F11 — монитор · F12 — перетаскивание";
            _doneHint.Font = new Font("Segoe UI", 10.5f);
            _doneHint.ForeColor = Sys.Muted;
            _doneHint.AutoSize = true;
            _doneHint.MaximumSize = new Size(540, 0);
            _doneHint.Location = new Point(0, 155);
            _page.Controls.Add(_doneHint);

            _ckLaunch = new CheckRow();
            _ckLaunch.Title = "Запустить FramePulse";
            _ckLaunch.Hint = "Открыть оверлей сразу после закрытия мастера";
            _ckLaunch.Checked = _launch;
            _ckLaunch.Location = new Point(0, 230);
            _ckLaunch.Width = 540;
            _page.Controls.Add(_ckLaunch);

            Label path = new Label();
            path.Text = "Папка: " + _dir;
            path.Font = new Font("Segoe UI", 9f);
            path.ForeColor = Sys.Faint;
            path.AutoSize = true;
            path.MaximumSize = new Size(540, 0);
            path.Location = new Point(0, 310);
            _page.Controls.Add(path);
        }

        void BtnBackClick(object sender, EventArgs e)
        {
            if (_step > 0 && _step < 3)
                ShowStep(_step - 1);
        }

        void BtnNextClick(object sender, EventArgs e)
        {
            if (_step == 0)
            {
                ShowStep(1);
            }
            else if (_step == 1)
            {
                _dir = _dirBox.Text.Trim();
                if (_dir.Length == 0)
                {
                    MessageBox.Show(this, "Укажите папку установки.", AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                ShowStep(2);
                BeginInstall(false);
            }
            else if (_step == 3)
            {
                Close();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_step == 2)
            {
                e.Cancel = true;
                return;
            }
            base.OnFormClosing(e);
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.Escape && _step != 2)
            {
                Close();
                return true;
            }
            return base.ProcessDialogKey(keyData);
        }

        void SetStatus(string s)
        {
            if (_status != null && _status.IsHandleCreated)
            {
                _status.Text = s;
                _status.Invalidate();
            }
        }

        void SetProgress(float v)
        {
            if (_prog != null)
            {
                _prog.Value = v;
                _prog.Invalidate();
            }
        }

        void AppendLog(string s)
        {
            Sys.Log(_log, s);
        }

        void BeginInstall(bool silent)
        {
            if (silent)
            {
                try
                {
                    int code = Install.Run(_dir, _desktop, _startup, _config, null, null);
                    Environment.Exit(code);
                }
                catch
                {
                    Environment.Exit(1);
                }
                return;
            }

            _btnBack.Enabled = false;
            _btnNext.Enabled = false;
            _side.Invalidate();

            MethodInvoker work = delegate
            {
                try
                {
                    int code = Install.Run(_dir, _desktop, _startup, _config, AppendLog, delegate(string st, float p)
                    {
                        MethodInvoker u = delegate
                        {
                            SetStatus(st);
                            SetProgress(p);
                        };
                        try { BeginInvoke(u); } catch { }
                    });
                    MethodInvoker done = delegate
                    {
                        if (code == 0)
                        {
                            ShowStep(3);
                        }
                        else
                        {
                            _btnBack.Visible = true;
                            _btnBack.Enabled = true;
                            _btnNext.Visible = true;
                            _btnNext.Enabled = true;
                            _btnNext.Text = "Повторить";
                            SetStatus("Ошибка установки");
                            MessageBox.Show(this, "Не удалось завершить установку. Смотрите журнал.", AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    };
                    try { BeginInvoke(done); } catch { }
                }
                catch (Exception ex)
                {
                    MethodInvoker done = delegate
                    {
                        _btnBack.Visible = true;
                        _btnBack.Enabled = true;
                        _btnNext.Visible = true;
                        _btnNext.Enabled = true;
                        _btnNext.Text = "Повторить";
                        MessageBox.Show(this, ex.Message, AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    };
                    try { BeginInvoke(done); } catch { }
                }
            };
            ThreadPool.QueueUserWorkItem(delegate { work(); });
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            if (_step == 3 && _ckLaunch != null && _ckLaunch.Checked)
            {
                try
                {
                    string exe = Path.Combine(_dir, AppInfo.ExeName);
                    if (File.Exists(exe))
                        Process.Start(exe);
                }
                catch { }
            }
        }
    }

    static class Install
    {
        public static int Run(string dir, bool desktop, bool startup, bool config, Action<string> log, Action<string, float> progress)
        {
            Action<string> L = delegate(string s) { if (log != null) log(s); };
            Action<string, float> P = delegate(string s, float v) { if (progress != null) progress(s, v); };

            P("Остановка процессов…", 0.05f);
            L("Останавливаю FramePulse / PresentMon…");
            Win.KillAll();

            P("Подготовка папки…", 0.15f);
            L("Каталог: " + dir);
            Win.MkDir(dir);

            string tmp = Path.Combine(Path.GetTempPath(), "fp_setup_" + Process.GetCurrentProcess().Id.ToString());
            Win.MkDir(tmp);
            try
            {
                P("Распаковка файлов…", 0.30f);
                string appTmp = Path.Combine(tmp, AppInfo.ExeName);
                string pmTmp = Path.Combine(tmp, "PresentMon.exe");
                string icoTmp = Path.Combine(tmp, "icon.ico");
                string cfgTmp = Path.Combine(tmp, "config.default.ini");
                Res.Save("fp.app.exe", appTmp);
                Res.Save("fp.pm.exe", pmTmp);
                Res.Save("fp.icon.ico", icoTmp);
                Res.Save("fp.config.ini", cfgTmp);
                L("Извлечено: FramePulse.exe, PresentMon.exe, icon.ico, config");

                P("Копирование…", 0.55f);
                string appDst = Path.Combine(dir, AppInfo.ExeName);
                string pmDst = Path.Combine(dir, "PresentMon.exe");
                string icoDst = Path.Combine(dir, "icon.ico");
                File.Copy(appTmp, appDst, true);
                File.Copy(pmTmp, pmDst, true);
                File.Copy(icoTmp, icoDst, true);
                L("Скопировано в " + dir);

                L("Удаляю старый FpsOverlay.exe…");
                Win.DeleteFile(Path.Combine(dir, "FpsOverlay.exe"));

                P("Ярлыки…", 0.70f);
                if (desktop)
                {
                    Win.Shortcut(AppInfo.DesktopLnk(), appDst, icoDst, AppInfo.Name, "");
                    L("Ярлык: рабочий стол");
                }
                else Win.DeleteFile(AppInfo.DesktopLnk());

                if (startup)
                {
                    Win.Shortcut(AppInfo.StartupLnk(), appDst, icoDst, AppInfo.Name, "");
                    L("Автозапуск: папка «Автозагрузка»");
                }
                else Win.DeleteFile(AppInfo.StartupLnk());

                Win.Shortcut(AppInfo.StartMenuLnk(), appDst, icoDst, AppInfo.Name, "");
                L("Ярлык: меню «Пуск»");

                P("Конфигурация…", 0.85f);
                string cfgDir = AppInfo.ConfigDir();
                string cfgPath = AppInfo.ConfigPath();
                Win.MkDir(cfgDir);
                if (config || !File.Exists(cfgPath))
                {
                    File.Copy(cfgTmp, cfgPath, true);
                    L("config.ini → " + cfgPath);
                }
                else L("config.ini сохранён (без изменений)");

                P("Реестр · запись в «Установка и удаление программ»…", 0.92f);
                string self = Assembly.GetExecutingAssembly().Location;
                string uninst = Path.Combine(dir, "Uninstall.exe");
                try { File.Copy(self, uninst, true); } catch { uninst = self; }

                long sizeKB = 0;
                try { sizeKB = new FileInfo(appDst).Length / 1024 + new FileInfo(pmDst).Length / 1024; } catch { }

                Win.WriteReg("DisplayName", AppInfo.Name);
                Win.WriteReg("DisplayVersion", AppInfo.Version);
                Win.WriteReg("Publisher", AppInfo.Publisher);
                Win.WriteReg("DisplayIcon", appDst);
                Win.WriteReg("InstallLocation", dir);
                Win.WriteReg("UninstallString", "\"" + uninst + "\" /uninstall");
                Win.WriteReg("QuietUninstallString", "\"" + uninst + "\" /uninstall /silent");
                Win.WriteReg("URLInfoAbout", AppInfo.Site);
                Win.WriteReg("EstimatedSize", sizeKB.ToString());
                Win.WriteReg("NoModify", "1");
                Win.WriteReg("NoRepair", "1");

                L("UninstallString: " + uninst);
                P("Готово", 1f);
                L("Установка завершена.");
                return 0;
            }
            finally
            {
                try { Directory.Delete(tmp, true); } catch { }
            }
        }
    }

    static class Uninstall
    {
        public static int Run(bool silent)
        {
            if (!silent)
            {
                DialogResult r = MessageBox.Show(
                    "Удалить FramePulse?\n\nФайлы программы и ярлыки будут удалены.\nconfig.ini останется в %APPDATA%\\FpsOverlay.",
                    AppInfo.Name,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (r != DialogResult.Yes) return 1;
            }

            try
            {
                Win.KillAll();

                string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(dir))
                    dir = AppInfo.DefaultDir();

                Win.DeleteFile(AppInfo.DesktopLnk());
                Win.DeleteFile(AppInfo.StartMenuLnk());
                Win.DeleteFile(AppInfo.StartupLnk());
                Win.DeleteRegKey();

                string self = Assembly.GetExecutingAssembly().Location;
                string[] files = new string[] {
                    AppInfo.ExeName, "FpsOverlay.exe", "PresentMon.exe", "icon.ico"
                };
                for (int i = 0; i < files.Length; i++)
                    Win.DeleteFile(Path.Combine(dir, files[i]));

                try
                {
                    string[] all = Directory.GetFiles(dir);
                    for (int i = 0; i < all.Length; i++)
                    {
                        if (!string.Equals(all[i], self, StringComparison.OrdinalIgnoreCase))
                            Win.DeleteFile(all[i]);
                    }
                }
                catch { }

                try
                {
                    if (Directory.Exists(dir) && Directory.GetFileSystemEntries(dir).Length == 0)
                        Directory.Delete(dir, false);
                }
                catch { }

                try
                {
                    if (File.Exists(self))
                    {
                        string cmd = Path.Combine(Path.GetTempPath(), "fp_un_cleanup.cmd");
                        File.WriteAllText(cmd,
                            "@echo off\r\n" +
                            "ping -n 2 127.0.0.1 >nul\r\n" +
                            "del /f /q \"" + self + "\"\r\n" +
                            "rmdir /s /q \"" + dir + "\" 2>nul\r\n" +
                            "del /f /q \"%~f0\"\r\n");
                        Process.Start(cmd);
                    }
                }
                catch { }

                if (!silent)
                {
                    MessageBox.Show("FramePulse удалён.", AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return 0;
            }
            catch (Exception ex)
            {
                if (!silent)
                    MessageBox.Show(ex.Message, AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }
    }
}
