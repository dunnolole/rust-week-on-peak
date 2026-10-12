using System;
using HarmonyLib;
using UnityEngine;

namespace RustWeekOnPEAK
{
    /// <summary>
    /// Bridges PEAK's own input sampling to the Rust layer, so gathering and crafting use
    /// the game's real bindings (primary use, secondary use, scroll) instead of guessing
    /// at raw keys.
    /// </summary>
    [HarmonyPatch(typeof(CharacterInput), "Sample")]
    internal static class CharacterInputSamplePatch
    {
        private static void Postfix(CharacterInput __instance)
        {
            try
            {
                Character ch = Character.localCharacter;
                if (ch == null || ch.input == null || ch.input != __instance) return;

                if (__instance.useSecondaryWasPressed) Crafting.HandleSecondary(ch);
                if (__instance.scrollForwardWasPressed) Crafting.HandleScroll(1);
                if (__instance.scrollBackwardWasPressed) Crafting.HandleScroll(-1);
                if (__instance.usePrimaryWasPressed) Gathering.HandlePrimary(ch);
            }
            catch { }
        }
    }
}
