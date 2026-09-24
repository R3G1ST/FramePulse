using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

namespace FpsOverlay
{
    public class FrameSample
    {
        public double Time;
        public double MsBetween;
        public string App;
    }

    public class PresentMonClient : IDisposable
    {
        Process _proc;
        Thread _thread;
        readonly object _lock = new object();
        readonly List<FrameSample> _samples = new List<FrameSample>();
        readonly double _window = 2.0;
        public volatile bool Running;
        public string LastError = "";
        public string CsvPath;

        public int SampleCount { get { lock (_lock) { return _samples.Count; } } }

        public void Start(string exePath, string processFilter)
        {
            Stop();
            if (!File.Exists(exePath))
            {
                LastError = "PresentMon.exe не найден";
                return;
            }

            CleanupSession();

            CsvPath = Path.Combine(Path.GetTempPath(), "fpsoverlay_presents.csv");
            string errPath = Path.Combine(Path.GetTempPath(), "fpsoverlay_presentmon.err");
            try { if (File.Exists(CsvPath)) File.Delete(CsvPath); } catch { }
            try { if (File.Exists(errPath)) File.Delete(errPath); } catch { }

            string sessionName = "FpsOverlay";
            string pmArgs = "--output_stdout --no_console_stats --stop_existing_session --session_name " + sessionName;
            if (!string.IsNullOrEmpty(processFilter))
                pmArgs += " --process_name " + processFilter;

            string cmdLine = "/c start \"\" /b \"" + exePath + "\" " + pmArgs + " > \"" + CsvPath + "\" 2> \"" + errPath + "\"";
            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", cmdLine)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            try
            {
                _proc = Process.Start(psi);
                Running = true;
                LastError = "";
                _errPath = errPath;
                _thread = new Thread(ReadLoop) { IsBackground = true, Name = "PresentMonReader" };
                _thread.Start();
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                Running = false;
            }
        }

