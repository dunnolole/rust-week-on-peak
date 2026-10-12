using System.Collections.Generic;
using UnityEngine;

namespace RustWeekOnPEAK
{
    /// <summary>
    /// The Rust survival layer: thirst and radiation the player tracks themselves, plus
    /// the existing PEAK hunger/cold shown the Rust way. Values are only applied to the
    /// local player, so solo play (and a host) behave exactly as expected.
    /// </summary>
    public class SurvivalSystem
    {
        /// <summary>The live system, so consume/gather/craft can reach the player's meters.</summary>
        public static SurvivalSystem Active;

        public float Thirst = 100f;      // 100 = hydrated, 0 = parched
        public float Radiation = 0f;     // 0 = clean, 100 = lethal

        public float ThirstPerSecond = 0.20f;
        public float RadiationPerSecond = 0.012f;

        public string ToastText;
        public float ToastUntil;

        private float _injuryTimer;
        private float _radTimer;
        private float _lastTime;

        private static readonly HashSet<string> Drinks = new HashSet<string>
        {
            "smallwaterbottle", "water", "scraptea", "warmingtea", "coolingtea",
            "healingtea", "maxhealthtea", "puretean", "radiationremovetea",
            "radiationresisttea", "oretea", "woodtea", "harvestingtea",
            "craftingtea", "supertea", "appletea", "berrytea", "bottle.vodka"
        };

        private static readonly HashSet<string> AntiRad = new HashSet<string>
        {
            "antiradpills", "radiationremovetea", "radiationresisttea"
        };

        private static readonly HashSet<string> Foods = new HashSet<string>
        {
            "granolabar", "can.beans", "can.tuna", "chocolate", "candycane",
            "black.berry", "blue.berry", "green.berry", "red.berry", "white.berry",
            "yellow.berry", "apple", "mushroom", "egg", "coconut", "corn", "potato",
            "bread.loaf", "bread.brioche.loaf", "pie.survivors", "chicken.raw",
            "meat.pork.cooked", "beefmeat.cooked", "fish.cooked", "fish.minnows", "honey"
        };

        public void Toast(string text)
        {
            ToastText = text;
            ToastUntil = Time.time + 2.6f;
        }

        public bool ToastActive
        {
            get { return !string.IsNullOrEmpty(ToastText) && Time.time < ToastUntil; }
        }

        public bool IsDrink(string shortname) { return shortname != null && Drinks.Contains(shortname); }
        public bool IsFood(string shortname) { return shortname != null && Foods.Contains(shortname); }

        /// <summary>Applies the survival effect of eating/drinking an item the player just consumed.</summary>
        public void Consume(string shortname)
        {
            if (string.IsNullOrEmpty(shortname)) return;
            string sn = shortname.ToLowerInvariant();

            if (AntiRad.Contains(sn))
            {
                Radiation = Mathf.Max(0f, Radiation - 45f);
                Toast("ANTI-RAD  radiation -45");
            }

            if (Drinks.Contains(sn))
            {
                Thirst = Mathf.Min(100f, Thirst + 35f);
                Toast("DRANK  thirst +35");
            }
            else if (Foods.Contains(sn))
            {
                Thirst = Mathf.Min(100f, Thirst + 6f);
                Toast("ATE  thirst +6");
            }
        }

        public void AddThirst(float amount)
        {
            Thirst = Mathf.Clamp(Thirst + amount, 0f, 100f);
        }

        public void AddRadiation(float amount)
        {
            Radiation = Mathf.Clamp(Radiation + amount, 0f, 100f);
        }

        public void Update()
        {
            float now = Time.time;
            float dt = _lastTime == 0f ? 0f : Mathf.Clamp(now - _lastTime, 0f, 0.5f);
            _lastTime = now;
            if (dt <= 0f) return;

            Character ch = Character.localCharacter;
            if (ch == null) return;

            bool day = DayNightManager.instance != null && DayNightManager.instance.isDay > 0.5f;

            Thirst -= ThirstPerSecond * dt * (day ? 1.25f : 1f);
            if (Thirst < 0f) Thirst = 0f;

            Radiation += RadiationPerSecond * dt * (day ? 1.6f : 0.7f);
            if (Radiation > 100f) Radiation = 100f;

            CharacterAfflictions afflictions = (ch.refs != null) ? ch.refs.afflictions : null;
            if (afflictions == null) return;
            if (ch.photonView == null || !ch.photonView.IsMine) return;

            if (Thirst <= 0f)
            {
                _injuryTimer -= dt;
                if (_injuryTimer <= 0f)
                {
                    _injuryTimer = 4f;
                    afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Injury, 0.02f);
                }
            }
            else
            {
                _injuryTimer = 0f;
            }

            if (Radiation >= 100f)
            {
                _radTimer -= dt;
                if (_radTimer <= 0f)
                {
                    _radTimer = 3f;
                    afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Poison, 0.02f);
                }
            }
            else
            {
                _radTimer = 0f;
            }
        }

        public float Hunger01()
        {
            Character ch = Character.localCharacter;
            if (ch != null && ch.refs != null && ch.refs.afflictions != null)
                return Mathf.Clamp01(1f - ch.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Hunger));
            return 1f;
        }

        /// <summary>Warmth 0..1 from PEAK's cold/hot statuses.</summary>
        public float Warmth01()
        {
            Character ch = Character.localCharacter;
            if (ch != null && ch.refs != null && ch.refs.afflictions != null)
            {
                float cold = ch.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Cold);
                float hot = ch.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Hot);
                return Mathf.Clamp01(1f - cold + hot * 0.5f);
            }
            return 1f;
        }

        public int Day()
        {
            return (DayNightManager.instance != null) ? DayNightManager.instance.dayCount : 1;
        }

        public string Clock()
        {
            if (DayNightManager.instance == null) return "--:--";
            float t = DayNightManager.instance.timeOfDay;
            int h = Mathf.FloorToInt(t);
            int m = Mathf.FloorToInt((t - h) * 60f);
            return string.Format("{0:00}:{1:00}", h, m);
        }
    }
}
