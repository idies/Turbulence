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
    public class GetMHDBoxFilterGradient : Worker
    {
        int filter_width;
        int dx;
        int overlap;

        private int resultSize = 9;
        //private float[] cachedAtomSum = new float[3];
        //private long cachedAtomZindex;

        public GetMHDBoxFilterGradient(TurbDataTable setInfo,
            int dx,
            float filterwidth)
        {
            this.setInfo = setInfo;
            this.dx = dx;
            int fw = (int)Math.Round(filterwidth / setInfo.Dx);
            this.filter_width = fw;
            int overlap = filter_width - 2 * dx;
            this.overlap = overlap > 0 ? overlap : 0;
            this.kernelSize = filter_width + dx;
            //this.cachedAtomZindex = -1;
        }

        public override SqlMetaData[] GetRecordMetaData()
        {
            return new SqlMetaData[] {
                new SqlMetaData("Req", SqlDbType.Int),
                new SqlMetaData("duxdx", SqlDbType.Real),
                new SqlMetaData("duydx", SqlDbType.Real),
                new SqlMetaData("duzdx", SqlDbType.Real),
                new SqlMetaData("duxdy", SqlDbType.Real),
                new SqlMetaData("duydy", SqlDbType.Real),
                new SqlMetaData("duzdy", SqlDbType.Real),
                new SqlMetaData("duxdz", SqlDbType.Real),
                new SqlMetaData("duydz", SqlDbType.Real),
                new SqlMetaData("duzdz", SqlDbType.Real),
                new SqlMetaData("Cubes Read", SqlDbType.Int)};
        }

        public override void GetAtomsForPoint(SQLUtility.MHDInputRequest request, long mask, int pointsPerCubeEstimate, Dictionary<long, List<int>> map, ref int total_points)
        {
            int X, Y, Z;
            int startz, starty, startx, endz, endy, endx;
            HashSet<long> atoms = new HashSet<long>(); //NOTE: HashSet requires .Net 3.5
            
            X = LagInterpolation.CalcNodeWithRound(request.x, setInfo.Dx);
            Y = LagInterpolation.CalcNodeWithRound(request.y, setInfo.Dx);
            Z = LagInterpolation.CalcNodeWithRound(request.z, setInfo.Dx);

            // The following computation will be repeated 3 times
            // Once for each of the 3 spatial dimensions
            // This is necessary because the kernel of computation is a box/line 
            // with different dimensions and not a cube

            // We start of with the kernel for dudx
            // This is the case for 2nd order finite difference
            // for which we only need data along a line in each of the x, y, z dimensions (2 points)
            startx  = X - dx - filter_width / 2;
            endx    = X - dx + filter_width / 2 - overlap;
            starty  = Y - filter_width / 2;
            endy    = Y + filter_width / 2;
            startz  = Z - filter_width / 2;
            endz    = Z + filter_width / 2;
            AddAtoms(startz, starty, startx, endz, endy, endx, atoms, mask);

            startx  = X + dx - filter_width / 2 + overlap;
            endx    = X + dx + filter_width / 2;
            starty  = Y - filter_width / 2;
            endy    = Y + filter_width / 2;
            startz  = Z - filter_width / 2;
            endz    = Z + filter_width / 2;
            AddAtoms(startz, starty, startx, endz, endy, endx, atoms, mask);

            // Next we look at the kernel for dudy
            startx  = X - filter_width / 2;
            endx    = X + filter_width / 2;
            starty  = Y - dx - filter_width / 2;
            endy    = Y - dx + filter_width / 2 - overlap;
            startz  = Z - filter_width / 2;
            endz    = Z + filter_width / 2;
            AddAtoms(startz, starty, startx, endz, endy, endx, atoms, mask);

            startx  = X - filter_width / 2;
            endx    = X + filter_width / 2;
            starty  = Y + dx - filter_width / 2 + overlap;
            endy    = Y + dx + filter_width / 2;
            startz  = Z - filter_width / 2;
            endz    = Z + filter_width / 2;
            AddAtoms(startz, starty, startx, endz, endy, endx, atoms, mask);

            // Next we look at the kernel for dudz
            startx  = X - filter_width / 2;
            endx    = X + filter_width / 2;
            starty  = Y - filter_width / 2;
            endy    = Y + filter_width / 2;
            startz  = Z - dx - filter_width / 2;
            endz    = Z - dx + filter_width / 2 - overlap;
            AddAtoms(startz, starty, startx, endz, endy, endx, atoms, mask);

            startx  = X - filter_width / 2;
            endx    = X + filter_width / 2;
            starty  = Y - filter_width / 2;
            endy    = Y + filter_width / 2;
            startz  = Z + dx - filter_width / 2 + overlap;
            endz    = Z + dx + filter_width / 2;
            AddAtoms(startz, starty, startx, endz, endy, endx, atoms, mask);

            foreach (long atom in atoms)
            {
                if (!map.ContainsKey(atom))
                {
                    //map[atom] = new List<int>(pointsPerCubeEstimate);
                    map[atom] = new List<int>();
                }
                //Debug.Assert(!map[zindex].Contains(request.request));
                map[atom].Add(request.request);
                request.numberOfCubes++;
                total_points++;
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
        /// Computes a box filter of the gradient of a vector field at a target location
        /// </summary>
        /// <remarks>
        /// Similar to GetMHDWorker
        /// </remarks>
        unsafe public double[] CalcBoxFilter(TurbulenceBlob blob, float xp, float yp, float zp, SQLUtility.MHDInputRequest input)
        {
            double[] up = new double[resultSize]; // Result value for the user

            double[] result = new double[resultSize]; // Result value for computations
            for (int i = 0; i < GetResultSize(); i++)
                result[i] = 0.0;

            int x = LagInterpolation.CalcNodeWithRound(xp, setInfo.Dx);
            int y = LagInterpolation.CalcNodeWithRound(yp, setInfo.Dx);
            int z = LagInterpolation.CalcNodeWithRound(zp, setInfo.Dx);

            // Wrap the coordinates into the grid space
            x = ((x % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
            y = ((y % setInfo.GridResolutionY) + setInfo.GridResolutionY) % setInfo.GridResolutionY;
            z = ((z % setInfo.GridResolutionZ) + setInfo.GridResolutionZ) % setInfo.GridResolutionZ;

            float[] data = blob.data;
            int startz = 0, starty = 0, startx = 0, endz = 0, endy = 0, endx = 0;
            blob.GetSubcubeStart(z - filter_width / 2 - dx, y - filter_width / 2 - dx, x - filter_width / 2 - dx, ref startz, ref starty, ref startx);
            blob.GetSubcubeEnd(z + filter_width / 2 + dx, y + filter_width / 2 + dx, x + filter_width / 2 + dx, ref endz, ref endy, ref endx);

            //if (startx == 0 && starty == 0 && startz == 0 && endx == blob.GetSide - 1 && endy == blob.GetSide - 1 && endz == blob.GetSide - 1)
            //{
            //    if (cachedAtomZindex == blob.Key)
            //    {
            //        up[0] = (float)cachedAtomSum[0];
            //        up[1] = (float)cachedAtomSum[1];
            //        up[2] = (float)cachedAtomSum[2];
            //        return up;
            //    }
            //}
            
            // We need to determine on which side of the target location the data being processed is
            // in order to use the correct coefficient in the FD computaion
            int xLocation = blob.GetRealX + startx;
            if (xLocation > x + filter_width / 2 + dx)
                xLocation -= setInfo.GridResolutionX;
            else if (xLocation < x - filter_width / 2 - dx)
                xLocation += setInfo.GridResolutionX;

            int yLocation = blob.GetRealY + starty;
            if (yLocation > y + filter_width / 2 + dx)
                yLocation -= setInfo.GridResolutionY;
            else if (yLocation < y - filter_width / 2 - dx)
                yLocation += setInfo.GridResolutionY;

            int zLocation = blob.GetRealZ + startz;
            if (zLocation > z + filter_width / 2 + dx)
                zLocation -= setInfo.GridResolutionZ;
            else if (zLocation < z - filter_width / 2 - dx)
                zLocation += setInfo.GridResolutionZ;

            int off0 = startx * blob.GetComponents;

            double FilterCoeff = Filtering.FilteringCoefficients(filter_width);
            double FDCoeff = 1.0 / 2.0 / (dx * setInfo.Dx);

            fixed (double* lagint = input.lagInt)
            {
                fixed (float* fdata = data)
                {
                    for (int iz = startz; iz <= endz; iz++)
                    {
                        int off1 = off0 + iz * blob.GetSide * blob.GetSide * blob.GetComponents;
                        int currentZLocation = zLocation + iz - startz;
                        for (int iy = starty; iy <= endy; iy++)
                        {
                            int off = off1 + iy * blob.GetSide * blob.GetComponents;
                            int currentYLocation = yLocation + iy - starty;
                            for (int ix = startx; ix <= endx; ix++)
                            {
                                int currentXLocation = xLocation + ix - startx;
                                if (y - filter_width / 2 <= currentYLocation && currentYLocation <= y + filter_width / 2
                                     && z - filter_width / 2 <= currentZLocation && currentZLocation <= z + filter_width / 2)
                                {
                                    if (x - dx - filter_width / 2 <= currentXLocation && currentXLocation <= x - dx + filter_width / 2 - overlap)
                                    {
                                        result[0] -= FilterCoeff * FDCoeff * fdata[off];
                                        if (setInfo.Components > 1)
                                        {
                                            result[3] -= FilterCoeff * FDCoeff * fdata[off + 1];
                                            result[6] -= FilterCoeff * FDCoeff * fdata[off + 2];
                                        }
                                    }
                                    if (x + dx - filter_width / 2 + overlap <= currentXLocation && currentXLocation <= x + dx + filter_width / 2)
                                    {
                                        result[0] += FilterCoeff * FDCoeff * fdata[off];
                                        if (setInfo.Components > 1)
                                        {
                                            result[3] += FilterCoeff * FDCoeff * fdata[off + 1];
                                            result[6] += FilterCoeff * FDCoeff * fdata[off + 2];
                                        }
                                    }
                                }

                                if (x - filter_width / 2 <= currentXLocation && currentXLocation <= x + filter_width / 2
                                     && z - filter_width / 2 <= currentZLocation && currentZLocation <= z + filter_width / 2)
                                {
                                    if (y - dx - filter_width / 2 <= currentYLocation && currentYLocation <= y - dx + filter_width / 2 - overlap)
                                    {
                                        result[1] -= FilterCoeff * FDCoeff * fdata[off];
                                        if (setInfo.Components > 1)
                                        {
                                            result[4] -= FilterCoeff * FDCoeff * fdata[off + 1];
                                            result[7] -= FilterCoeff * FDCoeff * fdata[off + 2];
                                        }
                                    }
                                    if (y + dx - filter_width / 2 + overlap <= currentYLocation && currentYLocation <= y + dx + filter_width / 2)
                                    {
                                        result[1] += FilterCoeff * FDCoeff * fdata[off];
                                        if (setInfo.Components > 1)
                                        {
                                            result[4] += FilterCoeff * FDCoeff * fdata[off + 1];
                                            result[7] += FilterCoeff * FDCoeff * fdata[off + 2];
                                        }
                                    }
                                }

                                if (y - filter_width / 2 <= currentYLocation && currentYLocation <= y + filter_width / 2
                                     && x - filter_width / 2 <= currentXLocation && currentXLocation <= x + filter_width / 2)
                                {
                                    if (z - dx - filter_width / 2 <= currentZLocation && currentZLocation <= z - dx + filter_width / 2 - overlap)
                                    {
                                        result[2] -= FilterCoeff * FDCoeff * fdata[off];
                                        if (setInfo.Components > 1)
                                        {
                                            result[5] -= FilterCoeff * FDCoeff * fdata[off + 1];
                                            result[8] -= FilterCoeff * FDCoeff * fdata[off + 2];
                                        }
                                    }
                                    if (z + dx - filter_width / 2 + overlap <= currentZLocation && currentZLocation <= z + dx + filter_width / 2)
                                    {
                                        result[2] += FilterCoeff * FDCoeff * fdata[off];
                                        if (setInfo.Components > 1)
                                        {
                                            result[5] += FilterCoeff * FDCoeff * fdata[off + 1];
                                            result[8] += FilterCoeff * FDCoeff * fdata[off + 2];
                                        }
                                    }
                                }
                                off += blob.GetComponents;
                            }
                        }
                    }
                }
            }

            //if (startx == 0 && starty == 0 && startz == 0 && endx == blob.GetSide - 1 && endy == blob.GetSide - 1 && endz == blob.GetSide - 1)
            //{
            //    cachedAtomZindex = blob.Key;
            //    cachedAtomSum[0] = (float)c1;
            //    cachedAtomSum[1] = (float)c2;
            //    cachedAtomSum[2] = (float)c3;
            //}

            for (int i = 0; i < resultSize; i++)
                up[i] = result[i];

            return up;
        }

    }

}
