using System;

namespace RhythmCore.Logic
{
    public enum HitResult
    {
        None,
        Perfect,
        Great,
        Miss
    }

    public static class Judgement
    {
        public const float WINDOW_PERFECT = 0.05f;
        public const float WINDOW_GREAT = 0.15f;
        public const float WINDOW_MISS = 0.2f; 


        public static HitResult EvaluateHit(double noteTime, double inputTime)
        {
            double diff = Math.Abs(noteTime - inputTime);

            if (diff <= WINDOW_PERFECT) return HitResult.Perfect;
            if (diff <= WINDOW_GREAT) return HitResult.Great;
            if (diff <= WINDOW_MISS && noteTime <= inputTime) return HitResult.Miss;

            return HitResult.None;
        }
    }
}