        void CleanupSession()
        {
            try
            {
                foreach (Process p in Process.GetProcessesByName("PresentMon"))
                {
                    try { p.Kill(); p.WaitForExit(1500); } catch { }
                    try { p.Dispose(); } catch { }
                }
            }
            catch { }
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("logman", "stop FpsOverlay -ets")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit(2000);
                }
            }
            catch { }
            Thread.Sleep(300);
        }

        string _errPath;

        void PollError()
        {
            try
            {
                if (string.IsNullOrEmpty(_errPath) || !File.Exists(_errPath)) return;
                string t = File.ReadAllText(_errPath).Trim();
                if (t.Length > 0) LastError = t.Split('\n')[0].Trim();
            }
            catch { }
        }

        void ReadLoop()
        {
            try
            {
                string headerLine = null;
                int idxApp = -1, idxTime = -1, idxMs = -1;
                bool timeInMs = false;
                long pos = 0;
                byte[] carry = new byte[0];

                while (Running)
                {
                    Thread.Sleep(250);
                    PollError();
                    if (!File.Exists(CsvPath)) continue;

                    try
                    {
                    long len;
                    using (FileStream fs = new FileStream(CsvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    {
                        len = fs.Length;
                        if (len <= pos) continue;
                        fs.Seek(pos, SeekOrigin.Begin);
                        byte[] chunk = new byte[len - pos];
                        int read = fs.Read(chunk, 0, chunk.Length);
                        pos = fs.Position;
                        byte[] all = new byte[carry.Length + read];
                        Buffer.BlockCopy(carry, 0, all, 0, carry.Length);
                        Buffer.BlockCopy(chunk, 0, all, carry.Length, read);

                        int lastNl = -1;
                        for (int i = all.Length - 1; i >= 0; i--)
                        {
                            if (all[i] == (byte)'\n')
                            {
                                lastNl = i;
                                if (i + 1 < all.Length && all[i + 1] == 0) lastNl = i + 1;
                                break;
                            }
                        }
                        int completeLen = lastNl >= 0 ? lastNl + 1 : all.Length;
                        carry = new byte[all.Length - completeLen];
                        Buffer.BlockCopy(all, completeLen, carry, 0, carry.Length);

                        string text = System.Text.Encoding.Unicode.GetString(all, 0, completeLen);
                        if (text.Length > 0 && text[0] == '\uFEFF') text = text.Substring(1);
                        foreach (string raw in text.Split('\n'))
                        {
                            string line = raw.TrimEnd('\r');
                            if (line.Length == 0) continue;
                            if (headerLine == null)
                            {
                                if (line.IndexOf("Application", StringComparison.OrdinalIgnoreCase) < 0) continue;
                                headerLine = line;
                                string[] h = line.Split(',');
                                for (int i = 0; i < h.Length; i++)
                                {
                                    string c = h[i].Trim();
                                    if (c.Equals("Application", StringComparison.OrdinalIgnoreCase)) idxApp = i;
                                    else if (c.Equals("TimeInSeconds", StringComparison.OrdinalIgnoreCase)) { idxTime = i; timeInMs = false; }
                                    else if (c.Equals("TimeInMs", StringComparison.OrdinalIgnoreCase)) { idxTime = i; timeInMs = true; }
                                    else if (c.Equals("MsBetweenPresents", StringComparison.OrdinalIgnoreCase)) idxMs = i;
                                }
                                if (idxApp < 0 || idxTime < 0 || idxMs < 0)
                                {
                                    LastError = "колонки CSV не найдены";
                                    break;
                                }
                                continue;
                            }
                            string[] parts = line.Split(',');
                            if (parts.Length <= Math.Max(idxApp, Math.Max(idxTime, idxMs))) continue;
                            double t, ms;
                            if (!double.TryParse(parts[idxTime], NumberStyles.Float, CultureInfo.InvariantCulture, out t)) continue;
                            if (timeInMs) t /= 1000.0;
                            if (!double.TryParse(parts[idxMs], NumberStyles.Float, CultureInfo.InvariantCulture, out ms)) continue;
                            FrameSample s = new FrameSample { Time = t, MsBetween = ms, App = parts[idxApp].Trim() };
                            lock (_lock)
                            {
                                _samples.Add(s);
                                double cutoff = t - _window;
                                int remove = 0;
                                while (remove < _samples.Count && _samples[remove].Time < cutoff) remove++;
                                if (remove > 0) _samples.RemoveRange(0, remove);
                            }
                        }
                    }
                    }
                    catch (IOException)
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
            Running = false;
        }

        public void GetStats(string filter, out double fps, out double frameTime, out double low1, out int count)
        {
            fps = 0; frameTime = 0; low1 = 0; count = 0;
            string useFilter = filter;
            if (!string.IsNullOrEmpty(useFilter))
            {
                CountFor(useFilter, out count);
                if (count == 0)
                {
                    useFilter = DominantApp();
                    if (string.IsNullOrEmpty(useFilter)) return;
                    CountFor(useFilter, out count);
                }
            }
            if (count == 0)
            {
                useFilter = DominantApp();
                if (string.IsNullOrEmpty(useFilter)) return;
                CountFor(useFilter, out count);
            }
            if (count == 0) return;

            List<double> msList = new List<double>();
            lock (_lock)
            {
                if (_samples.Count == 0) return;
                double maxT = _samples[_samples.Count - 1].Time;
                foreach (FrameSample s in _samples)
                {
                    if (s.Time < maxT - 1.0) continue;
                    if (useFilter != null && !s.App.Equals(useFilter, StringComparison.OrdinalIgnoreCase)) continue;
                    msList.Add(s.MsBetween);
                }
            }
            count = msList.Count;
            if (count == 0) return;
            double sum = 0;
            foreach (double m in msList) sum += m;
            frameTime = sum / count;
            fps = frameTime > 0.01 ? 1000.0 / frameTime : 0;
            msList.Sort();
            int idx = (int)(msList.Count * 0.99);
            if (idx >= msList.Count) idx = msList.Count - 1;
            double p99 = msList[idx];
            low1 = p99 > 0.01 ? 1000.0 / p99 : 0;
        }

        void CountFor(string app, out int count)
        {
            count = 0;
            lock (_lock)
            {
                if (_samples.Count == 0) return;
                double maxT = _samples[_samples.Count - 1].Time;
                foreach (FrameSample s in _samples)
                {
                    if (s.Time < maxT - 1.0) continue;
                    if (!s.App.Equals(app, StringComparison.OrdinalIgnoreCase)) continue;
                    count++;
                }
            }
        }

        string DominantApp()
        {
            lock (_lock)
            {
                if (_samples.Count == 0) return null;
                double maxT = _samples[_samples.Count - 1].Time;
                Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (FrameSample s in _samples)
                {
                    if (s.Time < maxT - 1.0) continue;
                    int n;
                    counts.TryGetValue(s.App, out n);
                    counts[s.App] = n + 1;
                }
                string best = null;
                int bestN = 0;
                foreach (KeyValuePair<string, int> kv in counts)
                {
                    if (kv.Value > bestN) { bestN = kv.Value; best = kv.Key; }
                }
                return best;
            }
        }

        public void Stop()
        {
            Running = false;
            try
            {
                if (_proc != null && !_proc.HasExited)
                {
                    _proc.Kill();
                    _proc.WaitForExit(2000);
                }
            }
            catch { }
            try { if (_proc != null) _proc.Dispose(); } catch { }
            _proc = null;
            CleanupSession();
            try { if (_thread != null && _thread.IsAlive) _thread.Join(1000); } catch { }
            _thread = null;
            lock (_lock) { _samples.Clear(); }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
