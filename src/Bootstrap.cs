using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RustWeekOnPEAK
{
    /// <summary>
    /// Brings the mashup up in stages so a slow disk (reading Rust's item files) never
    /// blocks the game, and reports each stage as a check for Melty's test runs.
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        public RustHud Hud;

        private bool _started;
        private string _lastScene = "";

        private void Awake()
        {
            Hud = gameObject.AddComponent<RustHud>();
        }

        private void Start()
        {
            if (!_started)
            {
                _started = true;
                StartCoroutine(Init());
            }
        }

        private void Update()
        {
            TestReporter.Tick();

            string scene = SceneManager.GetActiveScene().name;
            if (scene != _lastScene)
            {
                _lastScene = scene;
                BaseCamp.Reset();
            }

            BaseCamp.Tick();
            SelfTest.Update();
        }

        private IEnumerator Init()
        {
            yield return null;

            string rustDir = RustLocator.Find();
            string diag = "";

            if (rustDir == null)
            {
                Plugin.Log.LogWarning("[RustWeek] Rust install not found; running PEAK only.");
                TestReporter.Fail("rust-install", "Rust install not found on this PC");
                diag = "rust: not found";
            }
            else
            {
                TestReporter.Pass("rust-install");
                diag = "rust: " + rustDir + " (" + RustLocator.FoundBy + ")";
                Plugin.Log.LogInfo("[RustWeek] Rust install found at " + rustDir + " via " + RustLocator.FoundBy);
            }

            RustItemDb db = null;
            if (rustDir != null)
            {
                db = new RustItemDb();
                try
                {
                    db.Load(rustDir);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError("[RustWeek] reading Rust item files failed: " + e);
                    db = null;
                }
            }
            Plugin.Db = db;

            if (db != null && db.All.Count >= 300) TestReporter.Pass("rust-items");
            else TestReporter.Fail("rust-items", "read " + (db == null ? 0 : db.All.Count) + " Rust items");

            if (db != null && db.IconsFound >= 300) TestReporter.Pass("rust-icons");
            else TestReporter.Fail("rust-icons", "found " + (db == null ? 0 : db.IconsFound) + " Rust icons");

            if (db != null)
            {
                string peekDump = RustSwap.DumpPeakItems();

                for (int attempt = 0; attempt < 60 && RustSwap.Applied == 0; attempt++)
                {
                    RustSwap.Done = false;
                    try { RustSwap.Apply(db); }
                    catch (Exception e) { Plugin.Log.LogWarning("[RustWeek] item swap attempt failed: " + e.Message); }
                    if (RustSwap.Applied == 0) yield return new WaitForSecondsRealtime(0.5f);
                }

                if (RustSwap.Applied > 0) TestReporter.Pass("item-swap");
                else TestReporter.Fail("item-swap", "no PEAK item prefabs were available");

                WriteDiagnostics(rustDir, diag, db, peekDump);
            }
            else
            {
                TestReporter.Fail("item-swap", "no Rust items to map from");
                WriteDiagnostics(rustDir, diag, null, "");
            }
        }

        private void WriteDiagnostics(string rustDir, string rustLine, RustItemDb db, string peakDump)
        {
            try
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RustWeekOnPEAK");
                Directory.CreateDirectory(dir);

                var sb = new StringBuilder();
                sb.AppendLine("Rust Week on PEAK diagnostics");
                sb.AppendLine("time: " + DateTime.Now.ToString("s"));
                sb.AppendLine(rustLine);
                if (db != null)
                {
                    sb.AppendLine("rust items: " + db.All.Count + " from " + db.JsonFiles + " json files");
                    sb.AppendLine("rust icons on disk: " + db.IconsFound);
                    foreach (var kv in db.ByCategory)
                        sb.AppendLine("  category " + kv.Key + ": " + kv.Value.Count);
                }
                sb.AppendLine("PEAK items seen: " + RustSwap.PeekItemsSeen + ", relabelled: " + RustSwap.Applied);
                sb.AppendLine();
                sb.AppendLine("=== PEAK items -> Rust ===");
                sb.AppendLine(peakDump);
                sb.AppendLine();
                sb.AppendLine("=== mapping result ===");
                foreach (var kv in RustSwap.NameByPrefabName)
                {
                    string icon;
                    RustSwap.IconShortnameByPrefabName.TryGetValue(kv.Key, out icon);
                    sb.AppendLine(kv.Key + " -> " + kv.Value + "  [" + (icon ?? "?") + "]");
                }

                File.WriteAllText(Path.Combine(dir, "diagnostics.log"), sb.ToString());
                Plugin.Log.LogInfo("[RustWeek] diagnostics written to " + Path.Combine(dir, "diagnostics.log"));
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[RustWeek] diagnostics failed: " + e.Message);
            }
        }
    }
}
