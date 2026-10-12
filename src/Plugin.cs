using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace RustWeekOnPEAK
{
    [BepInPlugin(Guid, "Rust Week on PEAK", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "rustweek.onpeak";

        public static Plugin Instance;
        public static ManualLogSource Log;
        public static RustItemDb Db;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            try
            {
                new Harmony(Guid).PatchAll();
            }
            catch (System.Exception e)
            {
                Log.LogWarning("[RustWeek] Harmony patch failed: " + e.Message);
            }

            var go = new GameObject("RustWeekOnPEAK");
            DontDestroyOnLoad(go);
            go.AddComponent<Bootstrap>();

            Log.LogInfo("Rust Week on PEAK 1.0.0 loaded (PEAK host + Rust content).");
        }
    }
}
