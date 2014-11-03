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
    // TODO: Consider creating an abstract GetDerivative class,
    // from which both Gradient and Hessian can inherit.
    // If we change how the hydro and MHD derivatives are computed
    // (store FD coefficients in the DB) common functionality
    // can be moved to a parent abstract class.
    public class GetChannelLaplacian : Worker
    {
        protected BarycentricWeights weights_x;
        protected BarycentricWeights weights_y;
        protected BarycentricWeights weights_z;
        protected BarycentricWeights diff_matrix_x;
        protected BarycentricWeights diff_matrix_y;
        protected BarycentricWeights diff_matrix_z;

        private int LagIntOrder;
        private int FdOrder;
        // For the non-uniform y direction the size of the y-kernel of computation
        // is different than that of the uniform x and z directions.
        //private int kernelSizeY;
        private int FdOrderY;

        public GetChannelLaplacian(TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp,
            SqlConnection conn)
        {
            // In the case of Fd4Lag4 the computation is a combination of 
            // two computations (Finite Differencing and Lag. Interpolation)
            // The size of the kernel is the combined size of the two kernels
            this.setInfo = setInfo;
            this.spatialInterp = spatialInterp;

            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None_Fd4)
            {
                this.kernelSize = 5;
                //this.kernelSizeY = 6;
                this.FdOrder = 4;
                this.FdOrderY = 5;
                diff_matrix_x = GetUniformDiffMatrix(conn, "diff_matrix_x_r2_fd4");
                diff_matrix_y = GetNonuniformWeights(conn, "diff_matrix_y_r2_fd4");
                diff_matrix_z = GetUniformDiffMatrix(conn, "diff_matrix_z_r2_fd4");
            }
            else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None_Fd6)
            {
                this.kernelSize = 7;
                //this.kernelSizeY = 8;
                this.FdOrder = 6;
                this.FdOrderY = 7;
                diff_matrix_x = GetUniformDiffMatrix(conn, "diff_matrix_x_r2_fd6");
                diff_matrix_y = GetNonuniformWeights(conn, "diff_matrix_y_r2_fd6");
                diff_matrix_z = GetUniformDiffMatrix(conn, "diff_matrix_z_r2_fd6");
            }
            else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None_Fd8)
            {
                this.kernelSize = 9;
                //this.kernelSizeY = 10;
                this.FdOrder = 8;
                this.FdOrderY = 9;
                diff_matrix_x = GetUniformDiffMatrix(conn, "diff_matrix_x_r2_fd8");
                diff_matrix_y = GetNonuniformWeights(conn, "diff_matrix_y_r2_fd8");
                diff_matrix_z = GetUniformDiffMatrix(conn, "diff_matrix_z_r2_fd8");
            }
            else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Fd4Lag4)
            {
                // kernel size is the size of the FD kernel + the size of the Lagrange interpolation kernel - 1 = 5 + 4 - 1
                // for y it is 6 + 4 - 1
                this.kernelSize = 8;
                //this.kernelSizeY = 9;
                this.FdOrder = 4;
                this.FdOrderY = 5;
                this.LagIntOrder = 4;
                weights_x = GetUniformWeights(conn, "barycentric_weights_x_4");
                weights_y = GetNonuniformWeights(conn, "barycentric_weights_y_4");
                weights_z = GetUniformWeights(conn, "barycentric_weights_z_4");

                diff_matrix_x = GetUniformDiffMatrix(conn, "diff_matrix_x_r2_fd4");
                diff_matrix_y = GetNonuniformWeights(conn, "diff_matrix_y_r2_fd4");
                diff_matrix_z = GetUniformDiffMatrix(conn, "diff_matrix_z_r2_fd4");
            }
            else
            {
                throw new Exception(String.Format("Invalid Spatial Interpolation Option: {0}", spatialInterp));
            }
        }

        private BarycentricWeights GetUniformDiffMatrix(SqlConnection conn, string tableName)
        {
            BarycentricWeights uniform_diff_matrix = new UniformDifferentiationMatrix();
            uniform_diff_matrix.GetWeightsFromDB(conn, tableName);
            return uniform_diff_matrix;
        }
        private BarycentricWeights GetUniformWeights(SqlConnection conn, string tableName)
        {
            BarycentricWeights uniform_weights = new UniformBarycentricWeights();
            uniform_weights.GetWeightsFromDB(conn, tableName);
            return uniform_weights;
        }
        private BarycentricWeights GetNonuniformWeights(SqlConnection conn, string tableName)
        {
            BarycentricWeights nonuniform_weights = new NonUniformBarycentricWeights();
            nonuniform_weights.GetWeightsFromDB(conn, tableName);
            return nonuniform_weights;
        }

        public override SqlMetaData[] GetRecordMetaData()
        {
            return new SqlMetaData[] {
            new SqlMetaData("Req", SqlDbType.Int),
            new SqlMetaData("grad2ux", SqlDbType.Real),
            new SqlMetaData("grad2uy", SqlDbType.Real),
            new SqlMetaData("grad2uz", SqlDbType.Real), };
        }

        /// <summary>
        /// Determines the database atoms that overlap the kernel of computation for the given point
        /// </summary>
        /// <remarks>
        /// Similar to the way GetAtoms for GetChannelGradient works, with the difference
        /// that because of the mixed derivatives the FD kernels are planar patches as opposed to line segments
        /// </remarks>
        public override void GetAtomsForPoint(SQLUtility.MHDInputRequest request, long mask, int pointsPerCubeEstimate, Dictionary<long, List<int>> map, ref int total_points)
        {
            int startz, starty, startx, endz, endy, endx;
            HashSet<long> atoms = new HashSet<long>(); //NOTE: HashSet requires .Net 3.5

            // The following computation will be repeated 3 times
            // Once for each of the 3 spatial dimensions
            // This is necessary because the kernel of computation is a box/line segment
            // with different dimensions and not a cube

            // We start of with the kernel/stencil for d2udxdx.
            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Fd4Lag4)
            {
                // The computation has 2 componnets -- differentiation and interpolation
                // We get the start of the differentiation stencil and using that we get the start of the overall stencil
                startx = weights_x.GetStencilStart(request.cell_x, LagIntOrder);
                startx = diff_matrix_x.GetStencilStart(startx, FdOrder);
                endx = startx + kernelSize - 1;
                // For d2udxdx, the stencil for y and z does not include differentiation
                starty = weights_y.GetStencilStart(request.cell_y, LagIntOrder);
                endy = weights_y.GetStencilEnd(request.cell_y);
                startz = weights_z.GetStencilStart(request.cell_z, LagIntOrder);
                endz = startz + LagIntOrder - 1;
            }
            else
            {
                // This is the case for None_FD4, None_FD6, and None_FD8
                // for which we only need data along a line segment
                // In this case we are not performing Lagrange Polynomial interpolation  
                startx = diff_matrix_x.GetStencilStart(request.cell_x, FdOrder); // From X-4 to X+4 for None_FD8
                endx = startx + kernelSize - 1;
                starty = request.cell_y; // Only Y
                endy = request.cell_y;
                startz = request.cell_z; // Only Z
                endz = request.cell_z;
            }
            AddAtoms(startz, starty, startx, endz, endy, endx, atoms, mask);

            // Next we look at the kernel for d2udydy
            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Fd4Lag4)
            {
                startx = weights_x.GetStencilStart(request.cell_x, LagIntOrder); // From X-1 to X+2
                endx = startx + LagIntOrder - 1;
                starty = weights_y.GetStencilStart(request.cell_y, LagIntOrder); // From Y-3 to Y+5
                starty = diff_matrix_y.GetStencilStart(starty, FdOrderY);
                endy = weights_y.GetStencilEnd(request.cell_y);
                endy = diff_matrix_y.GetStencilEnd(endy);
                startz = weights_z.GetStencilStart(request.cell_z, LagIntOrder); // From Z-1 to Z+2
                endz = startz + LagIntOrder - 1;
            }
            else
            {
                startx = request.cell_x; // Only X
                endx = request.cell_x;
                starty = diff_matrix_y.GetStencilStart(request.cell_y, FdOrderY); // From Y-4 to Y+5 for None_FD8
                endy = diff_matrix_y.GetStencilEnd(request.cell_y);
                startz = request.cell_z; // Only Z
                endz = request.cell_z;
            }
            AddAtoms(startz, starty, startx, endz, endy, endx, atoms, mask);

            // Next we look at the kernel for d2udzdz
            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Fd4Lag4)
            {
                startx = weights_x.GetStencilStart(request.cell_x, LagIntOrder);
                endx = startx + LagIntOrder - 1;
                starty = weights_y.GetStencilStart(request.cell_y, LagIntOrder);
                endy = weights_y.GetStencilEnd(request.cell_y);
                startz = weights_z.GetStencilStart(request.cell_z, LagIntOrder);
                startz = diff_matrix_z.GetStencilStart(startz, FdOrder);
                endz = startz + kernelSize - 1;
            }
            else
            {
                startx = request.cell_x; // Only X
                endx = request.cell_x;
                starty = request.cell_y; // Only Y
                endy = request.cell_y;
                startz = diff_matrix_z.GetStencilStart(request.cell_z, FdOrder); // From Z-4 to Z+4 for None_FD8
                endz = startz + kernelSize - 1;
            }
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

        public override float[] GetResult(TurbulenceBlob blob, SQLUtility.InputRequest input)
        {
            throw new NotImplementedException();
        }

        public override float[] GetResult(TurbulenceBlob blob, SQLUtility.MHDInputRequest input)
        {
            return CalcLaplacian(blob, input);
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
        unsafe public float[] CalcLaplacian(TurbulenceBlob blob, SQLUtility.MHDInputRequest input)
        {
            float[] result = new float[GetResultSize()]; // Result value for the user
            // Temp variables for the partial computations
            double[] ax = new double[setInfo.Components], ay = new double[setInfo.Components], az = new double[setInfo.Components];

            float[] data = blob.data;

            int x, y, z;
            int stencil_startx, stencil_starty, stencil_startz;
            int startz = 0, starty = 0, startx = 0, endz = 0, endy = 0, endx = 0;
            int iKernelIndexX = 0, iKernelIndexY = 0, iKernelIndexZ = 0;

            switch (spatialInterp)
            {
                #region SpatialInterpolation None_Fd4/6/8
                case TurbulenceOptions.SpatialInterpolation.None_Fd4:
                case TurbulenceOptions.SpatialInterpolation.None_Fd6:
                case TurbulenceOptions.SpatialInterpolation.None_Fd8:
                    // Wrap the coordinates into the grid space
                    x = ((input.cell_x % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
                    y = ((input.cell_y % setInfo.GridResolutionY) + setInfo.GridResolutionY) % setInfo.GridResolutionY;
                    z = ((input.cell_z % setInfo.GridResolutionZ) + setInfo.GridResolutionZ) % setInfo.GridResolutionZ;

                    stencil_startx = diff_matrix_x.GetStencilStart(x, FdOrder);
                    stencil_starty = diff_matrix_y.GetStencilStart(y, FdOrderY);
                    stencil_startz = diff_matrix_z.GetStencilStart(z, FdOrder);
                    
                    // Since the given blob may not hold all of the required data
                    // we determine where to start and end the partial computation
                    blob.GetSubcubeStart(stencil_startz, stencil_starty, stencil_startx, 
                        ref startz, ref starty, ref startx);
                    blob.GetSubcubeEnd(stencil_startz + kernelSize - 1, 
                        diff_matrix_y.GetStencilEnd(y), 
                        stencil_startx + kernelSize - 1, 
                        ref endz, ref endy, ref endx);

                    // We also need to determine where we are starting, e.g. f(x_(n-2)), f(x_(n-1)), etc.
                    iKernelIndexX = blob.GetRealX + startx - stencil_startx;
                    if (iKernelIndexX >= setInfo.GridResolutionX)
                        iKernelIndexX -= setInfo.GridResolutionX;
                    else if (iKernelIndexX < 0)
                        iKernelIndexX += setInfo.GridResolutionX;

                    iKernelIndexY = blob.GetRealY + starty - stencil_starty;
                    if (iKernelIndexY >= setInfo.GridResolutionY)
                        iKernelIndexY -= setInfo.GridResolutionY;
                    else if (iKernelIndexY < 0)
                        iKernelIndexY += setInfo.GridResolutionY;

                    iKernelIndexZ = blob.GetRealZ + startz - stencil_startz;
                    if (iKernelIndexZ >= setInfo.GridResolutionZ)
                        iKernelIndexZ -= setInfo.GridResolutionZ;
                    else if (iKernelIndexZ < 0)
                        iKernelIndexZ += setInfo.GridResolutionZ;

                    fixed (float* fdata = data)
                    {
                        int off = 0;
                        #region unmixed derivatives
                        if (y >= blob.GetBaseY && y < blob.GetBaseY + blob.GetSide
                                            && z >= blob.GetBaseZ && z < blob.GetBaseZ + blob.GetSide)
                        {
                            off = startx * setInfo.Components + (y - blob.GetBaseY) * blob.GetSide * setInfo.Components + (z - blob.GetBaseZ) * blob.GetSide * blob.GetSide * setInfo.Components;
                            for (int ix = startx; ix <= endx; ix++)
                            {
                                double coeff = diff_matrix_x[iKernelIndexX + ix - startx];
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
                                double coeff = diff_matrix_y[y, iKernelIndexY + iy - starty];
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
                                double coeff = diff_matrix_z[iKernelIndexZ + iz - startz];
                                for (int j = 0; j < setInfo.Components; j++)
                                {
                                    az[j] += coeff * fdata[off + j];
                                }
                                off += blob.GetSide * blob.GetSide * setInfo.Components;
                            }
                        } 
                        #endregion
                    }
                    break;
                #endregion
                case TurbulenceOptions.SpatialInterpolation.Fd4Lag4:
                    int lagInt_stencil_startz, lagInt_stencil_starty, lagInt_stencil_startx;

                    // The coefficients are computed only once and cached, so that they don't have to be 
                    // recomputed for each partial sum                    
                    if (input.lagInt == null)
                    {
                        lagInt_stencil_startz = weights_z.GetStencilStart(input.cell_z, LagIntOrder);
                        lagInt_stencil_starty = weights_y.GetStencilStart(input.cell_y, LagIntOrder);
                        lagInt_stencil_startx = weights_x.GetStencilStart(input.cell_x, LagIntOrder);
                        input.lagInt = new double[LagIntOrder * 3];

                        LagInterpolation.InterpolantBarycentricWeights(LagIntOrder, input.x, 
                            ((ChannelFlowDataTable)setInfo).GridValuesX(lagInt_stencil_startx, LagIntOrder),
                            weights_x.GetWeights(), 0, input.lagInt);
                        // The y weights are non-uniform and therefore we have to provide the cell index for the retrieval of the weights.
                        LagInterpolation.InterpolantBarycentricWeights(LagIntOrder, input.y, 
                            ((ChannelFlowDataTable)setInfo).GridValuesY(lagInt_stencil_starty, LagIntOrder),
                            weights_y.GetWeights(input.cell_y), 1, input.lagInt);
                        LagInterpolation.InterpolantBarycentricWeights(LagIntOrder, input.z, 
                            ((ChannelFlowDataTable)setInfo).GridValuesZ(lagInt_stencil_startz, LagIntOrder),
                            weights_z.GetWeights(), 2, input.lagInt);
                    }

                    // Wrap the coordinates into the grid space
                    x = ((input.cell_x % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
                    y = ((input.cell_y % setInfo.GridResolutionY) + setInfo.GridResolutionY) % setInfo.GridResolutionY;
                    z = ((input.cell_z % setInfo.GridResolutionZ) + setInfo.GridResolutionZ) % setInfo.GridResolutionZ;

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
                    
                    lagInt_stencil_startx = weights_x.GetStencilStart(x, LagIntOrder);
                    stencil_startx = diff_matrix_x.GetStencilStart(lagInt_stencil_startx, FdOrder);
                    lagInt_stencil_starty = weights_y.GetStencilStart(y, LagIntOrder);
                    stencil_starty = diff_matrix_y.GetStencilStart(lagInt_stencil_starty, FdOrderY);
                    lagInt_stencil_startz = weights_z.GetStencilStart(z, LagIntOrder);
                    stencil_startz = diff_matrix_z.GetStencilStart(lagInt_stencil_startz, FdOrder);

                    int stencil_endx = stencil_startx + kernelSize - 1;
                    int stencil_endy = weights_y.GetStencilEnd(y);
                    stencil_endy = diff_matrix_y.GetStencilEnd(stencil_endy);
                    int stencil_endz = stencil_startz + kernelSize - 1;

                    int lagint_index_startx = lagInt_stencil_startx - stencil_startx;
                    int lagint_index_starty = lagInt_stencil_starty - stencil_starty;
                    int lagint_index_startz = lagInt_stencil_startz - stencil_startz;
                    int lagint_index_endx = lagint_index_startx + LagIntOrder - 1;
                    int lagint_index_endy = lagint_index_starty + LagIntOrder - 1;
                    int lagint_index_endz = lagint_index_startz + LagIntOrder - 1;

                    // Since the given blob may not hold all of the required data
                    // we determine where to start and end the partial computation
                    blob.GetSubcubeStart(stencil_startz, stencil_starty, stencil_startx, ref startz, ref starty, ref startx);
                    blob.GetSubcubeEnd(stencil_endz, stencil_endy, stencil_endx, ref endz, ref endy, ref endx);

                    // We also need to determine where we are starting, e.g. n - 3, n - 2, ..., n + 4
                    iKernelIndexX = blob.GetRealX + startx - stencil_startx;
                    if (iKernelIndexX >= setInfo.GridResolutionX)
                        iKernelIndexX -= setInfo.GridResolutionX;
                    else if (iKernelIndexX < 0)
                        iKernelIndexX += setInfo.GridResolutionX;

                    iKernelIndexY = blob.GetRealY + starty - stencil_starty;
                    if (iKernelIndexY >= setInfo.GridResolutionY)
                        iKernelIndexY -= setInfo.GridResolutionY;
                    else if (iKernelIndexY < 0)
                        iKernelIndexY += setInfo.GridResolutionY;

                    iKernelIndexZ = blob.GetRealZ + startz - stencil_startz;
                    if (iKernelIndexZ >= setInfo.GridResolutionZ)
                        iKernelIndexZ -= setInfo.GridResolutionZ;
                    else if (iKernelIndexZ < 0)
                        iKernelIndexZ += setInfo.GridResolutionZ;

                    fixed (double* lagint = input.lagInt)
                    {
                        fixed (float* fdata = data)
                        {
                            int off0 = startx * setInfo.Components;
                            for (int iz = startz; iz <= endz; iz++)
                            {
                                double[] bx = new double[setInfo.Components], by = new double[setInfo.Components], bz = new double[setInfo.Components];
                                int KernelIndexZ = iKernelIndexZ + iz - startz;
                                int off1 = off0 + iz * blob.GetSide * blob.GetSide * setInfo.Components;
                                for (int iy = starty; iy <= endy; iy++)
                                {
                                    double[] cx = new double[setInfo.Components], cy = new double[setInfo.Components], cz = new double[setInfo.Components];
                                    int KernelIndexY = iKernelIndexY + iy - starty;
                                    int off = off1 + iy * blob.GetSide * setInfo.Components;
                                    for (int ix = startx; ix <= endx; ix++)
                                    {
                                        int KernelIndexX = iKernelIndexX + ix - startx;
                                        #region d2uidxdx
                                        // the Y and Z dimensions range from the 2nd to the 5th row in the cube
                                        // since for the 4th-order Lagrange Polynomial interpolation we need the 4 nearest rows in each dim.
                                        // to the target point
                                        if (KernelIndexY >= lagint_index_starty && KernelIndexY <= lagint_index_endy &&
                                            KernelIndexZ >= lagint_index_startz && KernelIndexZ <= lagint_index_endz)
                                        {
                                            // Each point may be part of up to LagIntOrder interplation kernels
                                            // Therefore, we check each possible kernel and if the point falls inside it
                                            // we update the partial sum accordingly
                                            for (int i = 0; i < LagIntOrder; i++)
                                            {
                                                // The kernel index ranges from 0 to 4 for 4th-order finite differencing
                                                if (KernelIndexX - i >= 0 && KernelIndexX - i <= FdOrder)
                                                {
                                                    double c = lagint[i] * diff_matrix_x[KernelIndexX - i];
                                                    for (int j = 0; j < setInfo.Components; j++)
                                                    {
                                                        cx[j] += c * fdata[off + j];
                                                    }
                                                }
                                            }
                                        }
                                        #endregion
                                        #region d2uidydy
                                        // Same as for d2uidxdx, with the exception that we don't check at this point
                                        // which kernel the point falls into, since these are lines in the y-direction
                                        if (KernelIndexX >= lagint_index_startx && KernelIndexX <= lagint_index_endx &&
                                            KernelIndexZ >= lagint_index_startz && KernelIndexZ <= lagint_index_endz)
                                        {
                                            double c = lagint[KernelIndexX - lagint_index_startx];
                                            for (int j = 0; j < setInfo.Components; j++)
                                            {
                                                cy[j] += c * fdata[off + j];
                                            }
                                        }
                                        #endregion
                                        #region d2uidzdz
                                        // Same as for d2uidxdx, with the exception that we don't check at this point
                                        // which kernel the point falls into, since these are lines in the z-direction
                                        if (KernelIndexX >= lagint_index_startx && KernelIndexX <= lagint_index_endx &&
                                            KernelIndexY >= lagint_index_starty && KernelIndexY <= lagint_index_endy)
                                        {
                                            double c = lagint[KernelIndexX - lagint_index_startx];
                                            for (int j = 0; j < setInfo.Components; j++)
                                            {
                                                cz[j] += c * fdata[off + j];
                                            }
                                        }
                                        #endregion
                                        off += setInfo.Components;
                                    }
                                    if (KernelIndexY >= lagint_index_starty && KernelIndexY <= lagint_index_endy)
                                    {
                                        double b = lagint[1 * LagIntOrder + KernelIndexY - lagint_index_starty];
                                        for (int j = 0; j < setInfo.Components; j++)
                                        {
                                            bx[j] += cx[j] * b;
                                            bz[j] += cz[j] * b;
                                        }
                                    }
                                    #region d2uidydy
                                    // The 2 computations have been merged into 1 since they share the same outer loop
                                    for (int i = 0; i < LagIntOrder; i++)
                                    {
                                        int cell_index = lagInt_stencil_starty + i;
                                        int FD_stencil_start = diff_matrix_y.GetStencilStart(cell_index, FdOrderY);
                                        int FDIndex = stencil_starty + KernelIndexY - FD_stencil_start;
                                        // The kernel index ranges from 0 to 5 for 4th-order finite differencing
                                        // for the non-uniform y direction
                                        if (FDIndex >= 0 && FDIndex <= FdOrderY)
                                        {
                                            // First, we compute d2uidydy
                                            double coeff_y = lagint[1 * LagIntOrder + i] * diff_matrix_y[cell_index, FDIndex];
                                            for (int k = 0; k < setInfo.Components; k++)
                                            {
                                                by[k] += coeff_y * cy[k];
                                            }
                                        }
                                    }
                                    #endregion
                                }
                                if (KernelIndexZ >= lagint_index_startz && KernelIndexZ <= lagint_index_endz)
                                {
                                    double a = lagint[2 * LagIntOrder + KernelIndexZ - lagint_index_startz];
                                    for (int j = 0; j < setInfo.Components; j++)
                                    {
                                        ax[j] += bx[j] * a;
                                        ay[j] += by[j] * a;
                                    }
                                }
                                #region d2uidzdz
                                for (int i = 0; i < LagIntOrder; i++)
                                {
                                    // The kernel index ranges from 0 to 4 for 4th-order finite differencing
                                    if (KernelIndexZ - i >= 0 && KernelIndexZ - i <= FdOrder)
                                    {
                                        double c = lagint[2 * LagIntOrder + i] * diff_matrix_z[KernelIndexZ - i];
                                        for (int k = 0; k < setInfo.Components; k++)
                                        {
                                            az[k] += c * bz[k];
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

            result[0] = (float)(ax[0] + ay[0] + az[0]);
            result[1] = (float)(ax[1] + ay[1] + az[1]);
            result[2] = (float)(ax[2] + ay[2] + az[2]);
            
            return result;
        }

    }

}
