using UnityEngine;

namespace RustWeekOnPEAK
{
    /// <summary>A small Rust style survival readout drawn over the game, plus the
    /// gather/craft prompts that drive the Rust survival loop.</summary>
    public class RustHud : MonoBehaviour
    {
        public SurvivalSystem Survival = new SurvivalSystem();
        public bool Drew;

        private Texture2D _white;
        private GUIStyle _label;
        private GUIStyle _title;
        private GUIStyle _small;
        private GUIStyle _dim;
        private bool _reportedHud;

        private void Awake()
        {
            SurvivalSystem.Active = Survival;

            _white = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _white.SetPixel(0, 0, Color.white);
            _white.Apply();
        }

        private bool ShouldShow()
        {
            if (TestReporter.TestMode) return true;
            return GameHandler.IsInGameplayScene && Character.localCharacter != null;
        }

        private void Update()
        {
            if (ShouldShow()) Survival.Update();
        }

        private void EnsureStyles()
        {
            if (_title != null) return;
            _title = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
            _title.normal.textColor = new Color(0.87f, 0.35f, 0.18f);
            _label = new GUIStyle(GUI.skin.label) { fontSize = 11 };
            _label.normal.textColor = Color.white;
            _small = new GUIStyle(GUI.skin.label) { fontSize = 10 };
            _small.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
            _dim = new GUIStyle(GUI.skin.label) { fontSize = 10 };
            _dim.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
        }

        private void OnGUI()
        {
            if (_white == null) return;
            if (!ShouldShow()) return;

            EnsureStyles();

            const float w = 300f;
            const float h = 178f;
            float x = Screen.width - w - 18f;
            float y = 18f;

            Fill(new Rect(x - 6f, y - 6f, w + 12f, h + 12f), new Color(0f, 0f, 0f, 0.55f));
            Fill(new Rect(x - 6f, y - 6f, w + 12f, 2f), new Color(0.87f, 0.35f, 0.18f, 0.95f));

            GUI.Label(new Rect(x, y - 2f, w, 20f), "RUST WEEK ON PEAK", _title);

            bool day = DayNightManager.instance != null && DayNightManager.instance.isDay > 0.5f;
            GUI.Label(new Rect(x + 206f, y, 90f, 18f),
                "DAY " + Survival.Day() + "  " + Survival.Clock() + (day ? "  SUN" : "  MOON"), _small);

            float ry = y + 24f;
            Bar(x, ry, "HUNGER", Survival.Hunger01(), new Color(0.85f, 0.65f, 0.2f));
            Bar(x, ry + 22f, "THIRST", Survival.Thirst / 100f, new Color(0.25f, 0.6f, 0.9f));
            Bar(x, ry + 44f, "RADIATION", Survival.Radiation / 100f, new Color(0.5f, 0.9f, 0.25f));
            Bar(x, ry + 66f, "WARMTH", Survival.Warmth01(), new Color(0.9f, 0.5f, 0.3f));

            float sy = ry + 92f;
            GUI.Label(new Rect(x, sy, w, 18f), BaseCamp.StatusLine, _small);

            Character ch = Character.localCharacter;
            bool rock = Gathering.HoldingTool(ch);
            string hint;
            if (Crafting.Open)
                hint = "CRAFT MENU:  LMB craft   scroll pick   RMB close";
            else if (rock)
                hint = "ROCK:  LMB gather   RMB craft";
            else
                hint = "Hold the ROCK to gather and craft";
            GUI.Label(new Rect(x, sy + 16f, w, 18f), hint, _small);

            if (Gathering.MessageActive)
                GUI.Label(new Rect(x, sy + 32f, w, 18f), Gathering.LastMessage, _label);
            else if (Crafting.ResultActive)
                GUI.Label(new Rect(x, sy + 32f, w, 18f), Crafting.LastResult, _label);
            else if (Survival.ToastActive)
                GUI.Label(new Rect(x, sy + 32f, w, 18f), Survival.ToastText, _label);

            if (Crafting.Open) DrawCrafting();

            Drew = true;
            if (!_reportedHud)
            {
                _reportedHud = true;
                TestReporter.Pass("rust-hud");
            }
        }

        private void DrawCrafting()
        {
            const float cw = 300f;
            float ch = 40f + Crafting.Recipes.Count * 26f;
            float x = 18f;
            float y = Screen.height - ch - 18f;

            Fill(new Rect(x - 6f, y - 6f, cw + 12f, ch + 12f), new Color(0f, 0f, 0f, 0.68f));
            Fill(new Rect(x - 6f, y - 6f, cw + 12f, 2f), new Color(0.87f, 0.35f, 0.18f, 0.95f));
            GUI.Label(new Rect(x, y - 2f, cw, 20f), "CRAFTING", _title);

            Character ch2 = Character.localCharacter;
            float ry = y + 22f;
            for (int i = 0; i < Crafting.Recipes.Count; i++)
            {
                Crafting.Recipe r = Crafting.Recipes[i];
                bool can = Crafting.CanCraft(ch2, r);
                bool sel = i == Crafting.Selected;

                var sb = new System.Text.StringBuilder();
                sb.Append(sel ? "> " : "  ").Append(r.Name);
                for (int k = 0; k < r.Ingredients.Length; k++)
                    sb.Append("  ").Append(ShortLabel(r.Ingredients[k]))
                      .Append(' ').Append(RustItems.Count(ch2, r.Ingredients[k])).Append('/').Append(r.Counts[k]);

                GUI.Label(new Rect(x, ry, cw, 22f), sb.ToString(), can ? _label : _dim);
                ry += 26f;
            }
        }

        private static string ShortLabel(string shortname)
        {
            switch (shortname)
            {
                case "wood": return "WOOD";
                case "rock": return "STONE";
                case "plantfiber": return "FIBRE";
                default: return shortname.ToUpperInvariant();
            }
        }

        private void Bar(float x, float y, string label, float fraction, Color color)
        {
            fraction = Mathf.Clamp01(fraction);
            GUI.Label(new Rect(x, y, 78f, 18f), label, _label);
            float bx = x + 80f;
            float bw = 214f;
            Fill(new Rect(bx, y + 3f, bw, 14f), new Color(0f, 0f, 0f, 0.7f));
            Fill(new Rect(bx + 1f, y + 4f, (bw - 2f) * fraction, 12f), color);
        }

        private void Fill(Rect r, Color c)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, _white);
            GUI.color = old;
        }
    }
}
