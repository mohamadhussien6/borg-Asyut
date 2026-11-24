using NUnit.Framework;
namespace Ma3alem.Tests
{
    public sealed class RulesTests
    {
        [Test] public void PermanentAssiutPlateHasOpaqueContiguousPanels()
        {
            const string path = "Assets/Ma3alem/Art/Environment_Assiut_Static.png";
            var sprites = new System.Collections.Generic.List<UnityEngine.Sprite>();
            foreach (var asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is UnityEngine.Sprite sprite) sprites.Add(sprite);
            Assert.That(sprites.Count, Is.EqualTo(3)); sprites.Sort((left, right) => left.rect.x.CompareTo(right.rect.x));
            var texture = new UnityEngine.Texture2D(2, 2);
            try
            {
                Assert.That(UnityEngine.ImageConversion.LoadImage(texture, System.IO.File.ReadAllBytes(path)), Is.True);
                float edge = 0;
                foreach (var sprite in sprites)
                {
                    Assert.That(sprite.rect.x, Is.EqualTo(edge)); edge = sprite.rect.xMax;
                    Assert.That(sprite.rect.y, Is.Zero); Assert.That(sprite.rect.height, Is.EqualTo(texture.height));
                }
                Assert.That(edge, Is.EqualTo(texture.width));
                Assert.That(texture.width / (float)texture.height, Is.EqualTo(16f / 9).Within(0.01f));
                byte minimum = 255; foreach (var pixel in texture.GetPixels32()) if (pixel.a < minimum) minimum = pixel.a;
                Assert.That(minimum, Is.EqualTo(255), "Permanent scenery must be opaque.");
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }
        [Test] public void NarrowPlayfieldRoutesKeepClearanceAndReachableTransitions()
        {
            const float halfWidth = 4.276f;
            for (int seed = 0; seed < 10; seed++)
            {
                var route = new PlatformSpawner.Route(seed, 12, 29.43f, 9.5f, halfWidth, airAcceleration: 22);
                for (int floor = 0; floor < 5000; floor++)
                {
                    var previous = route.Previous; var next = route.Next();
                    Assert.That(System.Math.Abs(next.X) + next.Width / 2, Is.LessThanOrEqualTo(halfWidth - 0.16f));
                    double apex = 144 / (2 * 29.43), gap = next.Y - previous.Y;
                    double flight = 12 / 29.43 + System.Math.Sqrt(2 * (apex - gap) / (29.43 * 1.15));
                    double accelerating = System.Math.Min(flight, 9.5 / 22.0);
                    double reach = (0.5 * 22 * accelerating * accelerating + 9.5 * (flight - accelerating)) * 0.7 - 0.55;
                    Assert.That(System.Math.Abs(next.X - previous.X), Is.LessThanOrEqualTo(reach + 0.001));
                }
            }
        }
        [Test] public void RouteRejectsCandidatesWithoutLosingDeterminism()
        {
            var route = new PlatformSpawner.Route(81473, 12, 29.43f, 7, 6.65f);
            var repeated = new PlatformSpawner.Route(81473, 12, 29.43f, 7, 6.65f);
            for (int i = 0; i < 1000; i++)
            {
                var placement = route.Next(); var same = repeated.Next();
                Assert.That(placement.X, Is.EqualTo(same.X));
                Assert.That(placement.Y, Is.EqualTo(same.Y));
            }
            Assert.That(route.RejectedCandidates, Is.GreaterThan(0));
            Assert.That(route.RejectedCandidates, Is.EqualTo(repeated.RejectedCandidates));
        }
        [Test] public void JumpTiming_BuffersAndExpires() { ContractCases.JumpTiming_BuffersAndExpires(); }
        [Test] public void HeightScore_TracksMaximumOnly() { ContractCases.HeightScore_TracksMaximumOnly(); }
        [Test] public void Route_SeededReachabilityAndBounds() { ContractCases.Route_SeededReachabilityAndBounds(); }
        [Test] public void PoolWindow_StaysBelowCapacity() { ContractCases.PoolWindow_StaysBelowCapacity(); }
        [Test] public void Voice_PriorityCooldownAndPreemption() { ContractCases.Voice_PriorityCooldownAndPreemption(); }
        [Test] public void Arabic_JoiningAndNumberOrder() { ContractCases.Arabic_JoiningAndNumberOrder(); }
        [Test] public void FloorCombo_ChainsResetsAndNeverReawardsOldFloors()
        {
            var combo = new ScoreManager.FloorCombo();
            combo.Begin(0); combo.Land(3);
            Assert.That(combo.Count, Is.EqualTo(1)); Assert.That(combo.Bonus, Is.EqualTo(30));
            combo.Begin(3); combo.Land(7);
            Assert.That(combo.Count, Is.EqualTo(2)); Assert.That(combo.Bonus, Is.EqualTo(120));
            combo.Land(7); Assert.That(combo.Bonus, Is.EqualTo(120), "Duplicate contact cannot award twice.");
            combo.Begin(7); combo.Land(8); Assert.That(combo.Count, Is.Zero);
            combo.Begin(3); combo.Land(7); Assert.That(combo.Bonus, Is.EqualTo(120), "Old floors cannot be farmed.");
            combo.Begin(8); combo.Break(); combo.Land(12); Assert.That(combo.Count, Is.Zero);
            Assert.That(combo.Bonus, Is.EqualTo(120), "Breaking retains earned bonus.");
            combo.Begin(8); combo.Land(9); combo.Begin(6); combo.Land(9);
            Assert.That(combo.Bonus, Is.EqualTo(120), "A floor previously reached without a bonus cannot be farmed later.");
        }
        [Test] public void TwoFloorCombosRewardChainsExpireAndNeverDuplicate()
        {
            var combo = new ScoreManager.FloorCombo();
            combo.Begin(0, 0); combo.Land(2, 0.8); Assert.That(combo.Count, Is.EqualTo(1)); Assert.That(combo.Bonus, Is.EqualTo(15));
            combo.Begin(2, 1); combo.Land(4, 1.8); Assert.That(combo.Count, Is.EqualTo(2)); Assert.That(combo.Bonus, Is.EqualTo(45));
            combo.Land(4, 2); Assert.That(combo.Bonus, Is.EqualTo(45));
            Assert.That(combo.Expired(4.81), Is.True);
            combo.Begin(4, 5); combo.Land(6, 5.8); Assert.That(combo.Count, Is.EqualTo(1)); Assert.That(combo.Bonus, Is.EqualTo(60));
            combo.Begin(2, 6); combo.Land(6, 6.8); Assert.That(combo.Count, Is.Zero); Assert.That(combo.Bonus, Is.EqualTo(60));
        }
        [Test] public void Route_WidthsVaryWithinEveryZone()
        {
            var route = new PlatformSpawner.Route(81473, 12, 29.43f, 7, 6.65f);
            var widths = new System.Collections.Generic.HashSet<float>[4];
            for (int i = 0; i < 4; i++) widths[i] = new System.Collections.Generic.HashSet<float>();
            for (int floor = 1; floor <= 120; floor++)
            {
                var p = route.Next(); int zone = floor >= 91 ? 3 : floor >= 56 ? 2 : floor >= 26 ? 1 : 0;
                widths[zone].Add(p.Width);
            }
            foreach (var zone in widths) Assert.That(zone.Count, Is.GreaterThan(5));
        }
        [TestCase("Scared", true)] [TestCase("Scared_01", true)] [TestCase("Scared_02", true)]
        [TestCase("Scared_notes", false)] [TestCase("Scared_1", false)] [TestCase("Falling_01", false)]
        public void VoiceFilenames_MatchOnlyApprovedTakes(string name, bool expected)
        { Assert.That(Ma3alem.Editor.ProjectTools.MatchesCue(name, "Scared"), Is.EqualTo(expected)); }

        [Test] public void AllNineSaidiRecordingsAreBoundWithoutProcessing()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Ma3alem/Prefabs/Audio.prefab");
            var serialized = new UnityEditor.SerializedObject(prefab.GetComponent<AudioManager>());
            var groups = serialized.FindProperty("voices");
            Assert.That(groups.arraySize, Is.EqualTo(9));
            for (int i = 0; i < 9; i++)
            {
                var group = groups.GetArrayElementAtIndex(i);
                var clip = group.FindPropertyRelative("clips").GetArrayElementAtIndex(0).objectReferenceValue;
                Assert.That(clip.name, Is.EqualTo(((AudioManager.VoiceCue)i).ToString()));
                var importer = (UnityEditor.AudioImporter)UnityEditor.AssetImporter.GetAtPath(UnityEditor.AssetDatabase.GetAssetPath(clip));
                Assert.That(importer.forceToMono, Is.False);
                Assert.That(new UnityEditor.SerializedObject(importer).FindProperty("m_Normalize").boolValue, Is.False);
                Assert.That(importer.defaultSampleSettings.compressionFormat, Is.EqualTo(UnityEngine.AudioCompressionFormat.PCM));
                Assert.That(importer.defaultSampleSettings.sampleRateSetting, Is.EqualTo(UnityEditor.AudioSampleRateSetting.PreserveSampleRate));
            }
        }
        [NUnit.Framework.Test] public void FloorNumberingAndSmoothDifficulty()
        {
            var route = new PlatformSpawner.Route(81473, 12, 29.43f, 7, 6.65f);
            for (int floor = 1; floor <= 100; floor++)
            {
                var placement = route.Next();
                NUnit.Framework.Assert.That(placement.Floor, NUnit.Framework.Is.EqualTo(floor));
                NUnit.Framework.Assert.That(placement.Width, NUnit.Framework.Is.InRange(2.4f, 5.53f));
            }
        }
        [TestCase(26)] [TestCase(56)] [TestCase(91)]
        public void StageWidthFactorsAreContinuousAtNewBoundaries(int boundary)
        {
            var tuning = new PlatformSpawner.Difficulty();
            var starts = new[] { 0f, 26f, 56f, 91f };
            float before = tuning.WidthFactor(boundary - 1, starts);
            float at = tuning.WidthFactor(boundary, starts);
            float after = tuning.WidthFactor(boundary + 1, starts);
            Assert.That(at, Is.LessThanOrEqualTo(before));
            Assert.That(after, Is.LessThanOrEqualTo(at));
            Assert.That(before - at, Is.LessThan(0.003f), "No stage-boundary width-factor cliff.");
            Assert.That(at - after, Is.LessThan(0.003f), "No difficulty reset after the transition.");
            Assert.That(after, Is.InRange(tuning.zone4Width, tuning.zone1Width));
        }
    }
}
