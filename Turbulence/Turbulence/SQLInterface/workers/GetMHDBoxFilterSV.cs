using System;
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
        private int resultSize;
        BigArray<double> sums;

        public GetMHDBoxFilterSV(TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp,
            float filterwidth)
        {
            this.setInfo = setInfo;
            this.spatialInterp = spatialInterp;
            int filter_width = (int)Math.Round(filterwidth / setInfo.Dx);
            this.kernelSize = filter_width;
            // We return 8 sums per component
            this.resultSize = setInfo.Components;
        }

        public GetMHDBoxFilterSV(TurbDataTable setInfo,
            int filterwidth)
        {
            this.setInfo = setInfo;
            this.spatialInterp = spatialInterp;
            this.kernelSize = filterwidth;
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

            int startz = Z - KernelSize / 2, starty = Y - KernelSize / 2, startx = X - KernelSize / 2;
            int endz = Z + KernelSize / 2, endy = Y + KernelSize / 2, endx = X + KernelSize / 2;

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

        public override double[] GetResult(TurbulenceBlob blob, SQLUtility.InputRequest input)
        {
            throw new NotImplementedException();
        }

        public override double[] GetResult(TurbulenceBlob blob, SQLUtility.MHDInputRequest input)
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

        /// <summary>
        /// Produces a flattened 3d array, where each element in the array is the filtered value for the field.
        /// Elements in the array are offset by step.
        /// </summary>
        /// <param name="coordinates">Coordinates, at which the filtered cutout is to be generated.
        /// Given in the format [x,y,z,xwidth,ywidth,zwidth], where x,y,z are the bottom left corner
        /// and xwidth, ywidth, zwidth is top right corner.</param>
        /// <param name="step">The step size for the result.</param>
        /// <returns>float[]</returns>
        public float[] GetResult(int[] coordinates, int step)
        {
            double c = Filtering.FilteringCoefficients(KernelSize);

            // These are the widths of the summed volumes array.
            int xwidth, ywidth, zwidth;
            xwidth = cutout_coordinates[3] - cutout_coordinates[0];
            ywidth = cutout_coordinates[4] - cutout_coordinates[1];
            zwidth = cutout_coordinates[5] - cutout_coordinates[2];
            int result_x_width, result_y_width, result_z_width;
            result_x_width = (coordinates[3] - 1 - coordinates[0]) / step + 1;
            result_y_width = (coordinates[4] - 1 - coordinates[1]) / step + 1;
            result_z_width = (coordinates[5] - 1 - coordinates[2]) / step + 1;
            int result_size = setInfo.Components * result_x_width * result_y_width * result_z_width;
            ulong off0;
            int dest = 0;
            
            float[] result = new float[result_size];
            double[] temp_result = new double[setInfo.Components];

            for (int z = coordinates[2]; z < coordinates[5]; z += step)
            {
                for (int y = coordinates[1]; y < coordinates[4]; y += step)
                {
                    for (int x = coordinates[0]; x < coordinates[3]; x += step)
                    {
                        int lowz = z - KernelSize / 2 - 1, lowy = y - KernelSize / 2 - 1, lowx = x - KernelSize / 2 - 1;
                        int highz = z + KernelSize / 2, highy = y + KernelSize / 2, highx = x + KernelSize / 2;

                        // The sum at the top right corner should always be within bounds.
                        off0 = ((ulong)(highz - cutout_coordinates[2]) * (ulong)ywidth * (ulong)xwidth +
                            (ulong)(highy - cutout_coordinates[1]) * (ulong)xwidth +
                            (ulong)highx - (ulong)cutout_coordinates[0]) * (ulong)setInfo.Components;
                        for (int component = 0; component < setInfo.Components; component++)
                        {
                            temp_result[component] = sums[off0 + (ulong)component];
                        }

                        // The rest may not be within bounds, meaning that the sums at those locations are 0.
                        if (lowz >= cutout_coordinates[2] && lowz < cutout_coordinates[5] &&
                            lowy >= cutout_coordinates[1] && lowy < cutout_coordinates[4] &&
                            lowx >= cutout_coordinates[0] && lowx < cutout_coordinates[3])
                        {
                            off0 = ((ulong)(lowz - cutout_coordinates[2]) * (ulong)ywidth * (ulong)xwidth +
                                (ulong)(lowy - cutout_coordinates[1]) * (ulong)xwidth + 
                                (ulong)lowx - (ulong)cutout_coordinates[0]) * (ulong)setInfo.Components;
                            for (int component = 0; component < setInfo.Components; component++)
                            {
                                temp_result[component] -= sums[off0 + (ulong)component];
                            }
                        }
                        if (lowz >= cutout_coordinates[2] && lowz < cutout_coordinates[5] &&
                            lowy >= cutout_coordinates[1] && lowy < cutout_coordinates[4] &&
                            highx >= cutout_coordinates[0] && highx < cutout_coordinates[3])
                        {
                            off0 = ((ulong)(lowz - cutout_coordinates[2]) * (ulong)ywidth * (ulong)xwidth +
                                (ulong)(lowy - cutout_coordinates[1]) * (ulong)xwidth +
                                (ulong)highx - (ulong)cutout_coordinates[0]) * (ulong)setInfo.Components;
                            for (int component = 0; component < setInfo.Components; component++)
                            {
                                temp_result[component] += sums[off0 + (ulong)component];
                            }
                        }
                        if (lowz >= cutout_coordinates[2] && lowz < cutout_coordinates[5] &&
                            highy >= cutout_coordinates[1] && highy < cutout_coordinates[4] &&
                            lowx >= cutout_coordinates[0] && lowx < cutout_coordinates[3])
                        {
                            off0 = ((ulong)(lowz - cutout_coordinates[2]) * (ulong)ywidth * (ulong)xwidth +
                                (ulong)(highy - cutout_coordinates[1]) * (ulong)xwidth +
                                (ulong)lowx - (ulong)cutout_coordinates[0]) * (ulong)setInfo.Components;
                            for (int component = 0; component < setInfo.Components; component++)
                            {
                                temp_result[component] += sums[off0 + (ulong)component];
                            }
                        }
                        if (lowz >= cutout_coordinates[2] && lowz < cutout_coordinates[5] &&
                            highy >= cutout_coordinates[1] && highy < cutout_coordinates[4] &&
                            highx >= cutout_coordinates[0] && highx < cutout_coordinates[3])
                        {
                            off0 = ((ulong)(lowz - cutout_coordinates[2]) * (ulong)ywidth * (ulong)xwidth +
                                (ulong)(highy - cutout_coordinates[1]) * (ulong)xwidth + 
                                (ulong)highx - (ulong)cutout_coordinates[0]) * (ulong)setInfo.Components;
                            for (int component = 0; component < setInfo.Components; component++)
                            {
                                temp_result[component] -= sums[off0 + (ulong)component];
                            }
                        }
                        if (highz >= cutout_coordinates[2] && highz < cutout_coordinates[5] &&
                            lowy >= cutout_coordinates[1] && lowy < cutout_coordinates[4] &&
                            lowx >= cutout_coordinates[0] && lowx < cutout_coordinates[3])
                        {
                            off0 = ((ulong)(highz - cutout_coordinates[2]) * (ulong)ywidth * (ulong)xwidth +
                                (ulong)(lowy - cutout_coordinates[1]) * (ulong)xwidth + 
                                (ulong)lowx - (ulong)cutout_coordinates[0]) * (ulong)setInfo.Components;
                            for (int component = 0; component < setInfo.Components; component++)
                            {
                                temp_result[component] += sums[off0 + (ulong)component];
                            }
                        }
                        if (highz >= cutout_coordinates[2] && highz < cutout_coordinates[5] &&
                            lowy >= cutout_coordinates[1] && lowy < cutout_coordinates[4] &&
                            highx >= cutout_coordinates[0] && highx < cutout_coordinates[3])
                        {
                            off0 = ((ulong)(highz - cutout_coordinates[2]) * (ulong)ywidth * (ulong)xwidth +
                                (ulong)(lowy - cutout_coordinates[1]) * (ulong)xwidth + 
                                (ulong)highx - (ulong)cutout_coordinates[0]) * (ulong)setInfo.Components;
                            for (int component = 0; component < setInfo.Components; component++)
                            {
                                temp_result[component] -= sums[off0 + (ulong)component];
                            }
                        }
                        if (highz >= cutout_coordinates[2] && highz < cutout_coordinates[5] &&
                            highy >= cutout_coordinates[1] && highy < cutout_coordinates[4] &&
                            lowx >= cutout_coordinates[0] && lowx < cutout_coordinates[3])
                        {
                            off0 = ((ulong)(highz - cutout_coordinates[2]) * (ulong)ywidth * (ulong)xwidth +
                                (ulong)(highy - cutout_coordinates[1]) * (ulong)xwidth + 
                                (ulong)lowx - (ulong)cutout_coordinates[0]) * (ulong)setInfo.Components;
                            for (int component = 0; component < setInfo.Components; component++)
                            {
                                temp_result[component] -= sums[off0 + (ulong)component];
                            }
                        }

                        for (int i = 0; i < setInfo.Components; i++)
                        {
                            result[dest + i] = (float)(temp_result[i] * c);
                        }
                        dest += setInfo.Components;
                    }
                }
            }

            return result;
        }

        public override int GetResultSize()
        {
            return resultSize;
        }

        public override void GetData(short datasetID, string turbinfodb, int timestep, int[] coordinates)
        {
            cutout_coordinates = GetCutoutCoordinates(coordinates);
            int x_width, y_width, z_width;
            x_width = cutout_coordinates[3] - cutout_coordinates[0];
            y_width = cutout_coordinates[4] - cutout_coordinates[1];
            z_width = cutout_coordinates[5] - cutout_coordinates[2];
            ulong cutout_size = (ulong)setInfo.Components * (ulong)x_width * (ulong)y_width * (ulong)z_width;
            if (cutout_size > int.MaxValue / sizeof(float))
            {
                //big_cutout = new BigArray<float>(cutout_size);
                //using_big_cutout = true;
                throw new Exception("Cutout size is too big!");
            }
            else
            {
                //cutout = new float[cutout_size];
            }

            InitializeSummedVolumes(x_width, y_width, z_width);

            GetCutout(datasetID, turbinfodb, timestep);
        }

        public override int[] GetCutoutCoordinates(int[] coordinates)
        {
            int startx = coordinates[0] - KernelSize / 2;
            if (startx < 0)
                startx = startx - startx % setInfo.atomDim - setInfo.atomDim;
            else
                startx = startx - startx % setInfo.atomDim;
            int starty = coordinates[1] - KernelSize / 2;
            if (starty < 0)
                starty = starty - starty % setInfo.atomDim - setInfo.atomDim;
            else
                starty = starty - starty % setInfo.atomDim;
            int startz = coordinates[2] - KernelSize / 2;
            if (startz < 0)
                startz = startz - startz % setInfo.atomDim - setInfo.atomDim;
            else
                startz = startz - startz % setInfo.atomDim;
            // The end coordinates should never really be less than 0.
            int endx = coordinates[3] + KernelSize / 2;
            endx = endx - endx % setInfo.atomDim + setInfo.atomDim;
            int endy = coordinates[4] + KernelSize / 2;
            endy = endy - endy % setInfo.atomDim + setInfo.atomDim;
            int endz = coordinates[5] + KernelSize / 2;
            endz = endz - endz % setInfo.atomDim + setInfo.atomDim;
            return new int[] { startx, starty, startz, endx, endy, endz };
        }
        
        protected override void GetLocalCutout(TurbDataTable table, string dbname, int timestep,
            int[] local_coordinates,
            SqlConnection connection)
        {
            int startx, starty, startz, x_width, y_width, z_width;
            x_width = cutout_coordinates[3] - cutout_coordinates[0];
            y_width = cutout_coordinates[4] - cutout_coordinates[1];
            z_width = cutout_coordinates[5] - cutout_coordinates[2];

            byte[] rawdata = new byte[table.BlobByteSize];

            string tableName = String.Format("{0}.dbo.{1}", dbname, table.TableName);
            int atomWidth = table.atomDim;

            string queryString = GetQueryString(local_coordinates, tableName, dbname, timestep);
                        
            TurbulenceBlob atom = new TurbulenceBlob(table);

            SqlCommand command = new SqlCommand(
                queryString, connection);
            command.CommandTimeout = 600;
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    // read in the current blob
                    long thisBlob = reader.GetSqlInt64(0).Value;
                    int bytesread = 0;
                    while (bytesread < table.BlobByteSize)
                    {
                        int bytes = (int)reader.GetBytes(1, table.SqlArrayHeaderSize, rawdata, bytesread, table.BlobByteSize - bytesread);
                        bytesread += bytes;
                    }

                    atom.Setup(timestep, new Morton3D(thisBlob), rawdata);

                    // Check for wrap around:
                    startx = cutout_coordinates[0];
                    if (atom.GetBaseX > local_coordinates[3])
                        startx += setInfo.GridResolutionX;
                    else if (atom.GetBaseX + atom.GetSide < local_coordinates[0])
                        startx -= setInfo.GridResolutionX;

                    starty = cutout_coordinates[1];
                    if (atom.GetBaseY > local_coordinates[4])
                        starty += setInfo.GridResolutionY;
                    else if (atom.GetBaseY + atom.GetSide < local_coordinates[1])
                        starty -= setInfo.GridResolutionY;

                    startz = cutout_coordinates[2];
                    if (atom.GetBaseZ > local_coordinates[5])
                        startz += setInfo.GridResolutionZ;
                    else if (atom.GetBaseZ + atom.GetSide < local_coordinates[2])
                        startz -= setInfo.GridResolutionZ;

                    UpdateSummedVolumes(atom, startx, starty, startz, x_width, y_width, z_width);
                }
            }
        }

        protected override string GetQueryString(int startx, int starty, int startz, int endx, int endy, int endz, string tableName, string dbname, int timestep)
        {
            return String.Format(
                   "select t.zindex, t.data " +
                   "from {7} as t inner join " +
                   "(select zindex from {8}..zindex where " +
                       "X >= {0} & -{6} and X < {3} and Y >= {1} & -{6} and Y < {4} and Z >= {2} & -{6} and z < {5}) " +
                   "as c " +
                   "on t.zindex = c.zindex " +
                   "and t.timestep = {9}",
                   startx, starty, startz,
                   endx, endy, endz,
                   setInfo.atomDim, tableName, dbname, timestep);
        }
    }
}
