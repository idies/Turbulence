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
    public class GetMHDBoxFilterSGS : Worker
    {
        int filter_width;

        private int resultSize = 9;
        private double[] cachedAtomSum = new double[9];
        private long cachedAtomZindex;

        public GetMHDBoxFilterSGS(TurbDataTable setInfo,
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

        public override SqlMetaData[] GetRecordMetaData()
        {
            return new SqlMetaData[] {
                new SqlMetaData("Req", SqlDbType.Int),
                new SqlMetaData("XX", SqlDbType.Real),
                new SqlMetaData("YY", SqlDbType.Real),
                new SqlMetaData("ZZ", SqlDbType.Real),
                new SqlMetaData("XY", SqlDbType.Real),
                new SqlMetaData("XZ", SqlDbType.Real),
                new SqlMetaData("YZ", SqlDbType.Real),
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

        unsafe public double[] CalcBoxFilter(TurbulenceBlob blob, float xp, float yp, float zp, SQLUtility.MHDInputRequest input)
        {
            double[] up = new double[9]; // Result value for the user

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
                    up[3] = cachedAtomSum[3];
                    up[4] = cachedAtomSum[4];
                    up[5] = cachedAtomSum[5];
                    up[6] = cachedAtomSum[6];
                    up[7] = cachedAtomSum[7];
                    up[8] = cachedAtomSum[8];
                    return up;
                }
            }

            int off0 = startx * blob.GetComponents;

            double c = Filtering.FilteringCoefficients(filter_width);
            double [] partial_sum = new double[resultSize];

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
                                partial_sum[0] += c * fdata[off] * fdata[off];
                                partial_sum[1] += c * fdata[off + 1] * fdata[off + 1];
                                partial_sum[2] += c * fdata[off + 2] * fdata[off + 2];
                                partial_sum[3] += c * fdata[off] * fdata[off + 1];
                                partial_sum[4] += c * fdata[off] * fdata[off + 2];
                                partial_sum[5] += c * fdata[off + 1] * fdata[off + 2];
                                partial_sum[6] += c * fdata[off];
                                partial_sum[7] += c * fdata[off + 1];
                                partial_sum[8] += c * fdata[off + 2];
                                off += blob.GetComponents;
                            }
                        }
                    }
                }
            }

            if (startx == 0 && starty == 0 && startz == 0 && endx == blob.GetSide - 1 && endy == blob.GetSide - 1 && endz == blob.GetSide - 1)
            {
                cachedAtomZindex = blob.Key;
                cachedAtomSum[0] = partial_sum[0];
                cachedAtomSum[1] = partial_sum[1];
                cachedAtomSum[2] = partial_sum[2];
                cachedAtomSum[3] = partial_sum[3];
                cachedAtomSum[4] = partial_sum[4];
                cachedAtomSum[5] = partial_sum[5];
                cachedAtomSum[6] = partial_sum[6];
                cachedAtomSum[7] = partial_sum[7];
                cachedAtomSum[8] = partial_sum[8];
            }

            up[0] = partial_sum[0];
            up[1] = partial_sum[1];
            up[2] = partial_sum[2];
            up[3] = partial_sum[3];
            up[4] = partial_sum[4];
            up[5] = partial_sum[5];
            up[6] = partial_sum[6];
            up[7] = partial_sum[7];
            up[8] = partial_sum[8];

            return up;
        }
    }
}
