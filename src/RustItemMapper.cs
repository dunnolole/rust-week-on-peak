using System;
using System.Collections.Generic;

namespace RustWeekOnPEAK
{
    /// <summary>
    /// Decides which Rust item each PEAK item becomes.
    ///
    /// The first choice is an explicit table keyed by PEAK's *prefab* name. PEAK's own
    /// display names are unreliable (several prefabs share a display name, or show an
    /// unrelated one), so matching on the prefab name is what stops items turning into
    /// the wrong Rust thing. Anything not in the table falls back to keyword rules on
    /// the display name, and then to a stable pick, so a future PEAK build still maps.
    /// </summary>
    public class RustItemMapper
    {
        private readonly RustItemDb _db;

        private static readonly string[] Berries =
        {
            "black.berry", "blue.berry", "green.berry",
            "red.berry", "white.berry", "yellow.berry"
        };

        // PEAK prefab name -> Rust shortname. Exact match, checked before any rule.
        private static readonly Dictionary<string, string> Curated =
            new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // ---- food & drink ----
            { "Airplane Food", "can.beans" },
            { "Granola Bar", "granolabar" },
            { "Energy Drink", "scraptea" },
            { "Sports Drink", "smallwaterbottle" },
            { "FortifiedMilk", "smallwaterbottle" },
            { "Lollipop", "candycane" },
            { "Lollipop_Prop", "candycane" },
            { "Marshmallow", "granolabar" },
            { "ScoutCookies", "chocolate" },
            { "ScoutCookies_Vanilla", "chocolate" },
            { "TrailMix", "granolabar" },
            { "Glizzy", "can.beans" },
            { "Glizzy_CattailVariant", "can.beans" },
            { "Item_Coconut", "coconut" },
            { "Item_Coconut_half", "coconut" },
            { "Item_Honeycomb", "honey" },
            { "Beehive", "honey" },
            { "Egg", "egg" },
            { "NestEgg", "egg" },
            { "EggRaven", "egg" },
            { "NestEgg_Raven", "egg" },
            { "EggTurkey", "chicken.raw" },
            { "Frog", "fish.minnows" },
            { "FrogLegs", "meat.pork.cooked" },

            // The starter tool AND the stone resource: Rust's rock is both.
            { "Stone", "rock" },

            // ---- berries (by colour) ----
            { "Apple Berry Green", "green.berry" },
            { "Apple Berry Red", "red.berry" },
            { "Apple Berry Yellow", "yellow.berry" },
            { "Berrynana Blue", "blue.berry" },
            { "Berrynana Brown", "black.berry" },
            { "Berrynana Pink", "red.berry" },
            { "Berrynana Yellow", "yellow.berry" },
            { "Clusterberry Black", "black.berry" },
            { "Clusterberry Red", "red.berry" },
            { "Clusterberry Yellow", "yellow.berry" },
            { "Clusterberry_UNUSED", "green.berry" },
            { "Kingberry Purple", "blue.berry" },
            { "Kingberry Yellow", "yellow.berry" },
            { "Kingberry Green", "green.berry" },
            { "Napberry", "blue.berry" },
            { "Pepper Berry", "red.berry" },
            { "Prickleberry_Red", "red.berry" },
            { "Prickleberry_Gold", "yellow.berry" },
            { "Winterberry Orange", "yellow.berry" },
            { "Winterberry Yellow", "yellow.berry" },
            { "Berrynana Peel Blue Variant", "plantfiber" },
            { "Berrynana Peel Brown Variant", "plantfiber" },
            { "Berrynana Peel Yellow", "plantfiber" },
            { "Berrynana Peel Pink Variant", "plantfiber" },
            { "Shroomberry_Yellow", "mushroom" },
            { "Shroomberry_Red", "mushroom" },
            { "Shroomberry_Purple", "mushroom" },
            { "Shroomberry_Green", "mushroom" },
            { "Shroomberry_Blue", "mushroom" },

            // ---- fungi ----
            { "Mushroom Normie", "mushroom" },
            { "Mushroom Glow", "mushroom" },
            { "Mushroom Normie Poison", "mushroom" },
            { "Mushroom Lace", "mushroom" },
            { "Mushroom Lace Poison", "mushroom" },
            { "Mushroom Cluster", "mushroom" },
            { "Mushroom Cluster Poison", "mushroom" },
            { "Mushroom Chubby", "mushroom" },
            { "ShelfShroom", "mushroom" },
            { "BounceShroom", "mushroom" },
            { "CloudFungus", "mushroom" },
            { "WarpFungus", "mushroom" },
            { "HealingPuffShroom", "mushroom" },

