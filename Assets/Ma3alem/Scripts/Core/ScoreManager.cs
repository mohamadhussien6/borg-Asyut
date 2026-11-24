using System;
using UnityEngine;

namespace Ma3alem
{
    public sealed class ScoreManager : MonoBehaviour
    {
        public static bool SuppressReviewPersistence { get; set; }
        public const string BestKey = "Ma3alem.Best.v1";
        public const string TotalBestKey = "Ma3alem.TotalBest.v2";
        public HeightTracker Height { get; } = new HeightTracker();
        public FloorCombo Combo { get; } = new FloorCombo();
        public int Score => Height.Score + Combo.Bonus;
        public int Best { get; private set; }
        public event Action<int> Changed;
        public event Action NewRecord;
        public event Action<int> ComboChanged;
        private bool recordAnnounced;
        public void Initialize()
        {
            // A legacy height record is retained separately; v2 starts with that baseline.
            Best = Mathf.Max(0, PlayerPrefs.GetInt(TotalBestKey, PlayerPrefs.GetInt(BestKey, 0)));
        }
        public void BeginJump(int floor) { Combo.Begin(floor, Time.timeAsDouble); }
        public void LandOnFloor(int floor)
        {
            int before = Score;
            Combo.Land(floor, Time.timeAsDouble);
            ComboChanged?.Invoke(Combo.Count);
            if (before != Score) { Changed?.Invoke(Score); CheckRecord(); }
        }
        public void EndCombo() { Combo.Break(); ComboChanged?.Invoke(0); }
        private void Update()
        {
            if (Combo.Count > 0 && Combo.Expired(Time.timeAsDouble)) EndCombo();
        }
        private void CheckRecord()
        {
            if (!recordAnnounced && Score > Best) { recordAnnounced = true; NewRecord?.Invoke(); }
        }
        public void Observe(float height)
        {
            int before = Score;
            Height.Observe(height);
            if (before != Score) Changed?.Invoke(Score);
            CheckRecord();
        }
        public void SaveBest()
        {
            Best = Mathf.Max(Best, Score);
            if (SuppressReviewPersistence) return;
            PlayerPrefs.SetInt(BestKey, Mathf.Max(PlayerPrefs.GetInt(BestKey, 0), Height.Score));
            PlayerPrefs.SetInt(TotalBestKey, Best);
            PlayerPrefs.Save();
        }
        public sealed class FloorCombo
        {
            public int Count { get; private set; }
            public int Bonus { get; private set; }
            public int HighestAwardedFloor { get; private set; }
            public int LastSkippedFloors { get; private set; }
            private int takeoff;
            private int highestLandedFloor;
            private bool airborne;
            private double chainDeadline = double.PositiveInfinity;
            public bool Expired(double now) => now > chainDeadline;
            public void Begin(int floor, double now = 0)
            {
                if (airborne) return;
                if (Expired(now)) Count = 0;
                takeoff = floor; airborne = true;
            }
            public void Land(int floor, double now = 0)
            {
                int advanced = floor - Math.Max(takeoff, highestLandedFloor);
                highestLandedFloor = Math.Max(highestLandedFloor, floor);
                if (!airborne) return;
                airborne = false;
                // A two-floor advance clears one intermediate floor and qualifies for a classic-style chain.
                LastSkippedFloors = Math.Max(0, floor - takeoff - 1);
                if (advanced < 2) { Count = 0; return; }
                HighestAwardedFloor = floor;
                if (Expired(now)) Count = 0;
                Count++; chainDeadline = now + 3;
                Bonus += (advanced - 1) * 15 * Math.Min(Count, 5);
            }
            public void Break() { Count = 0; airborne = false; LastSkippedFloors = 0; chainDeadline = double.PositiveInfinity; }
        }
        public sealed class HeightTracker
        {
            public float MaximumHeight { get; private set; }
            public int Score => (int)Math.Floor(MaximumHeight * 10f);
            public void Observe(float height)
            {
                if (!float.IsNaN(height) && !float.IsInfinity(height) && height > MaximumHeight) MaximumHeight = height;
            }
        }
    }
}
