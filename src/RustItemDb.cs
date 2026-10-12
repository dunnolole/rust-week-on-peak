using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace RustWeekOnPEAK
{
    /// <summary>One item as Rust defines it on the player's own disk.</summary>
    public class RustItem
    {
        public string Shortname;
        public string Name;
        public string Description;
        public string Category;
        public string Rarity;
        public int Stackable;
        public long ItemId;
        public string IconPath;

        public override string ToString() { return Name + " (" + Shortname + ")"; }
    }

    /// <summary>
    /// Reads Rust's own item files (Bundles/items/*.json and their *.png icons) from the
    /// player's install. Nothing here is bundled with the mashup.
    /// </summary>
    public class RustItemDb
    {
        public readonly List<RustItem> All = new List<RustItem>();
        public readonly Dictionary<string, RustItem> ByShortname = new Dictionary<string, RustItem>();
        public readonly Dictionary<string, List<RustItem>> ByCategory = new Dictionary<string, List<RustItem>>();
        public int JsonFiles;
        public int IconsFound;
        public string ItemsDir;

        private readonly Dictionary<string, Texture2D> _iconCache = new Dictionary<string, Texture2D>();
        private readonly HashSet<string> _iconFailed = new HashSet<string>();

        public void Load(string rustDir)
        {
            All.Clear();
            ByShortname.Clear();
            ByCategory.Clear();
            _iconCache.Clear();
            _iconFailed.Clear();
            JsonFiles = 0;
            IconsFound = 0;

            ItemsDir = Path.Combine(rustDir, "Bundles", "items");
            if (!Directory.Exists(ItemsDir)) throw new DirectoryNotFoundException(ItemsDir);

            string[] files = Directory.GetFiles(ItemsDir, "*.json");
            JsonFiles = files.Length;

            foreach (string f in files)
            {
                try
                {
                    var root = JObject.Parse(File.ReadAllText(f));
                    string shortname = (string)root["shortname"];
                    if (string.IsNullOrEmpty(shortname)) continue;
                    if (ByShortname.ContainsKey(shortname)) continue;

                    var item = new RustItem
                    {
                        Shortname = shortname,
                        Name = (string)root["Name"] ?? shortname,
                        Description = (string)root["Description"] ?? "",
                        Category = (string)root["Category"] ?? "Misc",
                        Rarity = (string)root["rarity"] ?? "",
                        Stackable = ToInt(root["stackable"]),
                        ItemId = ToLong(root["itemid"]),
                    };

                    string icon = Path.Combine(ItemsDir, shortname + ".png");
                    if (File.Exists(icon)) { item.IconPath = icon; IconsFound++; }

                    All.Add(item);
                    ByShortname[shortname] = item;
                    List<RustItem> bucket;
                    if (!ByCategory.TryGetValue(item.Category, out bucket))
                    {
                        bucket = new List<RustItem>();
                        ByCategory[item.Category] = bucket;
                    }
                    bucket.Add(item);
                }
                catch { }
            }

            // Stable order so the mapping is identical on every PC and every run.
            foreach (var kv in ByCategory) kv.Value.Sort(CompareStable);
            All.Sort(CompareStable);
        }

        private static int CompareStable(RustItem a, RustItem b)
        {
            int c = a.ItemId.CompareTo(b.ItemId);
            if (c != 0) return c;
            return string.CompareOrdinal(a.Shortname, b.Shortname);
        }

        public List<RustItem> Category(string name)
        {
            List<RustItem> list;
            if (ByCategory.TryGetValue(name, out list)) return list;
            return new List<RustItem>();
        }

        public RustItem Get(string shortname)
        {
            RustItem it;
            ByShortname.TryGetValue(shortname, out it);
            return it;
        }

        /// <summary>Lazy-loads a Rust icon as a Unity texture. Only called for items we actually use.</summary>
        public Texture2D GetIcon(RustItem item)
        {
            if (item == null || string.IsNullOrEmpty(item.Shortname)) return null;
            Texture2D tex;
            if (_iconCache.TryGetValue(item.Shortname, out tex)) return tex;
            if (_iconFailed.Contains(item.Shortname)) return null;

            string path = item.IconPath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                path = Path.Combine(ItemsDir, item.Shortname + ".png");
                if (!File.Exists(path))
                {
                    string alt = Path.Combine(ItemsDir, item.Shortname.Replace('.', ' ') + ".png");
                    path = File.Exists(alt) ? alt : null;
                }
            }
            if (path == null)
            {
                _iconFailed.Add(item.Shortname);
                return null;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.name = "rustweek_" + item.Shortname;
                if (!tex.LoadImage(bytes))
                {
                    _iconFailed.Add(item.Shortname);
                    return null;
                }
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                _iconCache[item.Shortname] = tex;
                return tex;
            }
            catch
            {
                _iconFailed.Add(item.Shortname);
                return null;
            }
        }

        private static int ToInt(JToken t) { try { return t == null ? 0 : (int)t; } catch { return 0; } }
        private static long ToLong(JToken t) { try { return t == null ? 0L : (long)t; } catch { return 0L; } }
    }
}