            // ---- medical ----
            { "Antidote", "syringe.medical" },
            { "Bandages", "bandage" },
            { "FirstAidKit", "largemedkit" },
            { "Cure-All", "syringe.medical" },
            { "MedicinalRoot", "plantfiber" },
            { "Mandrake", "plantfiber" },
            { "Mandrake_Hidden", "plantfiber" },
            { "AloeVera", "medical.honey.bandage" },
            { "Sunscreen", "paper" },
            { "Heat Pack", "warmingtea" },
            { "HealingDart Variant", "bow.hunting" },

            // ---- tools & lights ----
            { "Anti-Rope Spool", "rope" },
            { "RopeSpool", "rope" },
            { "RopeShooter", "crossbow" },
            { "RopeShooterAnti", "crossbow" },
            { "ClimbingSpike", "metalpipe" },
            { "ClimbingChalk", "sulfur" },
            { "Binoculars", "tool.binoculars" },
            { "Binoculars_Prop", "tool.binoculars" },
            { "Compass", "compass" },
            { "Pirate Compass", "compass" },
            { "Warp Compass", "compass" },
            { "Cheat Compass", "compass" },
            { "Cheat Compass 1", "compass" },
            { "Flare", "flare" },
            { "Lantern", "flashlight.held" },
            { "Lantern_Faerie", "flashlight.held" },
            { "Torch", "torch" },
            { "Candle", "torch" },

            // ---- instruments / noisemakers ----
            { "Bugle", "fun.trumpet" },
            { "Bugle_Magic", "fun.trumpet" },
            { "Bugle_Scoutmaster Variant", "fun.trumpet" },
            { "Bugle_Prop Variant", "fun.trumpet" },
            { "Megaphone", "fun.cowbell" },
            { "BingBong", "fun.cowbell" },
            { "BingBong_Prop Variant", "fun.cowbell" },

            // ---- gear & toys ----
            { "ChainShooter", "metalpipe" },
            { "RescueHook", "hammer.salvaged" },
            { "RescueHook_Infinite", "hammer.salvaged" },
            { "Frisbee", "snowball" },
            { "Basketball", "snowball" },
            { "Warpsketball", "snowball" },
            { "Snowball", "snowball" },
            { "Balloon", "snowball" },
            { "BalloonBunch", "snowball" },
            { "Dynamite", "explosive.timed" },
            { "ScoutCannonItem", "cannon" },
            { "CactusBall", "plantfiber" },
            { "Parasol", "parachute" },
            { "Parasol_Roots Variant", "parachute" },
            { "Glider", "parachute" },
            { "Parachute", "parachute" },
            { "Backpack", "largebackpack" },
            { "Fannypack", "smallbackpack" },
            { "Passport", "blueidtag" },
            { "Jetpack", "gears" },
            { "Rocketpack", "gears" },
            { "AntiZooka", "rocket.launcher" },
            { "ScoutsHonor", "dogtagneutral" },

            // ---- relics, remains & story props ----
            { "Strange Gem", "kickgems" },
            { "AncientIdol", "kickgems" },
            { "Amulet_SuperJump", "kickgems" },
            { "Amulet_InfiniteStamina", "kickgems" },
            { "Amulet_Clone", "kickgems" },
            { "Amulet_Healing", "kickgems" },
            { "ScoutmasterSoul", "kickgems" },
            { "Cursed Skull", "skull.human" },
            { "Skull", "skull.human" },
            { "BookOfBones", "bone.fragments" },
            { "RitualDagger", "knife.combat" },
            { "Scorpion", "bone.fragments" },
            { "Shell Big", "bone.fragments" },
            { "Bugfix", "plantfiber" },
            { "EarlyWorm", "fishing.tackle" },
            { "MagicBean", "seed.hemp" },
            { "PandorasBox", "box.wooden" },
            { "PortableStovetopItem", "campfire" },
            { "ScoutEffigy", "scarecrow" },
            { "Flag_Plantable_Checkpoint", "twitchrivalsflag" },

            // ---- notes & buildables ----
            { "Guidebook", "note" },
            { "GuidebookPage", "note" },
            { "GuidebookPage_0_Intro", "note" },
            { "GuidebookPage_1_Mushrooms", "note" },
            { "GuidebookPage_2_Campfire", "note" },
            { "GuidebookPage_3_Revival", "note" },
            { "GuidebookPage_4_Poison", "note" },
            { "GuidebookPage_5_Zombies", "note" },
            { "GuidebookPage_6_BodyHeat", "note" },
            { "GuidebookPage_7_BurningSun", "note" },
            { "GuidebookPage_8_Magma", "note" },
            { "GuidebookPage_9_Gloom", "note" },
            { "GuidebookPage_10_Sleepy", "note" },
            { "GuidebookPage_11_Awake", "note" },
            { "GuidebookPage_12_Crashout", "note" },
            { "GuidebookPage_13_FirstTeams", "note" },
            { "GuidebookPageScroll Variant", "note" },
            { "BaseConstructable Variant", "building.planner" },
            { "BaseObject", "building.planner" },
            { "TestConstructable Variant", "building.planner" },
        };

