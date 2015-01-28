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
    public class GetMHDBoxFilter : Worker
    {
        int filter_width;

        private int resultSize = 3;
        private double[] cachedAtomSum = new double[3];
        private long cachedAtomZindex;

        public GetMHDBoxFilter(TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp,
            float filterwidth)
        {
            this.setInfo = setInfo;
            this.spatialInterp = spatialInterp;
            int fw = (int)Math.Round(filterwidth / setInfo.Dx);
            this.filter_width = fw;
            this.kernelSize = filter_width;
            this.cachedAtomZindex = -1;
        }

        public GetMHDBoxFilter(TurbDataTable setInfo,
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
            return new SqlMetaData[] {
                new SqlMetaData("Req", SqlDbType.Int),
                new SqlMetaData("X", SqlDbType.Real),
                new SqlMetaData("Y", SqlDbType.Real),
                new SqlMetaData("Z", SqlDbType.Real),
                new SqlMetaData("Cubes Read", SqlDbType.Int)};
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

        public override double[] GetResult(TurbulenceBlob blob, SQLUtility.InputRequest input)
        {
            throw new NotImplementedException();
        }

        public override double[] GetResult(TurbulenceBlob blob, SQLUtility.MHDInputRequest input)
        {
            float xp = input.x;
            float yp = input.y;
            float zp = input.z;
            return CalcBoxFilter(blob, xp, yp, zp, input);
        }

        public override int GetResultSize()
        {
            return resultSize;
        }


        /// <summary>
        /// Computes a box filter of a vector field at a target location
        /// </summary>
        /// <remarks>
        /// Similar to GetMHDWorker
        /// </remarks>
        unsafe public double[] CalcBoxFilter(TurbulenceBlob blob, float xp, float yp, float zp, SQLUtility.MHDInputRequest input)
        {
            double[] up = new double[3]; // Result value for the user

            int x = LagInterpolation.CalcNodeWithRound(xp, setInfo.Dx);
            int y = LagInterpolation.CalcNodeWithRound(yp, setInfo.Dx);
            int z = LagInterpolation.CalcNodeWithRound(zp, setInfo.Dx);

            // Wrap the coordinates into the grid space
            x = ((x % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
            y = ((y % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
            z = ((z % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;

            float[] data = blob.data;
            int startz = 0, starty = 0, startx = 0, endz = 0, endy = 0, endx = 0;
            blob.GetSubcubeStart(z - (filter_width / 2), y - (filter_width / 2), x - (filter_width / 2), ref startz, ref starty, ref startx);
            blob.GetSubcubeEnd(z + (filter_width / 2), y + (filter_width / 2), x + (filter_width / 2), ref endz, ref endy, ref endx);

            if (startx == 0 && starty == 0 && startz == 0 && endx == blob.GetSide - 1 && endy == blob.GetSide - 1 && endz == blob.GetSide - 1)
            {
                if (cachedAtomZindex == blob.Key)
                {
                    up[0] = cachedAtomSum[0];
                    up[1] = cachedAtomSum[1];
                    up[2] = cachedAtomSum[2];
                    return up;
                }
            }

            int off0 = startx * blob.GetComponents;

            double c = Filtering.FilteringCoefficients(filter_width);
            double c1 = 0.0, c2 = 0.0, c3 = 0.0;

            fixed (double* lagint = input.lagInt)
            {
                fixed (float* fdata = data)
                {
                    for (int iz = startz; iz <= endz; iz++)
                    {
                        int off1 = off0 + iz * blob.GetSide * blob.GetSide * blob.GetComponents;
                        for (int iy = starty; iy <= endy; iy++)
                        {
                            int off = off1 + iy * blob.GetSide * blob.GetComponents;
                            for (int ix = startx; ix <= endx; ix++)
                            {
                                c1 += c * fdata[off];
                                if (blob.GetComponents > 1)
                                {
                                    c2 += c * fdata[off + 1];
                                    c3 += c * fdata[off + 2];
                                }
                                off += blob.GetComponents;
                            }
                        }
                    }
                }
            }

            if (startx == 0 && starty == 0 && startz == 0 && endx == blob.GetSide - 1 && endy == blob.GetSide - 1 && endz == blob.GetSide - 1)
            {
                cachedAtomZindex = blob.Key;
                cachedAtomSum[0] = c1;
                cachedAtomSum[1] = c2;
                cachedAtomSum[2] = c3;
            }

            up[0] = c1;
            up[1] = c2;
            up[2] = c3;

            return up;
        }

        /// <summary>
        /// Produces a flattened 3d array, where each element in the array is the filtered value for the field.
        /// Elements in the array are offset by step.
        /// </summary>
        /// <param name="coordinates">Coordinates, at which the filtered cutout is to be generated.
        /// Given in the format [x,y,z,xwidth,ywidth,zwidth], where x,y,z are the bottom left corner
        /// and xwidth, ywidth, zwidth is top right corner.</param>
        /// <param name="x_stride">The stride size along x.</param>
        /// <param name="y_stride">The stride size along y.</param>
        /// <param name="z_stride">The stride size along z.</param>
        /// <returns>float[]</returns>
        public float[] GetResult(int[] coordinates, int x_stride, int y_stride, int z_stride)
        {
            double c = Filtering.FilteringCoefficients(KernelSize);

            // These are the widths of the summed volumes array.
            ulong xwidth, ywidth, zwidth;
            xwidth = (ulong)(cutout_coordinates[3] - cutout_coordinates[0]);
            ywidth = (ulong)(cutout_coordinates[4] - cutout_coordinates[1]);
            zwidth = (ulong)(cutout_coordinates[5] - cutout_coordinates[2]);
            int result_x_width, result_y_width, result_z_width;
            result_x_width = (coordinates[3] - 1 - coordinates[0]) / x_stride + 1;
            result_y_width = (coordinates[4] - 1 - coordinates[1]) / y_stride + 1;
            result_z_width = (coordinates[5] - 1 - coordinates[2]) / z_stride + 1;
            int result_size = setInfo.Components * result_x_width * result_y_width * result_z_width;
            ulong off1, off2, off3, off4, off5, off;
            int lowz, lowy, lowx, highz, highy, highx;
            ulong x_y_plane_size = ywidth * xwidth;
            int dest = 0;

            float[] result = new float[result_size];
            double[] temp_result = new double[setInfo.Components];

            for (int z = coordinates[2]; z < coordinates[5]; z += z_stride)
            {
                lowz = z - KernelSize / 2;
                highz = z + KernelSize / 2;
                off1 = (ulong)(lowz - cutout_coordinates[2]) * x_y_plane_size;

                for (int y = coordinates[1]; y < coordinates[4]; y += y_stride)
                {
                    lowy = y - KernelSize / 2;
                    highy = y + KernelSize / 2;
                    off2 = off1 + (ulong)(lowy - cutout_coordinates[1]) * xwidth;

                    for (int x = coordinates[0]; x < coordinates[3]; x += x_stride)
                    {
                        lowx = x - KernelSize / 2;
                        highx = x + KernelSize / 2;
                        off3 = off2 + (ulong)(lowx - cutout_coordinates[0]);

                        //For each (x, y, z) point go through the kernel and compute the filter:
                        for (int kernel_z = lowz; kernel_z <= highz; kernel_z++)
                        {
                            off4 = off3;
                            for (int kernel_y = lowy; kernel_y <= highy; kernel_y++)
                            {
                                off5 = off4;
                                for (int kernel_x = lowx; kernel_x <= highx; kernel_x++)
                                {
                                    off = off5 * (ulong)setInfo.Components;
                                    result[dest] += (float)(c * GetDataItem(off5));
                                    if (setInfo.Components > 1)
                                    {
                                        result[dest + 1] += (float)(c * GetDataItem(off + 1));
                                        result[dest + 2] += (float)(c * GetDataItem(off + 2));
                                    }
                                    off5++;
                                }
                                off4 += xwidth;
                            }
                            off3 += x_y_plane_size;
                        }
                        dest += setInfo.Components;
                    }
                }
            }

            return result;
        }

    }

}
