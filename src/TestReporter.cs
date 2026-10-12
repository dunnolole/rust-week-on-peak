using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace RustWeekOnPEAK
{
    /// <summary>
    /// Reports the mod's own checks to Melty when a test run is active. Results are
    /// recorded all the time and written out once a test file exists, so a test that
    /// starts slightly after the plugin loads still sees them.
    /// </summary>
    public static class TestReporter
    {
        public static bool TestMode { get; private set; }

        private static string _checksPath;
        private static DateTime _lastScan = DateTime.MinValue;
        private static readonly Dictionary<string, string> _results = new Dictionary<string, string>();
        private static readonly HashSet<string> _written = new HashSet<string>();

        public static void Pass(string name)
        {
            if (!_results.ContainsKey(name)) _results[name] = "pass " + name;
        }

        public static void Fail(string name, string why)
        {
            _results[name] = "fail " + name + ": " + why;
        }

        public static void Tick()
        {
            if (!TestMode && (DateTime.UtcNow - _lastScan).TotalSeconds > 2.0)
            {
                _lastScan = DateTime.UtcNow;
                Scan();
            }
            if (!TestMode) return;

            foreach (var kv in _results)
            {
                if (_written.Contains(kv.Key)) continue;
                if (Write(kv.Value)) _written.Add(kv.Key);
            }
        }

        private static void Scan()
        {
            try
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Melty", "test");
                if (!Directory.Exists(dir)) return;

                foreach (string f in Directory.GetFiles(dir, "*.json"))
                {
                    try
                    {
                        JObject o = JObject.Parse(File.ReadAllText(f));
                        string checks = (string)o["checks"];
                        if (string.IsNullOrEmpty(checks)) continue;
                        if (!Path.IsPathRooted(checks)) checks = Path.Combine(dir, checks);
                        _checksPath = checks;
                        TestMode = true;
                        Plugin.Log.LogInfo("[RustWeek] test mode active for " + Path.GetFileName(f)
                            + " -> " + checks);
                        return;
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static bool Write(string line)
        {
            try
            {
                string path = _checksPath;
                if (string.IsNullOrEmpty(path)) return false;
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(path, line + Environment.NewLine);
                Plugin.Log.LogInfo("[RustWeek] check " + line);
                return true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[RustWeek] could not write check: " + e.Message);
                return false;
            }
        }
    }
}
