using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace FpsOverlay
{
    public class OverlayForm : Form
    {
        Config _cfg;
        PresentMonClient _pm;
        HardwareMonitor _hw;
        Timer _uiTimer;
        Timer _hwTimer;
        Timer _fgTimer;
        NotifyIcon _tray;
        ContextMenuStrip _menu;
        bool _interactive;
        string _fgProcess = "";
        string _status = "Запуск...";
        float _scale = 1f;

        double _fps, _ft, _low1;
        int _frames;

        static readonly Color PanelBg = Color.FromArgb(8, 10, 14);
        static readonly Color TextMain = Color.FromArgb(255, 255, 255);
        static readonly Color TextDim = Color.FromArgb(210, 220, 240);
        static readonly Color Accent = Color.FromArgb(160, 200, 255);
        static readonly Color Good = Color.FromArgb(100, 230, 150);
        static readonly Color Warn = Color.FromArgb(255, 210, 80);
        static readonly Color Bad = Color.FromArgb(255, 100, 100);
        static readonly Color Neon = Color.FromArgb(190, 230, 255);
        static readonly Color NeonBar = Color.FromArgb(120, 190, 255);
        static readonly Color ValLine = Color.FromArgb(90, 120, 170);
        static readonly Color StatusCol = Color.FromArgb(200, 210, 230);

        int S(int v) { return (int)Math.Round(v * _scale); }
        float Sf(float v) { return v * _scale; }

        public OverlayForm()
        {
            _cfg = Config.Load();
            _pm = new PresentMonClient();
            _hw = new HardwareMonitor();

            try
            {
                uint dpi = Native.GetDpiForSystem();
                if (dpi >= 96) _scale = dpi / 96f;
            }
            catch { _scale = 1f; }

            Text = "FramePulse";
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = PanelBg;
            Opacity = 1.0;
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Size = new Size(S(210), S(160));
            KeyPreview = true;

            BuildTray();
            ApplyClickThrough();
            PositionWindow();

            _hw.Init();

            string pmPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PresentMon.exe");
            _pm.Start(pmPath, _cfg.ProcessFilter);

            _fgTimer = new Timer { Interval = 750 };
            _fgTimer.Tick += (s, e) => UpdateForeground();
            _fgTimer.Start();

            _hwTimer = new Timer { Interval = 1000 };
            _hwTimer.Tick += (s, e) => _hw.Poll();
            _hwTimer.Start();

            _uiTimer = new Timer { Interval = 500 };
            _uiTimer.Tick += (s, e) => RefreshStats();
            _uiTimer.Start();

            UpdateForeground();
            RefreshStats();
        }

        ToolStripMenuItem _miShow;
        ToolStripMenuItem _miSettings;
        ToolStripMenuItem _miDrag;

        void BuildTray()
        {
            Icon icon = null;
            try
            {
                string ico = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                if (File.Exists(ico)) icon = new Icon(ico);
                else icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }

            _menu = new ContextMenuStrip();

            _miShow = new ToolStripMenuItem("Скрыть оверлей");
            _miShow.Click += (s, e) => ToggleOverlayVisible();
            _menu.Items.Add(_miShow);

            _miSettings = new ToolStripMenuItem("Настройки…");
            _miSettings.Click += (s, e) => OpenSettings();
            _menu.Items.Add(_miSettings);

            _menu.Items.Add(new ToolStripSeparator());

            _miDrag = new ToolStripMenuItem("Свободное перемещение");
            _miDrag.Click += (s, e) => ToggleInteractive();
            _menu.Items.Add(_miDrag);

            var miRestart = new ToolStripMenuItem("Перезапустить PresentMon");
            miRestart.Click += (s, e) => RestartPm();
            _menu.Items.Add(miRestart);

            _menu.Items.Add(new ToolStripSeparator());

            var miExit = new ToolStripMenuItem("Закрыть");
            miExit.Click += (s, e) => ExitApp();
            _menu.Items.Add(miExit);

            _menu.Opening += (s, e) =>
            {
                _miShow.Text = Visible ? "Скрыть оверлей" : "Показать оверлей";
                if (_miDrag != null)
                {
                    _miDrag.Checked = _interactive;
                    _miDrag.Text = _interactive ? "Свободное перемещение (вкл)" : "Свободное перемещение";
                }
            };

            _tray = new NotifyIcon
            {
                Icon = icon,
                Text = "FramePulse",
                Visible = true,
                ContextMenuStrip = _menu
            };
            _tray.DoubleClick += (s, e) => ToggleOverlayVisible();
            try
            {
                _tray.ShowBalloonTip(4000, "FramePulse",
                    "ПКМ по иконке — меню\nДвойной клик — скрыть/показать",
                    ToolTipIcon.Info);
            }
            catch { }
        }

        void ToggleOverlayVisible()
        {
            if (Visible)
            {
                Hide();
            }
            else
            {
                Show();
                Activate();
                PresentLayered();
            }
        }

        void OpenSettings()
        {
            using (var f = new SettingsForm(_cfg, _interactive, ApplyLive, ApplyPosition, RestartPm, drag =>
            {
                if (drag != _interactive) ToggleInteractive();
            }))
            {
                f.ShowDialog();
            }
        }

        void ApplyLive()
        {
            _cfg.Save();
            SyncSize();
            PresentLayered(true);
        }

        void ApplyPosition()
        {
            _cfg.Save();
            SyncSize();
            PositionWindow();
            PresentLayered(true);
        }

        void RestartPm()
        {
            string pmPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PresentMon.exe");
            _pm.Start(pmPath, _cfg.ProcessFilter);
        }

        void SaveAndPlace()
        {
            _cfg.Save();
            PositionWindow();
            PresentLayered();
        }

        static Screen ScreenByIndex(int idx)
        {
            Screen[] all = Screen.AllScreens;
            if (idx < 0 || idx >= all.Length) return Screen.PrimaryScreen;
            return all[idx];
        }

        int IndexOfScreen(Screen s)
        {
            Screen[] all = Screen.AllScreens;
            for (int i = 0; i < all.Length; i++)
                if (all[i] == s || all[i].DeviceName == s.DeviceName) return i;
            return 0;
        }

        void PositionWindow()
        {
            if (_cfg.AbsX != int.MinValue && _cfg.AbsY != int.MinValue)
            {
                Point p = ClampToVirtualScreen(new Point(_cfg.AbsX, _cfg.AbsY));
                Location = p;
                return;
            }
            Screen scr = ScreenByIndex(_cfg.MonitorIndex);
            Rectangle sa = scr.WorkingArea;
            int x, y;
            int ox = Math.Abs(_cfg.OffsetX), oy = Math.Abs(_cfg.OffsetY);
            switch (_cfg.Corner)
            {
                case 1:
                    x = sa.Right - Width - ox;
                    y = sa.Top + oy;
                    break;
                case 2:
                    x = sa.Left + ox;
                    y = sa.Bottom - Height - oy;
                    break;
                case 3:
                    x = sa.Right - Width - ox;
                    y = sa.Bottom - Height - oy;
                    break;
                default:
                    x = sa.Left + ox;
                    y = sa.Top + oy;
                    break;
            }
            Location = new Point(x, y);
            _cfg.AbsX = Location.X;
            _cfg.AbsY = Location.Y;
            _cfg.MonitorIndex = IndexOfScreen(scr);
            _cfg.Save();
        }

        Point ClampToVirtualScreen(Point p)
        {
            Rectangle vs = SystemInformation.VirtualScreen;
            int x = Math.Max(vs.Left, Math.Min(p.X, vs.Right - Width));
            int y = Math.Max(vs.Top, Math.Min(p.Y, vs.Bottom - Height));
            if (x < vs.Left) x = vs.Left;
            if (y < vs.Top) y = vs.Top;
            return new Point(x, y);
        }

        void MoveToMonitor(int idx)
        {
            Screen scr = ScreenByIndex(idx);
            Rectangle sa = scr.WorkingArea;
            int ox = Math.Abs(_cfg.OffsetX), oy = Math.Abs(_cfg.OffsetY);
            int x, y;
            switch (_cfg.Corner)
            {
                case 1:
                    x = sa.Right - Width - ox;
                    y = sa.Top + oy;
                    break;
                case 2:
                    x = sa.Left + ox;
                    y = sa.Bottom - Height - oy;
                    break;
                case 3:
                    x = sa.Right - Width - ox;
                    y = sa.Bottom - Height - oy;
                    break;
                default:
                    x = sa.Left + ox;
                    y = sa.Top + oy;
                    break;
            }
            _cfg.MonitorIndex = idx;
            _cfg.AbsX = x;
            _cfg.AbsY = y;
            _cfg.Save();
            Location = new Point(x, y);
        }

        void ToggleInteractive()
        {
            _interactive = !_interactive;
            ApplyClickThrough();
            if (_interactive)
            {
                BringToFront();
                TopMost = true;
                Activate();
                Focus();
            }
            PresentLayered();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Native.RegisterHotKey(Handle, 1, Native.MOD_CONTROL | Native.MOD_SHIFT | Native.MOD_NOREPEAT, Native.VK_F10);
            Native.RegisterHotKey(Handle, 2, Native.MOD_CONTROL | Native.MOD_SHIFT | Native.MOD_NOREPEAT, Native.VK_F11);
            Native.RegisterHotKey(Handle, 3, Native.MOD_CONTROL | Native.MOD_SHIFT | Native.MOD_NOREPEAT, Native.VK_F12);
            ApplyClickThrough();
            PresentLayered();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            Native.UnregisterHotKey(Handle, 1);
            Native.UnregisterHotKey(Handle, 2);
            Native.UnregisterHotKey(Handle, 3);
            base.OnHandleDestroyed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == 1) Visible = !Visible;
                else if (id == 2)
                {
                    Screen cur = Screen.FromHandle(Handle);
                    int n = Screen.AllScreens.Length;
                    int i = IndexOfScreen(cur);
                    MoveToMonitor((i + 1) % n);
                }
                else if (id == 3) ToggleInteractive();
                return;
            }
            base.WndProc(ref m);
        }

        void ApplyClickThrough()
        {
            int ex = Native.GetWindowLong(Handle, Native.GWL_EXSTYLE);
            int style = ex | Native.WS_EX_LAYERED | Native.WS_EX_TOOLWINDOW;
            if (_interactive) style &= ~Native.WS_EX_TRANSPARENT;
            else style |= Native.WS_EX_TRANSPARENT;
            Native.SetWindowLong(Handle, Native.GWL_EXSTYLE, style);
        }

        void UpdateForeground()
        {
            if (!string.IsNullOrEmpty(_cfg.ProcessFilter))
            {
                _fgProcess = _cfg.ProcessFilter;
                return;
            }
            string name = HardwareMonitor.GetForegroundProcessName();
            if (!string.IsNullOrEmpty(name)) _fgProcess = name;
        }

        void RefreshStats()
        {
            if (_pm.Running)
            {
                _pm.GetStats(_fgProcess, out _fps, out _ft, out _low1, out _frames);
                if (_frames == 0)
                    _pm.GetStats("", out _fps, out _ft, out _low1, out _frames);
                _status = _frames > 0 ? _fgProcess : "нет кадров (" + _pm.SampleCount + " замеров)";
            }
            else
            {
                _fps = 0; _frames = 0;
                _status = string.IsNullOrEmpty(_pm.LastError) ? "PresentMon остановлен" : _pm.LastError;
            }
            PresentLayered();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape && _interactive)
            {
                _interactive = false;
                ApplyClickThrough();
                PresentLayered();
            }
            base.OnKeyDown(e);
        }

        Point _dragStart;
        bool _dragging;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (_interactive && e.Button == MouseButtons.Left)
            {
                _dragging = true;
                _dragStart = e.Location;
                Capture = true;
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_dragging && _interactive)
            {
                Location = new Point(Location.X + e.X - _dragStart.X, Location.Y + e.Y - _dragStart.Y);
                PresentLayered();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && _dragging)
            {
                _dragging = false;
                Capture = false;
                _cfg.AbsX = Location.X;
                _cfg.AbsY = Location.Y;
                _cfg.MonitorIndex = IndexOfScreen(Screen.FromHandle(Handle));
                _cfg.Save();
            }
            PresentLayered();
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            PresentLayered();
        }

        void PresentLayered()
        {
            PresentLayered(false);
        }

        void PresentLayered(bool force)
        {
            if (!IsHandleCreated) return;
            if (!Visible && !force) return;
            try
            {
                SyncSize();
                int w = Math.Max(1, Width);
                int h = Math.Max(1, Height);
                using (Bitmap bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                        DrawOverlay(g, w, h);
                    }
                    Native.PresentBitmap(Handle, bmp, Location);
                }
            }
            catch { }
        }

        int ContentEndY()
        {
            int y = S(6);
            if (_cfg.ShowFps) y += _frames > 0 ? S(24) : S(18);
            if (_cfg.ShowFrameTime) y += S(15);
            y += S(1) + S(5);
            if (_cfg.ShowCpu) y += S(18);
            if (_cfg.ShowGpu) y += S(18);
            if (_cfg.ShowTemp) y += S(16);
            if (_cfg.ShowVram && _hw.VramTotalMB > 0) y += S(16);
            if (_cfg.ShowRam && _hw.RamTotalMB > 0) y += S(16);
            return y;
        }

        int CalcHeight()
        {
            int h = ContentEndY() + S(4) + S(13) + S(6);
            return Math.Max(h, S(72));
        }

        void SyncSize()
        {
            int w = S(210);
            int h = CalcHeight();
            if (w == Width && h == Height) return;
            bool bottom = (_cfg.Corner == 2 || _cfg.Corner == 3) && _cfg.AbsX == int.MinValue;
            Size = new Size(w, h);
            if (bottom) PositionWindow();
        }

        void DrawOverlay(Graphics g, int width, int height)
        {
            int panelAlpha = Math.Max(40, Math.Min(255, _cfg.Opacity));
            float sc = _scale;
            int r = S(6);

            SmoothingMode oldSm = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.None;
            using (GraphicsPath panel = ValPanel(new Rectangle(0, 0, width - 1, height - 1), r))
            using (Region rg = new Region(panel))
            {
                g.Clip = rg;
                using (SolidBrush bg = new SolidBrush(Color.FromArgb(panelAlpha, PanelBg)))
                    g.FillRectangle(bg, 0, 0, width, height);
                g.ResetClip();
            }
            g.SmoothingMode = oldSm;
            using (SolidBrush accent = new SolidBrush(Color.FromArgb(Math.Min(255, panelAlpha + 20), Neon.R, Neon.G, Neon.B)))
                g.FillRectangle(accent, S(1), S(8), S(3), height - S(16));

            int y = S(6);
            int pad = S(10);
            int rowFps = S(24);
            int rowFt = S(15);
            int rowBar = S(18);
            int rowSm = S(16);

            using (Font fNum = new Font("Segoe UI", Sf(13f), FontStyle.Bold))
            using (Font fLbl = new Font("Segoe UI", Sf(7.5f)))
            using (Font fSm = new Font("Segoe UI", Sf(8f), FontStyle.Bold))
            using (SolidBrush brMain = new SolidBrush(TextMain))
            using (SolidBrush brDim = new SolidBrush(TextDim))
            using (SolidBrush brNeon = new SolidBrush(Neon))
            {
                if (_cfg.ShowFps && _frames > 0)
                {
                    string fpsText = ((int)Math.Round(_fps)).ToString();
                    Size sz = TextRenderer.MeasureText(g, fpsText, fNum);
                    g.DrawString(fpsText, fNum, brNeon, pad, y);
                    g.DrawString("FPS", fLbl, brDim, pad + sz.Width + S(3), y + sz.Height - S(13));
                    y += rowFps;
                }
                else if (_cfg.ShowFps)
                {
                    g.DrawString("FPS  --", fSm, brDim, pad, y + S(2));
                    y += S(18);
                }

                {
                    string clock = DateTime.Now.ToString("HH:mm");
                    Size cs = TextRenderer.MeasureText(g, clock, fSm);
                    g.DrawString(clock, fSm, brDim, width - pad - cs.Width, S(8));
                }

                if (_cfg.ShowFrameTime)
                {
                    string ft = _frames > 0
                        ? string.Format(CultureInfo.InvariantCulture, "{0:0.0} ms  low1 {1:0}", _ft, _low1)
                        : "--";
                    g.DrawString(ft, fLbl, brDim, pad, y);
                    y += rowFt;
                }

                y += S(1);
                using (Pen lp = new Pen(ValLine, Math.Max(1f, sc)))
                    g.DrawLine(lp, pad, y, width - pad, y);
                y += S(5);

                if (_cfg.ShowCpu)
                {
                    DrawBar(g, "ЦП", _hw.CpuUtil, pad, y, width, fSm, fLbl, brDim, brMain, sc);
                    y += rowBar;
                }
                if (_cfg.ShowGpu)
                {
                    DrawBar(g, "ГП", _hw.GpuUtil, pad, y, width, fSm, fLbl, brDim, brMain, sc);
                    y += rowBar;
                }
                if (_cfg.ShowTemp)
                {
                    string t = _hw.GpuTemp > 0 ? _hw.GpuTemp + "°C" : "--";
                    Color tc = _hw.GpuTemp >= 85 ? Bad : (_hw.GpuTemp >= 75 ? Warn : TextMain);
                    g.DrawString("ТЕМП", fLbl, brDim, pad, y + S(1));
                    using (SolidBrush bt = new SolidBrush(tc))
                        g.DrawString(t, fSm, bt, pad + S(44), y);
                    y += rowSm;
                }
                if (_cfg.ShowVram && _hw.VramTotalMB > 0)
                {
                    g.DrawString("ГП", fLbl, brDim, pad, y + S(1));
                    g.DrawString(string.Format(CultureInfo.InvariantCulture, "{0:0.0}/{1:0.0} GB", _hw.VramUsedMB / 1024.0, _hw.VramTotalMB / 1024.0), fSm, brMain, pad + S(44), y);
                    y += rowSm;
                }
                if (_cfg.ShowRam && _hw.RamTotalMB > 0)
                {
                    g.DrawString("ОЗУ", fLbl, brDim, pad, y + S(1));
                    g.DrawString(string.Format(CultureInfo.InvariantCulture, "{0:0.0}/{1:0.0} GB", _hw.RamUsedMB / 1024.0, _hw.RamTotalMB / 1024.0), fSm, brMain, pad + S(44), y);
                    y += rowSm;
                }
            }

            using (Font fSt = new Font("Segoe UI", Sf(6.5f)))
            {
                string st = _status ?? "";
                int maxCh = Math.Max(16, (int)((width - pad * 2) / (Sf(6.5f) * 0.55f)));
                if (st.Length > maxCh) st = st.Substring(0, maxCh);
                Size stSz = TextRenderer.MeasureText(g, st, fSt);
                int stY = y + S(4);
                if (stY + stSz.Height > height - S(3))
                    stY = height - stSz.Height - S(3);
                if (stY < S(4)) stY = S(4);
                g.DrawString(st, fSt, new SolidBrush(StatusCol), pad, stY);
            }

            if (_interactive)
            {
                SmoothingMode sm2 = g.SmoothingMode;
                g.SmoothingMode = SmoothingMode.None;
                using (Pen pen = new Pen(Accent, Math.Max(2f, sc * 2f)))
                    g.DrawRectangle(pen, 1, 1, width - 3, height - 3);
                g.SmoothingMode = sm2;
                using (Font fHint = new Font("Segoe UI", Sf(7f)))
                {
                    string hint = "перетащи  esc=выкл";
                    Size hs = TextRenderer.MeasureText(g, hint, fHint);
                    g.DrawString(hint, fHint, new SolidBrush(Accent), width - pad - hs.Width, S(8));
                }
            }
        }

        void DrawBar(Graphics g, string label, double val, int x, int y, int width, Font fSm, Font fLbl, SolidBrush brDim, SolidBrush brMain, float sc)
        {
            val = Math.Max(0, Math.Min(100, val));
            g.DrawString(label, fLbl, brDim, x, y + (int)Math.Round(sc));
            g.DrawString(((int)Math.Round(val)) + "%", fSm, brMain, x + (int)Math.Round(44 * sc), y);
            int bw = width - x - (int)Math.Round(72 * sc) - (int)Math.Round(10 * sc);
            if (bw < (int)Math.Round(20 * sc)) bw = (int)Math.Round(20 * sc);
            Rectangle bar = new Rectangle(x + (int)Math.Round(72 * sc), y + (int)Math.Round(4 * sc), bw, Math.Max(5, (int)Math.Round(6 * sc)));
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(160, 30, 40, 55)))
                g.FillRectangle(bg, bar);
            Color c = val >= 90 ? Bad : (val >= 75 ? Warn : NeonBar);
            int w = (int)(bar.Width * val / 100.0);
            if (w > 0)
            {
                Rectangle fill = new Rectangle(bar.X, bar.Y, w, bar.Height);
                using (SolidBrush b = new SolidBrush(c))
                    g.FillRectangle(b, fill);
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            PresentLayered();
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            PresentLayered();
        }

        static GraphicsPath ValPanel(Rectangle r, int radius)
        {
            int d = radius * 2;
            GraphicsPath p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        static GraphicsPath Rounded(Rectangle r, int radius)
        {
            return ValPanel(r, radius);
        }

        void ExitApp()
        {
            _cfg.Save();
            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_tray != null) _tray.Visible = false;
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_uiTimer != null) _uiTimer.Dispose();
                if (_hwTimer != null) _hwTimer.Dispose();
                if (_fgTimer != null) _fgTimer.Dispose();
                if (_pm != null) _pm.Dispose();
                if (_hw != null) _hw.Shutdown();
                if (_tray != null) _tray.Dispose();
                if (_menu != null) _menu.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
