using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;
using Zorro.Core;

namespace RustWeekOnPEAK
{
    /// <summary>
    /// Turns PEAK's item list into Rust's: each PEAK item prefab gets a Rust name and a
    /// Rust icon read from the player's own Rust install. Names are also written into
    /// PEAK's localization table so every place the game shows an item name shows the
    /// Rust one.
    /// </summary>
    public static class RustSwap
    {
        public static readonly Dictionary<ushort, string> NameByItemId = new Dictionary<ushort, string>();
        public static readonly Dictionary<string, string> NameByPrefabName = new Dictionary<string, string>();
        public static readonly Dictionary<string, string> IconShortnameByPrefabName = new Dictionary<string, string>();
        public static readonly Dictionary<string, Item> PeakItemByPrefabName = new Dictionary<string, Item>();
        public static readonly Dictionary<string, Item> PeakItemByShortname = new Dictionary<string, Item>();

        public static int Applied;
        public static int PeekItemsSeen;
        public static bool Done;

        public static void Apply(RustItemDb db)
        {
            if (Done) return;
            Done = true;

            var mapper = new RustItemMapper(db);
            List<Item> items = GatherPeakItems();
            PeekItemsSeen = items.Count;

            var mainTable = GetMainTable();

            foreach (Item item in items)
            {
                if (item == null || item.UIData == null) continue;
                string key = item.gameObject.name;
                if (string.IsNullOrEmpty(key))
                {
                    key = "item_" + item.GetInstanceID();
                }
                string original = item.UIData.itemName;
                if (string.IsNullOrEmpty(original)) original = key;

                RustItem rust = mapper.Map(key, original);
                if (rust == null) continue;

                item.UIData.itemName = rust.Name;
                Texture2D icon = db.GetIcon(rust);
                if (icon != null)
                {
                    item.UIData.icon = icon;
                    item.UIData.altIcon = icon;
                    item.UIData.hasAltIcon = false;
                    item.UIData.hasColorBlindIcon = false;
                }

                NameByPrefabName[key] = rust.Name;
                IconShortnameByPrefabName[key] = rust.Shortname;
                PeakItemByPrefabName[key] = item;
                if (!PeakItemByShortname.ContainsKey(rust.Shortname))
                    PeakItemByShortname[rust.Shortname] = item;
                if (item.itemID != 0) NameByItemId[item.itemID] = rust.Name;

                if (mainTable != null)
                    RegisterLocalized(mainTable, original, rust.Name);

                Applied++;
            }

            Plugin.Log.LogInfo(string.Format(
                "[RustWeek] item swap applied: {0} of {1} PEAK items relabelled to Rust items.",
                Applied, PeekItemsSeen));
        }

