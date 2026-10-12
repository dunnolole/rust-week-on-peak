using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace RustWeekOnPEAK
{
    /// <summary>
    /// The "first minute" of the run: when a solo world starts, hand the player a Rust
    /// starter kit (a Rock to gather with, water and food) and drop a little camp next
    /// to where they landed, once per world.
    /// </summary>
    public static class BaseCamp
    {
        public static string StatusLine = "BASE CAMP: searching for a landing spot";

        private static bool _done;
        private static readonly List<string> _starterPrefabs = new List<string>();
        private static readonly List<string> _starterLabels = new List<string>();

        private static readonly HashSet<string> MedicalShortnames = new HashSet<string>
        {
            "bandage", "largemedkit", "syringe.medical", "healingtea"
        };

        private static readonly HashSet<string> LightShortnames = new HashSet<string>
        {
            "torch", "flare", "flashlight.held"
        };

        public static void Reset()
        {
            _done = false;
            StatusLine = "BASE CAMP: searching for a landing spot";
        }

        public static void Tick()
        {
            if (_done) return;
            if (!GameHandler.IsInGameplayScene) return;

            Character ch = Character.localCharacter;
            if (ch == null) return;

            if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient)
            {
                _done = true;
                StatusLine = "BASE CAMP: placed by the host";
                return;
            }

            _done = true;
            Place(ch);
        }

        private static void Place(Character ch)
        {
            try
            {
                // The Rock is the key item: without it there is no gathering or crafting.
                int kit = 0;
                kit += RustItems.Give(ch, "rock", 1);
                kit += RustItems.Give(ch, "smallwaterbottle", 1);
                kit += RustItems.Give(ch, "granolabar", 1);

                if (_starterPrefabs.Count == 0) PickPrefabs();
                Vector3 fwd = ch.transform.forward;
                Vector3 right = ch.transform.right;
                Vector3 basePos = ch.transform.position + fwd * 2.2f + Vector3.up * 0.2f;

                int placed = 0;
                for (int i = 0; i < _starterPrefabs.Count; i++)
                {
                    float offset = (i - (_starterPrefabs.Count - 1) * 0.5f) * 1.1f;
                    if (Spawn(_starterPrefabs[i], basePos + right * offset)) placed++;
                }

                StatusLine = "BASE CAMP: kit " + kit + "/3 (ROCK, WATER, FOOD) + " + placed + " drop";
                Plugin.Log.LogInfo("[RustWeek] base camp: " + StatusLine);
            }
            catch (Exception e)
            {
                StatusLine = "BASE CAMP: skipped (" + e.GetType().Name + ")";
                Plugin.Log.LogWarning("[RustWeek] base camp failed: " + e.Message);
            }
        }

        private static void PickPrefabs()
        {
            string light = null, med = null;
            foreach (var kv in RustSwap.IconShortnameByPrefabName)
            {
                string prefab = kv.Key;
                if (prefab.StartsWith("C_", StringComparison.Ordinal)) continue;      // hidden chess props
                if (prefab.Contains("_Prop")) continue;                                // prop-only variants
                if (prefab.Contains("Variant")) continue;

                string sn = kv.Value;
                if (light == null && LightShortnames.Contains(sn)) light = prefab;
                if (med == null && MedicalShortnames.Contains(sn)) med = prefab;
            }

            Add(light, "light");
            Add(med, "medical");
        }

        private static void Add(string prefab, string label)
        {
            if (string.IsNullOrEmpty(prefab)) return;
            _starterPrefabs.Add(prefab);
            _starterLabels.Add(label);
        }

        private static bool Spawn(string prefabName, Vector3 pos)
        {
            try
            {
                if (!PhotonNetwork.InRoom) return false;
                PhotonNetwork.Instantiate("0_Items/" + prefabName, pos,
                    Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), 0);
                return true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[RustWeek] base camp could not place " + prefabName + ": " + e.Message);
                return false;
            }
        }
    }
}
