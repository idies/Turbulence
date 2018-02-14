using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Turbulence.TurbLib;
using Turbulence.SciLib;
using Turbulence.SQLInterface;
using System.Collections.Generic;

using System.Diagnostics;

namespace Turbulence.SQLInterface.workers
{
    public class GetMHDWorker : Worker
    {
        double[] lagDenominator = null;
        int numPointsInKernel = 0;

        public GetMHDWorker(TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp)
        {
            this.kernelSize = 0;
            this.setInfo = setInfo;
            this.spatialInterp = spatialInterp;

            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Lag4)
            {
                this.kernelSize = 4;
                lagDenominator = new double[4];
                LagInterpolation.InterpolantDenominator(4, lagDenominator);
            }
            else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Lag6)
            {
                this.kernelSize = 6;
                lagDenominator = new double[6];
                LagInterpolation.InterpolantDenominator(6, lagDenominator);
            }
            else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Lag8)
            {
                this.kernelSize = 8;
                lagDenominator = new double[8];
                LagInterpolation.InterpolantDenominator(8, lagDenominator);
            }
            else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None)
            {
                kernelSize = 0;
                numPointsInKernel = 1;
                return;
                // do nothing
            }
            else
            {
                throw new Exception(String.Format("Invalid Spatial Interpolation Option: {0}", spatialInterp));
            }
            numPointsInKernel = kernelSize * kernelSize * kernelSize;
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

        /// <summary>
        /// Determines the database atoms that overlap the kernel of computation for the given point
        /// </summary>
        /// <remarks>
        /// This methods should only need to be called when spatial interpolation is performed
        /// i.e. when kernelSize != 0
        /// </remarks>
        public override void GetAtomsForPoint(SQLUtility.MHDInputRequest request, long mask, int pointsPerCubeEstimate, Dictionary<long, List<int>> map, ref int total_points)
        {
            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None)
                throw new Exception("GetAtomsForPoint should only be called when spatial interpolation is performed!");

            int X, Y, Z;
            X = LagInterpolation.CalcNode(request.x, setInfo.Dx);
            Y = LagInterpolation.CalcNode(request.y, setInfo.Dx);
            Z = LagInterpolation.CalcNode(request.z, setInfo.Dx);
            // For Lagrange Polynomial interpolation we need a cube of data 
            
            int startz = Z - kernelSize / 2 + 1, starty = Y - kernelSize / 2 + 1, startx = X - kernelSize / 2 + 1;
            int endz = Z + kernelSize / 2, endy = Y + kernelSize / 2, endx = X + kernelSize / 2;

            // we do not want a request to appear more than once in the list for an atom
            // with the below logic we are going to check distinct atoms only
            // we want to start at the start of a DB atom
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
            return CalcLagInterpolation(blob, xp, yp, zp, ref input.lagInt);
        }

        public override int GetResultSize()
        {
            return 3;
        }


        /// <summary>
        /// New version of the CalcVelocity function (also used for magnetic field and vector potential)
        /// </summary>
        /// <remarks>
        /// The Lagrangian evaluation function [LagInterpolation.EvaluateOpt] was moved
        /// into the function and some loop unrolling was performed.
        /// </remarks>
        unsafe public double[] CalcLagInterpolation(TurbulenceBlob blob, float xp, float yp, float zp, ref double[] lagInt)
        {
            double[] up = new double[3]; // Result value for the user

            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None)
            {
                int xi = LagInterpolation.CalcNodeWithRound(xp, setInfo.Dx);
                int yi = LagInterpolation.CalcNodeWithRound(yp, setInfo.Dy);
                int zi = LagInterpolation.CalcNodeWithRound(zp, setInfo.Dz);

                float[] data = blob.data;
                int off0 = blob.GetLocalOffsetMHD(zi, yi, xi, 0);
                up[0] = data[off0];
                up[1] = data[off0 + 1];
                up[2] = data[off0 + 2];
            }
            else
            {
                int x = LagInterpolation.CalcNode(xp, setInfo.Dx);
                int y = LagInterpolation.CalcNode(yp, setInfo.Dy);
                int z = LagInterpolation.CalcNode(zp, setInfo.Dz);

                // The coefficients are computed only once and cached, so that they don't have to be 
                // recomputed for each partial sum
                if (lagInt == null)
                {
                    lagInt = new double[kernelSize * 3];

                    LagInterpolation.InterpolantN(kernelSize, xp, x, setInfo.Dx, lagDenominator, 0, lagInt);
                    LagInterpolation.InterpolantN(kernelSize, yp, y, setInfo.Dy, lagDenominator, 1, lagInt);
                    LagInterpolation.InterpolantN(kernelSize, zp, z, setInfo.Dz, lagDenominator, 2, lagInt);
                }

                // Wrap the coordinates into the grid space
                x = ((x % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
                y = ((y % setInfo.GridResolutionY) + setInfo.GridResolutionY) % setInfo.GridResolutionY;
                z = ((z % setInfo.GridResolutionZ) + setInfo.GridResolutionZ) % setInfo.GridResolutionZ;

                float[] data = blob.data;
                //int off0 = blob.GetLocalOffset(z - (kernelSize / 2), y - (kernelSize / 2), x - (kernelSize / 2), 0);
                int startz = 0, starty = 0, startx = 0, endz = 0, endy = 0, endx = 0;
                blob.GetSubcubeStart(z - (kernelSize / 2) + 1, y - (kernelSize / 2) + 1, x - (kernelSize / 2) + 1, ref startz, ref starty, ref startx);
                blob.GetSubcubeEnd(z + (kernelSize / 2), y + (kernelSize / 2), x + (kernelSize / 2), ref endz, ref endy, ref endx);
                int off0 = startx * setInfo.Components;

                //int iLagInt;
                int iLagIntx = blob.GetRealX - x + startx + kernelSize / 2 - 1;
                //iLagIntx = ((iLagIntx % blob.GetGridResolutionX) + blob.GetGridResolutionX) % blob.GetGridResolutionX;
                if (iLagIntx >= setInfo.GridResolutionX)
                    iLagIntx -= setInfo.GridResolutionX;
                else if (iLagIntx < 0)
                    iLagIntx += setInfo.GridResolutionX;
                int iLagInty = blob.GetRealY - y + starty + kernelSize / 2 - 1;
                //iLagInty = ((iLagInty % setInfo.GridResolutionY) + setInfo.GridResolutionY) % setInfo.GridResolutionY;
                if (iLagInty >= setInfo.GridResolutionY)
                    iLagInty -= setInfo.GridResolutionY;
                else if (iLagInty < 0)
                    iLagInty += setInfo.GridResolutionY;
                int iLagIntz = blob.GetRealZ - z + startz + kernelSize / 2 - 1;
                //iLagIntz = ((iLagIntz % setInfo.GridResolutionZ) + setInfo.GridResolutionZ) % setInfo.GridResolutionZ;
                if (iLagIntz >= setInfo.GridResolutionZ)
                    iLagIntz -= setInfo.GridResolutionZ;
                else if (iLagIntz < 0)
                    iLagIntz += setInfo.GridResolutionZ;

                fixed (double* lagint = lagInt)
                {
                    fixed (float* fdata = data)
                    {
                        double a1 = 0.0, a2 = 0.0, a3 = 0.0;
                        for (int iz = startz; iz <= endz; iz++)
                        {
                            double b1 = 0.0, b2 = 0.0, b3 = 0.0;
                            int off1 = off0 + iz * blob.GetSide * blob.GetSide * blob.GetComponents;
                            for (int iy = starty; iy <= endy; iy++)
                            {
                                double c1 = 0.0, c2 = 0.0, c3 = 0.0;
                                int off = off1 + iy * blob.GetSide * blob.GetComponents;
                                for (int ix = startx; ix <= endx; ix++)
                                {
                                    //int off = (ix + iy * blob.GetSide + iz * blob.GetSide * blob.GetSide) * blob.GetComponents;
                                    //need to determine the distance from the point, on which we are centered
                                    //iLagInt = blob.GetRealX + ix - x + kernelSize / 2 - 1;
                                    //iLagInt = ((iLagInt % blob.GetGridResolution) + blob.GetGridResolution) % blob.GetGridResolution;
                                    //double c = lagint[iLagInt];
                                    double c = lagint[iLagIntx + ix - startx];
                                    c1 += c * fdata[off];
                                    c2 += c * fdata[off + 1];
                                    c3 += c * fdata[off + 2];
                                    off += blob.GetComponents;
                                }
                                //need to determine the distance from the point, on which we are centered
                                //iLagInt = blob.GetRealY + iy - y + kernelSize / 2 - 1;
                                //iLagInt = ((iLagInt % blob.GetGridResolution) + blob.GetGridResolution) % blob.GetGridResolution;
                                //double b = lagint[1 * kernelSize + iLagInt];
                                double b = lagint[1 * kernelSize + iLagInty + iy - starty];
                                b1 += c1 * b;
                                b2 += c2 * b;
                                b3 += c3 * b;
                            }
                            //need to determine the distance from the point, on which we are centered
                            //iLagInt = blob.GetRealZ + iz - z + kernelSize / 2 - 1;
                            //iLagInt = ((iLagInt % blob.GetGridResolution) + blob.GetGridResolution) % blob.GetGridResolution;
                            //double a = lagint[2 * kernelSize + iLagInt];
                            double a = lagint[2 * kernelSize + iLagIntz + iz - startz];
                            a1 += b1 * a;
                            a2 += b2 * a;
                            a3 += b3 * a;
                        }
                        up[0] = a1;
                        up[1] = a2;
                        up[2] = a3;
                    }
                }
            }
            return up;
        }
        
        /// <summary>
        /// Obtain the norm of the field at each point on the grid specified by the coordinates parameter from the given cutout.
        /// Each point that has a norm higher than the given threshold is stored in the set and the set is returned.
        /// The coordinates and the cutout_coordinates are expected to be identical.
        /// NOTE: Values are not interpolated as the target locations are on grid nodes.
        /// </summary>
        /// <param name="cutout"></param>
        /// <param name="cutout_coordiantes"></param>
        /// <param name="coordiantes"></param>
        /// <param name="threshold"></param>
        /// <returns></returns>
        public override HashSet<SQLUtility.PartialResult> GetThresholdUsingCutout(int[] coordiantes, double threshold, int workertype)
        {
            if (spatialInterp != TurbulenceOptions.SpatialInterpolation.None)
            {
                throw new Exception("Invalid interpolation option specified!");
            }
            for (int i = 0; i < cutout_coordinates.Length; i++)
            {
                if (cutout_coordinates[i] != coordiantes[i])
                {
                    throw new Exception("Specified coordinates and cutout coordinates are not identical!");
                }
            }

            int[] cutout_dimensions = new int[] { cutout_coordinates[5] - cutout_coordinates[2],
                                                  cutout_coordinates[4] - cutout_coordinates[1],
                                                  cutout_coordinates[3] - cutout_coordinates[0] };

            HashSet<SQLUtility.PartialResult> points_above_threshold = new HashSet<SQLUtility.PartialResult>();
            SQLUtility.PartialResult point;
            long zindex = 0;
            for (int z = coordiantes[2]; z < coordiantes[5]; z++)
            {
                for (int y = coordiantes[1]; y < coordiantes[4]; y++)
                {
                    for (int x = coordiantes[0]; x < coordiantes[3]; x++)
                    {
                        zindex = new Morton3D(z, y, x);
                        ulong sourceIndex = (((ulong)z - (ulong)cutout_coordinates[2]) * (ulong)cutout_dimensions[2] * (ulong)cutout_dimensions[1] +
                            ((ulong)y - (ulong)cutout_coordinates[1]) * (ulong)cutout_dimensions[2] +
                            ((ulong)x - (ulong)cutout_coordinates[0])) * (ulong)setInfo.Components;

                        point = new SQLUtility.PartialResult(zindex, GetResultSize(), numPointsInKernel);
                        for (ulong c = 0; c < (ulong)setInfo.Components; c++)
                        {
                            point.result[c] = GetDataItem(sourceIndex + c);
                        }

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

    }

}
