using System;
using System.Globalization;
using System.IO;

namespace FpsOverlay
{
    public class Config
    {
        public int Corner = 2;
        public int Opacity = 140;
        public bool ShowFps = true;
        public bool ShowFrameTime = true;
        public bool ShowCpu = true;
        public bool ShowGpu = true;
        public bool ShowRam = true;
        public bool ShowTemp = true;
        public bool ShowVram = true;
        public string ProcessFilter = "";
        public int OffsetX = 20;
        public int OffsetY = 20;
        public int AbsX = int.MinValue;
        public int AbsY = int.MinValue;
        public int MonitorIndex = -1;

        public static string Dir
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FpsOverlay"); }
        }

        public static string PathConfig
        {
            get { return Path.Combine(Dir, "config.ini"); }
        }

        public static Config Load()
        {
            Config c = new Config();
            try
            {
                if (!File.Exists(PathConfig)) return c;
                foreach (string line in File.ReadAllLines(PathConfig))
                {
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string k = line.Substring(0, eq).Trim();
                    string v = line.Substring(eq + 1).Trim();
                    switch (k)
                    {
                        case "corner": c.Corner = ClampInt(v, 0, 3, 2); break;
                        case "opacity": c.Opacity = ClampInt(v, 50, 255, c.Opacity); break;
                        case "show_fps": c.ShowFps = v == "1"; break;
                        case "show_ft": c.ShowFrameTime = v == "1"; break;
                        case "show_cpu": c.ShowCpu = v == "1"; break;
                        case "show_gpu": c.ShowGpu = v == "1"; break;
                        case "show_ram": c.ShowRam = v == "1"; break;
                        case "show_temp": c.ShowTemp = v == "1"; break;
                        case "show_vram": c.ShowVram = v == "1"; break;
                        case "process": c.ProcessFilter = v; break;
                        case "offset_x": c.OffsetX = ClampInt(v, -2000, 8000, c.OffsetX); break;
                        case "offset_y": c.OffsetY = ClampInt(v, -2000, 8000, c.OffsetY); break;
                        case "abs_x": c.AbsX = ClampInt(v, -20000, 20000, int.MinValue); break;
                        case "abs_y": c.AbsY = ClampInt(v, -20000, 20000, int.MinValue); break;
                        case "monitor": c.MonitorIndex = ClampInt(v, -1, 15, -1); break;
                    }
                }
            }
            catch { }
            return c;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                string[] lines = {
                    "corner=" + Corner,
                    "opacity=" + Opacity,
                    "show_fps=" + (ShowFps ? "1" : "0"),
                    "show_ft=" + (ShowFrameTime ? "1" : "0"),
                    "show_cpu=" + (ShowCpu ? "1" : "0"),
                    "show_gpu=" + (ShowGpu ? "1" : "0"),
                    "show_ram=" + (ShowRam ? "1" : "0"),
                    "show_temp=" + (ShowTemp ? "1" : "0"),
                    "show_vram=" + (ShowVram ? "1" : "0"),
                    "process=" + ProcessFilter,
                    "offset_x=" + OffsetX,
                    "offset_y=" + OffsetY,
                    "abs_x=" + AbsX,
                    "abs_y=" + AbsY,
                    "monitor=" + MonitorIndex
                };
                File.WriteAllLines(PathConfig, lines);
            }
            catch { }
        }

        static int ClampInt(string s, int min, int max, int def)
        {
            int v;
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
                return Math.Max(min, Math.Min(max, v));
            return def;
        }
    }
}
