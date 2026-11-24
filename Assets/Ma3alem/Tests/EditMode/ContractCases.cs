using System;
using System.Collections.Generic;
using RTLTMPro;
namespace Ma3alem.Tests
{
    public static class ContractCases
    {
        private static void Check(bool condition, string reason) { if (!condition) throw new InvalidOperationException(reason); }
        public static void JumpTiming_BuffersAndExpires()
        {
            var jump = new PlayerController.JumpTiming();
            jump.Press(0); Check(!jump.Consume(0, 0.125, 0.125), "Cannot jump before ground contact");
            jump.Ground(0.125); Check(jump.Consume(0.125, 0.125, 0.125), "Buffered press must work at boundary");
            Check(!jump.Consume(0.13, 0.125, 0.125), "One press must not double-jump");
            jump.Ground(1); jump.Press(1.125); Check(jump.Consume(1.125, 0.125, 0.125), "Coyote boundary must work");
            jump.Ground(2); jump.Press(2.126); Check(!jump.Consume(2.126, 0.125, 0.125), "Expired coyote must fail");
            jump.Press(3); jump.Ground(3.126); Check(!jump.Consume(3.126, 0.125, 0.125), "Expired buffer must fail");
        }
        public static void HeightScore_TracksMaximumOnly()
        {
            var height = new ScoreManager.HeightTracker();
            height.Observe(-10); Check(height.Score == 0, "Negative starting height");
            height.Observe(12.5f); Check(height.Score == 125, "Height conversion");
            height.Observe(8); height.Observe(float.NaN); height.Observe(float.PositiveInfinity);
            Check(height.Score == 125, "Falling and non-finite input cannot change score");
            height.Observe(12.59f); Check(height.Score == 125, "Points round down");
            height.Observe(20); Check(height.Score == 200, "New maximum advances score");
        }
        public static void Route_SeededReachabilityAndBounds()
        {
            for (int seed = 0; seed < 8; seed++)
            {
                var route = new PlatformSpawner.Route(seed, 12, 29.43f, 7, 6.65f);
                var repeat = new PlatformSpawner.Route(seed, 12, 29.43f, 7, 6.65f);
                for (int i = 0; i < 10000; i++)
                {
                    var previous = route.Previous; var next = route.Next(); var same = repeat.Next();
                    Check(next.X == same.X && next.Y == same.Y && next.Width == same.Width, "Seed must reproduce route");
                    Check(next.Width >= 2 && next.Width <= 5.53f, "Width bounds");
                    Check(Math.Abs(next.X) + next.Width / 2 <= 6.501f, "Wall clearance");
                    float gap = next.Y - previous.Y;
                    Check(gap > 1 && gap < 1.9f, "Vertical gap bounds");
                    float y = 0, vy = 12, x = 0, vx = 0, highest = 0;
                    while (vy > 0 || y > gap)
                    {
                        vx = Math.Min(7, vx + 16 * 0.02f); x += vx * 0.02f;
                        vy -= 29.43f * 0.02f; y += vy * 0.02f; highest = Math.Max(highest, y);
                    }
                    Check(highest > gap + 0.3f, "Numerical jump must clear ledge with reserve");
                    Check(Math.Abs(next.X - previous.X) < x * 0.8f, "Standing-start horizontal travel reserve");
                }
            }
        }
        public static void PoolWindow_StaysBelowCapacity()
        {
            var route = new PlatformSpawner.Route(81473, 12, 29.43f, 7, 6.65f);
            var heights = new Queue<float>(); heights.Enqueue(0);
            float camera = 4.5f;
            for (int step = 0; step < 20000; step++)
            {
                camera += 0.25f;
                while (heights.Count > 0 && heights.Peek() < camera - 8.8f) heights.Dequeue();
                while (route.Previous.Y < camera + 11) heights.Enqueue(route.Next().Y);
                Check(heights.Count < 28, "Visible plus buffered platform window exceeds prewarm capacity");
            }
        }
        public static void Voice_PriorityCooldownAndPreemption()
        {
            var policy = new AudioManager.VoicePolicy();
            Check(policy.CanPlay(AudioManager.VoiceCue.Jump, 0, false, 0), "First request");
            policy.Start(AudioManager.VoiceCue.Jump, 0);
            Check(!policy.CanPlay(AudioManager.VoiceCue.Jump, 5.99, false, 0), "Jump cooldown");
            Check(policy.CanPlay(AudioManager.VoiceCue.Jump, 6, false, 0), "Cooldown expiry");
            Check(policy.CanPlay(AudioManager.VoiceCue.Scared, 0.1, true, 10), "Fear preempts jump inside global gap");
            policy.Start(AudioManager.VoiceCue.Falling, 2);
            Check(!policy.CanPlay(AudioManager.VoiceCue.GameOver, 3, true, 90), "Game Over cannot cut falling reaction");
            Check(policy.CanPlay(AudioManager.VoiceCue.Restart, 2.01, true, 90), "Restart interrupts stale previous run");
            Check(!policy.CanPlay(AudioManager.VoiceCue.Falling, 5.99, false, 0), "Fall cooldown");
            Check(policy.CanPlay(AudioManager.VoiceCue.Falling, 6, false, 0), "Fall cooldown expiry");
            Check(!policy.CanPlay(AudioManager.VoiceCue.Scared, 2.1, false, 0), "Global gap prevents chatter");
        }
        public static string Shape(string text)
        {
            var output = new FastStringBuilder(256);
            RTLSupport.FixRTL(text, output, false, false, true);
            return output.ToString();
        }
        public static void Arabic_JoiningAndNumberOrder()
        {
            Check(Shape("بب") == "\uFE90\uFE91", "Connected beh initial/final in visual RTL order");
            Check(Shape("لا") == "\uFEFB", "Lam-alef ligature");
            Check(Shape("النتيجة: 123").Contains("123"), "Score digits must not reverse");
            Check(Shape("اضغط R علشان تعيد").Contains("R"), "Latin keyboard label retained");
        }
    }
}
