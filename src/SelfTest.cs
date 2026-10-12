using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RustWeekOnPEAK
{
    /// <summary>
    /// A tiny, file-gated self check used while building the mod. Put a file at
    /// %LOCALAPPDATA%\RustWeekOnPEAK\selftest.txt and the mod starts a solo run, waits
    /// for the starter kit and writes what it found into the BepInEx log. With the file
    /// absent it does nothing at all, so it is safe to ship.
    /// </summary>
    public static class SelfTest
    {
        public static readonly bool Requested = Detect();

        private static bool _startedSolo;
        private static bool _sawCharacter;
        private static bool _kitChecked;
        private static float _kitAt = -1f;
        private static float _harvestAt = -1f;
        private static int _harvests;
        private static bool _craftChecked;

        private static bool Detect()
        {
            try
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RustWeekOnPEAK");
                bool on = File.Exists(Path.Combine(dir, "selftest.txt"));
                if (on) Plugin.Log.LogInfo("[RustWeek][selftest] enabled by selftest.txt");
                return on;
            }
            catch { return false; }
        }

        public static void Update()
        {
            if (!Requested) return;
            try
            {
                if (!_startedSolo && SceneManager.GetActiveScene().name == MainMenu.SceneName)
                {
                    MainMenu menu = UnityEngine.Object.FindObjectOfType<MainMenu>();
                    if (menu != null)
                    {
                        MethodInfo m = typeof(MainMenu).GetMethod(
                            "StartSoloSession", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (m != null)
                        {
                            _startedSolo = true;
                            Plugin.Log.LogInfo("[RustWeek][selftest] starting a solo session");
                            m.Invoke(menu, null);
                        }
                    }
                }

                Character ch = Character.localCharacter;
                if (ch == null) return;

                if (!_sawCharacter)
                {
                    _sawCharacter = true;
                    _kitAt = Time.time + 3.5f;
                    Plugin.Log.LogInfo("[RustWeek][selftest] local character spawned: " + ch.name);
                }

                bool kitDone = BaseCamp.StatusLine != null
                    && (BaseCamp.StatusLine.Contains("kit") || BaseCamp.StatusLine.Contains("skipped")
                        || BaseCamp.StatusLine.Contains("placed by"));

                if (!_kitChecked && (kitDone || (_kitAt > 0f && Time.time >= _kitAt + 40f)))
                {
                    _kitChecked = true;
                    Plugin.Log.LogInfo("[RustWeek][selftest] base camp status: " + BaseCamp.StatusLine);
                    Dump(ch, "after base camp");
                    _harvestAt = Time.time + 1.0f;
                }

                if (_kitChecked && _harvests < 2 && _harvestAt > 0f && Time.time >= _harvestAt)
                {
                    _harvests++;
                    _harvestAt = Time.time + 1.3f;
                    int before = Resources(ch);
                    Gathering.ForceHarvest(ch);
                    int after = Resources(ch);
                    Plugin.Log.LogInfo("[RustWeek][selftest] harvest " + _harvests + ": resources " + before + " -> " + after);
                }

                if (_kitChecked && _harvests >= 2 && !_craftChecked && Time.time >= _harvestAt)
                {
                    _craftChecked = true;
                    EmptyTools(ch);
                    int gave = RustItems.Give(ch, "wood", 2);
                    Plugin.Log.LogInfo("[RustWeek][selftest] craft setup: wood added=" + gave
                        + " slots: " + Slots(ch));
                    Crafting.Craft(ch, Crafting.Recipes[0]);
                    Plugin.Log.LogInfo("[RustWeek][selftest] craft result: " + Crafting.LastResult);
                    Dump(ch, "after craft");

                    if (SurvivalSystem.Active != null)
                    {
                        SurvivalSystem.Active.Thirst = 40f;
                        Plugin.Log.LogInfo("[RustWeek][selftest] thirst before drink: " + SurvivalSystem.Active.Thirst);
                        SurvivalSystem.Active.Consume("smallwaterbottle");
                        Plugin.Log.LogInfo("[RustWeek][selftest] thirst after drink: " + SurvivalSystem.Active.Thirst);
                        SurvivalSystem.Active.Radiation = 50f;
                        SurvivalSystem.Active.Consume("antiradpills");
                        Plugin.Log.LogInfo("[RustWeek][selftest] radiation after antirad: " + SurvivalSystem.Active.Radiation);
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[RustWeek][selftest] " + e.Message);
            }
        }

        private static int Resources(Character ch)
        {
            return RustItems.Count(ch, "wood") + RustItems.Count(ch, "rock")
                 + RustItems.Count(ch, "plantfiber");
        }

        /// <summary>Frees room for the craft test without touching the player's ID tag.</summary>
        private static readonly string[] TestJunk =
        {
            "rock", "wood", "plantfiber", "smallwaterbottle", "granolabar",
            "torch", "bandage", "flare", "largemedkit", "campfire"
        };

        private static void EmptyTools(Character ch)
        {
            try
            {
                if (ch == null || ch.player == null || ch.player.itemSlots == null) return;
                foreach (ItemSlot slot in ch.player.itemSlots)
                {
                    if (slot == null || slot.IsEmpty()) continue;
                    string sn = RustSwap.ShortnameForItem(slot.prefab);
                    if (sn == null) continue;
                    bool junk = false;
                    foreach (string j in TestJunk) if (j == sn) { junk = true; break; }
                    if (junk) ch.player.EmptySlot(Zorro.Core.Optionable<byte>.Some(slot.itemSlotID), true);
                }
            }
            catch { }
        }

        private static string Slots(Character ch)
        {
            var sb = new StringBuilder();
            if (ch != null && ch.player != null && ch.player.itemSlots != null)
            {
                foreach (ItemSlot slot in ch.player.itemSlots)
                {
                    if (slot == null || slot.IsEmpty()) continue;
                    sb.Append('[').Append(RustSwap.ShortnameForItem(slot.prefab) ?? "?").Append(']');
                }
            }
            return sb.ToString();
        }

        private static void Dump(Character ch, string label)
        {
            Plugin.Log.LogInfo("[RustWeek][selftest] " + label + " slots: " + Slots(ch));
            Plugin.Log.LogInfo("[RustWeek][selftest] rock prefab resolved: "
                + (RustSwap.PeakItemForShortname("rock") != null));
        }
    }
}
