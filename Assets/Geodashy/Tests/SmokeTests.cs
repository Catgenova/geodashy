using System.Collections;
using Geodashy.Core;
using Geodashy.Gameplay;
using Geodashy.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Geodashy.Tests
{
    /// <summary>
    /// Smoke tests that run in CI before an apk is built: every catalog entry draws, share codes round-trip,
    /// and every built-in level survives a few seconds of an auto-jumping bot without logging an error.
    /// </summary>
    public class SmokeTests
    {
        [Test]
        public void EveryCatalogEntryRendersAPlaceholder()
        {
            foreach (var def in ObjectCatalog.All)
            {
                var sprite = PlaceholderSpriteFactory.ForDefinition(def);
                Assert.IsNotNull(sprite, "no placeholder for " + def.id);
            }
            Assert.Greater(ObjectCatalog.All.Count, 400, "the catalog lost entries");
        }

        [Test]
        public void CatalogIdsAreUnique()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var def in ObjectCatalog.All) Assert.IsTrue(seen.Add(def.id), "duplicate id " + def.id);
        }

        [Test]
        public void ShareCodeRoundTrips()
        {
            var level = LevelData.CreateNew("Round trip");
            level.objects.Add(new LevelObject { uid = 1, type = "castle_stone", x = 3f, y = 1f });
            var code = LevelShare.Encode(level);
            Assert.IsTrue(code.StartsWith(LevelShare.Prefix));
            Assert.IsTrue(LevelShare.TryDecode(code, out var back, out var err), err);
            Assert.AreEqual("Round trip", back.name);
            Assert.AreEqual(1, back.objects.Count);
            Assert.AreEqual("castle_stone", back.objects[0].type);
        }

        [Test]
        public void StampsKeepRelativeLayout()
        {
            var a = new LevelObject { uid = 1, type = "castle_stone", x = 2f, y = 0f };
            var b = new LevelObject { uid = 2, type = "castle_stone", x = 4f, y = 0f };
            var stamp = Stamp.FromObjects("pair", new[] { a, b });
            var placed = stamp.Place(new Vector2(10f, 5f));
            Assert.AreEqual(2, placed.Count);
            Assert.AreEqual(2f, placed[1].x - placed[0].x, 0.001f);
            Assert.AreEqual(5f, placed[0].y, 0.001f);
        }

        [UnityTest]
        public IEnumerator EveryBuiltInLevelSurvivesAnAutoJumpBot()
        {
            var levels = Resources.LoadAll<TextAsset>("Levels");
            Assert.Greater(levels.Length, 0, "no built-in levels in Resources/Levels");
            foreach (var ta in levels)
            {
                Assert.IsTrue(LevelSerializer.TryFromJson(ta.text, out var level, out var err), ta.name + ": " + err);
                var camGo = new GameObject("Test Camera");
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 5.5f;
                var root = new GameObject("Test Root");
                var background = ParallaxBackground.Create(root.transform, cam, level.settings);
                var ground = GroundRenderer.Create(root.transform, cam, level.settings);
                var runnerGo = new GameObject("Test Runner");
                runnerGo.transform.SetParent(root.transform, false);
                var runner = runnerGo.AddComponent<GameRunner>();
                float clock = 0f;
                runner.autoInput = () =>
                {
                    clock += Time.deltaTime;
                    bool fire = clock > 0.4f;
                    if (fire) clock = 0f;
                    return (fire, fire);   // a tap every 0.4 s: hoppers hop, flyers get a nudge
                };
                bool exited = false;
                runner.Begin(level, cam, background, ground, null, () => exited = true, Difficulty.Training, "editor");
                float t = 0f;
                while (t < 3f && !exited)
                {
                    t += Time.deltaTime;
                    yield return null;
                }
                runner.Shutdown();
                Object.Destroy(root);
                Object.Destroy(camGo);
                yield return null;
            }
        }
    }
}
