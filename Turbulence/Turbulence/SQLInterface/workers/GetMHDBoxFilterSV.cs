﻿using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Turbulence.TurbLib;
using Turbulence.SciLib;
using Turbulence.SQLInterface;
using System.Collections.Generic;

namespace Turbulence.SQLInterface.workers
{
    public class GetMHDBoxFilterSV : Worker
    {
        int filter_width;

        private int resultSize;
        BigArray<double> sums;

        public GetMHDBoxFilterSV(TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp,
            float filterwidth)
        {
            this.setInfo = setInfo;
            this.spatialInterp = spatialInterp;
            int fw = (int)Math.Round(filterwidth / setInfo.Dx);
            this.filter_width = fw;
            this.kernelSize = filter_width;
            // We return 8 sums per component
            this.resultSize = setInfo.Components;
        }

        public override SqlMetaData[] GetRecordMetaData()
        {
            SqlMetaData[] recordMetaData = new SqlMetaData[1 + setInfo.Components];
            recordMetaData[0] = new SqlMetaData("Req", SqlDbType.Int);

            for (int i = 0; i < setInfo.Components; i++)
            {
                recordMetaData[1 + i] = new SqlMetaData(String.Format("Component{0}", i), SqlDbType.Real);
            }

            return recordMetaData;
        }

