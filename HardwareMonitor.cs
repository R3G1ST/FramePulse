using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FpsOverlay
{
    public class HardwareMonitor
    {
        [DllImport("nvml.dll")]
        static extern int nvmlInit_v2();
        [DllImport("nvml.dll")]
        static extern int nvmlShutdown_v2();
        [DllImport("nvml.dll")]
        static extern int nvmlDeviceGetHandleByIndex_v2(uint index, out IntPtr device);
        [DllImport("nvml.dll")]
        static extern int nvmlDeviceGetTemperature(IntPtr device, int sensorType, out uint temp);
        [DllImport("nvml.dll")]
        static extern int nvmlDeviceGetUtilizationRates(IntPtr device, out NvmlUtilization util);
        [DllImport("nvml.dll")]
        static extern int nvmlDeviceGetMemoryInfo(IntPtr device, out NvmlMemory mem);
        [DllImport("nvml.dll")]
        static extern int nvmlDeviceGetName(IntPtr device, byte[] name, uint length);

        [StructLayout(LayoutKind.Sequential)]
        struct NvmlUtilization { public uint gpu; public uint memory; }
        [StructLayout(LayoutKind.Sequential)]
        struct NvmlMemory { public ulong total; public ulong used; public ulong free; }

        const int NVML_SUCCESS = 0;
        const int NVML_TEMPERATURE_GPU = 0;

        bool _nvmlReady;
        IntPtr _dev;
        PerformanceCounter _cpu;
        bool _cpuPrimed;

        public string GpuName = "NVIDIA GPU";
        public uint GpuTemp;
        public uint GpuUtil;
        public ulong VramUsedMB;
        public ulong VramTotalMB;
        public double CpuUtil;
        public long RamUsedMB;
        public long RamTotalMB;
        public bool NvmlOk;

        public void Init()
        {
            try
            {
                if (nvmlInit_v2() == NVML_SUCCESS)
                {
                    IntPtr dev;
                    if (nvmlDeviceGetHandleByIndex_v2(0, out dev) == NVML_SUCCESS)
                    {
                        _dev = dev;
                        byte[] buf = new byte[96];
                        if (nvmlDeviceGetName(_dev, buf, (uint)buf.Length) == NVML_SUCCESS)
                            GpuName = System.Text.Encoding.ASCII.GetString(buf).TrimEnd('\0');
                        _nvmlReady = true;
                        NvmlOk = true;
                    }
                }
            }
            catch { NvmlOk = false; }

            try
            {
                _cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpu.NextValue();
                _cpuPrimed = true;
            }
            catch { _cpu = null; }

            Native.MEMORYSTATUSEX ms = new Native.MEMORYSTATUSEX();
            ms.dwLength = (uint)Marshal.SizeOf(typeof(Native.MEMORYSTATUSEX));
            if (Native.GlobalMemoryStatusEx(ref ms))
                RamTotalMB = (long)(ms.ullTotalPhys / (1024UL * 1024UL));
        }

        public void Poll()
        {
            if (_cpuPrimed && _cpu != null)
            {
                try { CpuUtil = _cpu.NextValue(); }
                catch { }
            }

            Native.MEMORYSTATUSEX ms = new Native.MEMORYSTATUSEX();
            ms.dwLength = (uint)Marshal.SizeOf(typeof(Native.MEMORYSTATUSEX));
            if (Native.GlobalMemoryStatusEx(ref ms))
            {
                RamUsedMB = (long)((ms.ullTotalPhys - ms.ullAvailPhys) / (1024UL * 1024UL));
                RamTotalMB = (long)(ms.ullTotalPhys / (1024UL * 1024UL));
            }

            if (_nvmlReady)
            {
                uint t;
                if (nvmlDeviceGetTemperature(_dev, NVML_TEMPERATURE_GPU, out t) == NVML_SUCCESS)
                    GpuTemp = t;
                NvmlUtilization u;
                if (nvmlDeviceGetUtilizationRates(_dev, out u) == NVML_SUCCESS)
                    GpuUtil = u.gpu;
                NvmlMemory mem;
                if (nvmlDeviceGetMemoryInfo(_dev, out mem) == NVML_SUCCESS)
                {
                    VramUsedMB = mem.used / (1024UL * 1024UL);
                    VramTotalMB = mem.total / (1024UL * 1024UL);
                }
            }
        }

        public void Shutdown()
        {
            try { if (_cpu != null) _cpu.Dispose(); } catch { }
            try { if (_nvmlReady) nvmlShutdown_v2(); } catch { }
        }

        public static string GetForegroundProcessName()
        {
            try
            {
                IntPtr hwnd = Native.GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return "";
                uint pid;
                Native.GetWindowThreadProcessId(hwnd, out pid);
                if (pid == 0) return "";
                Process p = Process.GetProcessById((int)pid);
                return p.ProcessName + ".exe";
            }
            catch { return ""; }
        }
    }
}
