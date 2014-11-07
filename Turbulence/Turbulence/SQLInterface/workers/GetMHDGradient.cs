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
    public class GetMHDGradient : Worker
    {
        protected double[] CenteredFiniteDiffCoeff = null;
        protected double[] lagDenominator = null;
        protected int numPointsInKernel = 0;

        public GetMHDGradient(TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp)
        {
            this.setInfo = setInfo;
            this.spatialInterp = spatialInterp;

            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None_Fd4)
            {
                this.kernelSize = 4;
                CenteredFiniteDiffCoeff = new double[5] { 
                    1.0 / 12.0 / setInfo.Dx, -2.0 / 3.0 / setInfo.Dx, 
                    0.0, 
                    2.0 / 3.0 / setInfo.Dx, -1.0 / 12.0 / setInfo.Dx };
                this.numPointsInKernel = 3 * kernelSize;
            }
            else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None_Fd6)
            {
                this.kernelSize = 6;
                CenteredFiniteDiffCoeff = new double[7] { 
                    -1.0 / 60.0 / setInfo.Dx, 3.0 / 20.0 / setInfo.Dx, -3.0 / 4.0 / setInfo.Dx, 
                    0, 
                    3.0 / 4.0 / setInfo.Dx, -3.0 / 20.0 / setInfo.Dx, 1.0 / 60.0 / setInfo.Dx };
                this.numPointsInKernel = 3 * kernelSize;
            }
            else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None_Fd8)
            {
                this.kernelSize = 8;
                CenteredFiniteDiffCoeff = new double[9] { 
                    1.0 / 280.0 / setInfo.Dx, -4.0 / 105.0 / setInfo.Dx, 1.0 / 5.0 / setInfo.Dx, -4.0 / 5.0 / setInfo.Dx, 
                    0, 
                    4.0 / 5.0 / setInfo.Dx, -1.0 / 5.0 / setInfo.Dx, 4.0 / 105.0 / setInfo.Dx, -1.0 / 280.0 / setInfo.Dx };
                this.numPointsInKernel = 3 * kernelSize;
            }
            else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Fd4Lag4)
            {
                // kernel size is the size of the FD kernel + the size of the Lagrange interpolation kernel  = 4 + 4
                this.kernelSize = 8;
                CenteredFiniteDiffCoeff = new double[5] { 
                    1.0 / 12.0 / setInfo.Dx, -2.0 / 3.0 / setInfo.Dx, 
                    0, 
                    2.0 / 3.0 / setInfo.Dx, -1.0 / 12.0 / setInfo.Dx };
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
                new SqlMetaData("duxdx", SqlDbType.Real),
                new SqlMetaData("duxdy", SqlDbType.Real),
                new SqlMetaData("duxdz", SqlDbType.Real),
                new SqlMetaData("duydx", SqlDbType.Real),
                new SqlMetaData("duydy", SqlDbType.Real),
                new SqlMetaData("duydz", SqlDbType.Real),
                new SqlMetaData("duzdx", SqlDbType.Real),
                new SqlMetaData("duzdy", SqlDbType.Real),
                new SqlMetaData("duzdz", SqlDbType.Real) };
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
            return CalcGradient(blob, input);
        }

        public override int GetResultSize()
        {
            return 3 * setInfo.Components;
        }

        /// <summary>
        /// New version of the CalcGradient function.
        /// </summary>
        /// <remarks>
        /// The Lagrangian evaluation function [LagInterpolation.EvaluateOpt] was moved
        /// into the function and some loop unrolling was performed.
        /// </remarks>
        unsafe public double[] CalcGradient(TurbulenceBlob blob, SQLUtility.MHDInputRequest input)
        {
            // Result value for the user
            double[] result = new double[3 * setInfo.Components];
            // Temp variables for the partial computations
            double[] ax = new double[setInfo.Components], ay = new double[setInfo.Components], az = new double[setInfo.Components];

            float[] data = blob.data;

            int length = 0;

            int x, y, z;
            int startz = 0, starty = 0, startx = 0, endz = 0, endy = 0, endx = 0;
            int iLagIntx = 0, iLagInty = 0, iLagIntz = 0;

            switch (spatialInterp)
            {
                #region SpatialInterpolation None_Fd4/6/8
                case TurbulenceOptions.SpatialInterpolation.None_Fd4:
                case TurbulenceOptions.SpatialInterpolation.None_Fd6:
                case TurbulenceOptions.SpatialInterpolation.None_Fd8:
                    length = kernelSize / 2;
                    x = LagInterpolation.CalcNodeWithRound(input.x, setInfo.Dx);
                    y = LagInterpolation.CalcNodeWithRound(input.y, setInfo.Dx);
                    z = LagInterpolation.CalcNodeWithRound(input.z, setInfo.Dx);
                    // Wrap the coordinates into the grid space
                    x = ((x % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
                    y = ((y % setInfo.GridResolutionY) + setInfo.GridResolutionY) % setInfo.GridResolutionY;
                    z = ((z % setInfo.GridResolutionZ) + setInfo.GridResolutionZ) % setInfo.GridResolutionZ;

                    // We are computing the following: 
                    // 2/3dx[f(x_(n+1)) - f(x_(n-1))] - 1/12dx[f(x_(n+2)) - f(x_(n-2))]

                    // Since the given blob may not hold all of the required data
                    // we determine where to start and end the partial computation
                    blob.GetSubcubeStart(z - length, y - length, x - length, ref startz, ref starty, ref startx);
                    blob.GetSubcubeEnd(z + length, y + length, x + length, ref endz, ref endy, ref endx);

                    // We also need to determine where we are starting, e.g. f(x_(n-2)), f(x_(n-1)), etc.
                    iLagIntx = blob.GetRealX - x + startx + length;
                    if (iLagIntx >= blob.GetGridResolution)
                        iLagIntx -= blob.GetGridResolution;
                    else if (iLagIntx < 0)
                        iLagIntx += blob.GetGridResolution;

                    iLagInty = blob.GetRealY - y + starty + length;
                    if (iLagInty >= blob.GetGridResolution)
                        iLagInty -= blob.GetGridResolution;
                    else if (iLagInty < 0)
                        iLagInty += blob.GetGridResolution;

                    iLagIntz = blob.GetRealZ - z + startz + length;
                    if (iLagIntz >= blob.GetGridResolution)
                        iLagIntz -= blob.GetGridResolution;
                    else if (iLagIntz < 0)
                        iLagIntz += blob.GetGridResolution;

                    fixed (double* FDCoeff = CenteredFiniteDiffCoeff)
                    {
                        fixed (float* fdata = data)
                        {
                            int off = 0;
                            if (y >= blob.GetBaseY && y < blob.GetBaseY + blob.GetSide
                                && z >= blob.GetBaseZ && z < blob.GetBaseZ + blob.GetSide)
                            {
                                off = startx * setInfo.Components + (y - blob.GetBaseY) * blob.GetSide * setInfo.Components + (z - blob.GetBaseZ) * blob.GetSide * blob.GetSide * setInfo.Components;
                                for (int ix = startx; ix <= endx; ix++)
                                {
                                    double coeff = FDCoeff[iLagIntx + ix - startx];
                                    for (int j = 0; j < setInfo.Components; j++)
                                    {
                                        ax[j] += coeff * fdata[off + j];
                                    }
                                    off += setInfo.Components;
                                }
                            }
                            if (x >= blob.GetBaseX && x < blob.GetBaseX + blob.GetSide
                                && z >= blob.GetBaseZ && z < blob.GetBaseZ + blob.GetSide)
                            {
                                off = (x - blob.GetBaseX) * setInfo.Components + starty * blob.GetSide * setInfo.Components + (z - blob.GetBaseZ) * blob.GetSide * blob.GetSide * setInfo.Components;
                                for (int iy = starty; iy <= endy; iy++)
                                {
                                    double coeff = FDCoeff[iLagInty + iy - starty];
                                    for (int j = 0; j < setInfo.Components; j++)
                                    {
                                        ay[j] += coeff * fdata[off + j];
                                    }
                                    off += blob.GetSide * setInfo.Components;
                                }
                            }
                            if (x >= blob.GetBaseX && x < blob.GetBaseX + blob.GetSide
                                && y >= blob.GetBaseY && y < blob.GetBaseY + blob.GetSide)
                            {
                                off = (x - blob.GetBaseX) * setInfo.Components + (y - blob.GetBaseY) * blob.GetSide * setInfo.Components + startz * blob.GetSide * blob.GetSide * setInfo.Components;
                                for (int iz = startz; iz <= endz; iz++)
                                {
                                    double coeff = FDCoeff[iLagIntz + iz - startz];
                                    for (int j = 0; j < setInfo.Components; j++)
                                    {
                                        az[j] += coeff * fdata[off + j];
                                    }
                                    off += blob.GetSide * blob.GetSide * setInfo.Components;
                                }
                            }
                        }
                    }
                    break;
                #endregion
                case TurbulenceOptions.SpatialInterpolation.Fd4Lag4:
                    int LagIntOrder = 4;
                    int FdOrder = 4;

                    x = LagInterpolation.CalcNode(input.x, setInfo.Dx);
                    y = LagInterpolation.CalcNode(input.y, setInfo.Dx);
                    z = LagInterpolation.CalcNode(input.z, setInfo.Dx);
                    
                    // The coefficients are computed only once and cached, so that they don't have to be 
                    // recomputed for each partial sum                    
                    if (input.lagInt == null)
                    {
                        input.lagInt = new double[LagIntOrder * 3];

                        LagInterpolation.InterpolantN(LagIntOrder, input.x, x, setInfo.Dx, lagDenominator, 0, input.lagInt);
                        LagInterpolation.InterpolantN(LagIntOrder, input.y, y, setInfo.Dy, lagDenominator, 1, input.lagInt);
                        LagInterpolation.InterpolantN(LagIntOrder, input.z, z, setInfo.Dz, lagDenominator, 2, input.lagInt);
                    }
                
                    // Wrap the coordinates into the grid space
                    x = ((x % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
                    y = ((y % setInfo.GridResolutionY) + setInfo.GridResolutionY) % setInfo.GridResolutionY;
                    z = ((z % setInfo.GridResolutionZ) + setInfo.GridResolutionZ) % setInfo.GridResolutionZ;

                    // This computation has 2 stages:
                    // 4th-order finite difference evaluation of the derivative (see above)
                    // and 4th-order Lagrange Polynomial interpolation of this derivative

                    // The entire computation operates over the parts of a cube of size 8x8x8
                    // from n - 3 to n + 4 as below
                    // E.g. for the computation of duxdx (or duydx, duzdx, since here ux, uy, uz are the components of the velocity)
                    // we need the part of the cube from [2,2,0] to [5,5,7], 
                    // while for the computation of duxdy (or duydy, duzdy)
                    // we need the part of the cube from [2,0,2] to [5,7,5], etc.

                    // Since the given blob may not hold all of the required data
                    // we determine where to start and end the partial computation
                    blob.GetSubcubeStart(z - kernelSize / 2 + 1, y - kernelSize / 2 + 1, x - kernelSize / 2 + 1, ref startz, ref starty, ref startx);
                    blob.GetSubcubeEnd(z + kernelSize / 2, y + kernelSize / 2, x + kernelSize / 2, ref endz, ref endy, ref endx);
                
                    // We also need to determine where we are starting, e.g. n - 3, n - 2, ..., n + 4
                    iLagIntx = blob.GetRealX - x + startx + 3;
                    if (iLagIntx >= blob.GetGridResolution)
                        iLagIntx -= blob.GetGridResolution;
                    else if (iLagIntx < 0)
                        iLagIntx += blob.GetGridResolution;

                    iLagInty = blob.GetRealY - y + starty + 3;
                    if (iLagInty >= blob.GetGridResolution)
                        iLagInty -= blob.GetGridResolution;
                    else if (iLagInty < 0)
                        iLagInty += blob.GetGridResolution;

                    iLagIntz = blob.GetRealZ - z + startz + 3;
                    if (iLagIntz >= blob.GetGridResolution)
                        iLagIntz -= blob.GetGridResolution;
                    else if (iLagIntz < 0)
                        iLagIntz += blob.GetGridResolution;

                    fixed (double* lagint = input.lagInt, FDCoeff = CenteredFiniteDiffCoeff)
                    {
                        fixed (float* fdata = data)
                        {
                            int off0 = startx * setInfo.Components;
                            for (int iz = startz; iz <= endz; iz++)
                            {
                                double[] bx = new double[setInfo.Components], by = new double[setInfo.Components], bz = new double[setInfo.Components];
                                int LagIntIndexZ = iLagIntz + iz - startz;
                                int off1 = off0 + iz * blob.GetSide * blob.GetSide * setInfo.Components;
                                for (int iy = starty; iy <= endy; iy++)
                                {
                                    double[] cx = new double[setInfo.Components], cy = new double[setInfo.Components], cz = new double[setInfo.Components];
                                    int LagIntIndexY = iLagInty + iy - starty;
                                    int off = off1 + iy * blob.GetSide * setInfo.Components;
                                    for (int ix = startx; ix <= endx; ix++)
                                    {
                                        int LagIntIndexX = iLagIntx + ix - startx;
                                        #region d_x u_i
                                        // first we compute d_x u_i
                                        // the Y and Z dimensions range from the 2nd to the 5th row in the cube
                                        // since for the 4th-order Lagrange Polynomial interpolation we need the 4 nearest rows in each dim.
                                        // to the target point
                                        if (LagIntIndexY >= 2 && LagIntIndexY <= 5 && LagIntIndexZ >= 2 && LagIntIndexZ <= 5)
                                        {
                                            for (int i = 0; i < LagIntOrder; i++)
                                            {
                                                // The kernel index ranges from 0 to 4 for 4th-order finite differencing
                                                if (LagIntIndexX - i >= 0 && LagIntIndexX - i <= FdOrder)
                                                {
                                                    double c = lagint[i] * FDCoeff[LagIntIndexX - i];
                                                    for (int j = 0; j < setInfo.Components; j++)
                                                    {
                                                        cx[j] += c * fdata[off + j];
                                                    }
                                                }
                                            }
                                        }
                                        #endregion
                                        #region d_y u_i
                                        // next we compute d_y u_i
                                        // the X and Z dimensions range from the 2nd to the 5th row in the cube
                                        // since for the 4th-order Lagrange Polynomial interpolation we need 4 nearest rows in each dim.
                                        // to the target point
                                        if (LagIntIndexX >= 2 && LagIntIndexX <= 5 && LagIntIndexZ >= 2 && LagIntIndexZ <= 5)
                                        {
                                            double c = lagint[LagIntIndexX - 2];
                                            for (int j = 0; j < setInfo.Components; j++)
                                            {
                                                cy[j] += c * fdata[off + j];
                                            }
                                        }
                                        #endregion
                                        #region d_z u_i
                                        // finally we compute d_z u_i
                                        // the X and Y dimensions range from the 2nd to the 5th row in the cube
                                        // since for the 4th-order Lagrange Polynomial interpolation we need 4 nearest rows in each dim.
                                        // to the target point
                                        if (LagIntIndexX >= 2 && LagIntIndexX <= 5 && LagIntIndexY >= 2 && LagIntIndexY <= 5)
                                        {
                                            double c = lagint[LagIntIndexX - 2];
                                            for (int j = 0; j < setInfo.Components; j++)
                                            {
                                                cz[j] += c * fdata[off + j];
                                            }
                                        }
                                        #endregion
                                        off += setInfo.Components;
                                    }
                                    if (LagIntIndexY >= 2 && LagIntIndexY <= 5)
                                    {
                                        double b = lagint[1 * LagIntOrder + LagIntIndexY - 2];
                                        for (int j = 0; j < setInfo.Components; j++)
                                        {
                                            bx[j] += cx[j] * b;
                                            bz[j] += cz[j] * b;
                                        }
                                    }
                                    #region d_y u_i
                                    for (int i = 0; i < LagIntOrder; i++)
                                    {
                                        // The kernel index ranges from 0 to 4 for 4th-order finite differencing
                                        if (LagIntIndexY - i >= 0 && LagIntIndexY - i <= FdOrder)
                                        {
                                            double c = lagint[1 * LagIntOrder + i] * FDCoeff[LagIntIndexY - i];
                                            for (int j = 0; j < setInfo.Components; j++)
                                            {
                                                by[j] += c * cy[j];
                                            }
                                        }
                                    }
                                    #endregion
                                }
                                if (LagIntIndexZ >= 2 && LagIntIndexZ <= 5)
                                {
                                    double a = lagint[2 * LagIntOrder + LagIntIndexZ - 2];
                                    for (int j = 0; j < setInfo.Components; j++)
                                    {
                                        ax[j] += bx[j] * a;
                                        ay[j] += by[j] * a;
                                    }
                                }
                                #region d_z u_i
                                for (int i = 0; i < LagIntOrder; i++)
                                {
                                    // The kernel index ranges from 0 to 4 for 4th-order finite differencing
                                    if (LagIntIndexZ - i >= 0 && LagIntIndexZ - i <= FdOrder)
                                    {
                                        double c = lagint[2 * LagIntOrder + i] * FDCoeff[LagIntIndexZ - i];
                                        for (int j = 0; j < setInfo.Components; j++)
                                        {
                                            az[j] += c * bz[j];
                                        }
                                    }
                                }
                                #endregion
                            }
                        }
                    }

                    break;
                default:
                    throw new Exception("Invalid Spatial Interpolation Option");
            }

            for (int j = 0; j < setInfo.Components; j++)
            {
                result[0 + 3 * j] = ax[j];
                result[1 + 3 * j] = ay[j];
                result[2 + 3 * j] = az[j];
            }
            return result;
        }

        protected void GetKernelStartEnd(int z, int y, int x, int[] cutout_coordinates,
            ref long zindex,
            ref int startz, ref int endz, ref int starty, ref int endy, ref int startx, ref int endx)
        {
            zindex = new Morton3D(z, y, x);
            // For each target point determine its kernel and perform the computation.
            if (z - kernelSize / 2 < cutout_coordinates[2])
            {
                // In this case we don't have all of the data for the entire computation
                throw new Exception("The given cutout does not have all of the data for the computation of the curl of the entire region!");
            }
            else
            {
                // In this case we have the data at the start of the kernel
                startz = z - kernelSize / 2;
                if (z + kernelSize / 2 > cutout_coordinates[5])
                {
                    // In this case we don't have all of the data for the entire computation
                    throw new Exception("The given cutout does not have all of the data for the computation of the curl of the entire region!");
                }
                else
                {
                    // In this case we have the data at the end of the kernel
                    endz = z + kernelSize / 2;
                }
            }

            if (y - kernelSize / 2 < cutout_coordinates[1])
            {
                // In this case we don't have all of the data for the entire computation
                throw new Exception("The given cutout does not have all of the data for the computation of the curl of the entire region!");
            }
            else
            {
                // In this case we have the data at the start of the kernel
                starty = y - kernelSize / 2;
                if (y + kernelSize / 2 > cutout_coordinates[4])
                {
                    // In this case we don't have all of the data for the entire computation
                    throw new Exception("The given cutout does not have all of the data for the computation of the curl of the entire region!");
                }
                else
                {
                    // In this case we have the data at the end of the kernel
                    endy = y + kernelSize / 2;
                }
            }

            if (x - kernelSize / 2 < cutout_coordinates[0])
            {
                // In this case we don't have all of the data for the entire computation
                throw new Exception("The given cutout does not have all of the data for the computation of the curl of the entire region!");
            }
            else
            {
                // In this case we have the data at the start of the kernel
                startx = x - kernelSize / 2;
                if (x + kernelSize / 2 > cutout_coordinates[3])
                {
                    // In this case we don't have all of the data for the entire computation
                    throw new Exception("The given cutout does not have all of the data for the computation of the curl of the entire region!");
                }
                else
                {
                    // In this case we have the data at the end of the kernel
                    endx = x + kernelSize / 2;
                }
            }
        }

        /// <summary>
        /// For each point on the grid specified by the coordinates parameter computes the curl using the given data cutout.
        /// Each point that has a norm higher than the given threshold is stored in the set and the set is returned.
        /// NOTE: Values are not interpolated as the target locations are on grid nodes.
        /// </summary>
        /// <param name="cutout"></param>
        /// <param name="cutout_coordiantes"></param>
        /// <param name="coordiantes"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        public override HashSet<SQLUtility.PartialResult> GetThresholdUsingCutout(BigArray<float> cutout, int[] cutout_coordinates, int[] coordiantes, double threshold)
        {
            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Fd4Lag4)
            {
                throw new Exception("Invalid interpolation option specified!");
            }
            HashSet<SQLUtility.PartialResult> points_above_threshold = new HashSet<SQLUtility.PartialResult>();
            SQLUtility.PartialResult point;
            int[] cutout_dimensions = new int[] { cutout_coordinates[5] - cutout_coordinates[2],
                                                  cutout_coordinates[4] - cutout_coordinates[1],
                                                  cutout_coordinates[3] - cutout_coordinates[0] };
            int startz = 0, endz = 0, starty = 0, endy = 0, startx = 0, endx = 0, offset_y = 0;
            long zindex = 0;
            for (int z = coordiantes[2]; z < coordiantes[5]; z++)
            {
                for (int y = coordiantes[1]; y < coordiantes[4]; y++)
                {
                    for (int x = coordiantes[0]; x < coordiantes[3]; x++)
                    {
                        GetKernelStartEnd(z, y, x, cutout_coordinates, ref zindex, ref startz, ref endz, ref starty, ref endy, ref startx, ref endx);

                        point = new SQLUtility.PartialResult(zindex, GetResultSize(), numPointsInKernel);
                        GetResultUsingCutout(ref point, cutout, x, y, z, startx, starty, startz, endx, endy, endz, cutout_coordinates, cutout_dimensions, offset_y);

                        // Compute the norm.
                        double norm = 0.0f;
                        for (int i = 0; i < GetResultSize(); i++)
                        {
                            norm += point.result[i] * point.result[i];
                        }
                        norm = Math.Sqrt(norm);
                        point.norm = norm;
                        if (norm > threshold)
                        {
                            points_above_threshold.Add(point);
                            if (points_above_threshold.Count > MAX_NUMBER_THRESHOLD_POINTS)
                            {
                                throw new Exception(String.Format("The number of points above the threshold exeeds max allowed number: {0}!", MAX_NUMBER_THRESHOLD_POINTS));
                            }
                        }
                    }
                }
            }

            return points_above_threshold;
        }

        /// <summary>
        /// For each point on the grid specified by the coordinates parameter computes the curl using the given data cutout.
        /// Each point that has a norm higher than the given threshold is stored in the set and the set is returned.
        /// NOTE: Values are not interpolated as the target locations are on grid nodes.
        /// </summary>
        /// <param name="cutout"></param>
        /// <param name="cutout_coordiantes"></param>
        /// <param name="coordiantes"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        public override HashSet<SQLUtility.PartialResult> GetThresholdUsingCutout(float[] cutout, int[] cutout_coordinates, int[] coordiantes, double threshold)
        {
            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Fd4Lag4)
            {
                throw new Exception("Invalid interpolation option specified!");
            }
            HashSet<SQLUtility.PartialResult> points_above_threshold = new HashSet<SQLUtility.PartialResult>();
            SQLUtility.PartialResult point;
            int[] cutout_dimensions = new int[] { cutout_coordinates[5] - cutout_coordinates[2],
                                                  cutout_coordinates[4] - cutout_coordinates[1],
                                                  cutout_coordinates[3] - cutout_coordinates[0] };
            int startz = 0, endz = 0, starty = 0, endy = 0, startx = 0, endx = 0, offset_y = 0;
            long zindex = 0;
            for (int z = coordiantes[2]; z < coordiantes[5]; z++)
            {
                for (int y = coordiantes[1]; y < coordiantes[4]; y++)
                {
                    for (int x = coordiantes[0]; x < coordiantes[3]; x++)
                    {
                        GetKernelStartEnd(z, y, x, cutout_coordinates, ref zindex, ref startz, ref endz, ref starty, ref endy, ref startx, ref endx);

                        point = new SQLUtility.PartialResult(zindex, GetResultSize(), numPointsInKernel);
                        GetResultUsingCutout(ref point, cutout, x, y, z, startx, starty, startz, endx, endy, endz, cutout_coordinates, cutout_dimensions, offset_y);

                        // Compute the norm.
                        double norm = 0.0f;
                        for (int i = 0; i < GetResultSize(); i++)
                        {
                            norm += point.result[i] * point.result[i];
                        }
                        norm = Math.Sqrt(norm);
                        point.norm = norm;
                        if (norm > threshold)
                        {
                            points_above_threshold.Add(point);
                            if (points_above_threshold.Count > MAX_NUMBER_THRESHOLD_POINTS)
                            {
                                throw new Exception(String.Format("The number of points above the threshold exeeds max allowed number: {0}!", MAX_NUMBER_THRESHOLD_POINTS));
                            }
                        }
                    }
                }
            }

            return points_above_threshold;
        }

        protected virtual void GetResultUsingCutout(ref SQLUtility.PartialResult point, float[] cutout, int x, int y, int z, int startx, int starty, int startz, int endx, int endy, int endz,
            int[] cutout_coordinates, int[] cutout_dimensions, int offset_y)
        {
            for (int x_i = startx; x_i <= endx; x_i++)
            {
                //Determine the kernel index, at which this data point is for the kernel of the above taget point.
                int iKernelIndexX = x_i - x + kernelSize / 2;

                double coeff = CenteredFiniteDiffCoeff[iKernelIndexX];
                int sourceIndex = ((z - cutout_coordinates[2]) * cutout_dimensions[2] * cutout_dimensions[1] +
                    (y - cutout_coordinates[1]) * cutout_dimensions[2] +
                    (x_i - cutout_coordinates[0])) * setInfo.Components;
                point.result[0] += coeff * cutout[sourceIndex];     //dux/dx
                point.result[3] += coeff * cutout[sourceIndex + 1]; //duy/dx
                point.result[6] += coeff * cutout[sourceIndex + 2]; //duz/dx
                point.numPointsProcessed++;
            }
            for (int y_i = starty; y_i <= endy; y_i++)
            {
                //Determine the kernel index, at which this data point is for the kernel of the above taget point.
                int iKernelIndexY = y_i - y + kernelSize / 2 - offset_y;

                double coeff = CenteredFiniteDiffCoeff[iKernelIndexY];
                int sourceIndex = ((z - cutout_coordinates[2]) * cutout_dimensions[2] * cutout_dimensions[1] +
                    (y_i - cutout_coordinates[1]) * cutout_dimensions[2] +
                    (x - cutout_coordinates[0])) * setInfo.Components;
                point.result[1] += coeff * cutout[sourceIndex];     //dux/dy
                point.result[4] += coeff * cutout[sourceIndex + 1]; //duy/dy
                point.result[7] += coeff * cutout[sourceIndex + 2]; //duz/dy
                point.numPointsProcessed++;
            }
            for (int z_i = startz; z_i <= endz; z_i++)
            {
                //Determine the kernel index, at which this data point is for the kernel of the above taget point.
                int iKernelIndexZ = z_i - z + kernelSize / 2;

                double coeff = CenteredFiniteDiffCoeff[iKernelIndexZ];
                int sourceIndex = ((z_i - cutout_coordinates[2]) * cutout_dimensions[2] * cutout_dimensions[1] +
                    (y - cutout_coordinates[1]) * cutout_dimensions[2] +
                    (x - cutout_coordinates[0])) * setInfo.Components;
                point.result[2] += coeff * cutout[sourceIndex];     //dux/dz
                point.result[5] += coeff * cutout[sourceIndex + 1]; //duy/dz
                point.result[8] += coeff * cutout[sourceIndex + 2]; //duz/dz
                point.numPointsProcessed++;
            }
        }

        protected virtual void GetResultUsingCutout(ref SQLUtility.PartialResult point, BigArray<float> cutout, int x, int y, int z, int startx, int starty, int startz, int endx, int endy, int endz,
            int[] cutout_coordinates, int[] cutout_dimensions, int offset_y)
        {
            for (int x_i = startx; x_i <= endx; x_i++)
            {
                //Determine the kernel index, at which this data point is for the kernel of the above taget point.
                int iKernelIndexX = x_i - x + kernelSize / 2;

                double coeff = CenteredFiniteDiffCoeff[iKernelIndexX];
                ulong sourceIndex = (((ulong)z - (ulong)cutout_coordinates[2]) * (ulong)cutout_dimensions[2] * (ulong)cutout_dimensions[1] +
                    ((ulong)y - (ulong)cutout_coordinates[1]) * (ulong)cutout_dimensions[2] +
                    ((ulong)x_i - (ulong)cutout_coordinates[0])) * (ulong)setInfo.Components;
                point.result[0] += coeff * cutout[sourceIndex];     //dux/dx
                point.result[3] += coeff * cutout[sourceIndex + 1]; //duy/dx
                point.result[6] += coeff * cutout[sourceIndex + 2]; //duz/dx
                point.numPointsProcessed++;
            }
            for (int y_i = starty; y_i <= endy; y_i++)
            {
                //Determine the kernel index, at which this data point is for the kernel of the above taget point.
                int iKernelIndexY = y_i - y + kernelSize / 2 - offset_y;

                double coeff = CenteredFiniteDiffCoeff[iKernelIndexY];
                ulong sourceIndex = (((ulong)z - (ulong)cutout_coordinates[2]) * (ulong)cutout_dimensions[2] * (ulong)cutout_dimensions[1] +
                    ((ulong)y_i - (ulong)cutout_coordinates[1]) * (ulong)cutout_dimensions[2] +
                    ((ulong)x - (ulong)cutout_coordinates[0])) * (ulong)setInfo.Components;
                point.result[1] += coeff * cutout[sourceIndex];     //dux/dy
                point.result[4] += coeff * cutout[sourceIndex + 1]; //duy/dy
                point.result[7] += coeff * cutout[sourceIndex + 2]; //duz/dy
                point.numPointsProcessed++;
            }
            for (int z_i = startz; z_i <= endz; z_i++)
            {
                //Determine the kernel index, at which this data point is for the kernel of the above taget point.
                int iKernelIndexZ = z_i - z + kernelSize / 2;

                double coeff = CenteredFiniteDiffCoeff[iKernelIndexZ];
                ulong sourceIndex = (((ulong)z_i - (ulong)cutout_coordinates[2]) * (ulong)cutout_dimensions[2] * (ulong)cutout_dimensions[1] +
                    ((ulong)y - (ulong)cutout_coordinates[1]) * (ulong)cutout_dimensions[2] +
                    ((ulong)x - (ulong)cutout_coordinates[0])) * (ulong)setInfo.Components;
                point.result[2] += coeff * cutout[sourceIndex];     //dux/dz
                point.result[5] += coeff * cutout[sourceIndex + 1]; //duy/dz
                point.result[8] += coeff * cutout[sourceIndex + 2]; //duz/dz
                point.numPointsProcessed++;
            }
        }

    }

}
