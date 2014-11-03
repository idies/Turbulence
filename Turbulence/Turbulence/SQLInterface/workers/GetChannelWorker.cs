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
    public abstract class GetChannelWorker : Worker
    {
        protected BarycentricWeights weights_x;
        protected BarycentricWeights weights_y;
        protected BarycentricWeights weights_z;

        int numPointsInKernel = 0;
        
        public GetChannelWorker(TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp,
            SqlConnection conn)
        {
            this.kernelSize = 0;
            this.setInfo = setInfo;
            this.spatialInterp = spatialInterp;

            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Lag4)
            {
                this.kernelSize = 4;
                SetWeightsX(conn, "barycentric_weights_x_4");
                SetWeightsY(conn, "barycentric_weights_y_4");
                SetWeightsZ(conn, "barycentric_weights_z_4");
            }
            else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Lag6)
            {
                this.kernelSize = 6;
                SetWeightsX(conn, "barycentric_weights_x_6");
                SetWeightsY(conn, "barycentric_weights_y_6");
                SetWeightsZ(conn, "barycentric_weights_z_6");
            }
            else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Lag8)
            {
                this.kernelSize = 8;
                SetWeightsX(conn, "barycentric_weights_x_8");
                SetWeightsY(conn, "barycentric_weights_y_8");
                SetWeightsZ(conn, "barycentric_weights_z_8");
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

        private void SetWeightsX(SqlConnection conn, string tableName)
        {
            weights_x = new UniformBarycentricWeights();
            weights_x.GetWeightsFromDB(conn, tableName);
        }
        private void SetWeightsY(SqlConnection conn, string tableName)
        {
            weights_y = new NonUniformBarycentricWeights();
            weights_y.GetWeightsFromDB(conn, tableName);
        }
        private void SetWeightsZ(SqlConnection conn, string tableName)
        {
            weights_z = new UniformBarycentricWeights();
            weights_z.GetWeightsFromDB(conn, tableName);
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

            // For Lagrange Polynomial interpolation we need a cube of data 

            int startz = weights_z.GetStencilStart(request.cell_z, kernelSize), 
                starty = weights_y.GetStencilStart(request.cell_y, kernelSize), 
                startx = weights_x.GetStencilStart(request.cell_x, kernelSize);
            int endz = startz + kernelSize - 1, endy = weights_y.GetStencilEnd(request.cell_y), endx = startx + kernelSize - 1;

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
                        // NOTE: We shouldn't need to wrap y.
                        int yi = ((y % setInfo.GridResolutionY) + setInfo.GridResolutionY) % setInfo.GridResolutionY;
                        int zi = ((z % setInfo.GridResolutionZ) + setInfo.GridResolutionZ) % setInfo.GridResolutionZ;

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
        public override HashSet<SQLUtility.PartialResult> GetThresholdUsingCutout(BigArray<float> cutout, int[] cutout_coordinates, int[] coordiantes, double threshold)
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
                            point.result[c] = cutout[sourceIndex + c];
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
        public override HashSet<SQLUtility.PartialResult> GetThresholdUsingCutout(float[] cutout, int[] cutout_coordinates, int[] coordiantes, double threshold)
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
                            point.result[c] = cutout[sourceIndex + c];
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
