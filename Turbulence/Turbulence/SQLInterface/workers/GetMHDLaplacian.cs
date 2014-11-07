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
    public class GetMHDLaplacian : Worker
    {
        double[] CenteredFiniteDiffCoeff = null;
        double[] lagDenominator = null;

        public GetMHDLaplacian(TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp)
        {
            this.setInfo = setInfo;
            this.spatialInterp = spatialInterp;

            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None_Fd4)
            {
                this.kernelSize = 4;
                CenteredFiniteDiffCoeff = new double[5] { 
                    -1.0 / 12.0 / setInfo.Dx / setInfo.Dx, 4.0 / 3.0 / setInfo.Dx / setInfo.Dx, 
                    -15.0 / 6.0 / setInfo.Dx / setInfo.Dx, 
                    4.0 / 3.0 / setInfo.Dx / setInfo.Dx, -1.0 / 12.0 / setInfo.Dx / setInfo.Dx };
            }
            else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None_Fd6)
            {
                this.kernelSize = 6;
                CenteredFiniteDiffCoeff = new double[7] { 
                    1.0 / 90.0 / setInfo.Dx / setInfo.Dx, -3.0 / 20.0 / setInfo.Dx / setInfo.Dx, 3.0 / 2.0 / setInfo.Dx / setInfo.Dx, 
                    -49.0 / 18.0 / setInfo.Dx / setInfo.Dx, 
                    3.0 / 2.0 / setInfo.Dx / setInfo.Dx, -3.0 / 20.0 / setInfo.Dx / setInfo.Dx, 1.0 / 90.0 / setInfo.Dx / setInfo.Dx };
            }
            else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None_Fd8)
            {
                this.kernelSize = 8;
                CenteredFiniteDiffCoeff = new double[9] { 
                    9.0 / 3152.0 / setInfo.Dx / setInfo.Dx, -104.0 / 8865.0 / setInfo.Dx / setInfo.Dx, -207.0 / 2955.0 / setInfo.Dx / setInfo.Dx, 792.0 / 591.0 / setInfo.Dx / setInfo.Dx, 
                    -35777.0 / 14184.0 / setInfo.Dx / setInfo.Dx, 
                    792.0 / 591.0 / setInfo.Dx / setInfo.Dx, -207.0 / 2955.0 / setInfo.Dx / setInfo.Dx, -104.0 / 8865.0 / setInfo.Dx / setInfo.Dx, 9.0 / 3152.0 / setInfo.Dx / setInfo.Dx };
            }
            else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Fd4Lag4)
            {
                this.kernelSize = 8;
                CenteredFiniteDiffCoeff = new double[5] { 
                    -1.0 / 12.0 / setInfo.Dx / setInfo.Dx, 4.0 / 3.0 / setInfo.Dx / setInfo.Dx, 
                    -15.0 / 6.0 / setInfo.Dx / setInfo.Dx, 
                    4.0 / 3.0 / setInfo.Dx / setInfo.Dx, -1.0 / 12.0 / setInfo.Dx / setInfo.Dx };
                lagDenominator = new double[4];
                LagInterpolation.InterpolantDenominator(4, lagDenominator);
            }
            else
            {
                throw new Exception(String.Format("Invalid Spatial Interpolation Option: {0}", spatialInterp));
            }
        }

        public override SqlMetaData[] GetRecordMetaData()
        {
            return new SqlMetaData[] {
                new SqlMetaData("Req", SqlDbType.Int),
                new SqlMetaData("grad2ux", SqlDbType.Real),
                new SqlMetaData("grad2uy", SqlDbType.Real),
                new SqlMetaData("grad2uz", SqlDbType.Real),
                new SqlMetaData("Cubes Read", SqlDbType.Int) };
        }

        public override void GetAtomsForPoint(SQLUtility.MHDInputRequest request, long mask, int pointsPerCubeEstimate, Dictionary<long, List<int>> map, ref int total_points)
        {
            int X, Y, Z;
            int startz, starty, startx, endz, endy, endx;
            HashSet<long> atoms = new HashSet<long>(); //NOTE: HashSet requires .Net 3.5
            long zindex;

            // The following computation will be repeated 3 times
            // Once for each of the 3 spatial dimensions
            // This is necessary because the kernel of computation is a box/line 
            // with different dimensions and not a cube

            // We start of with the kernel for dudx
            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Fd4Lag4)
            {
                // The integer coordinates are computed only once
                X = LagInterpolation.CalcNode(request.x, setInfo.Dx);
                Y = LagInterpolation.CalcNode(request.y, setInfo.Dx);
                Z = LagInterpolation.CalcNode(request.z, setInfo.Dx);

                int LagIntOrder = 4;
                //x_values = new int[] { X - kernelSize / 2 + 1, X + kernelSize / 2 };    // From X-3 to X+4
                //y_values = new int[] { Y - LagIntOrder / 2 + 1, Y + LagIntOrder / 2 };  // From Y-1 to Y+2
                //z_values = new int[] { Z - LagIntOrder / 2 + 1, Z + LagIntOrder / 2 };  // From Z-1 to Z+2

                startx = X - kernelSize / 2 + 1; // From X-3 to X+4
                endx = X + kernelSize / 2;
                starty = Y - LagIntOrder / 2 + 1; // From Y-1 to Y+2
                endy = Y + LagIntOrder / 2;
                startz = Z - LagIntOrder / 2 + 1; // From Z-1 to Z+2
                endz = Z + LagIntOrder / 2;
            }
            else
            {
                // The integer coordinates are computed only once
                X = LagInterpolation.CalcNodeWithRound(request.x, setInfo.Dx);
                Y = LagInterpolation.CalcNodeWithRound(request.y, setInfo.Dx);
                Z = LagInterpolation.CalcNodeWithRound(request.z, setInfo.Dx);

                // This is the case for None_FD4, None_FD6, and None_FD8
                // for which we only need data along a line in each of the x, y, z dimensions
                // In this case we are not performing Lagrange Polynomial interpolation 

                //x_values = new int[] { X - kernelSize / 2, X + kernelSize / 2 };
                //y_values = new int[] { Y };
                //z_values = new int[] { Z }; 
                startx = X - kernelSize / 2; // From X-4 to X+4
                endx = X + kernelSize / 2;
                starty = Y; // Only Y
                endy = Y;
                startz = Z; // Only Z
                endz = Z;
            }

            // with the below logic we are going to check distinct atoms only
            // we want to start at the start of a DB atom
            startz = startz - ((startz % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;
            starty = starty - ((starty % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;
            startx = startx - ((startx % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;
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
                            if (!atoms.Contains(zindex))
                                atoms.Add(zindex);
                        }
                    }
                }
            }

            // Next we look at the kernel for dudy
            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Fd4Lag4)
            {
                int LagIntOrder = 4;
                //x_values = new int[] { X - LagIntOrder / 2 + 1, X + LagIntOrder / 2 };
                //y_values = new int[] { Y - kernelSize / 2 + 1, Y + kernelSize / 2 };
                //z_values = new int[] { Z - LagIntOrder / 2 + 1, Z + LagIntOrder / 2 };

                startx = X - LagIntOrder / 2 + 1; // From X-1 to X+2
                endx = X + LagIntOrder / 2;
                starty = Y - kernelSize / 2 + 1; // From Y-3 to Y+4
                endy = Y + kernelSize / 2;
                startz = Z - LagIntOrder / 2 + 1; // From Z-1 to Z+2
                endz = Z + LagIntOrder / 2;
            }
            else
            {
                //x_values = new int[] { X };
                //y_values = new int[] { Y - kernelSize / 2, Y + kernelSize / 2 };
                //z_values = new int[] { Z };
                startx = X; // Only X
                endx = X;
                starty = Y - kernelSize / 2; // From Y-4 to Y+4
                endy = Y + kernelSize / 2;
                startz = Z; // Only Z
                endz = Z;
            }

            // with the below logic we are going to check distinct atoms only
            // we want to start at the start of a DB atom
            startz = startz - ((startz % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;
            starty = starty - ((starty % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;
            startx = startx - ((startx % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;
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
                            if (!atoms.Contains(zindex))
                                atoms.Add(zindex);
                        }
                    }
                }
            }

            // Next we look at the kernel for dudz
            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Fd4Lag4)
            {
                int LagIntOrder = 4;
                //x_values = new int[] { X - LagIntOrder / 2 + 1, X + LagIntOrder / 2 };
                //y_values = new int[] { Y - LagIntOrder / 2 + 1, Y + LagIntOrder / 2 };
                //z_values = new int[] { Z - kernelSize / 2 + 1, Z + kernelSize / 2 };

                startx = X - LagIntOrder / 2 + 1; // From X-1 to X+2
                endx = X + LagIntOrder / 2;
                starty = Y - LagIntOrder / 2 + 1; // From Y-1 to Y+2
                endy = Y + LagIntOrder / 2;
                startz = Z - kernelSize / 2 + 1; // From Z-3 to Z+4
                endz = Z + kernelSize / 2;
            }
            else
            {
                //x_values = new int[] { X };
                //y_values = new int[] { Y };
                //z_values = new int[] { Z - kernelSize / 2, Z + kernelSize / 2 };
                startx = X; // Only X
                endx = X;
                starty = Y; // Only Y
                endy = Y;
                startz = Z - kernelSize / 2; // From Z-4 to Z+4
                endz = Z + kernelSize / 2;
            }

            // with the below logic we are going to check distinct atoms only
            // we want to start at the start of a DB atom
            startz = startz - ((startz % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;
            starty = starty - ((starty % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;
            startx = startx - ((startx % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;
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
                            if (!atoms.Contains(zindex))
                                atoms.Add(zindex);
                        }
                    }
                }
            }

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
            return CalcLaplacian(blob, xp, yp, zp, input);
        }

        public override int GetResultSize()
        {
            return 3;
        }


        /// <summary>
        /// New version of the CalcLaplacian function.
        /// </summary>
        /// <remarks>
        /// The finite difference evaluations of the unmixed second partial derivatives are carried out.
        /// These are very similar to the computation of the gradients.
        /// The only difference is in the coefficients as they have a dx^2 term in the denominator.
        /// Also, since the Laplacian is the sum of the unmixed second partial derivatives we are
        /// summing these as part of the partial-sums.
        /// </remarks>
        unsafe public double[] CalcLaplacian(TurbulenceBlob blob, float xp, float yp, float zp, SQLUtility.MHDInputRequest input)
        {
            double[] result = new double[GetResultSize()]; // Result value for the user
            for (int i = 0; i < GetResultSize(); i++)
                result[i] = 0.0;

            float[] data = blob.data;

            int x, y, z;
            int startz = 0, starty = 0, startx = 0, endz = 0, endy = 0, endx = 0;
            int KernelStartX = 0, KernelStartY = 0, KernelStartZ = 0;

            switch (spatialInterp)
            {
                #region SpatialInterpolation None_Fd4/6/8
                case TurbulenceOptions.SpatialInterpolation.None_Fd4:
                case TurbulenceOptions.SpatialInterpolation.None_Fd6:
                case TurbulenceOptions.SpatialInterpolation.None_Fd8:
                    int length = kernelSize / 2;
                    x = LagInterpolation.CalcNodeWithRound(xp, setInfo.Dx);
                    y = LagInterpolation.CalcNodeWithRound(yp, setInfo.Dx);
                    z = LagInterpolation.CalcNodeWithRound(zp, setInfo.Dx);
                    // Wrap the coordinates into the grid space
                    x = ((x % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
                    y = ((y % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
                    z = ((z % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;

                    // We are computing the following: 
                    // 4/3dx^2[f(x_(m+1, y_n)) + f(x_(m-1, y_n)) - 2f(x_m, y_n)] - 1/12dx^2[f(x_(m+2, y_n)) + f(x_(m-2, y_n)) - 2f(x_m, y_n)]

                    // Since the given blob may not hold all of the required data
                    // we determine where to start and end the partial computation
                    blob.GetSubcubeStart(z - length, y - length, x - length, ref startz, ref starty, ref startx);
                    blob.GetSubcubeEnd(z + length, y + length, x + length, ref endz, ref endy, ref endx);

                    // We also need to determine where we are starting, e.g. f(x_(n-2)), f(x_(n-1)), etc.
                    KernelStartX = blob.GetRealX - x + startx + length;
                    if (KernelStartX >= blob.GetGridResolution)
                        KernelStartX -= blob.GetGridResolution;
                    else if (KernelStartX < 0)
                        KernelStartX += blob.GetGridResolution;

                    KernelStartY = blob.GetRealY - y + starty + length;
                    if (KernelStartY >= blob.GetGridResolution)
                        KernelStartY -= blob.GetGridResolution;
                    else if (KernelStartY < 0)
                        KernelStartY += blob.GetGridResolution;

                    KernelStartZ = blob.GetRealZ - z + startz + length;
                    if (KernelStartZ >= blob.GetGridResolution)
                        KernelStartZ -= blob.GetGridResolution;
                    else if (KernelStartZ < 0)
                        KernelStartZ += blob.GetGridResolution;

                    fixed (double* FDCoeff = CenteredFiniteDiffCoeff)
                    {
                        fixed (float* fdata = data)
                        {
                            int off = 0;
                            if (y >= blob.GetBaseY && y < blob.GetBaseY + blob.GetSide
                                && z >= blob.GetBaseZ && z < blob.GetBaseZ + blob.GetSide)
                            {
                                off = startx * blob.GetComponents + (y - blob.GetBaseY) * blob.GetSide * blob.GetComponents + (z - blob.GetBaseZ) * blob.GetSide * blob.GetSide * blob.GetComponents;
                                for (int ix = startx; ix <= endx; ix++)
                                {
                                    double coeff = FDCoeff[KernelStartX + ix - startx];
                                    result[0] += coeff * fdata[off];
                                    result[1] += coeff * fdata[off + 1];
                                    result[2] += coeff * fdata[off + 2];
                                    off += blob.GetComponents;
                                }
                            }
                            if (x >= blob.GetBaseX && x < blob.GetBaseX + blob.GetSide
                                && z >= blob.GetBaseZ && z < blob.GetBaseZ + blob.GetSide)
                            {
                                off = (x - blob.GetBaseX) * blob.GetComponents + starty * blob.GetSide * blob.GetComponents + (z - blob.GetBaseZ) * blob.GetSide * blob.GetSide * blob.GetComponents;
                                for (int iy = starty; iy <= endy; iy++)
                                {
                                    double coeff = FDCoeff[KernelStartY + iy - starty];
                                    result[0] += coeff * fdata[off];
                                    result[1] += coeff * fdata[off + 1];
                                    result[2] += coeff * fdata[off + 2];
                                    off += blob.GetSide * blob.GetComponents;
                                }
                            }
                            if (x >= blob.GetBaseX && x < blob.GetBaseX + blob.GetSide
                                && y >= blob.GetBaseY && y < blob.GetBaseY + blob.GetSide)
                            {
                                off = (x - blob.GetBaseX) * blob.GetComponents + (y - blob.GetBaseY) * blob.GetSide * blob.GetComponents + startz * blob.GetSide * blob.GetSide * blob.GetComponents;
                                for (int iz = startz; iz <= endz; iz++)
                                {
                                    double coeff = FDCoeff[KernelStartZ + iz - startz];
                                    result[0] += coeff * fdata[off];
                                    result[1] += coeff * fdata[off + 1];
                                    result[2] += coeff * fdata[off + 2];
                                    off += blob.GetSide * blob.GetSide * blob.GetComponents;
                                }
                            }
                        }
                    }
                    break;
                #endregion
                case TurbulenceOptions.SpatialInterpolation.Fd4Lag4:
                    int LagIntOrder = 4;
                    int FdOrder = 4;

                    x = LagInterpolation.CalcNode(xp, setInfo.Dx);
                    y = LagInterpolation.CalcNode(yp, setInfo.Dx);
                    z = LagInterpolation.CalcNode(zp, setInfo.Dx);
                    
                    // The coefficients are computed only once and cached, so that they don't have to be 
                    // recomputed for each partial sum                    
                    if (input.lagInt == null)
                    {
                        input.lagInt = new double[LagIntOrder * 3];

                        LagInterpolation.InterpolantN(LagIntOrder, xp, x, setInfo.Dx, lagDenominator, 0, input.lagInt);
                        LagInterpolation.InterpolantN(LagIntOrder, yp, y, setInfo.Dy, lagDenominator, 1, input.lagInt);
                        LagInterpolation.InterpolantN(LagIntOrder, zp, z, setInfo.Dz, lagDenominator, 2, input.lagInt);
                    }
                
                    // Wrap the coordinates into the grid space
                    x = ((x % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
                    y = ((y % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
                    z = ((z % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;

                    // This computation has 2 stages:
                    // 4th-order finite difference evaluation of the derivative (see above)
                    // and 4th-order Lagrange Polynomial interpolation of this derivative

                    // The entire computation operates over the parts of a cube of size 8x8x8
                    // from n - 3 to n + 4 as below
                    // E.g. for the computation of duxdx (or duydx, duzdx, since here ux, uy, uz are the components of the velocity)
                    // we need the part of the cube from [2,2,0] to [5,5,7], 
                    // while for the computation of duxdy (ro duydy, duzdy)
                    // we need the part of the cube from [2,0,2] to [5,7,5], etc.

                    // Since the given blob may not hold all of the required data
                    // we determine where to start and end the partial computation
                    blob.GetSubcubeStart(z - kernelSize / 2 + 1, y - kernelSize / 2 + 1, x - kernelSize / 2 + 1, ref startz, ref starty, ref startx);
                    blob.GetSubcubeEnd(z + kernelSize / 2, y + kernelSize / 2, x + kernelSize / 2, ref endz, ref endy, ref endx);
                
                    // We also need to determine where we are starting, e.g. n - 3, n - 2, ..., n + 4
                    KernelStartX = blob.GetRealX - x + startx + 3;
                    if (KernelStartX >= blob.GetGridResolution)
                        KernelStartX -= blob.GetGridResolution;
                    else if (KernelStartX < 0)
                        KernelStartX += blob.GetGridResolution;

                    KernelStartY = blob.GetRealY - y + starty + 3;
                    if (KernelStartY >= blob.GetGridResolution)
                        KernelStartY -= blob.GetGridResolution;
                    else if (KernelStartY < 0)
                        KernelStartY += blob.GetGridResolution;

                    KernelStartZ = blob.GetRealZ - z + startz + 3;
                    if (KernelStartZ >= blob.GetGridResolution)
                        KernelStartZ -= blob.GetGridResolution;
                    else if (KernelStartZ < 0)
                        KernelStartZ += blob.GetGridResolution;

                    fixed (double* lagint = input.lagInt, FDCoeff = CenteredFiniteDiffCoeff)
                    {
                        fixed (float* fdata = data)
                        {
                            double[] ax = new double[3] { 0.0, 0.0, 0.0 }, ay = new double[3] { 0.0, 0.0, 0.0 }, az = new double[3] { 0.0, 0.0, 0.0 };
                            int off0 = startx * blob.GetComponents;
                            for (int iz = startz; iz <= endz; iz++)
                            {
                                double[] bx = new double[3] { 0.0, 0.0, 0.0 }, by = new double[3] { 0.0, 0.0, 0.0 }, bz = new double[3] { 0.0, 0.0, 0.0 };
                                int KernelIndexZ = KernelStartZ + iz - startz;
                                int off1 = off0 + iz * blob.GetSide * blob.GetSide * blob.GetComponents;
                                for (int iy = starty; iy <= endy; iy++)
                                {
                                    double[] cx = new double[3] { 0.0, 0.0, 0.0 }, cy = new double[3] { 0.0, 0.0, 0.0 }, cz = new double[3] { 0.0, 0.0, 0.0 };
                                    int KernelIndexY = KernelStartY + iy - starty;
                                    int off = off1 + iy * blob.GetSide * blob.GetComponents;
                                    for (int ix = startx; ix <= endx; ix++)
                                    {
                                        int KernelIndexX = KernelStartX + ix - startx;
                                        #region d_x u_i
                                        // first we compute d_x u_i
                                        // the Y and Z dimensions range from the 2nd to the 5th row in the cube
                                        // since for the 4th-order Lagrange Polynomial interpolation we need the 4 nearest rows in each dim.
                                        // to the target point
                                        if (KernelIndexY >= 2 && KernelIndexY <= 5 && KernelIndexZ >= 2 && KernelIndexZ <= 5)
                                        {
                                            for (int i = 0; i < LagIntOrder; i++)
                                            {
                                                // The kernel index ranges from 0 to 4 for 4th-order finite differencing
                                                if (KernelIndexX - i >= 0 && KernelIndexX - i <= FdOrder)
                                                {
                                                    double c = lagint[i] * FDCoeff[KernelIndexX - i];
                                                    cx[0] += c * fdata[off];
                                                    cx[1] += c * fdata[off + 1];
                                                    cx[2] += c * fdata[off + 2];
                                                }
                                            }
                                        }
                                        #endregion
                                        #region d_y u_i
                                        // next we compute d_y u_i
                                        // the X and Z dimensions range from the 2nd to the 5th row in the cube
                                        // since for the 4th-order Lagrange Polynomial interpolation we need 4 nearest rows in each dim.
                                        // to the target point
                                        if (KernelIndexX >= 2 && KernelIndexX <= 5 && KernelIndexZ >= 2 && KernelIndexZ <= 5)
                                        {
                                            double c = lagint[KernelIndexX - 2];
                                            cy[0] += c * fdata[off];
                                            cy[1] += c * fdata[off + 1];
                                            cy[2] += c * fdata[off + 2];
                                        }
                                        #endregion
                                        #region d_z u_i
                                        // finally we compute d_z u_i
                                        // the X and Y dimensions range from the 2nd to the 5th row in the cube
                                        // since for the 4th-order Lagrange Polynomial interpolation we need 4 nearest rows in each dim.
                                        // to the target point
                                        if (KernelIndexX >= 2 && KernelIndexX <= 5 && KernelIndexY >= 2 && KernelIndexY <= 5)
                                        {
                                            double c = lagint[KernelIndexX - 2];
                                            cz[0] += c * fdata[off];
                                            cz[1] += c * fdata[off + 1];
                                            cz[2] += c * fdata[off + 2];
                                        }
                                        #endregion
                                        off += blob.GetComponents;
                                    }
                                    if (KernelIndexY >= 2 && KernelIndexY <= 5)
                                    {
                                        double b = lagint[1 * LagIntOrder + KernelIndexY - 2];
                                        for (int j = 0; j < 3; j++)
                                        {
                                            bx[j] += cx[j] * b;
                                            bz[j] += cz[j] * b;
                                        }
                                    }
                                    #region d_y u_i
                                    for (int i = 0; i < LagIntOrder; i++)
                                    {
                                        // The kernel index ranges from 0 to 4 for 4th-order finite differencing
                                        if (KernelIndexY - i >= 0 && KernelIndexY - i <= FdOrder)
                                        {
                                            double c = lagint[1 * LagIntOrder + i] * FDCoeff[KernelIndexY - i];
                                            by[0] += c * cy[0];
                                            by[1] += c * cy[1];
                                            by[2] += c * cy[2];
                                        }
                                    }
                                    #endregion
                                }
                                if (KernelIndexZ >= 2 && KernelIndexZ <= 5)
                                {
                                    double a = lagint[2 * LagIntOrder + KernelIndexZ - 2];
                                    for (int j = 0; j < 3; j++)
                                    {
                                        ax[j] += bx[j] * a;
                                        ay[j] += by[j] * a;
                                    }
                                }
                                #region d_z u_i
                                for (int i = 0; i < LagIntOrder; i++)
                                {
                                    // The kernel index ranges from 0 to 4 for 4th-order finite differencing
                                    // NOTE: nOrder is the Lagrange polynomial interpolation order
                                    if (KernelIndexZ - i >= 0 && KernelIndexZ - i <= FdOrder)
                                    {
                                        double c = lagint[2 * LagIntOrder + i] * FDCoeff[KernelIndexZ - i];
                                        az[0] += c * bz[0];
                                        az[1] += c * bz[1];
                                        az[2] += c * bz[2];
                                    }
                                }
                                #endregion
                            }
                            result[0] = ax[0] + ay[0] + az[0];
                            result[1] = ax[1] + ay[1] + az[1];
                            result[2] = ax[2] + ay[2] + az[2];
                        }
                    }

                    break;
                default:
                    throw new Exception("Invalid Spatial Interpolation Option");
            }
            
            return result;
        }

    }

}
