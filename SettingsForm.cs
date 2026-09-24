using System;
using System.Drawing;
using System.Windows.Forms;

namespace FpsOverlay
{
    public class SettingsForm : Form
    {
        readonly Config _cfg;
        readonly Action _apply;
        readonly Action _applyPos;
        readonly Action _restartPm;
        readonly Action<bool> _setDrag;

        CheckBox _fps, _ft, _cpu, _gpu, _temp, _vram, _ram;
        TrackBar _opacity;
        Label _opacityVal;
        ComboBox _corner;
        ComboBox _monitor;
        TextBox _process;
        Button _btnDrag, _btnRestart, _btnClose, _btnCancel;
        bool _drag;
        bool _live;
        bool _processChanged;

        int _oCorner, _oOpacity, _oMon;
        string _oProc;
        bool _oFps, _oFt, _oCpu, _oGpu, _oTemp, _oVram, _oRam, _oDrag;

        public bool ProcessChanged { get { return _processChanged; } }

        public SettingsForm(Config cfg, bool dragMode, Action apply, Action applyPos, Action restartPm, Action<bool> setDrag)
        {
            _cfg = cfg;
            _drag = dragMode;
            _apply = apply;
            _applyPos = applyPos;
            _restartPm = restartPm;
            _setDrag = setDrag;

            Snapshot();

            Text = "FramePulse — настройки";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            ClientSize = new Size(420, 560);
            BackColor = Color.FromArgb(18, 20, 26);
            Font = new Font("Segoe UI", 9f);
            KeyPreview = true;

            BuildUi();
            LoadFromConfig();
            _live = true;
        }

        void Snapshot()
        {
            _oCorner = _cfg.Corner;
            _oOpacity = _cfg.Opacity;
            _oMon = _cfg.MonitorIndex;
            _oProc = _cfg.ProcessFilter;
            _oFps = _cfg.ShowFps;
            _oFt = _cfg.ShowFrameTime;
            _oCpu = _cfg.ShowCpu;
            _oGpu = _cfg.ShowGpu;
            _oTemp = _cfg.ShowTemp;
            _oVram = _cfg.ShowVram;
            _oRam = _cfg.ShowRam;
            _oDrag = _drag;
        }

        void Restore()
        {
            _cfg.Corner = _oCorner;
            _cfg.Opacity = _oOpacity;
            _cfg.MonitorIndex = _oMon;
            _cfg.ProcessFilter = _oProc;
            _cfg.ShowFps = _oFps;
            _cfg.ShowFrameTime = _oFt;
            _cfg.ShowCpu = _oCpu;
            _cfg.ShowGpu = _oGpu;
            _cfg.ShowTemp = _oTemp;
            _cfg.ShowVram = _oVram;
            _cfg.ShowRam = _oRam;
            _drag = _oDrag;
            if (_setDrag != null) _setDrag(_drag);
            if (_applyPos != null) _applyPos();
            else if (_apply != null) _apply();
        }

        void Live()
        {
            if (!_live) return;
            PushToConfig();
            if (_apply != null) _apply();
            if (_applyPos != null) _applyPos();
        }

        void LivePos()
        {
            if (!_live) return;
            PushToConfig();
            if (_applyPos != null) _applyPos();
            else if (_apply != null) _apply();
        }

        void PushToConfig()
        {
            _cfg.ShowFps = _fps.Checked;
            _cfg.ShowFrameTime = _ft.Checked;
            _cfg.ShowCpu = _cpu.Checked;
            _cfg.ShowGpu = _gpu.Checked;
            _cfg.ShowTemp = _temp.Checked;
            _cfg.ShowVram = _vram.Checked;
            _cfg.ShowRam = _ram.Checked;
            _cfg.Opacity = _opacity.Value;
            if (_corner.SelectedIndex >= 0 && _corner.SelectedIndex <= 3)
            {
                _cfg.Corner = _corner.SelectedIndex;
                _cfg.AbsX = int.MinValue;
                _cfg.AbsY = int.MinValue;
            }
            if (_monitor.SelectedIndex >= 0) _cfg.MonitorIndex = _monitor.SelectedIndex;
            string np = (_process.Text ?? "").Trim();
            if (np != (_cfg.ProcessFilter ?? "")) _processChanged = true;
            _cfg.ProcessFilter = np;
        }

