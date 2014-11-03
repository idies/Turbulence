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
    //TODO: Combine this with the GetMHDHessian
    //      A loop over the components needs to be added
    public class GetMHDPressureHessian : Worker
    {
        float[,] CenteredFiniteDiffCoeff = null;
        double[] lagDenominator = null;

        public GetMHDPressureHessian(TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp)
        {
            this.setInfo = setInfo;
            this.spatialInterp = spatialInterp;
            this.kernelSize = 0;

            switch(spatialInterp)
            {
                case TurbulenceOptions.SpatialInterpolation.None_Fd4:
                    this.kernelSize += 4; // kernelSize will be 4 in the case of None_Fd4 and 8 in the case of Fd4Lag4 (4+4)
                    CenteredFiniteDiffCoeff = new float[5, 5];
                    for (int j = 0; j < 5; j++)
                        for (int i = 0; i < 5; i++)
                            CenteredFiniteDiffCoeff[i, j] = 0.0f;

                    CenteredFiniteDiffCoeff[2, 0] = -1.0f / 12.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[2, 1] = 4.0f / 3.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[2, 2] = -15.0f / 6.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[2, 3] = 4.0f / 3.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[2, 4] = -1.0f / 12.0f / setInfo.DxFloat / setInfo.DxFloat;


                    CenteredFiniteDiffCoeff[0, 0] = -1.0f / 48.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[0, 4] = 1.0f / 48.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[4, 4] = -1.0f / 48.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[4, 0] = 1.0f / 48.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[1, 1] = 1.0f / 3.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[1, 3] = -1.0f / 3.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[3, 3] = 1.0f / 3.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[3, 1] = -1.0f / 3.0f / setInfo.DxFloat / setInfo.DxFloat;
                    break;
                case TurbulenceOptions.SpatialInterpolation.None_Fd6:
                    this.kernelSize = 6;
                    CenteredFiniteDiffCoeff = new float[7, 7];
                    for (int j = 0; j < 5; j++)
                        for (int i = 0; i < 5; i++)
                            CenteredFiniteDiffCoeff[i, j] = 0.0f;

                    CenteredFiniteDiffCoeff[3, 0] = 1.0f / 90.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[3, 1] = -3.0f / 20.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[3, 2] = 3.0f / 2.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[3, 3] = -49.0f / 18.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[3, 4] = 3.0f / 2.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[3, 5] = -3.0f / 20.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[3, 6] = 1.0f / 90.0f / setInfo.DxFloat / setInfo.DxFloat;


                    CenteredFiniteDiffCoeff[0, 0] = 1.0f / 360.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[0, 6] = -1.0f / 360.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[6, 6] = 1.0f / 360.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[6, 0] = -1.0f / 360.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[1, 1] = -3.0f / 80.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[1, 5] = 3.0f / 80.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[5, 5] = -3.0f / 80.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[5, 1] = 3.0f / 80.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[2, 2] = 3.0f / 8.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[2, 4] = -3.0f / 8.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[4, 4] = 3.0f / 8.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[4, 2] = -3.0f / 8.0f / setInfo.DxFloat / setInfo.DxFloat;
                    break;
                case TurbulenceOptions.SpatialInterpolation.None_Fd8:
                    this.kernelSize = 8;
                    CenteredFiniteDiffCoeff = new float[9, 9];
                    for (int j = 0; j < 5; j++)
                        for (int i = 0; i < 5; i++)
                            CenteredFiniteDiffCoeff[i, j] = 0.0f;

                    CenteredFiniteDiffCoeff[4, 0] = 9.0f / 3152.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[4, 1] = -104.0f / 8865.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[4, 2] = -207.0f / 2955.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[4, 3] = 792.0f / 591.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[4, 4] = -35777.0f / 14184.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[4, 5] = 792.0f / 591.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[4, 6] = -207.0f / 2955.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[4, 7] = -104.0f / 8865.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[4, 8] = 9.0f / 3152.0f / setInfo.DxFloat / setInfo.DxFloat;


                    CenteredFiniteDiffCoeff[0, 0] = -1.0f / 2240.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[0, 8] = 1.0f / 2240.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[8, 8] = -1.0f / 2240.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[8, 0] = 1.0f / 2240.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[1, 1] = 2.0f / 315.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[1, 7] = -2.0f / 315.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[7, 7] = 2.0f / 315.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[7, 1] = -2.0f / 315.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[2, 2] = -1.0f / 20.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[2, 6] = 1.0f / 20.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[6, 6] = -1.0f / 20.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[6, 2] = 1.0f / 20.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[3, 3] = 14.0f / 35.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[3, 5] = -14.0f / 35.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[5, 5] = 14.0f / 35.0f / setInfo.DxFloat / setInfo.DxFloat;
                    CenteredFiniteDiffCoeff[5, 3] = -14.0f / 35.0f / setInfo.DxFloat / setInfo.DxFloat;
                    break;
                case TurbulenceOptions.SpatialInterpolation.Fd4Lag4:
                    this.kernelSize = 4;
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
                new SqlMetaData("d2pdxdx", SqlDbType.Real),
                new SqlMetaData("d2pdxdy", SqlDbType.Real),
                new SqlMetaData("d2pdxdz", SqlDbType.Real),
                new SqlMetaData("d2pdydy", SqlDbType.Real),
                new SqlMetaData("d2pdydz", SqlDbType.Real),
                new SqlMetaData("d2pdzdz", SqlDbType.Real),
                new SqlMetaData("Cubes Read", SqlDbType.Int) };
        }

        //public override void GetAtomsForPoint(float xp, float yp, float zp, long mask, HashSet<long> atoms)
        //{
        //    int X, Y, Z;
        //    int[] x_values;
        //    int[] y_values;
        //    int[] z_values;

        //    if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Fd4Lag4)
        //    {
        //        X = LagInterpolation.CalcNode(xp, setInfo.dx);
        //        Y = LagInterpolation.CalcNode(yp, setInfo.dx);
        //        Z = LagInterpolation.CalcNode(zp, setInfo.dx);

        //        // For Lagrange Polynomial interpolation and the computation of the mixed derivatives we need a cube of data
        //        // For 4^3 we have to check 3 points in each dimension (the corners and the middle point)
        //        // For 8^3 and larger we would only have to check the corners
        //        x_values = new int[] { X - kernelSize / 2 + 1, X + kernelSize / 2 };
        //        y_values = new int[] { Y - kernelSize / 2 + 1, Y + kernelSize / 2 };
        //        z_values = new int[] { Z - kernelSize / 2 + 1, Z + kernelSize / 2 };
        //    }
        //    else
        //    {
        //        X = LagInterpolation.CalcNodeWithRound(xp, setInfo.dx);
        //        Y = LagInterpolation.CalcNodeWithRound(yp, setInfo.dx);
        //        Z = LagInterpolation.CalcNodeWithRound(zp, setInfo.dx);

        //        // In this case we are not performing Lagrange Polynomial interpolation 
        //        // and we need a different sized cube of data points
        //        // For 4^3 we have to check 3 points in each dimension (the corners and the middle point)
        //        // For 8^3 and larger we would only have to check the corners
        //        x_values = new int[] { X - kernelSize / 2, X + kernelSize / 2 };
        //        y_values = new int[] { Y - kernelSize / 2, Y + kernelSize / 2 };
        //        z_values = new int[] { Z - kernelSize / 2, Z + kernelSize / 2 };
        //    }

        //    long zindex;

        //    foreach (int x in x_values)
        //    {
        //        foreach (int y in y_values)
        //        {
        //            foreach (int z in z_values)
        //            {
        //                // Wrap the coordinates into the grid space
        //                int xi = ((x % setInfo.GridResolution) + setInfo.GridResolution) % setInfo.GridResolution;
        //                int yi = ((y % setInfo.GridResolution) + setInfo.GridResolution) % setInfo.GridResolution;
        //                int zi = ((z % setInfo.GridResolution) + setInfo.GridResolution) % setInfo.GridResolution;

        //                zindex = new Morton3D(zi, yi, xi).Key & mask;
        //                if (!atoms.Contains(zindex))
        //                {
        //                    atoms.Add(zindex);
        //                }
        //            }
        //        }
        //    }
        //}
        
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

        public override float[] GetResult(TurbulenceBlob blob, SQLUtility.InputRequest input)
        {
            throw new NotImplementedException();
        }

        public override float[] GetResult(TurbulenceBlob blob, SQLUtility.MHDInputRequest input)
        {
            float xp = input.x;
            float yp = input.y;
            float zp = input.z; 
            return CalcHessian(blob, xp, yp, zp, input);
        }

        public override int GetResultSize()
        {
            return 6;
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
        unsafe public float[] CalcHessian(TurbulenceBlob blob, float xp, float yp, float zp, SQLUtility.MHDInputRequest input)
        {
            float[] result = new float[GetResultSize()]; // Result value for the user
            for (int i = 0; i < GetResultSize(); i++)
                result[i] = 0.0f;

            float[] data = blob.data;

            int nOrder = 0;
            int length = 0;

            int x, y, z;
            int startz = 0, starty = 0, startx = 0, endz = 0, endy = 0, endx = 0;
            int iLagIntx = 0, iLagInty = 0, iLagIntz = 0;

            switch (spatialInterp)
            {
                #region SpatialInterpolation None_Fd4/6/8
                case TurbulenceOptions.SpatialInterpolation.None_Fd4:
                    // nOrder will be:
                    // 4 if spatialInterp is None_Fd4
                    // 6 if it is None_Fd6
                    // 8 if it is None_Fd8
                    nOrder += 4;
                    length = nOrder / 2;
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

                    fixed (float* FDCoeff = CenteredFiniteDiffCoeff, fdata = data)
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
                                // This is why we add (nOrder + 1) * (nOrder / 2) to the offset
                                float coeff = FDCoeff[iLagIntx + ix - startx + (nOrder + 1) * (nOrder / 2)];
                                result[0] += coeff * fdata[off];
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
                                float coeff = FDCoeff[iLagInty + iy - starty + (nOrder + 1) * (nOrder / 2)];
                                result[3] += coeff * fdata[off];
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
                                float coeff = FDCoeff[iLagIntz + iz - startz + (nOrder + 1) * (nOrder / 2)];
                                result[5] += coeff * fdata[off];
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
                                if (LagIntIndexY != nOrder / 2)
                                {
                                    // For the Turb dataset Vx, Vy, Vz ARE stored together
                                    off = startx * blob.GetComponents + iy * blob.GetSide * blob.GetComponents + (z - blob.GetRealZ) * blob.GetSide * blob.GetSide * blob.GetComponents;
                                    for (int ix = startx; ix <= endx; ix++)
                                    {
                                        // Since FDCoeff is now a pointer we need to index into the one dimensional representation
                                        // of the 2d array
                                        float coeff = FDCoeff[iLagIntx + ix - startx + LagIntIndexY * (nOrder + 1)];
                                        if (coeff != 0.0f)
                                        {
                                            result[1] += coeff * fdata[off];
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
                                if (LagIntIndexZ != nOrder / 2)
                                {
                                    off = startx * blob.GetComponents + (y - blob.GetRealY) * blob.GetSide * blob.GetComponents + iz * blob.GetSide * blob.GetSide * blob.GetComponents;
                                    for (int ix = startx; ix <= endx; ix++)
                                    {
                                        float coeff = FDCoeff[iLagIntx + ix - startx + LagIntIndexZ * (nOrder + 1)];
                                        //float coeff = CenteredFiniteDiffCoeff[iLagIntx + ix - startx, iLagIntz + iz - startz];
                                        if (coeff != 0.0f)
                                        {
                                            result[2] += coeff * fdata[off];
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
                                if (LagIntIndexZ != nOrder / 2)
                                {
                                    off = (x - blob.GetRealX) * blob.GetComponents + starty * blob.GetSide * blob.GetComponents + iz * blob.GetSide * blob.GetSide * blob.GetComponents;
                                    for (int iy = starty; iy <= endy; iy++)
                                    {
                                        float coeff = FDCoeff[iLagInty + iy - starty + LagIntIndexZ * (nOrder + 1)];
                                        //float coeff = CenteredFiniteDiffCoeff[iLagInty + iy - starty, iLagIntz + iz - startz];
                                        if (coeff != 0.0f)
                                        {
                                            result[4] += coeff * fdata[off];
                                        }
                                        off += blob.GetSide * blob.GetComponents;
                                    }
                                }
                            }
                        } 
                        #endregion
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.None_Fd6:
                    nOrder += 2;
                    goto case TurbulenceOptions.SpatialInterpolation.None_Fd4;
                case TurbulenceOptions.SpatialInterpolation.None_Fd8:
                    nOrder += 4;
                    goto case TurbulenceOptions.SpatialInterpolation.None_Fd4;
                #endregion
                case TurbulenceOptions.SpatialInterpolation.Fd4Lag4:
                    nOrder = 4;
                    length = nOrder / 2;

                    x = LagInterpolation.CalcNode(xp, setInfo.Dx);
                    y = LagInterpolation.CalcNode(yp, setInfo.Dx);
                    z = LagInterpolation.CalcNode(zp, setInfo.Dx);
                    
                    // The coefficients are computed only once and cached, so that they don't have to be 
                    // recomputed for each partial sum                    
                    if (input.lagInt == null)
                    {
                        input.lagInt = new double[nOrder * 3];

                        LagInterpolation.InterpolantN(nOrder, xp, x, setInfo.Dx, lagDenominator, 0, input.lagInt);
                        LagInterpolation.InterpolantN(nOrder, yp, y, setInfo.Dy, lagDenominator, 1, input.lagInt);
                        LagInterpolation.InterpolantN(nOrder, zp, z, setInfo.Dz, lagDenominator, 2, input.lagInt);
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
                    // E.g. for the computation of duxdxdx (or duydxdx, duzdxdx, since here ux, uy, uz are the components of the velocity)
                    // we need the part of the cube from [2,2,0] to [5,5,7], 
                    // while for the computation of duxdydy (or duydydy, duzdydy)
                    // we need the part of the cube from [2,0,2] to [5,7,5], etc.

                    // The lagrange interpolation of the mixed derivatives is executed over planer patches
                    // since for the kernel of the mixed derivative is a planer patch
                    // For example, in order to compute duxdxdy we need the following 8 points (in the x-y plane)
                    // [0,0], [0,4], [1,1], [1,3], [3,1], [3,3], [4,0], [4,4] 
                    // the z-coordinate ranges from 2 to 5
                    // Such planer patches will be interpolated according to the lagrange polynomial computation

                    // Since the given blob may not hold all of the required data
                    // we determine where to start and end the partial computation
                    blob.GetSubcubeStart(z - 3, y - 3, x - 3, ref startz, ref starty, ref startx);
                    blob.GetSubcubeEnd(z + 4, y + 4, x + 4, ref endz, ref endy, ref endx);
                
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

                    fixed (double* lagint = input.lagInt)
                    {
                        fixed (float* FDCoeff = CenteredFiniteDiffCoeff, fdata = data)
                        {
                            double ax = 0.0F, ay = 0.0F, az = 0.0F,
                                axy = 0.0F, axz = 0.0F, ayz = 0.0F;
                            int off0 = startx * blob.GetComponents;

                            // Since the finite differencing coefficients are stored in a 2d array
                            // we need to offset to the middle row when computing the unmixed derivatives
                            int FDOff = (nOrder + 1) * (nOrder / 2);
                            for (int iz = startz; iz <= endz; iz++)
                            {
                                double bx = 0.0F, by = 0.0F, bz = 0.0F,
                                    bxy = 0.0F, bxz = 0.0F, byz = 0.0F;
                                int LagIntIndexZ = iLagIntz + iz - startz;
                                int off1 = off0 + iz * blob.GetSide * blob.GetSide * blob.GetComponents;
                                for (int iy = starty; iy <= endy; iy++)
                                {
                                    double cx = 0.0F, cy = 0.0F, cz = 0.0F,
                                        cxy = 0.0F, cxz = 0.0F, cyz = 0.0F;
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
                                            // Each point may be part of up to nOrder interplation kernels
                                            // Therefore, we check each possible kernel and if the points falls inside it
                                            // we update the partial sum accordingly
                                            for (int i = 0; i < nOrder; i++)
                                            {
                                                // The kernel index ranges from 0 to 4 for 4th-order finite differencing
                                                // NOTE: nOrder is the Lagrange polynomial interpolation order
                                                if (LagIntIndexX - i >= 0 && LagIntIndexX - i <= 4)
                                                {
                                                    double c = lagint[i] * FDCoeff[LagIntIndexX - i + FDOff];
                                                    cx += c * fdata[off];
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
                                            cy += c * fdata[off];
                                        }
                                        #endregion
                                        #region d2uidzdz
                                        // Same as for d2uidxdx, with the exception that we don't check at this point
                                        // which kernel the point falls into, since these are lines in the z-direction
                                        if (LagIntIndexX >= 2 && LagIntIndexX <= 5 && LagIntIndexY >= 2 && LagIntIndexY <= 5)
                                        {
                                            double c = lagint[LagIntIndexX - 2];
                                            cz += c * fdata[off];
                                        }
                                        #endregion
                                        #region d2uidxdy
                                        // the Z dimension ranges from the 2nd to the 5th row in the cube
                                        // since for the 4th-order Lagrange Polynomial interpolation we need the 4 nearest rows in each dim.
                                        // to the target point
                                        if (LagIntIndexZ >= 2 && LagIntIndexZ <= 5)
                                        {
                                            // Each point may be part of more than one interplation kernels
                                            // Therefore, we check each possible kernel and if the points falls inside it
                                            // we update the partial sum accordingly (in this case the kernels are planer patches)
                                            for (int j = 0; j < nOrder; j++)
                                            {
                                                // The kernel index ranges from 0 to 4 for 4th-order finite differencing
                                                // the middle row does not play a role in the evaluation of the mixed derivative
                                                // NOTE: nOrder is the Lagrange polynomial interpolation order
                                                if (LagIntIndexY - j >= 0 && LagIntIndexY - j <= 4 && LagIntIndexY - j != 2)
                                                {
                                                    for (int i = 0; i < nOrder; i++)
                                                    {
                                                        if (LagIntIndexX - i >= 0 && LagIntIndexX - i <= 4)
                                                        {
                                                            double c = FDCoeff[LagIntIndexX - i + (LagIntIndexY - j) * (nOrder + 1)];
                                                            if (c != 0.0f)
                                                            {
                                                                c *= lagint[1 * nOrder + j] * lagint[i];
                                                                cxy += c * fdata[off];
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
                                            for (int j = 0; j < nOrder; j++)
                                            {
                                                if (LagIntIndexZ - j >= 0 && LagIntIndexZ - j <= 4 && LagIntIndexZ - j != 2)
                                                {
                                                    for (int i = 0; i < nOrder; i++)
                                                    {
                                                        if (LagIntIndexX - i >= 0 && LagIntIndexX - i <= 4)
                                                        {
                                                            double c = FDCoeff[LagIntIndexX - i + (LagIntIndexZ - j) * (nOrder + 1)];
                                                            if (c != 0.0f)
                                                            {
                                                                c *= lagint[2 * nOrder + j] * lagint[i];
                                                                cxz += c * fdata[off];
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
                                            cyz += c * fdata[off];
                                        }
                                        #endregion
                                        off += blob.GetComponents;
                                    }
                                    if (LagIntIndexY >= 2 && LagIntIndexY <= 5)
                                    {
                                        double b = lagint[1 * nOrder + LagIntIndexY - 2];
                                        bx += cx * b;
                                        bz += cz * b;
                                        bxz += cxz * b;
                                    }
                                    // for d2uidxdy we have already multiplied the partial sum
                                    // by the y-coefficients
                                    bxy += cxy;
                                    #region d2uidydy and d2uidydz
                                    // The 2 computation have been merged into 1 since they share the same outer loop
                                    for (int i = 0; i < nOrder; i++)
                                    {
                                        // The kernel index ranges from 0 to 4 for 4th-order finite differencing
                                        // NOTE: nOrder is the Lagrange polynomial interpolation order
                                        if (LagIntIndexY - i >= 0 && LagIntIndexY - i <= 4)
                                        {
                                            // First, we compute d2uidydy
                                            double c = lagint[1 * nOrder + i] * FDCoeff[LagIntIndexY - i + FDOff];
                                            by += c * cy;
                                            // Next, we compute d2uidydz
                                            for (int j = 0; j < nOrder; j++)
                                            {
                                                if (LagIntIndexZ - j >= 0 && LagIntIndexZ - j <= 4 && LagIntIndexZ - j != 2)
                                                {
                                                    c = FDCoeff[LagIntIndexY - i + (LagIntIndexZ - j) * (nOrder + 1)];
                                                    if (c != 0.0f)
                                                    {
                                                        c *= lagint[2 * nOrder + j] * lagint[1 * nOrder + i];
                                                        byz += c * cyz;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    #endregion
                                }
                                if (LagIntIndexZ >= 2 && LagIntIndexZ <= 5)
                                {
                                    double a = lagint[2 * nOrder + LagIntIndexZ - 2];
                                    ax += bx * a;
                                    ay += by * a;
                                    axy += bxy * a;
                                }
                                // for d2uidxdz we have already multiplied the partial sum
                                // by the z-coefficients
                                axz += bxz;
                                ayz += byz;
                                #region d2uidzdz
                                for (int i = 0; i < nOrder; i++)
                                {
                                    // The kernel index ranges from 0 to 4 for 4th-order finite differencing
                                    // NOTE: nOrder is the Lagrange polynomial interpolation order
                                    if (LagIntIndexZ - i >= 0 && LagIntIndexZ - i <= 4)
                                    {
                                        double c = lagint[2 * nOrder + i] * FDCoeff[LagIntIndexZ - i + FDOff];
                                        az += c * bz;
                                    }
                                }
                                #endregion
                            }
                            result[0] = (float)ax;
                            result[1] = (float)axy;
                            result[2] = (float)axz;
                            result[3] = (float)ay;
                            result[4] = (float)ayz;
                            result[5] = (float)az;
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
