using System;
using System.Collections.Generic;
using System.Text;

namespace Turbulence.SciLib
{
    public static class LagInterpolation
    {
        public static int CalcNode(double yp, double dx)
        {
            // dx = 2.0F * (float)Math.PI / (float)nx;
            return (int)(Math.Floor(yp / dx));
        }

        public static int CalcNodeWithRound(double yp, double dx)
        {
            // dx = 2.0F * (float)Math.PI / (float)nx;
            return (int)(Math.Round(yp / dx));
        }


        public static void Interpolant(float yp, float dx, int node, ref float[] lagInt, int nOrder)
        {
            switch (nOrder)
            {
                case 4:
                    Interpolant4(yp, dx, node, ref lagInt);
                    break;
                case 6:
                    Interpolant6(yp, dx, node, ref lagInt);
                    break;
                case 8:
                    Interpolant8(yp, dx, node, ref lagInt);
                    break;
                default:
                    break;
            }

        }

        /// <summary>
        /// Optimized version of Interpolant to use a one-dimensional array.
        /// </summary>
        public static void InterpolantOpt(int i, float yp, float dx, int node, float[] lagInt, int nOrder)
        {
            switch (nOrder)
            {
                case 4:
                    Interpolant4Opt(i, yp, dx, node, lagInt);
                    break;
                case 6:
                    Interpolant6Opt(i, yp, dx, node, lagInt);
                    break;
                case 8:
                    Interpolant8Opt(i, yp, dx, node, lagInt);
                    break;
                default:
                    break;
            }

        }

        public static void Interpolant_dx(float yp, float dx, int node, ref float[] lagInt, int nOrder)
        {
            switch (nOrder)
            {
                case 6:
                    Interpolant6_dx(yp, dx, node, ref lagInt);
                    break;
                case 8:
                    Interpolant8_dx(yp, dx, node, ref lagInt);
                    break;
                default:
                    break;
            }
        }

        public static void Interpolant_dx2(float yp, float dx, int node, ref float[] lagInt, int nOrder)
        {
            switch (nOrder)
            {
                case 6:
                    Interpolant6_dx2(yp, dx, node, ref lagInt);
                    break;
                case 8:
                    Interpolant8_dx2(yp, dx, node, ref lagInt);
                    break;
                default:
                    break;
            }
        }

        public static void Interpolant_dx3(float yp, float dx, int node, ref float[] lagInt, int nOrder)
        {
            switch (nOrder)
            {
                case 6:
                    Interpolant6_dx3(yp, dx, node, ref lagInt);
                    break;
                case 8:
                    Interpolant8_dx3(yp, dx, node, ref lagInt);
                    break;
                default:
                    break;
            }
        }