        void BuildUi()
        {
            int y = 14;
            int x = 14;
            int w = 392;

            AddSection("Отображение", ref y, x, w);
            _fps = AddCheck("FPS", ref y, x);
            _ft = AddCheck("Время кадра (ms / low1)", ref y, x);
            _cpu = AddCheck("Загрузка ЦП", ref y, x);
            _gpu = AddCheck("Загрузка ГП", ref y, x);
            _temp = AddCheck("Температура ГП", ref y, x);
            _vram = AddCheck("Видеопамять", ref y, x);
            _ram = AddCheck("Оперативная память", ref y, x);

            foreach (var cb in new[] { _fps, _ft, _cpu, _gpu, _temp, _vram, _ram })
                cb.CheckedChanged += (s, e) => Live();

            y += 6;
            AddSection("Прозрачность фона", ref y, x, w);
            _opacity = new TrackBar
            {
                Minimum = 50,
                Maximum = 255,
                TickFrequency = 15,
                Value = Math.Max(50, Math.Min(255, _cfg.Opacity)),
                Left = x,
                Top = y,
                Width = w - 70,
                AutoSize = false,
                Height = 32
            };
            _opacity.ValueChanged += (s, e) =>
            {
                _opacityVal.Text = _opacity.Value.ToString();
                Live();
            };
            Controls.Add(_opacity);
            _opacityVal = new Label
            {
                Left = x + w - 55,
                Top = y + 6,
                Width = 55,
                Text = _opacity.Value.ToString(),
                ForeColor = Color.FromArgb(190, 230, 255)
            };
            Controls.Add(_opacityVal);
            y += 40;

            AddSection("Позиция", ref y, x, w);
            var lblCorner = new Label { Left = x, Top = y + 4, Width = 80, Text = "Угол:", ForeColor = Color.FromArgb(210, 220, 240) };
            Controls.Add(lblCorner);
            _corner = new ComboBox
            {
                Left = x + 85,
                Top = y,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _corner.Items.AddRange(new object[] { "Слева сверху", "Справа сверху", "Слева снизу", "Справа снизу", "Свободная (перетаскивание)" });
            _corner.SelectedIndexChanged += (s, e) => LivePos();
            Controls.Add(_corner);
            y += 34;

            var lblMon = new Label { Left = x, Top = y + 4, Width = 80, Text = "Монитор:", ForeColor = Color.FromArgb(210, 220, 240) };
            Controls.Add(lblMon);
            _monitor = new ComboBox
            {
                Left = x + 85,
                Top = y,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                Screen sc = Screen.AllScreens[i];
                _monitor.Items.Add("Монитор " + (i + 1) + (sc.Primary ? " (основной)" : "") + " " + sc.Bounds.Width + "x" + sc.Bounds.Height);
            }
            _monitor.SelectedIndexChanged += (s, e) => LivePos();
            Controls.Add(_monitor);
            y += 40;

            AddSection("Процесс (фильтр)", ref y, x, w);
            _process = new TextBox
            {
                Left = x,
                Top = y,
                Width = w - 140,
                Text = _cfg.ProcessFilter ?? ""
            };
            Controls.Add(_process);
            _process.TextChanged += (s, e) =>
            {
                if (!_live) return;
                _cfg.ProcessFilter = (_process.Text ?? "").Trim();
                _processChanged = true;
            };
            var lblHint = new Label
            {
                Left = x + w - 135,
                Top = y + 4,
                Width = 135,
                Text = "пусто = активное окно",
                ForeColor = Color.FromArgb(140, 150, 170),
                Font = new Font("Segoe UI", 7.5f)
            };
            Controls.Add(lblHint);
            y += 36;

            _btnDrag = new Button
            {
                Left = x,
                Top = y,
                Width = 185,
                Height = 32,
                Text = _drag ? "Перемещение: ВКЛ" : "Свободное перемещение",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 40, 60),
                ForeColor = Color.FromArgb(190, 230, 255)
            };
            _btnDrag.FlatAppearance.BorderColor = Color.FromArgb(90, 120, 180);
            _btnDrag.Click += (s, e) =>
            {
                _drag = !_drag;
                _btnDrag.Text = _drag ? "Перемещение: ВКЛ" : "Свободное перемещение";
                if (_setDrag != null) _setDrag(_drag);
                if (_apply != null) _apply();
            };
            Controls.Add(_btnDrag);

            _btnRestart = new Button
            {
                Left = x + 195,
                Top = y,
                Width = 175,
                Height = 32,
                Text = "Перезапустить PresentMon",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 40, 60),
                ForeColor = Color.FromArgb(190, 230, 255)
            };
            _btnRestart.FlatAppearance.BorderColor = Color.FromArgb(90, 120, 180);
            _btnRestart.Click += (s, e) =>
            {
                PushToConfig();
                if (_processChanged && _restartPm != null) _restartPm();
                else if (_restartPm != null) _restartPm();
            };
            Controls.Add(_btnRestart);
            y += 48;

