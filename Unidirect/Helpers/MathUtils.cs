using System;

namespace Unidirect.Helpers
{
    public static class MathUtils
    {
        public static float Map(float input, float inMin, float inMax, float outMin, float outMax, bool clamp = false)
        {
            if (clamp)
                input = Math.Max(inMin, Math.Min(input, inMax));
            return (input - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
        }
    }
}