        // For velocity
        public static void Evaluate(ref float[, , ,] u, ref float[] up, ref float[] lagIntx,
            ref float[] lagInty, ref float[] lagIntz, int nOrder)
        {
            int nDim = 3;
            switch (nOrder)
            {
                case 4:
                    for (int idim = 0; idim < nDim; idim++)
                    {
                        up[idim] = 0.0F;
                        for (int ix = 0; ix < 4; ix++)
                        {
                            for (int iy = 0; iy < 4; iy++)
                            {
                                for (int iz = 0; iz < 4; iz++)
                                {
                                    up[idim] = up[idim] + u[iz, iy, ix, idim]
                                     * lagIntx[ix] * lagInty[iy] * lagIntz[iz];
                                }
                            }
                        }
                    }
                    break;
                case 6:
                    for (int idim = 0; idim < nDim; idim++)
                    {
                        up[idim] = 0.0F;
                        for (int ix = 0; ix < 6; ix++)
                        {
                            for (int iy = 0; iy < 6; iy++)
                            {
                                for (int iz = 0; iz < 6; iz++)
                                {
                                    up[idim] = up[idim] + u[iz, iy, ix, idim]
                                     * lagIntx[ix] * lagInty[iy] * lagIntz[iz];
                                }
                            }
                        }
                    }
                    break;
                case 8:
                    for (int idim = 0; idim < nDim; idim++)
                    {
                        up[idim] = 0.0F;
                        for (int ix = 0; ix < 8; ix++)
                        {
                            for (int iy = 0; iy < 8; iy++)
                            {
                                for (int iz = 0; iz < 8; iz++)
                                    up[idim] = up[idim] + u[iz, iy, ix, idim] *
                                        lagIntx[ix] * lagInty[iy] * lagIntz[iz];
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        // For velocity -- optimized [using 1 dimensional arrays ONLY]
        public static void EvaluateOpt(float[] u, double[] up, float[] lagInt, int nOrder, int comps, int dims)
        {
//#if (WORST || SORTED)
            EvaluateOpt(u, 0, up, lagInt, nOrder, comps, dims);
//#else
            //EvaluateOptIterationOrder(u, 0, up, lagInt, nOrder, comps, dims);
//#endif
        }

        public static void EvaluateOpt(float[] u, int offset, double[] up, float[] lagInt, int nOrder, int comps, int dims)
        {
            // URGENT: Add a flag for pressure (or not)
            int nDim = dims;
            switch (nOrder)
            {
                case 4:
                    for (int idim = 0; idim < nDim; idim++)
                    {
                        up[idim] = 0.0F;
                        for (int ix = 0; ix < 4; ix++)
                        {
                            for (int iy = 0; iy < 4; iy++)
                            {
                                for (int iz = 0; iz < 4; iz++)
                                {
                                    up[idim] = up[idim] + u[offset + iz * 4 * 4 * comps + iy * 4 * comps + ix * comps + idim]
                                     * lagInt[ix] * lagInt[1 * 4 + iy] * lagInt[2 * 4 + iz];
                                }
                            }
                        }
                    }
                    break;
                case 6:
                    for (int idim = 0; idim < nDim; idim++)
                    {
                        up[idim] = 0.0F;
                        for (int ix = 0; ix < 6; ix++)
                        {
                            for (int iy = 0; iy < 6; iy++)
                            {
                                for (int iz = 0; iz < 6; iz++)
                                {
                                    up[idim] = up[idim] + u[offset + iz * 6 * 6 * comps + iy * 6 * comps + ix * comps + idim]
                                     * lagInt[ix] * lagInt[1 * 6 + iy] * lagInt[2 * 6 + iz];
                                }
                            }
                        }
                    }
                    break;
                case 8:
                    for (int idim = 0; idim < nDim; idim++)
                    {
                        up[idim] = 0.0F;
                        for (int ix = 0; ix < 8; ix++)
                        {
                            for (int iy = 0; iy < 8; iy++)
                            {
                                for (int iz = 0; iz < 8; iz++)
                                    up[idim] = up[idim] + u[offset + iz * 8 * 8 * comps + iy * 8 * comps + ix * comps + idim] *
                                        lagInt[ix] * lagInt[1 * 8 + iy] * lagInt[2 * 8 + iz];
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        public static void EvaluateOptIterationOrder(float[] u, int offset, float[] up, float[] lagInt, int nOrder, int comps, int dims)
        {
            // URGENT: Add a flag for pressure (or not)
            int nDim = dims;
            switch (nOrder)
            {
                case 4:
                    for (int iz = 0; iz < 4; iz++)
                    {
                        for (int iy = 0; iy < 4; iy++)
                        {
                            for (int ix = 0; ix < 4; ix++)
                            {
                                for (int idim = 0; idim < nDim; idim++)
                                {
                                    up[idim] = up[idim] + u[offset + iz * 4 * 4 * comps + iy * 4 * comps + ix * comps + idim]
                                     * lagInt[ix] * lagInt[1 * 4 + iy] * lagInt[2 * 4 + iz];
                                }
                            }
                        }
                    }
                    break;
                case 6:
                    for (int iz = 0; iz < 6; iz++)
                    {
                        for (int iy = 0; iy < 6; iy++)
                        {
                            for (int ix = 0; ix < 6; ix++)
                            {
                                for (int idim = 0; idim < nDim; idim++)
                                {
                                    up[idim] = up[idim] + u[offset + iz * 6 * 6 * comps + iy * 6 * comps + ix * comps + idim]
                                     * lagInt[ix] * lagInt[1 * 6 + iy] * lagInt[2 * 6 + iz];
                                }
                            }
                        }
                    }
                    break;
                case 8:
                    for (int iz = 0; iz < 8; iz++)
                    {
                        for (int iy = 0; iy < 8; iy++)
                        {
                            for (int ix = 0; ix < 8; ix++)
                            {
                                for (int idim = 0; idim < nDim; idim++)
                                {
                                    up[idim] = up[idim] + u[offset + iz * 8 * 8 * comps + iy * 8 * comps + ix * comps + idim] *
                                        lagInt[ix] * lagInt[1 * 8 + iy] * lagInt[2 * 8 + iz];
                                }
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }


        public static float EvaluateOptOneDim(float[] u, int offset, int idim, float[] lagInt, int nOrder, int comps)
        {
            float value = 0.0F;
            switch (nOrder)
            {
                case 4:

                    value = 0.0F;
                    for (int ix = 0; ix < 4; ix++)
                    {
                        for (int iy = 0; iy < 4; iy++)
                        {
                            for (int iz = 0; iz < 4; iz++)
                            {
                                value = value + u[offset + iz * 4 * 4 * comps + iy * 4 * comps + ix * comps + idim]
                                 * lagInt[ix] * lagInt[1 * 4 + iy] * lagInt[2 * 4 + iz];
                            }
                        }
                    }

                    break;
                case 6:

                    value = 0.0F;
                    for (int ix = 0; ix < 6; ix++)
                    {
                        for (int iy = 0; iy < 6; iy++)
                        {
                            for (int iz = 0; iz < 6; iz++)
                            {
                                value = value + u[offset + iz * 6 * 6 * comps + iy * 6 * comps + ix * comps + idim]
                                 * lagInt[ix] * lagInt[1 * 6 + iy] * lagInt[2 * 6 + iz];
                            }
                        }
                    }

                    break;
                case 8:

                    value = 0.0F;
                    for (int ix = 0; ix < 8; ix++)
                    {
                        for (int iy = 0; iy < 8; iy++)
                        {
                            for (int iz = 0; iz < 8; iz++)
                                value = value + u[offset + iz * 8 * 8 * comps + iy * 8 * comps + ix * comps + idim] *
                                    lagInt[ix] * lagInt[1 * 8 + iy] * lagInt[2 * 8 + iz];
                        }
                    }

                    break;
                default:
                    break;
            }

            return value;
        }



        // For pressure
        public static void EvaluatePressure(ref float[, , ,] dataBlob, ref double p, ref float[] lagIntx,
            ref float[] lagInty, ref float[] lagIntz, int nOrder)
        {
            p = 0.0;
            switch (nOrder)
            {
                case 4:
                    for (int ix = 0; ix < 4; ix++)
                    {
                        for (int iy = 0; iy < 4; iy++)
                        {
                            for (int iz = 0; iz < 4; iz++)
                                p = p + dataBlob[iz, iy, ix, 0]
                                    * lagIntx[ix] * lagInty[iy] * lagIntz[iz];
                        }
                    }
                    break;
                case 6:
                    for (int ix = 0; ix < 6; ix++)
                    {
                        for (int iy = 0; iy < 6; iy++)
                        {
                            for (int iz = 0; iz < 6; iz++)
                                p = p + dataBlob[iz, iy, ix, 0]
                                    * lagIntx[ix] * lagInty[iy] * lagIntz[iz];
                        }
                    }
                    break;
                case 8:
                    for (int ix = 0; ix < 8; ix++)
                    {
                        for (int iy = 0; iy < 8; iy++)
                        {
                            for (int iz = 0; iz < 8; iz++)
                                p = p + dataBlob[iz, iy, ix, 0] *
                                    lagIntx[ix] * lagInty[iy] * lagIntz[iz];
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        public static void Interpolant4(float yp, float dx, int node, ref float[] lagInt)
        {
            float z = yp / dx - node;
            float z2 = z * z;
            float z3 = z2 * z;
            lagInt[0] = (-2 * z + 3 * z2 - z3) / 6;
            lagInt[1] = (2 - z - 2 * z2 + z3) / 2;
            lagInt[2] = (2 * z + z2 - z3) / 2;
            lagInt[3] = (-z + z3) / 6;
        }

        /// <summary>
        /// Optimized version of Interpolant4 to use a one-dimensional array.
        /// </summary>
        public static void Interpolant4Opt(int i, float yp, float dx, int node, float[] lagInt)
        {
            float z = yp / dx - node;
            float z2 = z * z;
            float z3 = z2 * z;
            lagInt[i * 4 + 0] = (-2 * z + 3 * z2 - z3) / 6;
            lagInt[i * 4 + 1] = (2 - z - 2 * z2 + z3) / 2;
            lagInt[i * 4 + 2] = (2 * z + z2 - z3) / 2;
            lagInt[i * 4 + 3] = (-z + z3) / 6;
        }

        public static void Interpolant6(float yp, float dx, int node, ref float[] lagInt)
        {
            float z = yp / dx - node;
            float z2 = z * z;
            float z3 = z2 * z;
            float z4 = z2 * z2;
            float z5 = z3 * z2;
            lagInt[0] = (6 * z - 5 * z2 - 5 * z3 + 5 * z4 - z5) / 120;
            lagInt[1] = (-12 * z + 16 * z2 - z3 - 4 * z4 + z5) / 24;
            lagInt[2] = (12 - 4 * z - 15 * z2 + 5 * z3 + 3 * z4 - z5) / 12;
            lagInt[3] = (12 * z + 8 * z2 - 7 * z3 - 2 * z4 + z5) / 12;
            lagInt[4] = (-6 * z - z2 + 7 * z3 + z4 - z5) / 24;
            lagInt[5] = (4 * z - 5 * z3 + z5) / 120;
        }

        /// <summary>
        /// Optimized version of Interpolant6 to use a one-dimensional array.
        /// </summary>
        public static void Interpolant6Opt(int i, float yp, float dx, int node, float[] lagInt)
        {
            float z = yp / dx - node;
            float z2 = z * z;
            float z3 = z2 * z;
            float z4 = z3 * z;
            float z5 = z4 * z;
            lagInt[i * 6 + 0] = (6 * z - 5 * z2 - 5 * z3 + 5 * z4 - z5) / 120;
            lagInt[i * 6 + 1] = (-12 * z + 16 * z2 - z3 - 4 * z4 + z5) / 24;
            lagInt[i * 6 + 2] = (12 - 4 * z - 15 * z2 + 5 * z3 + 3 * z4 - z5) / 12;
            lagInt[i * 6 + 3] = (12 * z + 8 * z2 - 7 * z3 - 2 * z4 + z5) / 12;
            lagInt[i * 6 + 4] = (-6 * z - z2 + 7 * z3 + z4 - z5) / 24;
            lagInt[i * 6 + 5] = (4 * z - 5 * z3 + z5) / 120;
        }

        public static void Interpolant6_dx(float yp, float dx, int node, ref float[] lagInt)
        {
            float z = yp / dx - node;
            float z2 = z * z;
            float z3 = z2 * z;
            float z4 = z3 * z;
            lagInt[0] = (6 - 10 * z - 15 * z2 + 20 * z3 - 5 * z4) / 120 / dx;
            lagInt[1] = (-12 + 32 * z - 3 * z2 - 16 * z3 + 5 * z4) / 24 / dx;
            lagInt[2] = (-4 - 30 * z + 15 * z2 + 12 * z3 - 5 * z4) / 12 / dx;
            lagInt[3] = (12 + 16 * z - 21 * z2 - 8 * z3 + 5 * z4) / 12 / dx;
            lagInt[4] = (-6 - 2 * z + 21 * z2 + 4 * z3 - 5 * z4) / 24 / dx;
            lagInt[5] = (4 - 15 * z2 + 5 * z4) / 120 / dx;
        }

        public static void Interpolant6_dx2(float yp, float dx, int node, ref float[] lagInt)
        {
            float z = yp / dx - node;
            float z2 = z * z;
            float z3 = z2 * z;
            float dx2 = dx * dx;
            lagInt[0] = (-10 - 30 * z + 60 * z2 - 20 * z3) / 120 / dx2;
            lagInt[1] = (32 - 6 * z - 48 * z2 + 20 * z3) / 24 / dx2;
            lagInt[2] = (-30 + 30 * z + 36 * z2 - 20 * z3) / 12 / dx2;
            lagInt[3] = (16 - 42 * z - 24 * z2 + 20 * z3) / 12 / dx2;
            lagInt[4] = (-2 + 42 * z + 12 * z2 - 20 * z3) / 24 / dx2;
            lagInt[5] = (-30 * z + 20 * z3) / 120 / dx2;
        }

        public static void Interpolant6_dx3(float yp, float dx, int node, ref float[] lagInt)
        {
            float z = yp / dx - node;
            float z2 = z * z;
            float dx3 = dx * dx * dx;
            lagInt[0] = (-30 + 120 * z - 60 * z2) / 120 / dx3;
            lagInt[1] = (-6 - 96 * z + 60 * z2) / 24 / dx3;
            lagInt[2] = (30 + 72 * z - 60 * z2) / 12 / dx3;
            lagInt[3] = (-42 - 48 * z + 60 * z2) / 12 / dx3;
            lagInt[4] = (42 + 24 * z - 60 * z2) / 24 / dx3;
            lagInt[5] = (-30 + 60 * z2) / 120 / dx3;
        }

        public static void Interpolant6_dx3test(float yp, float dx, int node, float[] lagInt)
        {
            float z = yp / dx - node;
            float z2 = z * z;
            lagInt[0] = (-30 + 120 * z - 60 * z2) / 120;
            lagInt[1] = (-6 - 96 * z + 60 * z2) / 24;
            lagInt[2] = (30 + 72 * z - 60 * z2) / 12;
            lagInt[3] = (-42 - 48 * z + 60 * z2) / 12;
            lagInt[4] = (42 + 24 * z - 60 * z2) / 24;
            lagInt[5] = (-30 + 60 * z2) / 120;
        }


        public static void Interpolant8(float yp, float dx, int node, ref float[] lagInt)
        {
            float z = yp / dx - node;
            float z2 = z * z;
            float z3 = z2 * z;
            float z4 = z3 * z;
            float z5 = z4 * z;
            float z6 = z5 * z;
            float z7 = z6 * z;
            lagInt[0] = -z * (z6 - 7 * z5 + 7 * z4 + 35 * z3 - 56 * z2 - 28F * z + 48) / 5040;
            lagInt[1] = z * (z6 - 6 * z5 - 2 * z4 + 60 * z3 - 71 * z2 - 54 * z + 72) / 720;
            lagInt[2] = -z * (z6 - 5 * z5 - 9 * z4 + 65 * z3 - 16 * z2 - 180 * z + 144) / 240;
            lagInt[3] = (z7 - 4 * z6 - 14 * z5 + 56 * z4 + 49 * z3 - 196 * z2 - 36 * z + 144) / 144;
            lagInt[4] = -z * (z6 - 3 * z5 - 17 * z4 + 39 * z3 + 88 * z2 - 108 * z - 144) / 144;
            lagInt[5] = z * (z6 - 2 * z5 - 18 * z4 + 20 * z3 + 89 * z2 - 18 * z - 72) / 240;
            lagInt[6] = -z * (z6 - z5 - 17 * z4 + 5 * z3 + 64 * z2 - 4 * z - 48) / 720;
            lagInt[7] = z * (z6 - 14 * z4 + 49 * z2 - 36) / 5040;
        }

        /// <summary>
        /// Optimized version of Interpolant8 to use a one-dimensional array.
        /// </summary>
        public static void Interpolant8Opt(int i, float yp, float dx, int node, float[] lagInt)
        {
            float z = yp / dx - node;
            float z2 = z * z;
            float z3 = z2 * z;
            float z4 = z3 * z;
            float z5 = z4 * z;
            float z6 = z5 * z;
            float z7 = z6 * z;
            lagInt[i * 8 + 0] = -z * (z6 - 7 * z5 + 7 * z4 + 35 * z3 - 56 * z2 - 28F * z + 48) / 5040;
            lagInt[i * 8 + 1] = z * (z6 - 6 * z5 - 2 * z4 + 60 * z3 - 71 * z2 - 54 * z + 72) / 720;
            lagInt[i * 8 + 2] = -z * (z6 - 5 * z5 - 9 * z4 + 65 * z3 - 16 * z2 - 180 * z + 144) / 240;
            lagInt[i * 8 + 3] = (z7 - 4 * z6 - 14 * z5 + 56 * z4 + 49 * z3 - 196 * z2 - 36 * z + 144) / 144;
            lagInt[i * 8 + 4] = -z * (z6 - 3 * z5 - 17 * z4 + 39 * z3 + 88 * z2 - 108 * z - 144) / 144;
            lagInt[i * 8 + 5] = z * (z6 - 2 * z5 - 18 * z4 + 20 * z3 + 89 * z2 - 18 * z - 72) / 240;
            lagInt[i * 8 + 6] = -z * (z6 - z5 - 17 * z4 + 5 * z3 + 64 * z2 - 4 * z - 48) / 720;
            lagInt[i * 8 + 7] = z * (z6 - 14 * z4 + 49 * z2 - 36) / 5040;
        }

        public static void Interpolant8_dx(float yp, float dx, int node, ref float[] lagInt)
        {
            float z = yp / dx - node;
            float z2 = z * z;
            float z3 = z2 * z;
            float z4 = z3 * z;
            float z5 = z4 * z;
            float z6 = z5 * z;
            lagInt[0] = -(7 * z6 - 42 * z5 + 35 * z4 + 140 * z3 - 168 * z2 - 56 * z + 48) / 5040 / dx;
            lagInt[1] = (7 * z6 - 36 * z5 - 10 * z4 + 240 * z3 - 213 * z2 - 108 * z + 72) / 720 / dx;
            lagInt[2] = -(7 * z6 - 30 * z5 - 45 * z4 + 260 * z3 - 48 * z2 - 360 * z + 144) / 240 / dx;
            lagInt[3] = (7 * z6 - 24 * z5 - 70 * z4 + 224 * z3 + 147 * z2 - 392 * z - 36) / 144 / dx;
            lagInt[4] = -(7 * z6 - 18 * z5 - 85 * z4 + 156 * z3 + 264 * z2 - 216 * z - 144) / 144 / dx;
            lagInt[5] = (7 * z6 - 12 * z5 - 90 * z4 + 80 * z3 + 267 * z2 - 36 * z - 72) / 240 / dx;
            lagInt[6] = -(7 * z6 - 6 * z5 - 85 * z4 + 20 * z3 + 192 * z2 - 8 * z - 48) / 720 / dx;
            lagInt[7] = (7 * z6 - 70 * z4 + 147 * z2 - 36) / 5040 / dx;
        }

        public static void Interpolant8_dx2(float yp, float dx, int node, ref float[] lagInt)
        {
            float z = yp / dx - node;
            float z2 = z * z;
            float z3 = z2 * z;
            float z4 = z3 * z;
            float z5 = z4 * z;
            float dx2 = dx * dx;
            lagInt[0] = -(42 * z5 - 210 * z4 + 140 * z3 + 420 * z2 - 336 * z - 56) / 5040 / dx2;
            lagInt[1] = (42 * z5 - 180 * z4 - 40 * z3 + 720 * z2 - 426 * z - 108) / 720 / dx2;
            lagInt[2] = -(42 * z5 - 150 * z4 - 180 * z3 + 780 * z2 - 96 * z - 360) / 240 / dx2;
            lagInt[3] = (42 * z5 - 120 * z4 - 280 * z3 + 672 * z2 + 294 * z - 392) / 144 / dx2;
            lagInt[4] = -(42 * z5 - 90 * z4 - 340 * z3 + 468 * z2 + 528 * z - 216) / 144 / dx2;
            lagInt[5] = (42 * z5 - 60 * z4 - 360 * z3 + 240 * z2 + 534 * z - 36) / 240 / dx2;
            lagInt[6] = -(42 * z5 - 30 * z4 - 340 * z3 + 60 * z2 + 384 * z - 8) / 720 / dx2;
            lagInt[7] = (42 * z5 - 280 * z3 + 294 * z) / 5040 / dx2;
        }

        public static void Interpolant8_dx3(float yp, float dx, int node, ref float[] lagInt)
        {
            float z = yp / dx - node;
            float z2 = z * z;
            float z3 = z2 * z;
            float z4 = z3 * z;
            float dx3 = dx * dx * dx;
            lagInt[0] = -(210 * z4 - 840 * z3 + 420 * z2 + 840 * z - 336) / 5040 / dx3;
            lagInt[1] = (210 * z4 - 720 * z3 - 120 * z2 + 1440 * z - 426) / 720 / dx3;
            lagInt[2] = -(210 * z4 - 600 * z3 - 540 * z2 + 1560 * z - 96) / 240 / dx3;
            lagInt[3] = (210 * z4 - 480 * z3 - 840 * z2 + 1344 * z + 294) / 144 / dx3;
            lagInt[4] = -(210 * z4 - 360 * z3 - 1020 * z2 + 936 * z + 528) / 144 / dx3;
            lagInt[5] = (210 * z4 - 240 * z3 - 1080 * z2 + 480 * z + 534) / 240 / dx3;
            lagInt[6] = -(210 * z4 - 120 * z3 - 1020 * z2 + 120 * z + 384) / 720 / dx3;
            lagInt[7] = (210 * z4 - 840 * z2 + 294) / 5040 / dx3;
        }




        /// <summary>
        /// Generate the coefficients for an Nth-order Lagrangian interpolation
        /// </summary>
        /// <remarks>
        /// This algorithm is from
        ///    "An Efficient Interpolation Procedure for High-Order Three-Dimensional Semi-Lagrangian Models"
        ///    R.J. Purser & L.M. Leslie
        ///    Monthly Weather Review, Volume 119, p2492
        /// </remarks>
        /// <param name="offset">Which component to calculate (x=0,y=1,z=2)</param>
        /// <param name="yp">Floating point value of coordinate</param>
        /// <param name="dx">Conversion factor from float to int</param>
        /// <param name="node">Integer value of coordinate</param>
        /// <param name="lagInt">Output array of coeffecients.  Must be at least offset*nOrder in size.</param>
        /// <param name="nOrder">Interpolation size (4/6/8/n)</param>
        /// <param name="denom">Precomputed denominator values (will be regenerated if null)</param>
        public static void InterpolantN(int nOrder, float yp, int node, float dx, float[] denom, int offset, float[] lagInt)
        {
            if (nOrder % 2 == 1 || nOrder <= 2)
            {
                throw new Exception("nOrder must be even and >= 2");
            }

            if (denom == null)
            {
                denom = new float[nOrder];
                InterpolantDenominator(nOrder, denom);
            }

            float z = (yp / dx) - node;

            // The q[i]=a[i]*b[i] products  for the different orders (4,6,8)
            // could be stored in a static array, since they do not change.
            // This would save 17 multiplications per point.
            int n = nOrder;
            float [] e = new float[nOrder];
            float [] f = new float[nOrder];
            //float [] a = new float[nOrder];
            //float [] b = new float[nOrder];
            float [] w = new float[nOrder];
            e[0]= 1.0f;
            f[n-1]= 1.0f;
            //a[0]= 1.0f;
            //b[n-1]= 1.0f;

            int i;

            for(i=1; i<n; i++) {
                e[i]=e[i-1]*(z+n/2-i);
                //a[i]=a[i-1]*(i);
                f[n-1-i] =f[n-i]*(z-n/2-1+i);
                //b[n-1-i]=b[n-i]*(-i);
            }

            for (i = 0; i < n;  i++)
            {
                lagInt[offset * nOrder + i] = e[i] * f[i] / (denom[i]);
                //lagInt[offset * nOrder + i] = e[i] * f[i] / (a[i] * b[i]);
                //w[i] = e[i] * f[i] / (a[i] * b[i]);
            }
        }

        /// <summary>
        /// Generate the coefficients for an Nth-order Lagrangian interpolation
        /// </summary>
        /// <remarks>
        /// This algorithm is from
        ///    "An Efficient Interpolation Procedure for High-Order Three-Dimensional Semi-Lagrangian Models"
        ///    R.J. Purser & L.M. Leslie
        ///    Monthly Weather Review, Volume 119, p2492
        /// </remarks>
        /// <param name="offset">Which component to calculate (x=0,y=1,z=2)</param>
        /// <param name="yp">Floating point value of coordinate</param>
        /// <param name="dx">Conversion factor from float to int</param>
        /// <param name="node">Integer value of coordinate</param>
        /// <param name="lagInt">Output array of coeffecients.  Must be at least offset*nOrder in size.</param>
        /// <param name="nOrder">Interpolation size (4/6/8/n)</param>
        /// <param name="denom">Precomputed denominator values (will be regenerated if null)</param>
        public static void InterpolantN(int nOrder, float yp, int node, double dx, double[] denom, int offset, double[] lagInt)
        {
            if (nOrder % 2 == 1 || nOrder <= 2)
            {
                throw new Exception("nOrder must be even and >= 2");
            }

            if (denom == null)
            {
                denom = new double[nOrder];
                InterpolantDenominator(nOrder, denom);
            }

            double z = (yp / dx) - node;

            // The q[i]=a[i]*b[i] products  for the different orders (4,6,8)
            // could be stored in a static array, since they do not change.
            // This would save 17 multiplications per point.
            int n = nOrder;
            double[] e = new double[nOrder];
            double[] f = new double[nOrder];
            //float [] a = new float[nOrder];
            //float [] b = new float[nOrder];
            double[] w = new double[nOrder];
            e[0] = 1.0f;
            f[n - 1] = 1.0f;
            //a[0]= 1.0f;
            //b[n-1]= 1.0f;

            int i;

            for (i = 1; i < n; i++)
            {
                e[i] = e[i - 1] * (z + n / 2 - i);
                //a[i]=a[i-1]*(i);
                f[n - 1 - i] = f[n - i] * (z - n / 2 - 1 + i);
                //b[n-1-i]=b[n-i]*(-i);
            }

            for (i = 0; i < n; i++)
            {
                lagInt[offset * nOrder + i] = e[i] * f[i] / (denom[i]);
                //lagInt[offset * nOrder + i] = e[i] * f[i] / (a[i] * b[i]);
                //w[i] = e[i] * f[i] / (a[i] * b[i]);
            }
        }

        /// <summary>
        /// Generate the denominator for the Interpolant.
        /// </summary>
        /// <param name="n">Nth-Order Interpolation</param>
        /// <param name="denom">Input array with at least nOrder elements</param>
        unsafe public static void InterpolantDenominator(int nOrder, float[] denom)
        {
            int n = nOrder;

            float[] a = new float[nOrder];
            float[] b = new float[nOrder];

            a[0]= 1.0f;
            b[n-1]= 1.0f;

            int i;
            for (i = 1; i < n; i++)
            {
                a[i] = a[i - 1] * (i);
                b[n - 1 - i] = b[n - i] * (-i);
            }

            for (i = 0; i < n; i++)
            {
                denom[i] = a[i] * b[i];
            }

        }

        /// <summary>
        /// Generate the denominator for the Interpolant.
        /// </summary>
        /// <param name="n">Nth-Order Interpolation</param>
        /// <param name="denom">Input array with at least nOrder elements</param>
        unsafe public static void InterpolantDenominator(int nOrder, double[] denom)
        {
            int n = nOrder;

            double[] a = new double[nOrder];
            double[] b = new double[nOrder];

            a[0] = 1.0f;
            b[n - 1] = 1.0f;

            int i;
            for (i = 1; i < n; i++)
            {
                a[i] = a[i - 1] * (i);
                b[n - 1 - i] = b[n - i] * (-i);
            }

            for (i = 0; i < n; i++)
            {
                denom[i] = a[i] * b[i];
            }

        }

        /// <summary>
        /// Generate the interpolation coefficients using the barycentric weights.
        /// </summary>
        /// <param name="offset">Which component to calculate (x=0,y=1,z=2)</param>
        /// <param name="theta_prime">Floating point value of coordinate</param>
        /// <param name="dx">Conversion factor from float to int</param>
        /// <param name="cell">Integer value of coordinate</param>
        /// <param name="lagInt">Output array of coeffecients.  Must be at least offset*nOrder in size.</param>
        /// <param name="nOrder">Interpolation size (4/6/8/n)</param>
        /// <param name="weights">Precomputed barycentric weights</param>
        public static void InterpolantBarycentricWeights(int nOrder, float theta_prime, double[] grid_value, double[] weights, int offset, double[] lagInt)
        {
            double denom = 0.0;
            for (int i = 0; i < nOrder; i++)
            {
                if (theta_prime - grid_value[i] >= -1.0e-8 && theta_prime - grid_value[i] <= 1.0e-8)
                {
                    lagInt[offset * nOrder + i] = 1;
                    return;
                }
                denom += weights[i] / (theta_prime - grid_value[i]);
            }

            for (int i = 0; i < nOrder; i++)
            {
                lagInt[offset * nOrder + i] = (weights[i] / (theta_prime - grid_value[i])) / denom;
            }
        }
    }
}
