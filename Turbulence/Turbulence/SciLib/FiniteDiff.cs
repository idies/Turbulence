using System;
using System.Collections.Generic;
using System.Text;

namespace Turbulence.SciLib
{
    class FiniteDiff
    {
        public static float SecFiniteDiff4(float dx, float x1, float x2, float x3, float x4, float x5)
        {
            float result;
            result = 4.0f / 3.0f / dx / dx * (x2 + x4 - 2.0f * x3) - 1.0f / 12.0f / dx / dx * (x1 + x5 - 2.0f * x3);
            return result;
        }

        public static float SecFiniteDiff6(float dx, float x1, float x2, float x3, float x4, float x5, float x6, float x7)
        {
            float result;
            result = 3.0f / 2.0f / dx / dx * (x3 + x5 - 2.0f * x4) - 3.0f / 20.0f / dx / dx * (x2 + x6 - 2.0f * x4)
                + 1.0f / 90.0f / dx / dx * (x1 + x7 - 2.0f * x4);
            return result;
        }

        public static float SecFiniteDiff8(float dx, float x1, float x2, float x3, float x4, float x5, float x6, float x7, float x8, float x9)
        {
            float result;
            result = 792.0f / 591.0f / dx / dx * (x4 + x6 - 2.0f * x5) - 207.0f / 2955.0f / dx / dx * (x3 + x7 - 2.0f * x5)
                - 104.0f / 8865.0f / dx / dx * (x2 + x8 - 2.0f * x5) + 9.0f / 3152.0f / dx / dx * (x1 + x9 - 2.0f * x5);
            return result;
        }

        public static float CrossFiniteDiff4(float dx, float x1, float x2, float x3, float x4, float x5, float x6, float x7, float x8)
        {
            float result;
            result = 1.0f / 3.0f / dx / dx * (x5 + x7 - x6 - x8) - 1.0f / 48.0f / dx / dx * (x1 + x3 - x2 - x4);
            return result;
        }

        public static float CrossFiniteDiff6(float dx, float x1, float x2, float x3, float x4, float x5, float x6, float x7, float x8,
            float x9, float x10, float x11, float x12)
        {
            float result;
            result = 3.0f / 8.0f / dx / dx * (x9 + x11 - x10 - x12) - 3.0f / 80.0f / dx / dx * (x5 + x7 - x6 - x8)
                + 1.0f / 360.0f / dx / dx * (x1 + x3 - x2 - x4);
            return result;
        }

        public static float CrossFiniteDiff8(float dx, float x1, float x2, float x3, float x4, float x5, float x6, float x7, float x8,
            float x9, float x10, float x11, float x12, float x13, float x14, float x15, float x16)
        {
            float result;
            result = 14.0f / 35.0f / dx / dx * (x13 + x15 - x14 - x16) - 1.0f / 20.0f / dx / dx * (x9 + x11 - x10 - x12)
                + 2.0f / 315.0f / dx / dx * (x5 + x7 - x6 - x8) - 1.0f / 2240.0f / dx / dx * (x1 + x3 - x2 - x4);
            return result;
        }
    }
}