        public override void GetAtomsForPoint(SQLUtility.MHDInputRequest request, long mask, int pointsPerCubeEstimate, Dictionary<long, List<int>> map, ref int total_points)
        {
            int X, Y, Z;
            X = LagInterpolation.CalcNodeWithRound(request.x, setInfo.Dx);
            Y = LagInterpolation.CalcNodeWithRound(request.y, setInfo.Dx);
            Z = LagInterpolation.CalcNodeWithRound(request.z, setInfo.Dx);

            int startz = Z - filter_width / 2, starty = Y - filter_width / 2, startx = X - filter_width / 2;
            int endz = Z + filter_width / 2, endy = Y + filter_width / 2, endx = X + filter_width / 2;

            // we do not want a request to appear more than once in the list for an atom
            // with the below logic we are going to check distinct atoms only
            // we want to start at the start of a DB atom and then move from atom to atom
            startz = startz - ((startz % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;
            starty = starty - ((starty % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;
            startx = startx - ((startx % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;

            long zindex;

            for (int z = startz; z <= endz; z += setInfo.atomDim)
            {
                for (int y = starty; y <= endy; y += setInfo.atomDim)
                {
                    for (int x = startx; x <= endx; x += setInfo.atomDim)
                    {
                        // Wrap the coordinates into the grid space
                        int xi = ((x % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
                        int yi = ((y % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
                        int zi = ((z % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;

                        if (setInfo.PointInRange(xi, yi, zi))
                        {
                            zindex = new Morton3D(zi, yi, xi).Key & mask;
                            if (!map.ContainsKey(zindex))
                            {
                                //map[zindex] = new List<int>(pointsPerCubeEstimate);
                                map[zindex] = new List<int>();
                            }
                            map[zindex].Add(request.request);
                            request.numberOfCubes++;
                            total_points++;
                        }
                    }
                }
            }
        }

        public override float[] GetResult(TurbulenceBlob blob, SQLUtility.InputRequest input)
        {
            throw new NotImplementedException();
        }

        public override float[] GetResult(TurbulenceBlob blob, SQLUtility.MHDInputRequest input)
        {
            throw new NotImplementedException();
        }

        public void InitializeSummedVolumes(int xwidth, int ywidth, int zwidth)
        {
            sums = new BigArray<double>((ulong)xwidth * (ulong)ywidth * (ulong)zwidth * (ulong)resultSize);
        }

        public void DeleteSummedVolumes()
        {
            sums = null;
        }

        public void UpdateSummedVolumes(TurbulenceBlob atom, int startx, int starty, int startz, int xwidth, int ywidth, int zwidth)
        {
            int data_index, sumsx, sumsy, sumsz, sums_index0, sums_index1, data_index1;
            ulong sums_index, temp_sums_index = 0;
            double[] temp_sum = new double[setInfo.Components];
            sums_index0 = atom.GetBaseX - startx;

            for (int atomz = 0; atomz < atom.GetSide; atomz++)
            {
                sumsz = atomz + atom.GetBaseZ - startz;
                sums_index1 = sums_index0 + sumsz * ywidth * xwidth;
                data_index1 = atomz * atom.GetSide * atom.GetSide * setInfo.Components;
                for (int atomy = 0; atomy < atom.GetSide; atomy++)
                {
                    sumsy = atomy + atom.GetBaseY - starty;
                    sums_index = (ulong)((sums_index1 + sumsy * xwidth) * setInfo.Components);
                    data_index = data_index1 + atomy * atom.GetSide * setInfo.Components;
                    for (int atomx = 0; atomx < atom.GetSide; atomx++)
                    {
                        sumsx = atomx + sums_index0;
                        //data_index = (atomz * atom.GetSide * atom.GetSide + atomy * atom.GetSide + atomx) * setInfo.Components;
                        //sums_index = (ulong)((sumsz * ywidth * xwidth + sumsy * xwidth + sumsx) * setInfo.Components);
                        for (int component = 0; component < setInfo.Components; component++)
                        {
                            sums[sums_index + (ulong)component] += atom.data[data_index + component];
                            temp_sum[component] = sums[sums_index + (ulong)component];
                        }

                        // We need to update point (x+1,y,z)
                        // Unless x+1 is greater than or equal to xwidth
                        if (sumsx + 1 < xwidth)
                        {
                            temp_sums_index = sums_index + (ulong)setInfo.Components;
                            for (int component = 0; component < setInfo.Components; component++)
                            {
                                sums[temp_sums_index + (ulong)component] += temp_sum[component];
                            }
                        }
                        // We need to update point (x,y+1,z)
                        // Unless y+1 is greater than or equal to ywidth
                        if (sumsy + 1 < ywidth)
                        {
                            temp_sums_index = sums_index + (ulong)(xwidth * setInfo.Components);
                            for (int component = 0; component < setInfo.Components; component++)
                            {
                                sums[temp_sums_index + (ulong)component] += temp_sum[component];
                            }
                        }
                        // We need to update point (x+1,y+1,z)
                        // Unless x+1 is greater than or equal to xwidth
                        // or y+1 is greater than or equal to ywidth
                        if ((sumsx + 1 < xwidth) && (sumsy + 1 < ywidth))
                        {
                            temp_sums_index = sums_index + (ulong)(xwidth * setInfo.Components + setInfo.Components);
                            for (int component = 0; component < setInfo.Components; component++)
                            {
                                sums[temp_sums_index + (ulong)component] -= temp_sum[component];
                            }
                        }
                        // We need to update point (x,y,z+1)
                        // Unless z+1 is greater than or equal to zwidth
                        if (sumsz + 1 < zwidth)
                        {
                            temp_sums_index = sums_index + (ulong)(ywidth * xwidth * setInfo.Components);
                            for (int component = 0; component < setInfo.Components; component++)
                            {
                                sums[temp_sums_index + (ulong)component] += temp_sum[component];
                            }
                        }
                        // We need to update point (x+1,y,z+1)
                        // Unless x+1 is greater than or equal to xwidth
                        // or z+1 is greater than or equal to zwidth
                        if ((sumsx + 1 < xwidth) && (sumsz + 1 < zwidth))
                        {
                            temp_sums_index = sums_index + (ulong)(ywidth * xwidth * setInfo.Components + setInfo.Components);
                            for (int component = 0; component < setInfo.Components; component++)
                            {
                                sums[temp_sums_index + (ulong)component] -= temp_sum[component];
                            }
                        }
                        // We need to update point (x,y+1,z+1)
                        // Unless z+1 is greater than or equal to zwidth
                        // or y+1 is greater than or equal to ywidth
                        if ((sumsz + 1 < zwidth) && (sumsy + 1 < ywidth))
                        {
                            temp_sums_index = sums_index + (ulong)(ywidth * xwidth * setInfo.Components + xwidth * setInfo.Components);
                            for (int component = 0; component < setInfo.Components; component++)
                            {
                                sums[temp_sums_index + (ulong)component] -= temp_sum[component];
                            }
                        }
                        // We need to update point (x+,y+1,z+1)
                        // Unless x+1 is greater than or equal to xwidth
                        // or y+1 is greater than or equal to ywidth
                        // or z+1 is greater than or equal to zwidth
                        if ((sumsx + 1 < xwidth) && (sumsy + 1 < ywidth) && (sumsz + 1 < zwidth))
                        {
                            temp_sums_index = sums_index + (ulong)(ywidth * xwidth * setInfo.Components + xwidth * setInfo.Components + setInfo.Components);
                            for (int component = 0; component < setInfo.Components; component++)
                            {
                                sums[temp_sums_index + (ulong)component] += temp_sum[component];
                            }
                        }

                        sums_index += (ulong)setInfo.Components;
                        data_index += setInfo.Components;
                    }
                }
            }
        }

        //public float[] GetResult(BigArray<double> sums, SQLUtility.MHDInputRequest input, int startx, int starty, int startz, int xwidth, int ywidth, int zwidth)
        public float[] GetResult(SQLUtility.MHDInputRequest input, int startx, int starty, int startz, int xwidth, int ywidth, int zwidth)
        {
            double c = Filtering.FilteringCoefficients(KernelSize);
            ulong off0;
            double[] result = new double[resultSize];
            float[] up = new float[setInfo.Components];

            int X = LagInterpolation.CalcNodeWithRound(input.x, setInfo.Dx);
            int Y = LagInterpolation.CalcNodeWithRound(input.y, setInfo.Dx);
            int Z = LagInterpolation.CalcNodeWithRound(input.z, setInfo.Dx);
            int lowz = Z - KernelSize / 2 - 1, lowy = Y - KernelSize / 2 - 1, lowx = X - KernelSize / 2 - 1;
            int highz = Z + KernelSize / 2, highy = Y + KernelSize / 2, highx = X + KernelSize / 2;

            // Wrap the coordinates into the grid space
            lowz = ((lowz % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
            lowy = ((lowy % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
            lowx = ((lowx % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;

            highz = ((highz % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
            if (highz < setInfo.StartZ || highz > setInfo.EndZ)
                highz = setInfo.EndZ;
            highy = ((highy % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
            if (highy < setInfo.StartY || highy > setInfo.EndY)
                highy = setInfo.EndY;
            highx = ((highx % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
            if (highx < setInfo.StartX || highx > setInfo.EndX)
                highx = setInfo.EndX;

            if (lowz >= startz && lowz < startz + zwidth &&
                lowy >= starty && lowy < starty + ywidth &&
                lowx >= startx && lowx < startx + xwidth)
            {
                off0 = ((ulong)(lowz - startz) * (ulong)ywidth * (ulong)xwidth +
                    (ulong)(lowy - starty) * (ulong)xwidth + (ulong)lowx - (ulong)startx) * (ulong)setInfo.Components;
                for (int component = 0; component < setInfo.Components; component++)
                {
                    result[component] -= sums[off0 + (ulong)component];
                }
            }
            if (lowz >= startz && lowz < startz + zwidth &&
                lowy >= starty && lowy < starty + ywidth &&
                highx >= startx && highx < startx + xwidth)
            {
                off0 = ((ulong)(lowz - startz) * (ulong)ywidth * (ulong)xwidth +
                    (ulong)(lowy - starty) * (ulong)xwidth + (ulong)highx - (ulong)startx) * (ulong)setInfo.Components;
                for (int component = 0; component < setInfo.Components; component++)
                {
                    result[component] += sums[off0 + (ulong)component];
                }
            }
            if (lowz >= startz && lowz < startz + zwidth &&
                highy >= starty && highy < starty + ywidth &&
                lowx >= startx && lowx < startx + xwidth)
            {
                off0 = ((ulong)(lowz - startz) * (ulong)ywidth * (ulong)xwidth +
                    (ulong)(highy - starty) * (ulong)xwidth + (ulong)lowx - (ulong)startx) * (ulong)setInfo.Components;
                for (int component = 0; component < setInfo.Components; component++)
                {
                    result[component] += sums[off0 + (ulong)component];
                }
            }
            if (lowz >= startz && lowz < startz + zwidth &&
                highy >= starty && highy < starty + ywidth &&
                highx >= startx && highx < startx + xwidth)
            {
                off0 = ((ulong)(lowz - startz) * (ulong)ywidth * (ulong)xwidth +
                    (ulong)(highy - starty) * (ulong)xwidth + (ulong)highx - (ulong)startx) * (ulong)setInfo.Components;
                for (int component = 0; component < setInfo.Components; component++)
                {
                    result[component] -= sums[off0 + (ulong)component];
                }
            }
            if (highz >= startz && highz < startz + zwidth &&
                lowy >= starty && lowy < starty + ywidth &&
                lowx >= startx && lowx < startx + xwidth)
            {
                off0 = ((ulong)(highz - startz) * (ulong)ywidth * (ulong)xwidth +
                    (ulong)(lowy - starty) * (ulong)xwidth + (ulong)lowx - (ulong)startx) * (ulong)setInfo.Components;
                for (int component = 0; component < setInfo.Components; component++)
                {
                    result[component] += sums[off0 + (ulong)component];
                }
            }
            if (highz >= startz && highz < startz + zwidth &&
                lowy >= starty && lowy < starty + ywidth &&
                highx >= startx && highx < startx + xwidth)
            {
                off0 = ((ulong)(highz - startz) * (ulong)ywidth * (ulong)xwidth +
                    (ulong)(lowy - starty) * (ulong)xwidth + (ulong)highx - (ulong)startx) * (ulong)setInfo.Components;
                for (int component = 0; component < setInfo.Components; component++)
                {
                    result[component] -= sums[off0 + (ulong)component];
                }
            }
            if (highz >= startz && highz < startz + zwidth &&
                highy >= starty && highy < starty + ywidth &&
                lowx >= startx && lowx < startx + xwidth)
            {
                off0 = ((ulong)(highz - startz) * (ulong)ywidth * (ulong)xwidth +
                    (ulong)(highy - starty) * (ulong)xwidth + (ulong)lowx - (ulong)startx) * (ulong)setInfo.Components;
                for (int component = 0; component < setInfo.Components; component++)
                {
                    result[component] -= sums[off0 + (ulong)component];
                }
            }
            if (highz >= startz && highz < startz + zwidth &&
                highy >= starty && highy < starty + ywidth &&
                highx >= startx && highx < startx + xwidth)
            {
                off0 = ((ulong)(highz - startz) * (ulong)ywidth * (ulong)xwidth +
                    (ulong)(highy - starty) * (ulong)xwidth + (ulong)highx - (ulong)startx) * (ulong)setInfo.Components;
                for (int component = 0; component < setInfo.Components; component++)
                {
                    result[component] += sums[off0 + (ulong)component];
                }
            }

            for (int i = 0; i < setInfo.Components; i++)
            {
                up[i] = (float)(result[i] * c);
            }
            return up;
        }

        public override int GetResultSize()
        {
            return resultSize;
        }

    }
}