        // keyword (lower case, matched against the PEAK display name) -> Rust shortname.
        // Only used for prefabs not covered by the curated table above.
        private static readonly KeyValuePair<string, string>[] Rules =
        {
            Kv("anti-rad", "antiradpills"),
            Kv("antirad", "antiradpills"),
            Kv("radiation", "antiradpills"),
            Kv("medkit", "largemedkit"),
            Kv("first aid", "largemedkit"),
            Kv("bandage", "bandage"),
            Kv("syringe", "syringe.medical"),
            Kv("antidote", "syringe.medical"),
            Kv("pill", "antiradpills"),
            Kv("medicine", "syringe.medical"),
            Kv("healing", "healingtea"),
            Kv("cure", "syringe.medical"),
            Kv("canteen", "smallwaterbottle"),
            Kv("flask", "smallwaterbottle"),
            Kv("water", "smallwaterbottle"),
            Kv("energy drink", "scraptea"),
            Kv("drink", "smallwaterbottle"),
            Kv("granola", "granolabar"),
            Kv("ration", "granolabar"),
            Kv("marshmallow", "granolabar"),
            Kv("hot dog", "can.beans"),
            Kv("hotdog", "can.beans"),
            Kv("bean", "can.beans"),
            Kv("tuna", "can.tuna"),
            Kv("fish", "fish.cooked"),
            Kv("meat", "meat.pork.cooked"),
            Kv("steak", "beefmeat.cooked"),
            Kv("frog", "meat.pork.cooked"),
            Kv("honey", "honey"),
            Kv("egg", "egg"),
            Kv("coconut", "coconut"),
            Kv("cookie", "chocolate"),
            Kv("biscuit", "chocolate"),
            Kv("bread", "bread.loaf"),
            Kv("chocolate", "chocolate"),
            Kv("lollipop", "candycane"),
            Kv("candy", "candycane"),
            Kv("food", "can.beans"),
            Kv("backpack", "largebackpack"),
            Kv("fannypack", "smallbackpack"),
            Kv("bag", "smallbackpack"),
            Kv("pickaxe", "pickaxe"),
            Kv("hatchet", "hatchet"),
            Kv("axe", "hatchet"),
            Kv("hammer", "hammer"),
            Kv("shovel", "shovel"),
            Kv("machete", "machete"),
            Kv("knife", "knife.skinning"),
            Kv("dagger", "knife.combat"),
            Kv("crowbar", "hammer.salvaged"),
            Kv("chalk", "sulfur"),
            Kv("rock", "rock"),
            Kv("binocular", "tool.binoculars"),
            Kv("compass", "compass"),
            Kv("bugle", "fun.trumpet"),
            Kv("megaphone", "fun.cowbell"),
            Kv("flare", "flare"),
            Kv("lantern", "flashlight.held"),
            Kv("torch", "torch"),
            Kv("candle", "torch"),
            Kv("campfire", "campfire"),
            Kv("tent", "tarp"),
            Kv("planner", "building.planner"),
            Kv("blueprint", "building.planner"),
            Kv("door", "door.hinged.wood"),
            Kv("wall", "wall.external.high"),
            Kv("ladder", "ladder.wooden.wall"),
            Kv("pistol", "pistol.revolver"),
            Kv("rifle", "rifle.semiauto"),
            Kv("shotgun", "shotgun.pump"),
            Kv("bow", "bow.hunting"),
            Kv("crossbow", "crossbow"),
            Kv("spear", "spear.wooden"),
            Kv("club", "bone.club"),
            Kv("grenade", "grenade.f1"),
            Kv("dynamite", "explosive.timed"),
            Kv("explosive", "explosives"),
            Kv("arrow", "arrow.wooden"),
            Kv("wood", "wood"),
            Kv("stick", "wood"),
            Kv("plank", "wood"),
            Kv("log", "wood"),
            Kv("stone", "stones"),
            Kv("metal", "metal.fragments"),
            Kv("scrap", "scrap"),
            Kv("cloth", "cloth"),
            Kv("rope", "rope"),
            Kv("leather", "leather"),
            Kv("bone", "bone.fragments"),
            Kv("skull", "skull.human"),
            Kv("gem", "kickgems"),
            Kv("idol", "kickgems"),
            Kv("crystal", "kickgems"),
            Kv("gold", "kickgems"),
            Kv("coin", "kickgems"),
            Kv("amulet", "kickgems"),
            Kv("seed", "seed.hemp"),
            Kv("flower", "seed.hemp"),
            Kv("beehive", "honey"),
            Kv("worm", "fishing.tackle"),
            Kv("cactus", "plantfiber"),
            Kv("plant", "plantfiber"),
            Kv("paper", "paper"),
            Kv("note", "note"),
            Kv("page", "note"),
            Kv("book", "note"),
            Kv("guide", "note"),
            Kv("heat pack", "warmingtea"),
            Kv("fuel", "lowgradefuel"),
            Kv("gear", "gears"),
            Kv("pipe", "metalpipe"),
            Kv("parachute", "parachute"),
            Kv("glider", "parachute"),
            Kv("parasol", "parachute"),
            Kv("flag", "twitchrivalsflag"),
            Kv("ball", "snowball"),
            Kv("card", "blueidtag"),
            Kv("tag", "dogtagneutral"),
            Kv("pass", "blueidtag"),
        };

