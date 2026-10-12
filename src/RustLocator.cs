using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace RustWeekOnPEAK
{
    /// <summary>
    /// Finds the player's own Rust install. Rust content is only ever read from the
    /// player's copy; nothing from Rust is shipped inside this mashup.
    /// </summary>
    internal static class RustLocator
    {
        public static string FoundBy = "not found";

        public static bool IsRustDir(string dir)
        {
            if (string.IsNullOrEmpty(dir)) return false;
            return File.Exists(Path.Combine(dir, "Rust.exe"))
                && Directory.Exists(Path.Combine(dir, "Bundles", "items"));
        }

        public static string Find()
        {
            // 1. Launch argument passed by the installer: --rust-dir=<path>
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length; i++)
                {
                    string a = args[i];
                    if (a.StartsWith("--rust-dir=", StringComparison.OrdinalIgnoreCase))
                    {
                        string d = Clean(a.Substring("--rust-dir=".Length));
                        if (IsRustDir(d)) { FoundBy = "launch arg"; return d; }
                    }
                    if (a.Equals("--rust-dir", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                    {
                        string d = Clean(args[i + 1]);
                        if (IsRustDir(d)) { FoundBy = "launch arg"; return d; }
                    }
                }
            }
            catch { }

            // 2. Environment variables (handy for manual runs / other installers).
            string[] envNames = { "RUSTWEEK_RUST_DIR", "RUST_DIR", "RUST_GAME_DIR" };
            foreach (string name in envNames)
            {
                try
                {
                    string d = Clean(Environment.GetEnvironmentVariable(name));
                    if (IsRustDir(d)) { FoundBy = "env " + name; return d; }
                }
                catch { }
            }

            // 3. Steam libraries from the registry.
            try
            {
                foreach (string steam in SteamRoots())
                {
                    foreach (string lib in Libraries(steam))
                    {
                        string cand = Path.Combine(lib, "steamapps", "common", "Rust");
                        if (IsRustDir(cand)) { FoundBy = "steam library"; return cand; }
                    }
                }
            }
            catch { }

            // 4. Well known paths.
            string[] defaults =
            {
                @"C:\Program Files (x86)\Steam\steamapps\common\Rust",
                @"C:\Program Files\Steam\steamapps\common\Rust",
                @"D:\Steam\steamapps\common\Rust",
                @"D:\SteamLibrary\steamapps\common\Rust",
                @"E:\SteamLibrary\steamapps\common\Rust",
            };
            foreach (string d in defaults)
                if (IsRustDir(d)) { FoundBy = "default path"; return d; }

            return null;
        }

        private static IEnumerable<string> SteamRoots()
        {
            var roots = new List<string>();
            TryReg(roots, Registry.CurrentUser, @"Software\Valve\Steam", "SteamPath");
            TryReg(roots, Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath");
            TryReg(roots, Registry.LocalMachine, @"SOFTWARE\Valve\Steam", "InstallPath");
            return roots;
        }

        private static void TryReg(List<string> roots, RegistryKey hive, string sub, string value)
        {
            try
            {
                using (RegistryKey k = hive.OpenSubKey(sub))
                {
                    if (k == null) return;
                    object v = k.GetValue(value);
                    if (v == null) return;
                    string s = Clean(v.ToString());
                    if (!string.IsNullOrEmpty(s) && !roots.Contains(s)) roots.Add(s);
                }
            }
            catch { }
        }

        private static IEnumerable<string> Libraries(string steamRoot)
        {
            var list = new List<string>();
            if (!string.IsNullOrEmpty(steamRoot)) list.Add(steamRoot);
            string vdf = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
            if (File.Exists(vdf))
            {
                try
                {
                    string text = File.ReadAllText(vdf);
                    foreach (Match m in Regex.Matches(text, "\"path\"\\s*\"([^\"]+)\""))
                    {
                        string p = Clean(m.Groups[1].Value);
                        if (!string.IsNullOrEmpty(p) && !list.Contains(p)) list.Add(p);
                    }
                }
                catch { }
            }
            return list;
        }

        private static string Clean(string p)
        {
            if (string.IsNullOrEmpty(p)) return p;
            return p.Trim().Trim('"').Replace('/', Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar);
        }
    }
}
