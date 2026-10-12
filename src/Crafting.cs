using System.Collections.Generic;
using UnityEngine;

namespace RustWeekOnPEAK
{
    /// <summary>
    /// A small Rust style crafting menu. Hold the Rock and press secondary use to open
    /// it, scroll to pick a recipe, primary use to craft. Everything it makes exists as
    /// a real PEAK item, so the result can be used or dropped like any other item.
    /// </summary>
    public static class Crafting
    {
        public class Recipe
        {
            public string Name;
            public string Output;
            public string[] Ingredients;
            public int[] Counts;
        }

        public static bool Open;
        public static int Selected;
        public static string LastResult;
        public static float LastResultUntil;

        public static readonly List<Recipe> Recipes = new List<Recipe>
        {
            new Recipe { Name = "Torch",         Output = "torch",       Ingredients = new[] { "wood" },                 Counts = new[] { 2 } },
            new Recipe { Name = "Flare",         Output = "flare",       Ingredients = new[] { "wood", "plantfiber" },   Counts = new[] { 1, 2 } },
            new Recipe { Name = "Bandage",       Output = "bandage",     Ingredients = new[] { "plantfiber" },           Counts = new[] { 3 } },
            new Recipe { Name = "First Aid Kit", Output = "largemedkit", Ingredients = new[] { "bandage", "honey" },     Counts = new[] { 2, 1 } },
            new Recipe { Name = "Campfire",      Output = "campfire",    Ingredients = new[] { "wood", "rock" },         Counts = new[] { 4, 2 } },
        };

        public static void Toggle()
        {
            Open = !Open;
        }

        /// <summary>Secondary use while holding the Rock opens or closes the menu.</summary>
        public static void HandleSecondary(Character ch)
        {
            if (Gathering.HoldingTool(ch)) Toggle();
        }

        public static void HandleScroll(int direction)
        {
            if (!Open || Recipes.Count == 0) return;
            Selected = (Selected + direction + Recipes.Count) % Recipes.Count;
        }

        public static bool CanCraft(Character ch, Recipe r)
        {
            if (ch == null) return false;
            for (int i = 0; i < r.Ingredients.Length; i++)
                if (RustItems.Count(ch, r.Ingredients[i]) < r.Counts[i]) return false;
            return true;
        }

        public static void CraftSelected(Character ch)
        {
            if (Recipes.Count == 0) return;
            if (Selected < 0 || Selected >= Recipes.Count) Selected = 0;
            Craft(ch, Recipes[Selected]);
        }

        public static void Craft(Character ch, Recipe r)
        {
            if (ch == null || r == null) return;

            if (!CanCraft(ch, r))
            {
                LastResult = "NEED MORE MATERIALS";
                LastResultUntil = Time.time + 2.5f;
                return;
            }

            for (int i = 0; i < r.Ingredients.Length; i++)
                RustItems.Take(ch, r.Ingredients[i], r.Counts[i]);

            int given = RustItems.Give(ch, r.Output, 1);
            LastResult = given > 0 ? ("CRAFTED " + r.Name.ToUpperInvariant()) : "HANDS FULL";
            LastResultUntil = Time.time + 2.5f;

            SurvivalSystem sys = SurvivalSystem.Active;
            if (sys != null) sys.Toast(LastResult);
        }

        public static bool ResultActive
        {
            get { return !string.IsNullOrEmpty(LastResult) && Time.time < LastResultUntil; }
        }
    }
}