        private static readonly string[] Fallback =
        {
            "wood", "stones", "cloth", "metal.fragments", "scrap", "lowgradefuel",
            "rope", "leather", "charcoal", "plantfiber", "paper", "gears"
        };

        public RustItemMapper(RustItemDb db) { _db = db; }

        /// <summary>Maps a PEAK item given its prefab name and its (unreliable) display name.</summary>
        public RustItem Map(string prefabName, string displayName)
        {
            string sn;
            if (!string.IsNullOrEmpty(prefabName) && Curated.TryGetValue(prefabName, out sn))
            {
                RustItem curated = _db.Get(sn);
                if (curated != null) return curated;
            }

            // Hidden chess-piece prefabs all look alike; treat them as a set of stones.
            if (!string.IsNullOrEmpty(prefabName) &&
                prefabName.StartsWith("C_", StringComparison.Ordinal))
            {
                RustItem stone = _db.Get("stones");
                if (stone != null) return stone;
            }

            return MapByDisplay(string.IsNullOrEmpty(displayName) ? prefabName : displayName);
        }

        /// <summary>Keyword/colour mapping used for anything the curated table misses.</summary>
        public RustItem MapByDisplay(string peakName)
        {
            if (string.IsNullOrEmpty(peakName)) peakName = "item";
            string lower = peakName.ToLowerInvariant();

            if (lower.Contains("berry") || lower.Contains("berr")) return BerryPick(lower);

            if (lower.Contains("shroom") || lower.Contains("mushroom") ||
                lower.Contains("fungus") || lower.Contains("fungi"))
            {
                RustItem m = _db.Get("mushroom");
                if (m != null) return m;
            }

            foreach (var rule in Rules)
            {
                if (lower.Contains(rule.Key))
                {
                    RustItem it = _db.Get(rule.Value);
                    if (it != null) return it;
                }
            }

            int h = StableHash(lower);
            for (int i = 0; i < Fallback.Length; i++)
            {
                RustItem it = _db.Get(Fallback[(h + i) % Fallback.Length]);
                if (it != null) return it;
            }

            List<RustItem> res = _db.Category("Resources");
            if (res.Count > 0) return res[h % res.Count];
            return _db.All.Count > 0 ? _db.All[h % _db.All.Count] : null;
        }

        private RustItem BerryPick(string lower)
        {
            string pick = null;
            if (lower.Contains("black")) pick = "black.berry";
            else if (lower.Contains("blue")) pick = "blue.berry";
            else if (lower.Contains("green")) pick = "green.berry";
            else if (lower.Contains("red")) pick = "red.berry";
            else if (lower.Contains("white")) pick = "white.berry";
            else if (lower.Contains("yellow") || lower.Contains("gold") || lower.Contains("orange")) pick = "yellow.berry";
            else if (lower.Contains("purple")) pick = "blue.berry";
            else if (lower.Contains("pink")) pick = "red.berry";
            else if (lower.Contains("brown")) pick = "black.berry";

            if (pick != null)
            {
                RustItem it = _db.Get(pick);
                if (it != null) return it;
            }

            int h = StableHash(lower);
            for (int i = 0; i < Berries.Length; i++)
            {
                RustItem it = _db.Get(Berries[(h + i) % Berries.Length]);
                if (it != null) return it;
            }
            return _db.Get("black.berry");
        }

        private static int StableHash(string s)
        {
            unchecked
            {
                int h = 23;
                for (int i = 0; i < s.Length; i++) h = h * 31 + s[i];
                return h & 0x7fffffff;
            }
        }

        private static KeyValuePair<string, string> Kv(string k, string v)
        {
            return new KeyValuePair<string, string>(k, v);
        }
    }
}
