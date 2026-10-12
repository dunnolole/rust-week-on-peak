using System;
using UnityEngine;

namespace RustWeekOnPEAK
{
    /// <summary>
    /// Rust style harvesting: while the player holds the Rock, a swing (primary use)
    /// knocks wood out of trees, stone out of rocks and plant fibre out of bushes.
    /// PEAK has no native chopping, so this is our own raycast on top of the world.
    /// </summary>
    public static class Gathering
    {
        public const string ToolShortname = "rock";

        public static string LastMessage;
        public static float LastMessageUntil;

        private static float _cooldownUntil;

        public static void Message(string text)
        {
            LastMessage = text;
            LastMessageUntil = Time.time + 2.6f;
        }

        public static bool MessageActive
        {
            get { return !string.IsNullOrEmpty(LastMessage) && Time.time < LastMessageUntil; }
        }

        /// <summary>True if the player is currently holding the Rust Rock.</summary>
        public static bool HoldingTool(Character ch)
        {
            return ch != null && ch.data != null &&
                   RustSwap.ShortnameForItem(ch.data.currentItem) == ToolShortname;
        }

        /// <summary>Called on a primary-use press from the local player.</summary>
        public static void HandlePrimary(Character ch)
        {
            if (Crafting.Open)
            {
                Crafting.CraftSelected(ch);
                return;
            }

            if (ch == null || ch.data == null) return;
            if (!HoldingTool(ch)) return;
            if (Time.time < _cooldownUntil) return;

            _cooldownUntil = Time.time + 1.0f;
            Harvest(ch);
        }

        /// <summary>Harvest without needing the tool equipped. Used by the developer self-test only.</summary>
        public static void ForceHarvest(Character ch)
        {
            if (ch == null) return;
            Harvest(ch);
        }

        private static void Harvest(Character ch)
        {
            string resource = DetectResource(ch);
            int amount = 1 + (UnityEngine.Random.value < 0.35f ? 1 : 0);
            int given = RustItems.Give(ch, resource, amount);
            string label = Label(resource);

            if (given > 0) Message("GATHERED  +" + given + " " + label);
            else Message("HANDS FULL  (" + label + ")");

            SurvivalSystem sys = SurvivalSystem.Active;
            if (sys != null) sys.Toast("HARVEST  +" + given + " " + label);
        }

        private static string DetectResource(Character ch)
        {
            try
            {
                Camera cam = Camera.main;
                Vector3 origin = (cam != null) ? cam.transform.position : ch.transform.position + Vector3.up * 1.4f;
                Vector3 dir = (cam != null) ? cam.transform.forward : ch.transform.forward;

                RaycastHit hit;
                if (Physics.Raycast(origin, dir, out hit, 6f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.transform != null && !hit.transform.IsChildOf(ch.transform))
                    {
                        string t = ChainName(hit.transform);
                        if (Contains(t, "tree", "log", "trunk", "branch", "palm", "stump",
                                         "timber", "pine", "birch", "foliage", "leaf"))
                            return "wood";
                        if (Contains(t, "rock", "stone", "boulder", "cliff", "crag",
                                         "pebble", "ore", "slate", "granite"))
                            return "rock";
                        if (Contains(t, "bush", "plant", "fern", "grass", "flower", "vine",
                                         "moss", "berry", "cactus", "shrub", "shroom", "fungus"))
                            return "plantfiber";
                    }
                }
            }
            catch { }

            // Unknown surface (usually the ground): split the yield so both are reachable.
            return UnityEngine.Random.value < 0.5f ? "rock" : "wood";
        }

        private static string ChainName(Transform t)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 6 && t != null; i++)
            {
                sb.Append(' ').Append(t.name);
                t = t.parent;
            }
            return sb.ToString().ToLowerInvariant();
        }

        private static bool Contains(string haystack, params string[] needles)
        {
            foreach (string n in needles)
                if (haystack.Contains(n)) return true;
            return false;
        }

        private static string Label(string shortname)
        {
            switch (shortname)
            {
                case "wood": return "WOOD";
                case "rock": return "STONE";
                case "plantfiber": return "PLANT FIBRE";
                default: return shortname.ToUpperInvariant();
            }
        }
    }
}
