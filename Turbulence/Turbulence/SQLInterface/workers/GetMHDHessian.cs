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
    public class GetMHDHessian : Worker
    {
        double[,] CenteredFiniteDiffCoeff = null;
        double[] lagDenominator = null;

        public GetMHDHessian(TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp)
        {
            // In the case of Fd4Lag4 the computation is a combination of 
            // two computations (Finite Differencing and Lag. Interpolation)
            // The size of the kernel is the combined size of the two kernels
            this.kernelSize = 0;
            this.setInfo = setInfo;
            this.spatialInterp = spatialInterp;

            switch(spatialInterp)
            {
                case TurbulenceOptions.SpatialInterpolation.None_Fd4:
                    kernelSize += 4; // kernelSize will be 4 in the case of None_Fd4 and 8 in the case of Fd4Lag4 (4+4)
                    CenteredFiniteDiffCoeff = new double[5, 5];
                    for (int j = 0; j < 5; j++)
                        for (int i = 0; i < 5; i++)
                            CenteredFiniteDiffCoeff[i, j] = 0.0;

                    CenteredFiniteDiffCoeff[2, 0] = -1.0 / 12.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[2, 1] = 4.0 / 3.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[2, 2] = -15.0 / 6.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[2, 3] = 4.0 / 3.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[2, 4] = -1.0 / 12.0 / setInfo.Dx / setInfo.Dx;


                    CenteredFiniteDiffCoeff[0, 0] = -1.0 / 48.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[0, 4] = 1.0 / 48.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[4, 4] = -1.0 / 48.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[4, 0] = 1.0 / 48.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[1, 1] = 1.0 / 3.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[1, 3] = -1.0 / 3.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[3, 3] = 1.0 / 3.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[3, 1] = -1.0 / 3.0 / setInfo.Dx / setInfo.Dx;
                    break;
                case TurbulenceOptions.SpatialInterpolation.None_Fd6:
                    kernelSize = 6;
                    CenteredFiniteDiffCoeff = new double[7, 7];
                    for (int j = 0; j < 5; j++)
                        for (int i = 0; i < 5; i++)
                            CenteredFiniteDiffCoeff[i, j] = 0.0;

                    CenteredFiniteDiffCoeff[3, 0] = 1.0 / 90.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[3, 1] = -3.0 / 20.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[3, 2] = 3.0 / 2.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[3, 3] = -49.0 / 18.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[3, 4] = 3.0 / 2.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[3, 5] = -3.0 / 20.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[3, 6] = 1.0 / 90.0 / setInfo.Dx / setInfo.Dx;


                    CenteredFiniteDiffCoeff[0, 0] = 1.0 / 360.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[0, 6] = -1.0 / 360.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[6, 6] = 1.0 / 360.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[6, 0] = -1.0 / 360.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[1, 1] = -3.0 / 80.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[1, 5] = 3.0 / 80.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[5, 5] = -3.0 / 80.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[5, 1] = 3.0 / 80.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[2, 2] = 3.0 / 8.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[2, 4] = -3.0 / 8.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[4, 4] = 3.0 / 8.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[4, 2] = -3.0 / 8.0 / setInfo.Dx / setInfo.Dx;
                    break;
                case TurbulenceOptions.SpatialInterpolation.None_Fd8:
                    kernelSize = 8;
                    CenteredFiniteDiffCoeff = new double[9, 9];
                    for (int j = 0; j < 5; j++)
                        for (int i = 0; i < 5; i++)
                            CenteredFiniteDiffCoeff[i, j] = 0.0;

                    CenteredFiniteDiffCoeff[4, 0] = 9.0 / 3152.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[4, 1] = -104.0 / 8865.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[4, 2] = -207.0 / 2955.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[4, 3] = 792.0 / 591.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[4, 4] = -35777.0 / 14184.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[4, 5] = 792.0 / 591.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[4, 6] = -207.0 / 2955.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[4, 7] = -104.0 / 8865.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[4, 8] = 9.0 / 3152.0 / setInfo.Dx / setInfo.Dx;


                    CenteredFiniteDiffCoeff[0, 0] = -1.0 / 2240.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[0, 8] = 1.0 / 2240.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[8, 8] = -1.0 / 2240.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[8, 0] = 1.0 / 2240.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[1, 1] = 2.0 / 315.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[1, 7] = -2.0 / 315.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[7, 7] = 2.0 / 315.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[7, 1] = -2.0 / 315.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[2, 2] = -1.0 / 20.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[2, 6] = 1.0 / 20.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[6, 6] = -1.0 / 20.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[6, 2] = 1.0 / 20.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[3, 3] = 14.0 / 35.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[3, 5] = -14.0 / 35.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[5, 5] = 14.0 / 35.0 / setInfo.Dx / setInfo.Dx;
                    CenteredFiniteDiffCoeff[5, 3] = -14.0 / 35.0 / setInfo.Dx / setInfo.Dx;
                    break;
                case TurbulenceOptions.SpatialInterpolation.Fd4Lag4:
                    kernelSize = 4;
                    lagDenominator = new double[4];
                    LagInterpolation.InterpolantDenominator(4, lagDenominator);
                    goto case TurbulenceOptions.SpatialInterpolation.None_Fd4;
                default:
                    throw new Exception(String.Format("Invalid Spatial Interpolation Option: {0}", spatialInterp));
            }
        }

        public override SqlMetaData[] GetRecordMetaData()
        {
            return new SqlMetaData[] {
                new SqlMetaData("Req", SqlDbType.Int),
                new SqlMetaData("d2uxdxdx", SqlDbType.Real),
                new SqlMetaData("d2uxdxdy", SqlDbType.Real),
                new SqlMetaData("d2uxdxdz", SqlDbType.Real),
                new SqlMetaData("d2uxdydy", SqlDbType.Real),
                new SqlMetaData("d2uxdydz", SqlDbType.Real),
                new SqlMetaData("d2uxdzdz", SqlDbType.Real),
                new SqlMetaData("d2uydxdx", SqlDbType.Real),
                new SqlMetaData("d2uydxdy", SqlDbType.Real),
                new SqlMetaData("d2uydxdz", SqlDbType.Real),
                new SqlMetaData("d2uydydy", SqlDbType.Real),
                new SqlMetaData("d2uydydz", SqlDbType.Real),
                new SqlMetaData("d2uydzdz", SqlDbType.Real),
                new SqlMetaData("d2uzdxdx", SqlDbType.Real),
                new SqlMetaData("d2uzdxdy", SqlDbType.Real),
                new SqlMetaData("d2uzdxdz", SqlDbType.Real),
                new SqlMetaData("d2uzdydy", SqlDbType.Real),
                new SqlMetaData("d2uzdydz", SqlDbType.Real),
                new SqlMetaData("d2uzdzdz", SqlDbType.Real),
                new SqlMetaData("Cubes Read", SqlDbType.Int) };
        }

        /// <summary>
        /// Determines the database atoms that overlap the kernel of computation for the given point
        /// </summary>
        public override void GetAtomsForPoint(SQLUtility.MHDInputRequest request, long mask, int pointsPerCubeEstimate, Dictionary<long, List<int>> map, ref int total_points)
        {
            int X, Y, Z;
            int startz, starty, startx, endz, endy, endx;

            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Fd4Lag4)
            {
                X = LagInterpolation.CalcNode(request.x, setInfo.Dx);
                Y = LagInterpolation.CalcNode(request.y, setInfo.Dx);
                Z = LagInterpolation.CalcNode(request.z, setInfo.Dx);

                //x_values = new int[] { X - kernelSize / 2 + 1, X + kernelSize / 2 };
                //y_values = new int[] { Y - kernelSize / 2 + 1, Y + kernelSize / 2 };
                //z_values = new int[] { Z - kernelSize / 2 + 1, Z + kernelSize / 2 };

                startz = Z - kernelSize / 2 + 1; // From Z-3 to Z+4
                endz = Z + kernelSize / 2;
                starty = Y - kernelSize / 2 + 1; // From Y-3 to Y+4
                endy = Y + kernelSize / 2;
                startx = X - kernelSize / 2 + 1; // From X-3 to X+4
                endx = X + kernelSize / 2;
            }
            else
            {
                X = LagInterpolation.CalcNodeWithRound(request.x, setInfo.Dx);
                Y = LagInterpolation.CalcNodeWithRound(request.y, setInfo.Dx);
                Z = LagInterpolation.CalcNodeWithRound(request.z, setInfo.Dx);

                // In this case we are not performing Lagrange Polynomial interpolation 
                // and we need a different sized cube of data points
                //x_values = new int[] { X - kernelSize / 2, X + kernelSize / 2 };
                //y_values = new int[] { Y - kernelSize / 2, Y + kernelSize / 2 };
                //z_values = new int[] { Z - kernelSize / 2, Z + kernelSize / 2 };

                startz = Z - kernelSize / 2;
                endz = Z + kernelSize / 2;
                starty = Y - kernelSize / 2;
                endy = Y + kernelSize / 2;
                startx = X - kernelSize / 2;
                endx = X + kernelSize / 2;
            }

            long zindex;

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
                            if (!map.ContainsKey(zindex))
                            {
                                //map[zindex] = new List<int>(pointsPerCubeEstimate);
                                map[zindex] = new List<int>();
                            }
                            //Debug.Assert(!map[zindex].Contains(request.request));
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
            return CalcHessian(blob, xp, yp, zp, input);
        }

        public override int GetResultSize()
        {
            return 18;
        }


        /// <summary>
        /// New version of the CalcHessian function.
        /// </summary>
        /// <remarks>
        /// The finite difference evaluations of the second partial derivatives are carried out.
        /// These are very similar to the computation of the Laplacian.
        /// The only difference is that we are not summing the unmixed second derivatives and
        /// that we have to compute the mixed derivatives as well.
        /// </remarks>
        unsafe public double[] CalcHessian(TurbulenceBlob blob, float xp, float yp, float zp, SQLUtility.MHDInputRequest input)
        {
            double[] result = new double[GetResultSize()]; // Result value for the user
            for (int i = 0; i < GetResultSize(); i++)
                result[i] = 0.0;

            float[] data = blob.data;

            int x, y, z;
            int startz = -1, starty = -1, startx = -1, endz = -1, endy = -1, endx = -1;
            int iLagIntx = 0, iLagInty = 0, iLagIntz = 0;

            switch (spatialInterp)
            {
                #region SpatialInterpolation None_Fd4/6/8
                case TurbulenceOptions.SpatialInterpolation.None_Fd4:
                case TurbulenceOptions.SpatialInterpolation.None_Fd6:
                case TurbulenceOptions.SpatialInterpolation.None_Fd8:
                    // kernelSize will be:
                    // 4 if spatialInterp is None_Fd4
                    // 6 if it is None_Fd6
                    // 8 if it is None_Fd8
                    int length = kernelSize / 2;
                    x = LagInterpolation.CalcNodeWithRound(xp, setInfo.Dx);
                    y = LagInterpolation.CalcNodeWithRound(yp, setInfo.Dx);
                    z = LagInterpolation.CalcNodeWithRound(zp, setInfo.Dx);
                    // Wrap the coordinates into the grid space
                    x = ((x % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
                    y = ((y % setInfo.GridResolutionY) + setInfo.GridResolutionY) % setInfo.GridResolutionY;
                    z = ((z % setInfo.GridResolutionZ) + setInfo.GridResolutionZ) % setInfo.GridResolutionZ;

                    // We are computing the following: 
                    // 4/3dx^2[f(x_(m+1, y_n)) + f(x_(m-1, y_n)) - 2f(x_m, y_n)] - 1/12dx^2[f(x_(m+2, y_n)) + f(x_(m-2, y_n)) - 2f(x_m, y_n)]

                    // Since the given blob may not hold all of the required data
                    // we determine where to start and end the partial computation
                    // we specify kernelSize + 1 as kernelSize points plus the one in the middle are needed 
                    // (5 points for 4th order, 7 for 6th and 9 for 8th)
                    blob.GetSubcubeStart(z - length, y - length, x - length, ref startz, ref starty, ref startx);
                    blob.GetSubcubeEnd(z + length, y + length, x + length, ref endz, ref endy, ref endx);

                    // We also need to determine where we are starting, e.g. f(x_(n-2)), f(x_(n-1)), etc.
                    iLagIntx = blob.GetRealX - x + startx + length;
                    if (iLagIntx >= setInfo.GridResolutionX)
                        iLagIntx -= setInfo.GridResolutionX;
                    else if (iLagIntx < 0)
                        iLagIntx += setInfo.GridResolutionX;

                    iLagInty = blob.GetRealY - y + starty + length;
                    if (iLagInty >= setInfo.GridResolutionY)
                        iLagInty -= setInfo.GridResolutionY;
                    else if (iLagInty < 0)
                        iLagInty += setInfo.GridResolutionY;

                    iLagIntz = blob.GetRealZ - z + startz + length;
                    if (iLagIntz >= setInfo.GridResolutionZ)
                        iLagIntz -= setInfo.GridResolutionZ;
                    else if (iLagIntz < 0)
                        iLagIntz += setInfo.GridResolutionZ;

                    fixed (double* FDCoeff = CenteredFiniteDiffCoeff)
                    {
                        fixed (float* fdata = data)
                        {
                            int off = 0;
                            #region unmixed derivatives
                            if (y >= blob.GetBaseY && y < blob.GetBaseY + blob.GetSide
                                                && z >= blob.GetBaseZ && z < blob.GetBaseZ + blob.GetSide)
                            {
                                off = startx * blob.GetComponents + (y - blob.GetBaseY) * blob.GetSide * blob.GetComponents + (z - blob.GetBaseZ) * blob.GetSide * blob.GetSide * blob.GetComponents;
                                for (int ix = startx; ix <= endx; ix++)
                                {
                                    // The coefficients for the unmixed derivatives are stored in the center row
                                    // This is why we add (kernelSize + 1) * (kernelSize / 2) to the offset
                                    double coeff = FDCoeff[iLagIntx + ix - startx + (kernelSize + 1) * (kernelSize / 2)];
                                    result[0] += coeff * fdata[off];
                                    result[6] += coeff * fdata[off + 1];
                                    result[12] += coeff * fdata[off + 2];
                                    off += blob.GetComponents;
                                }
                            }
                            if (x >= blob.GetBaseX && x < blob.GetBaseX + blob.GetSide
                                && z >= blob.GetBaseZ && z < blob.GetBaseZ + blob.GetSide)
                            {
                                off = (x - blob.GetBaseX) * blob.GetComponents + starty * blob.GetSide * blob.GetComponents + (z - blob.GetBaseZ) * blob.GetSide * blob.GetSide * blob.GetComponents;
                                for (int iy = starty; iy <= endy; iy++)
                                {
                                    // The coefficients for the unmixed derivatives are stored in the center row
                                    double coeff = FDCoeff[iLagInty + iy - starty + (kernelSize + 1) * (kernelSize / 2)];
                                    result[3] += coeff * fdata[off];
                                    result[9] += coeff * fdata[off + 1];
                                    result[15] += coeff * fdata[off + 2];
                                    off += blob.GetSide * blob.GetComponents;
                                }
                            }
                            if (x >= blob.GetBaseX && x < blob.GetBaseX + blob.GetSide
                                && y >= blob.GetBaseY && y < blob.GetBaseY + blob.GetSide)
                            {
                                off = (x - blob.GetBaseX) * blob.GetComponents + (y - blob.GetBaseY) * blob.GetSide * blob.GetComponents + startz * blob.GetSide * blob.GetSide * blob.GetComponents;
                                for (int iz = startz; iz <= endz; iz++)
                                {
                                    // The coefficients for the unmixed derivatives are stored in the center row
                                    double coeff = FDCoeff[iLagIntz + iz - startz + (kernelSize + 1) * (kernelSize / 2)];
                                    result[5] += coeff * fdata[off];
                                    result[11] += coeff * fdata[off + 1];
                                    result[17] += coeff * fdata[off + 2];
                                    off += blob.GetSide * blob.GetSide * blob.GetComponents;
                                }
                            }
                            #endregion
                            #region mixed derivatives
                            // the mixed derivatives needs data from a plane (either x-y, x-z or y-z)
                            if (z >= blob.GetBaseZ && z < blob.GetBaseZ + blob.GetSide)
                            {
                                for (int iy = starty; iy <= endy; iy++)
                                {
                                    int LagIntIndexY = iLagInty + iy - starty;
                                    // For the mixed derivatives the center line does not play a role
                                    if (LagIntIndexY != kernelSize / 2)
                                    {
                                        off = startx * blob.GetComponents + iy * blob.GetSide * blob.GetComponents + (z - blob.GetRealZ) * blob.GetSide * blob.GetSide * blob.GetComponents;
                                        for (int ix = startx; ix <= endx; ix++)
                                        {
                                            // Since FDCoeff is now a pointer we need to index into the one dimensional representation
                                            // of the 2d array
                                            double coeff = FDCoeff[iLagIntx + ix - startx + LagIntIndexY * (kernelSize + 1)];
                                            if (coeff != 0.0)
                                            {
                                                result[1] += coeff * fdata[off];
                                                result[7] += coeff * fdata[off + 1];
                                                result[13] += coeff * fdata[off + 2];
                                            }
                                            off += blob.GetComponents;
                                        }
                                    }
                                }
                            }
                            if (y >= blob.GetBaseY && y < blob.GetBaseY + blob.GetSide)
                            {
                                for (int iz = startz; iz <= endz; iz++)
                                {
                                    int LagIntIndexZ = iLagIntz + iz - startz;
                                    // For the mixed derivatives the center line does not play a role
                                    if (LagIntIndexZ != kernelSize / 2)
                                    {
                                        off = startx * blob.GetComponents + (y - blob.GetRealY) * blob.GetSide * blob.GetComponents + iz * blob.GetSide * blob.GetSide * blob.GetComponents;
                                        for (int ix = startx; ix <= endx; ix++)
                                        {
                                            double coeff = FDCoeff[iLagIntx + ix - startx + LagIntIndexZ * (kernelSize + 1)];
                                            //double coeff = CenteredFiniteDiffCoeff[iLagIntx + ix - startx, iLagIntz + iz - startz];
                                            if (coeff != 0.0)
                                            {
                                                result[2] += coeff * fdata[off];
                                                result[8] += coeff * fdata[off + 1];
                                                result[14] += coeff * fdata[off + 2];
                                            }
                                            off += blob.GetComponents;
                                        }
                                    }
                                }
                            }
                            if (x >= blob.GetBaseX && x < blob.GetBaseX + blob.GetSide)
                            {
                                for (int iz = startz; iz <= endz; iz++)
                                {
                                    int LagIntIndexZ = iLagIntz + iz - startz;
                                    // For the mixed derivatives the center line does not play a role
                                    if (LagIntIndexZ != kernelSize / 2)
                                    {
                                        off = (x - blob.GetRealX) * blob.GetComponents + starty * blob.GetSide * blob.GetComponents + iz * blob.GetSide * blob.GetSide * blob.GetComponents;
                                        for (int iy = starty; iy <= endy; iy++)
                                        {
                                            double coeff = FDCoeff[iLagInty + iy - starty + LagIntIndexZ * (kernelSize + 1)];
                                            //double coeff = CenteredFiniteDiffCoeff[iLagInty + iy - starty, iLagIntz + iz - startz];
                                            if (coeff != 0.0)
                                            {
                                                result[4] += coeff * fdata[off];
                                                result[10] += coeff * fdata[off + 1];
                                                result[16] += coeff * fdata[off + 2];
                                            }
                                            off += blob.GetSide * blob.GetComponents;
                                        }
                                    }
                                }
                            }
                            #endregion
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
                    y = ((y % setInfo.GridResolutionY) + setInfo.GridResolutionY) % setInfo.GridResolutionY;
                    z = ((z % setInfo.GridResolutionZ) + setInfo.GridResolutionZ) % setInfo.GridResolutionZ;

                    // This computation has 2 stages:
                    // 4th-order finite difference evaluation of the derivative (see above)
                    // and 4th-order Lagrange Polynomial interpolation of this derivative

                    // The entire computation operates over the parts of a cube of size 8x8x8
                    // from n - 3 to n + 4 as below
                    // E.g. for the computation of duxdxdx (or duydxdx, duzdxdx, since here ux, uy, uz are the components of the velocity)
                    // we need the part of the cube from [2,2,0] to [5,5,7], 
                    // while for the computation of duxdydy (or duydydy, duzdydy)
                    // we need the part of the cube from [2,0,2] to [5,7,5], etc.

                    // The lagrange interpolation of the mixed derivatives is executed over planer patches
                    // since the kernel of the mixed derivative is a planer patch
                    // For example, in order to compute duxdxdy we need the following 8 points (in the x-y plane)
                    // [0,0], [0,4], [1,1], [1,3], [3,1], [3,3], [4,0], [4,4] 
                    // the z-coordinate ranges from 2 to 5
                    // Such planer patches will be interpolated according to the lagrange polynomial computation

                    // Since the given blob may not hold all of the required data
                    // we determine where to start and end the partial computation
                    blob.GetSubcubeStart(z - kernelSize / 2 + 1, y - kernelSize / 2 + 1, x - kernelSize / 2 + 1, ref startz, ref starty, ref startx);
                    blob.GetSubcubeEnd(z + kernelSize / 2, y + kernelSize / 2, x + kernelSize / 2, ref endz, ref endy, ref endx);
                
                    // We also need to determine where we are starting, e.g. n - 3, n - 2, ..., n + 4
                    iLagIntx = blob.GetRealX - x + startx + 3;
                    if (iLagIntx >= setInfo.GridResolutionX)
                        iLagIntx -= setInfo.GridResolutionX;
                    else if (iLagIntx < 0)
                        iLagIntx += setInfo.GridResolutionX;

                    iLagInty = blob.GetRealY - y + starty + 3;
                    if (iLagInty >= setInfo.GridResolutionY)
                        iLagInty -= setInfo.GridResolutionY;
                    else if (iLagInty < 0)
                        iLagInty += setInfo.GridResolutionY;

                    iLagIntz = blob.GetRealZ - z + startz + 3;
                    if (iLagIntz >= setInfo.GridResolutionZ)
                        iLagIntz -= setInfo.GridResolutionZ;
                    else if (iLagIntz < 0)
                        iLagIntz += setInfo.GridResolutionZ;

                    fixed (double* lagint = input.lagInt, FDCoeff = CenteredFiniteDiffCoeff)
                    {
                        fixed (float* fdata = data)
                        {
                            double[] ax = new double[3] { 0.0, 0.0, 0.0 }, ay = new double[3] { 0.0, 0.0, 0.0 }, az = new double[3] { 0.0, 0.0, 0.0 },
                                axy = new double[3] { 0.0, 0.0, 0.0 }, axz = new double[3] { 0.0, 0.0, 0.0 }, ayz = new double[3] { 0.0, 0.0, 0.0 };
                            int off0 = startx * blob.GetComponents;

                            // Since the finite differencing coefficients are stored in a 2d array
                            // we need to offset to the middle row when computing the unmixed derivatives
                            int FDOff = (FdOrder + 1) * (FdOrder / 2);
                            for (int iz = startz; iz <= endz; iz++)
                            {
                                double[] bx = new double[3] { 0.0, 0.0, 0.0 }, by = new double[3] { 0.0, 0.0, 0.0 }, bz = new double[3] { 0.0, 0.0, 0.0 },
                                    bxy = new double[3] { 0.0, 0.0, 0.0 }, bxz = new double[3] { 0.0, 0.0, 0.0 }, byz = new double[3] { 0.0, 0.0, 0.0 };
                                int LagIntIndexZ = iLagIntz + iz - startz;
                                int off1 = off0 + iz * blob.GetSide * blob.GetSide * blob.GetComponents;
                                for (int iy = starty; iy <= endy; iy++)
                                {
                                    double[] cx = new double[3] { 0.0, 0.0, 0.0 }, cy = new double[3] { 0.0, 0.0, 0.0 }, cz = new double[3] { 0.0, 0.0, 0.0 },
                                        cxy = new double[3] { 0.0, 0.0, 0.0 }, cxz = new double[3] { 0.0, 0.0, 0.0 }, cyz = new double[3] { 0.0, 0.0, 0.0 };
                                    int LagIntIndexY = iLagInty + iy - starty;
                                    int off = off1 + iy * blob.GetSide * blob.GetComponents;
                                    for (int ix = startx; ix <= endx; ix++)
                                    {
                                        int LagIntIndexX = iLagIntx + ix - startx;
                                        #region d2uidxdx
                                        // the Y and Z dimensions range from the 2nd to the 5th row in the cube
                                        // since for the 4th-order Lagrange Polynomial interpolation we need the 4 nearest rows in each dim.
                                        // to the target point
                                        if (LagIntIndexY >= 2 && LagIntIndexY <= 5 && LagIntIndexZ >= 2 && LagIntIndexZ <= 5)
                                        {
                                            // Each point may be part of up to LagIntOrder interplation kernels
                                            // Therefore, we check each possible kernel and if the point falls inside it
                                            // we update the partial sum accordingly
                                            for (int i = 0; i < LagIntOrder; i++)
                                            {
                                                // The kernel index ranges from 0 to 4 for 4th-order finite differencing
                                                if (LagIntIndexX - i >= 0 && LagIntIndexX - i <= FdOrder)
                                                {
                                                    double c = lagint[i] * FDCoeff[LagIntIndexX - i + FDOff];
                                                    cx[0] += c * fdata[off];
                                                    cx[1] += c * fdata[off + 1];
                                                    cx[2] += c * fdata[off + 2];
                                                }
                                            }
                                        }
                                        #endregion
                                        #region d2uidydy
                                        // Same as for d2uidxdx, with the exception that we don't check at this point
                                        // which kernel the point falls into, since these are lines in the y-direction
                                        if (LagIntIndexX >= 2 && LagIntIndexX <= 5 && LagIntIndexZ >= 2 && LagIntIndexZ <= 5)
                                        {
                                            double c = lagint[LagIntIndexX - 2];
                                            cy[0] += c * fdata[off];
                                            cy[1] += c * fdata[off + 1];
                                            cy[2] += c * fdata[off + 2];
                                        }
                                        #endregion
                                        #region d2uidzdz
                                        // Same as for d2uidxdx, with the exception that we don't check at this point
                                        // which kernel the point falls into, since these are lines in the z-direction
                                        if (LagIntIndexX >= 2 && LagIntIndexX <= 5 && LagIntIndexY >= 2 && LagIntIndexY <= 5)
                                        {
                                            double c = lagint[LagIntIndexX - 2];
                                            cz[0] += c * fdata[off];
                                            cz[1] += c * fdata[off + 1];
                                            cz[2] += c * fdata[off + 2];
                                        }
                                        #endregion
                                        #region d2uidxdy
                                        // the Z dimension ranges from the 2nd to the 5th row in the cube
                                        // since for the 4th-order Lagrange Polynomial interpolation we need the 4 nearest rows in each dim.
                                        // to the target point
                                        if (LagIntIndexZ >= 2 && LagIntIndexZ <= 5)
                                        {
                                            // Each point may be part of more than one interplation kernel
                                            // Therefore, we check each possible kernel and if the points falls inside it
                                            // we update the partial sum accordingly (in this case the kernels are planer patches)
                                            for (int j = 0; j < LagIntOrder; j++)
                                            {
                                                // The kernel index ranges from 0 to 4 for 4th-order finite differencing
                                                // the middle row does not play a role in the evaluation of the mixed derivative
                                                if (LagIntIndexY - j >= 0 && LagIntIndexY - j <= FdOrder && LagIntIndexY - j != FdOrder / 2)
                                                {
                                                    for (int i = 0; i < LagIntOrder; i++)
                                                    {
                                                        if (LagIntIndexX - i >= 0 && LagIntIndexX - i <= FdOrder)
                                                        {
                                                            double c = FDCoeff[LagIntIndexX - i + (LagIntIndexY - j) * (FdOrder + 1)];
                                                            if (c != 0.0)
                                                            {
                                                                c *= lagint[1 * LagIntOrder + j] * lagint[i];
                                                                cxy[0] += c * fdata[off];
                                                                cxy[1] += c * fdata[off + 1];
                                                                cxy[2] += c * fdata[off + 2];
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        #endregion
                                        #region d2uidxdz
                                        // Same as for d2uidxdy but this time in the x-z plane
                                        if (LagIntIndexY >= 2 && LagIntIndexY <= 5)
                                        {
                                            for (int j = 0; j < LagIntOrder; j++)
                                            {
                                                if (LagIntIndexZ - j >= 0 && LagIntIndexZ - j <= FdOrder && LagIntIndexZ - j != FdOrder / 2)
                                                {
                                                    for (int i = 0; i < LagIntOrder; i++)
                                                    {
                                                        if (LagIntIndexX - i >= 0 && LagIntIndexX - i <= FdOrder)
                                                        {
                                                            double c = FDCoeff[LagIntIndexX - i + (LagIntIndexZ - j) * (FdOrder + 1)];
                                                            if (c != 0.0)
                                                            {
                                                                c *= lagint[2 * LagIntOrder + j] * lagint[i];
                                                                cxz[0] += c * fdata[off];
                                                                cxz[1] += c * fdata[off + 1];
                                                                cxz[2] += c * fdata[off + 2];
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        #endregion
                                        #region d2uidydz
                                        // In this case the computation is in the y-z plane
                                        if (LagIntIndexX >= 2 && LagIntIndexX <= 5)
                                        {
                                            double c = lagint[LagIntIndexX - 2];
                                            cyz[0] += c * fdata[off];
                                            cyz[1] += c * fdata[off + 1];
                                            cyz[2] += c * fdata[off + 2];
                                        }
                                        #endregion
                                        off += blob.GetComponents;
                                    }
                                    for (int j = 0; j < 3; j++)
                                    {
                                        if (LagIntIndexY >= 2 && LagIntIndexY <= 5)
                                        {
                                            double b = lagint[1 * LagIntOrder + LagIntIndexY - 2];
                                            bx[j] += cx[j] * b;
                                            bz[j] += cz[j] * b;
                                            bxz[j] += cxz[j] * b;
                                        }
                                        // for d2uidxdy we have already multiplied the partial sum
                                        // by the y-coefficients
                                        bxy[j] += cxy[j];
                                    }
                                    #region d2uidydy and d2uidydz
                                    // The 2 computations have been merged into 1 since they share the same outer loop
                                    for (int i = 0; i < LagIntOrder; i++)
                                    {
                                        // The kernel index ranges from 0 to 4 for 4th-order finite differencing
                                        if (LagIntIndexY - i >= 0 && LagIntIndexY - i <= FdOrder)
                                        {
                                            // First, we compute d2uidydy
                                            double c = lagint[1 * LagIntOrder + i] * FDCoeff[LagIntIndexY - i + FDOff];
                                            by[0] += c * cy[0];
                                            by[1] += c * cy[1];
                                            by[2] += c * cy[2];
                                            // Next, we compute d2uidydz
                                            for (int j = 0; j < LagIntOrder; j++)
                                            {
                                                if (LagIntIndexZ - j >= 0 && LagIntIndexZ - j <= FdOrder && LagIntIndexZ - j != FdOrder / 2)
                                                {
                                                    c = FDCoeff[LagIntIndexY - i + (LagIntIndexZ - j) * (FdOrder + 1)];
                                                    if (c != 0.0)
                                                    {
                                                        //c *= lagint[2 * LagIntOrder + j] * lagint[i];
                                                        c *= lagint[2 * LagIntOrder + j] * lagint[1 * LagIntOrder + i];
                                                        byz[0] += c * cyz[0];
                                                        byz[1] += c * cyz[1];
                                                        byz[2] += c * cyz[2];
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    #endregion
                                }
                                for (int j = 0; j < 3; j++)
                                {
                                    if (LagIntIndexZ >= 2 && LagIntIndexZ <= 5)
                                    {
                                        double a = lagint[2 * LagIntOrder + LagIntIndexZ - 2];
                                        ax[j] += bx[j] * a;
                                        ay[j] += by[j] * a;
                                        axy[j] += bxy[j] * a;
                                    }
                                    // for d2uidxdz we have already multiplied the partial sum
                                    // by the z-coefficients
                                    axz[j] += bxz[j];
                                    ayz[j] += byz[j];
                                }
                                #region d2uidzdz
                                for (int i = 0; i < LagIntOrder; i++)
                                {
                                    // The kernel index ranges from 0 to 4 for 4th-order finite differencing
                                    if (LagIntIndexZ - i >= 0 && LagIntIndexZ - i <= FdOrder)
                                    {
                                        double c = lagint[2 * LagIntOrder + i] * FDCoeff[LagIntIndexZ - i + FDOff];
                                        az[0] += c * bz[0];
                                        az[1] += c * bz[1];
                                        az[2] += c * bz[2];
                                    }
                                }
                                #endregion
                            }
                            result[0] = ax[0];
                            result[1] = axy[0];
                            result[2] = axz[0];
                            result[3] = ay[0];
                            result[4] = ayz[0];
                            result[5] = az[0];
                            result[6] = ax[1];
                            result[7] = axy[1];
                            result[8] = axz[1];
                            result[9] = ay[1];
                            result[10] = ayz[1];
                            result[11] = az[1];
                            result[12] = ax[2];
                            result[13] = axy[2];
                            result[14] = axz[2];
                            result[15] = ay[2];
                            result[16] = ayz[2];
                            result[17] = az[2];
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