        /// <summary>Rust shortname we gave a PEAK prefab (by prefab name).</summary>
        public static string ShortnameForPrefab(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName)) return null;
            int clone = prefabName.IndexOf("(Clone)", StringComparison.Ordinal);
            if (clone > 0) prefabName = prefabName.Substring(0, clone).TrimEnd();
            string sn;
            return IconShortnameByPrefabName.TryGetValue(prefabName, out sn) ? sn : null;
        }

        /// <summary>Rust shortname we gave a live PEAK Item instance/prefab.</summary>
        public static string ShortnameForItem(Item item)
        {
            if (item == null) return null;
            return ShortnameForPrefab(item.gameObject.name);
        }

        /// <summary>PEAK prefab (a real Item asset) that we mapped to a given Rust shortname.</summary>
        public static Item PeakItemForShortname(string shortname)
        {
            if (string.IsNullOrEmpty(shortname)) return null;
            Item it;
            return PeakItemByShortname.TryGetValue(shortname, out it) ? it : null;
        }

        /// <summary>Diagnostic: list PEAK's own items before they are relabelled.</summary>
        public static string DumpPeakItems()
        {
            var sb = new System.Text.StringBuilder();
            var seen = new HashSet<int>();
            try
            {
                List<Item> items = GatherPeakItems();
                foreach (Item item in items)
                {
                    if (item == null) continue;
                    int id = item.GetInstanceID();
                    if (!seen.Add(id)) continue;
                    string name = item.UIData != null ? item.UIData.itemName : "(no UIData)";
                    sb.Append(item.gameObject.name).Append(" | ").Append(name)
                      .Append(" | id=").Append(item.itemID)
                      .Append(" | tags=").Append((int)item.itemTags)
                      .Append(" | uses=").Append(item.totalUses)
                      .AppendLine();
                }
            }
            catch (Exception e)
            {
                sb.AppendLine("dump error: " + e.Message);
            }
            return sb.ToString();
        }

        private static List<Item> GatherPeakItems()
        {
            var seen = new HashSet<int>();
            var list = new List<Item>();

            try
            {
                ItemDatabase database = SingletonAsset<ItemDatabase>.Instance;
                if (database != null)
                {
                    if (database.itemLookup != null)
                        foreach (var kv in database.itemLookup) AddUnique(list, seen, kv.Value);
                    if (database.Objects != null)
                        foreach (Item it in database.Objects) AddUnique(list, seen, it);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[RustWeek] ItemDatabase load failed: " + e.Message);
            }

            try
            {
                Item[] loaded = Resources.LoadAll<Item>("0_Items");
                if (loaded != null)
                    foreach (Item it in loaded) AddUnique(list, seen, it);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[RustWeek] Resources.LoadAll(0_Items) failed: " + e.Message);
            }

            return list;
        }

        private static void AddUnique(List<Item> list, HashSet<int> seen, Item it)
        {
            if (it == null) return;
            int id = it.GetInstanceID();
            if (seen.Add(id)) list.Add(it);
        }

        private static Dictionary<string, List<string>> GetMainTable()
        {
            try
            {
                PropertyInfo p = typeof(LocalizedText).GetProperty(
                    "mainTable", BindingFlags.Public | BindingFlags.Static);
                if (p == null) return null;
                return (Dictionary<string, List<string>>)p.GetValue(null, null);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[RustWeek] localization table unavailable: " + e.Message);
                return null;
            }
        }

        private static void RegisterLocalized(Dictionary<string, List<string>> table, string peakName, string display)
        {
            try
            {
                string key = ("NAME_" + peakName).ToUpperInvariant();
                var row = new List<string>(14);
                for (int i = 0; i < 14; i++) row.Add(display);
                table[key] = row;
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(Item), "GetName")]
    internal static class ItemGetNamePatch
    {
        // Safety net: if PEAK ever resolves a name through a table we did not fill,
        // hand back the Rust name we assigned to this item.
        private static void Postfix(Item __instance, ref string __result)
        {
            if (__instance == null || __result == null) return;
            if (!__result.StartsWith("LOC:", StringComparison.Ordinal)) return;

            string name;
            if (RustSwap.NameByItemId.TryGetValue(__instance.itemID, out name))
            {
                __result = name;
                return;
            }
            string goName = __instance.gameObject.name;
            int clone = goName.IndexOf("(Clone)", StringComparison.Ordinal);
            if (clone > 0) goName = goName.Substring(0, clone);
            if (RustSwap.NameByPrefabName.TryGetValue(goName, out name)) __result = name;
        }
    }

    [HarmonyPatch(typeof(Item), "Consume")]
    internal static class ItemConsumePatch
    {
        // Drinking / eating now feeds the Rust survival layer: the water bottle you
        // renamed restores thirst, anti-rad items clear radiation.
        private static void Postfix(Item __instance, int consumerID)
        {
            try
            {
                if (__instance == null) return;

                Character ch = null;
                PhotonView pv = PhotonNetwork.GetPhotonView(consumerID);
                if (pv != null) ch = pv.GetComponent<Character>();
                if (ch == null) ch = Character.localCharacter;   // solo fallback
                if (ch == null || !ch.IsLocal) return;

                SurvivalSystem sys = SurvivalSystem.Active;
                if (sys == null) return;
                sys.Consume(RustSwap.ShortnameForItem(__instance));
            }
            catch { }
        }
    }
}