            _btnCancel = new Button
            {
                Left = x + w - 175,
                Top = y,
                Width = 80,
                Height = 34,
                Text = "Отмена",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 40, 40),
                ForeColor = Color.FromArgb(230, 200, 200)
            };
            _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(120, 80, 80);
            _btnCancel.Click += (s, e) =>
            {
                Restore();
                DialogResult = DialogResult.Cancel;
                Close();
            };
            Controls.Add(_btnCancel);

            _btnClose = new Button
            {
                Left = x + w - 85,
                Top = y,
                Width = 85,
                Height = 34,
                Text = "Готово",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 80, 140),
                ForeColor = Color.White
            };
            _btnClose.FlatAppearance.BorderColor = Color.FromArgb(120, 190, 255);
            _btnClose.Click += (s, e) =>
            {
                PushToConfig();
                if (_applyPos != null) _applyPos();
                else if (_apply != null) _apply();
                if (_processChanged && _restartPm != null) _restartPm();
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(_btnClose);

            AcceptButton = _btnClose;
            CancelButton = _btnCancel;
        }

        void AddSection(string title, ref int y, int x, int w)
        {
            var lbl = new Label
            {
                Left = x,
                Top = y,
                Width = w,
                Text = title,
                ForeColor = Color.FromArgb(160, 200, 255),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            Controls.Add(lbl);
            y += 22;
        }

        CheckBox AddCheck(string text, ref int y, int x)
        {
            var cb = new CheckBox
            {
                Left = x + 4,
                Top = y,
                Width = 340,
                Text = text,
                ForeColor = Color.FromArgb(230, 235, 245),
                AutoSize = true
            };
            Controls.Add(cb);
            y += 24;
            return cb;
        }

        void LoadFromConfig()
        {
            _fps.Checked = _cfg.ShowFps;
            _ft.Checked = _cfg.ShowFrameTime;
            _cpu.Checked = _cfg.ShowCpu;
            _gpu.Checked = _cfg.ShowGpu;
            _temp.Checked = _cfg.ShowTemp;
            _vram.Checked = _cfg.ShowVram;
            _ram.Checked = _cfg.ShowRam;
            _opacity.Value = Math.Max(50, Math.Min(255, _cfg.Opacity));
            _opacityVal.Text = _opacity.Value.ToString();
            _corner.SelectedIndex = _cfg.AbsX == int.MinValue ? _cfg.Corner : 4;
            if (_corner.SelectedIndex < 0) _corner.SelectedIndex = 2;
            _monitor.SelectedIndex = _cfg.MonitorIndex >= 0 && _cfg.MonitorIndex < _monitor.Items.Count ? _cfg.MonitorIndex : 0;
            _process.Text = _cfg.ProcessFilter ?? "";
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Restore();
                DialogResult = DialogResult.Cancel;
                Close();
            }
            base.OnKeyDown(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.None)
            {
                Restore();
                DialogResult = DialogResult.Cancel;
            }
            base.OnFormClosing(e);
        }
    }
}
