using RhythmCore.Data;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmCore.Logic
{ 
    public static class EasingHelper
    {
        public static float GetLinearIntegral(float t)
        {
            return 0.5f * t * t;
        }

        public static float GetEaseOutQuadIntegral(float t)
        {
            return (t * t) - ((t * t * t) / 3.0f);
        }

        public static float GetEaseInQuadIntegral(float t)
        {
            return (t * t * t) / 3.0f;
        }

        public static float GetEaseInOutQuadIntegral(float t)
        {
            if (t < 0.5f)
            {
                return (2.0f * t * t * t) / 3.0f;
            }
            else
            {
                return (-(2.0f * t * t * t) / 3.0f) + (2.0f * t * t) - t + (1.0f / 6.0f);
            }
        }
        public static float GetIntegratedProgress(float startSpeed, float endSpeed, float t, Data.EasingType type)
        {
            float deltaSpeed = endSpeed - startSpeed;
            float integralOfEase = 0;

            switch (type)
            {
                case Data.EasingType.Linear:
                    integralOfEase = GetLinearIntegral(t);
                    break;

                case Data.EasingType.EaseOutQuad:
                    integralOfEase = GetEaseOutQuadIntegral(t);
                    break;

                case EasingType.EaseInQuad: 
                    integralOfEase = GetEaseInQuadIntegral(t);
                    break;

                case EasingType.EaseInOutQuad: 
                    integralOfEase = GetEaseInOutQuadIntegral(t);
                    break;
            
                default:
                    integralOfEase = GetLinearIntegral(t);
                    break;
            }

            return (startSpeed * t) + (deltaSpeed * integralOfEase);
        }
        public static float Evaluate(float t, EasingType type)
        {
            t = Mathf.Clamp01(t);
            switch (type)
            {
                case EasingType.Linear:
                    return t;

                case EasingType.EaseInQuad:
                    return t * t;

                case EasingType.EaseOutQuad:
                    return t * (2 - t);

                case EasingType.EaseInOutQuad:
                    return t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t;

                default:
                    return t;
            }
        }

        public static float Interpolate(float startValue, float endValue, float t, EasingType type)
        {
            float easeT = Evaluate(t, type);
            return Mathf.LerpUnclamped(startValue, endValue, easeT);
        }
    };
}