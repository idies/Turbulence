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
    public class GetChannelGradient : Worker
    {
        protected BarycentricWeights weights_x;
        protected BarycentricWeights weights_y;
        protected BarycentricWeights weights_z;
        protected BarycentricWeights diff_matrix_x;
        protected BarycentricWeights diff_matrix_y;
        protected BarycentricWeights diff_matrix_z;

        protected int LagIntOrder;
        protected int FdOrder;

        protected int numPointsInKernel = 0;

        public GetChannelGradient(string dataset, TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp,
            SqlConnection conn)
        {
            this.setInfo = setInfo;
            this.spatialInterp = spatialInterp;

            if (dataset.Contains("channel"))
            {
                periodicX = true;
                periodicY = false;
                periodicZ = true;
            }
            else if (dataset.Contains("bl_zaki"))
            {
                periodicX = false;
                periodicY = false;
                periodicZ = true;
            }

            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None_Fd4)
            {
                this.kernelSize = 5;
                this.FdOrder = 4;
                if (dataset.Contains("channel"))
                {
                    diff_matrix_x = GetUniformDiffMatrix(conn, "diff_matrix_x_r1_fd4");
                    diff_matrix_y = GetNonuniformWeights(conn, "diff_matrix_y_r1_fd4");
                    diff_matrix_z = GetUniformDiffMatrix(conn, "diff_matrix_z_r1_fd4");
                }
                else if (dataset.Contains("bl_zaki"))
                {
                    diff_matrix_x = GetNonuniformWeights(conn, "BL_diff_matrix_x_r1_fd4");
                    diff_matrix_y = GetNonuniformWeights(conn, "BL_diff_matrix_y_r1_fd4");
                    diff_matrix_z = GetUniformDiffMatrix(conn, "BL_diff_matrix_z_r1_fd4");
                }
                this.numPointsInKernel = 3 * kernelSize;
            }
            else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None_Fd6)
            {
                this.kernelSize = 7;
                this.FdOrder = 6;
                if (dataset.Contains("channel"))
                {
                    diff_matrix_x = GetUniformDiffMatrix(conn, "diff_matrix_x_r1_fd6");
                    diff_matrix_y = GetNonuniformWeights(conn, "diff_matrix_y_r1_fd6");
                    diff_matrix_z = GetUniformDiffMatrix(conn, "diff_matrix_z_r1_fd6");
                }
                else if (dataset.Contains("bl_zaki"))
                {
                    //diff_matrix_x = GetNonuniformWeights(conn, "BL_diff_matrix_x_r1_fd6");
                    //diff_matrix_y = GetNonuniformWeights(conn, "BL_diff_matrix_y_r1_fd6");
                    //diff_matrix_z = GetUniformDiffMatrix(conn, "BL_diff_matrix_z_r1_fd6");
                }
                this.numPointsInKernel = 3 * kernelSize;
            }
            else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None_Fd8)
            {
                this.kernelSize = 9;
                this.FdOrder = 8;
                if (dataset.Contains("channel"))
                {
                    diff_matrix_x = GetUniformDiffMatrix(conn, "diff_matrix_x_r1_fd8");
                    diff_matrix_y = GetNonuniformWeights(conn, "diff_matrix_y_r1_fd8");
                    diff_matrix_z = GetUniformDiffMatrix(conn, "diff_matrix_z_r1_fd8");
                }
                else if (dataset.Contains("bl_zaki"))
                {
                    //diff_matrix_x = GetNonuniformWeights(conn, "BL_diff_matrix_x_r1_fd8");
                    //diff_matrix_y = GetNonuniformWeights(conn, "BL_diff_matrix_y_r1_fd8");
                    //diff_matrix_z = GetUniformDiffMatrix(conn, "BL_diff_matrix_z_r1_fd8");
                }
                this.numPointsInKernel = 3 * kernelSize;
            }
            else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Fd4Lag4)
            {
                // kernel size is the size of the FD kernel + the size of the Lagrange interpolation kernel - 1 (5 + 4 - 1)
                this.kernelSize = 8;
                this.FdOrder = 4;
                this.LagIntOrder = 4;
                if (dataset.Contains("channel"))
                {
                    weights_x = GetUniformWeights(conn, "barycentric_weights_x_4");
                    weights_y = GetNonuniformWeights(conn, "barycentric_weights_y_4");
                    weights_z = GetUniformWeights(conn, "barycentric_weights_z_4");

                    diff_matrix_x = GetUniformDiffMatrix(conn, "diff_matrix_x_r1_fd4");
                    diff_matrix_y = GetNonuniformWeights(conn, "diff_matrix_y_r1_fd4");
                    diff_matrix_z = GetUniformDiffMatrix(conn, "diff_matrix_z_r1_fd4");
                }
                else if (dataset.Contains("bl_zaki"))
                {
                    weights_x = GetNonuniformWeights(conn, "BL_barycentric_weights_x_4");
                    weights_y = GetNonuniformWeights(conn, "BL_barycentric_weights_y_4");
                    weights_z = GetUniformWeights(conn, "BL_barycentric_weights_z_4");

                    diff_matrix_x = GetNonuniformWeights(conn, "BL_diff_matrix_x_r1_fd4");
                    diff_matrix_y = GetNonuniformWeights(conn, "BL_diff_matrix_y_r1_fd4");
                    diff_matrix_z = GetUniformDiffMatrix(conn, "BL_diff_matrix_z_r1_fd4");
                }
            }
            else
            {
                throw new Exception(String.Format("Invalid Spatial Interpolation Option: {0}", spatialInterp));
            }
        }

        protected BarycentricWeights GetUniformDiffMatrix(SqlConnection conn, string tableName)
        {
            BarycentricWeights uniform_diff_matrix = new UniformDifferentiationMatrix();
            uniform_diff_matrix.GetWeightsFromDB(conn, tableName);
            return uniform_diff_matrix;
        }
        protected BarycentricWeights GetUniformWeights(SqlConnection conn, string tableName)
        {
            BarycentricWeights uniform_weights = new UniformBarycentricWeights();
            uniform_weights.GetWeightsFromDB(conn, tableName);
            return uniform_weights;
        }
        protected BarycentricWeights GetNonuniformWeights(SqlConnection conn, string tableName)
        {
            BarycentricWeights nonuniform_weights = new NonUniformBarycentricWeights();
            nonuniform_weights.GetWeightsFromDB(conn, tableName);
            return nonuniform_weights;
        }

        public override SqlMetaData[] GetRecordMetaData()
        {
            if (setInfo.Components == 3)
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
            else
            {
                return new SqlMetaData[] {
                new SqlMetaData("Req", SqlDbType.Int),
                new SqlMetaData("dpdx", SqlDbType.Real),
                new SqlMetaData("dpdy", SqlDbType.Real),
                new SqlMetaData("dpdz", SqlDbType.Real) };
            }
        }

        public override void GetAtomsForPoint(SQLUtility.MHDInputRequest request, long mask, int pointsPerCubeEstimate, Dictionary<long, List<int>> map, ref int total_points)
        {
            int startz, starty, startx, endz, endy, endx;
            HashSet<long> atoms = new HashSet<long>(); //NOTE: HashSet requires .Net 3.5

            // The following computation will be repeated 3 times
            // Once for each of the 3 spatial dimensions
            // This is necessary because the kernel of computation is a box/line 
            // with different dimensions and not a cube

            // We start of with the kernel for dudx
            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Fd4Lag4)
            {
                // The computation has 2 componnets -- differentiation and interpolation
                // We get the start of the differentiation stencil and using that we get the start of the overall stencil
                startx = weights_x.GetStencilStart(request.cell_x, LagIntOrder);
                startx = diff_matrix_x.GetStencilStart(startx, FdOrder);
                if (periodicX)
                {
                    endx = startx + kernelSize - 1;
                }
                else
                {
                    endx = weights_x.GetStencilEnd(request.cell_x);
                    endx = diff_matrix_x.GetStencilEnd(endx);
                }
                // For dudx, the stencil for y and z does not include differentiation
                starty = weights_y.GetStencilStart(request.cell_y, LagIntOrder);
                if (periodicY)
                {
                    endy = starty + LagIntOrder - 1;
                }
                else
                {
                    endy = weights_y.GetStencilEnd(request.cell_y);
                }
                startz = weights_z.GetStencilStart(request.cell_z, LagIntOrder);
                if (periodicZ)
                {
                    endz = startz + LagIntOrder - 1;
                }
                else
                {
                    endz = weights_z.GetStencilEnd(request.cell_z);
                }
            }
            else
            {
                // This is the case for None_FD4, None_FD6, and None_FD8
                // for which we only need data along a line in each of the x, y, z dimensions
                // In this case we are not performing Lagrange Polynomial interpolation  
                startx = diff_matrix_x.GetStencilStart(request.cell_x, FdOrder); // From X-4 to X+4 for None_FD8
                if (periodicX)
                {
                    endx = startx + kernelSize - 1;
                }
                else
                {
                    endx = diff_matrix_x.GetStencilEnd(request.cell_x);
                }
                starty = request.cell_y; // Only Y
                endy = request.cell_y;
                startz = request.cell_z; // Only Z
                endz = request.cell_z;
            }
            AddAtoms(startz, starty, startx, endz, endy, endx, atoms, mask);

            // Next we look at the kernel for dudy
            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Fd4Lag4)
            {
                startx = weights_x.GetStencilStart(request.cell_x, LagIntOrder); // From X-1 to X+2
                if (periodicX)
                {
                    endx = startx + LagIntOrder - 1;
                }
                else
                {
                    endx = weights_x.GetStencilEnd(request.cell_x);
                }
                starty = weights_y.GetStencilStart(request.cell_y, LagIntOrder); // From Y-3 to Y+4
                starty = diff_matrix_y.GetStencilStart(starty, FdOrder);
                if (periodicY)
                {
                    endy = starty + kernelSize - 1;
                }
                else
                {
                    endy = weights_y.GetStencilEnd(request.cell_y);
                    endy = diff_matrix_y.GetStencilEnd(endy);
                }
                startz = weights_z.GetStencilStart(request.cell_z, LagIntOrder);
                if (periodicZ)
                {
                    endz = startz + LagIntOrder - 1;
                }
                else
                {
                    endz = weights_z.GetStencilEnd(request.cell_z);
                }
            }
            else
            {
                startx = request.cell_x; // Only X
                endx = request.cell_x;
                starty = diff_matrix_y.GetStencilStart(request.cell_y, FdOrder); // From Y-4 to Y+4
                if (periodicY)
                {
                    endy = starty + kernelSize - 1;
                }
                else
                {
                    endy = diff_matrix_y.GetStencilEnd(request.cell_y);
                }
                startz = request.cell_z; // Only Z
                endz = request.cell_z;
            }
            AddAtoms(startz, starty, startx, endz, endy, endx, atoms, mask);

            // Next we look at the kernel for dudz
            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Fd4Lag4)
            {
                startx = weights_x.GetStencilStart(request.cell_x, LagIntOrder); // From X-1 to X+2
                if (periodicX)
                {
                    endx = startx + LagIntOrder - 1;
                }
                else
                {
                    endx = weights_x.GetStencilEnd(request.cell_x);
                }
                starty = weights_y.GetStencilStart(request.cell_y, LagIntOrder);
                if (periodicY)
                {
                    endy = starty + LagIntOrder - 1;
                }
                else
                {
                    endy = weights_y.GetStencilEnd(request.cell_y);
                }
                startz = weights_z.GetStencilStart(request.cell_z, LagIntOrder); // From Z-3 to Z+4
                startz = diff_matrix_z.GetStencilStart(startz, FdOrder);
                if (periodicZ)
                {
                    endz = startz + kernelSize - 1;
                }
                else
                {
                    endz = weights_z.GetStencilEnd(request.cell_z);
                    endz = diff_matrix_z.GetStencilEnd(endz);
                }
            }
            else
            {
                startx = request.cell_x; // Only X
                endx = request.cell_x;
                starty = request.cell_y; // Only Y
                endy = request.cell_y;
                startz = diff_matrix_z.GetStencilStart(request.cell_z, FdOrder);
                if (periodicZ)
                {
                    endz = startz + kernelSize - 1;
                }
                else
                {
                    endz = diff_matrix_z.GetStencilEnd(request.cell_z);
                }
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
        /// TODO: In the case of FD4Lag4 the triply nested loop over z, y and x results
        /// in wasted computations. The code needs to be refactored so that we loop over
        /// only the boxes that are part of the kernel of computation. For example:
        /// To compute dudx we need to loop from [2,2,0] to [5,5,7] along (z,y,x),
        /// instead of from [0,0,0] to [7,7,7].
        /// Similarly, for dudy and dudz and in GetMHDGradient/Hessian, etc.
        /// </remarks>
        unsafe public double[] CalcGradient(TurbulenceBlob blob, SQLUtility.MHDInputRequest input)
        {
            // Result value for the user
            double[] result = new double[3 * setInfo.Components];
            // Temp variables for the partial computations
            double[] ax = new double[setInfo.Components], ay = new double[setInfo.Components], az = new double[setInfo.Components];

            float[] data = blob.data;

            int x, y, z;
            int stencil_startx, stencil_starty, stencil_startz;
            int stencil_endx, stencil_endy, stencil_endz;
            int startz = 0, starty = 0, startx = 0, endz = 0, endy = 0, endx = 0;
            int iKernelIndexX = 0, iKernelIndexY = 0, iKernelIndexZ = 0;

            switch (spatialInterp)
            {
                #region SpatialInterpolation None_Fd4/6/8
                case TurbulenceOptions.SpatialInterpolation.None_Fd4:
                case TurbulenceOptions.SpatialInterpolation.None_Fd6:
                case TurbulenceOptions.SpatialInterpolation.None_Fd8:
                    // Wrap the coordinates into the grid space
                    x = periodicX ? ((input.cell_x % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX : input.cell_x;
                    y = periodicY ? ((input.cell_y % setInfo.GridResolutionY) + setInfo.GridResolutionY) % setInfo.GridResolutionY : input.cell_y;
                    z = periodicZ ? ((input.cell_z % setInfo.GridResolutionZ) + setInfo.GridResolutionZ) % setInfo.GridResolutionZ : input.cell_z;

                    stencil_startx = diff_matrix_x.GetStencilStart(x, FdOrder);
                    stencil_starty = diff_matrix_y.GetStencilStart(y, FdOrder);
                    stencil_startz = diff_matrix_z.GetStencilStart(z, FdOrder);
                    stencil_endx = periodicX ? stencil_startx + kernelSize - 1 : diff_matrix_x.GetStencilEnd(x);
                    stencil_endy = periodicY ? stencil_starty + kernelSize - 1 : diff_matrix_y.GetStencilEnd(y);
                    stencil_endz = periodicZ ? stencil_startz + kernelSize - 1 : diff_matrix_z.GetStencilEnd(z);

                    // Since the given blob may not hold all of the required data
                    // we determine where to start and end the partial computation
                    blob.GetSubcubeStart(stencil_startz, stencil_starty, stencil_startx,
                        ref startz, ref starty, ref startx);
                    blob.GetSubcubeEnd(stencil_endz, stencil_endy, stencil_endx,
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
                        if (y >= blob.GetBaseY && y < blob.GetBaseY + blob.GetSide
                            && z >= blob.GetBaseZ && z < blob.GetBaseZ + blob.GetSide)
                        {
                            off = startx * setInfo.Components + (y - blob.GetBaseY) * blob.GetSide * setInfo.Components + (z - blob.GetBaseZ) * blob.GetSide * blob.GetSide * setInfo.Components;
                            for (int ix = startx; ix <= endx; ix++)
                            {
                                double coeff = periodicX ? diff_matrix_x[iKernelIndexX + ix - startx] :
                                    diff_matrix_x[x, iKernelIndexX + ix - startx];
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
                                double coeff = periodicY ? diff_matrix_y[iKernelIndexY + iy - starty] :
                                    diff_matrix_y[y, iKernelIndexY + iy - starty];
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
                                double coeff = periodicZ ? diff_matrix_z[iKernelIndexZ + iz - startz] :
                                    diff_matrix_z[z, iKernelIndexZ + iz - startz];
                                for (int j = 0; j < setInfo.Components; j++)
                                {
                                    az[j] += coeff * fdata[off + j];
                                }
                                off += blob.GetSide * blob.GetSide * setInfo.Components;
                            }
                        }
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

                        if (periodicX)
                        {
                            LagInterpolation.InterpolantBarycentricWeights(LagIntOrder, input.x,
                                ((ChannelFlowDataTable)setInfo).GridValuesX(lagInt_stencil_startx, LagIntOrder),
                                weights_x.GetWeights(), 0, input.lagInt);
                        }
                        else
                        {
                            LagInterpolation.InterpolantBarycentricWeights(LagIntOrder, input.x,
                                ((ChannelFlowDataTable)setInfo).GridValuesX(lagInt_stencil_startx, LagIntOrder),
                                weights_x.GetWeights(input.cell_x), 0, input.lagInt);
                        }

                        if (periodicY)
                        {
                            LagInterpolation.InterpolantBarycentricWeights(LagIntOrder, input.y,
                                ((ChannelFlowDataTable)setInfo).GridValuesY(lagInt_stencil_starty, LagIntOrder),
                                weights_y.GetWeights(), 1, input.lagInt);
                        }
                        else
                        {
                            // The y weights are non-uniform and therefore we have to provide the cell index for the retrieval of the weights.
                            LagInterpolation.InterpolantBarycentricWeights(LagIntOrder, input.y,
                                ((ChannelFlowDataTable)setInfo).GridValuesY(lagInt_stencil_starty, LagIntOrder),
                                weights_y.GetWeights(input.cell_y), 1, input.lagInt);
                        }

                        if (periodicZ)
                        {
                            LagInterpolation.InterpolantBarycentricWeights(LagIntOrder, input.z,
                            ((ChannelFlowDataTable)setInfo).GridValuesZ(lagInt_stencil_startz, LagIntOrder),
                            weights_z.GetWeights(), 2, input.lagInt);
                        }
                        else
                        {
                            LagInterpolation.InterpolantBarycentricWeights(LagIntOrder, input.z,
                            ((ChannelFlowDataTable)setInfo).GridValuesZ(lagInt_stencil_startz, LagIntOrder),
                            weights_z.GetWeights(input.cell_z), 2, input.lagInt);
                        }
                    }

                    // Wrap the coordinates into the grid space
                    x = periodicX ? ((input.cell_x % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX : input.cell_x;
                    y = periodicY ? ((input.cell_y % setInfo.GridResolutionY) + setInfo.GridResolutionY) % setInfo.GridResolutionY : input.cell_y;
                    z = periodicZ ? ((input.cell_z % setInfo.GridResolutionZ) + setInfo.GridResolutionZ) % setInfo.GridResolutionZ : input.cell_z;

                    // This computation has 2 stages:
                    // 4th-order finite difference evaluation of the derivative (see above)
                    // and 4th-order Lagrange Polynomial interpolation of this derivative

                    // The entire computation operates over the parts of a cube of size 8x8x8
                    // from n - 3 to n + 4 as below
                    // E.g. for the computation of duxdx (or duydx, duzdx, since here ux, uy, uz are the components of the velocity)
                    // we need the part of the cube from [2,2,0] to [5,5,7], 
                    // while for the computation of duxdy (or duydy, duzdy)
                    // we need the part of the cube from [2,0,2] to [5,7,5], etc.

                    lagInt_stencil_startx = weights_x.GetStencilStart(x, LagIntOrder);
                    stencil_startx = diff_matrix_x.GetStencilStart(lagInt_stencil_startx, FdOrder);
                    lagInt_stencil_starty = weights_y.GetStencilStart(y, LagIntOrder);
                    stencil_starty = diff_matrix_y.GetStencilStart(lagInt_stencil_starty, FdOrder);
                    lagInt_stencil_startz = weights_z.GetStencilStart(z, LagIntOrder);
                    stencil_startz = diff_matrix_z.GetStencilStart(lagInt_stencil_startz, FdOrder);

                    //int stencil_endx = 0, stencil_endy = 0, stencil_endz = 0;//stencil_startx + kernelSize - 1;
                    if (periodicX)
                    {
                        stencil_endx = stencil_startx + kernelSize - 1;
                    }
                    else
                    {
                        stencil_endx = weights_x.GetStencilEnd(x);
                        stencil_endx = diff_matrix_x.GetStencilEnd(stencil_endx);
                    }

                    if (periodicY)
                    {
                        stencil_endy = stencil_starty + kernelSize - 1;
                    }
                    else
                    {
                        stencil_endy = weights_y.GetStencilEnd(y);
                        stencil_endy = diff_matrix_y.GetStencilEnd(stencil_endy);
                    }

                    if (periodicZ)
                    {
                        stencil_endz = stencil_startz + kernelSize - 1;
                    }
                    else
                    {
                        stencil_endz = weights_z.GetStencilEnd(z);
                        stencil_endz = diff_matrix_z.GetStencilEnd(stencil_endz);
                    }

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
                                        #region d_x u_i
                                        // first we compute d_x u_i
                                        // the Y and Z dimensions range from the 2nd to the 5th row in the cube
                                        // since for the 4th-order Lagrange Polynomial interpolation we need the 4 nearest rows in each dim.
                                        // to the target point
                                        if (KernelIndexY >= lagint_index_starty && KernelIndexY <= lagint_index_endy &&
                                            KernelIndexZ >= lagint_index_startz && KernelIndexZ <= lagint_index_endz)
                                        {
                                            for (int i = 0; i < LagIntOrder; i++)
                                            {
                                                int cell_index = lagInt_stencil_startx + i;
                                                int FD_stencil_start = diff_matrix_x.GetStencilStart(cell_index, FdOrder);
                                                int FDIndex = stencil_startx + KernelIndexX - FD_stencil_start;
                                                // The kernel index ranges from 0 to 4 for 4th-order finite differencing
                                                //if (KernelIndexX - i >= 0 && KernelIndexX - i <= FdOrder)
                                                if (FDIndex >= 0 && FDIndex <= FdOrder)
                                                {
                                                    double c = periodicX ? lagint[i] * diff_matrix_x[FDIndex] :
                                                        lagint[i] * diff_matrix_x[cell_index, FDIndex];

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
                                        #region d_z u_i
                                        // finally we compute d_z u_i
                                        // the X and Y dimensions range from the 2nd to the 5th row in the cube
                                        // since for the 4th-order Lagrange Polynomial interpolation we need 4 nearest rows in each dim.
                                        // to the target point
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
                                    #region d_y u_i
                                    for (int i = 0; i < LagIntOrder; i++)
                                    {
                                        int cell_index = lagInt_stencil_starty + i;
                                        int FD_stencil_start = diff_matrix_y.GetStencilStart(cell_index, FdOrder);
                                        int FDIndex = stencil_starty + KernelIndexY - FD_stencil_start;
                                        // The kernel index ranges from 0 to 4 for 4th-order finite differencing
                                        if (FDIndex >= 0 && FDIndex <= FdOrder)
                                        {
                                            // We have to get the differentiation matrix weights from the non-uniform y matrix
                                            // by supplying the grid cell and the index of the particular weight value.
                                            //double c = lagint[1 * LagIntOrder + i] * diff_matrix_y[cell_index, FDIndex];
                                            double c = periodicY ? lagint[1 * LagIntOrder + i] * diff_matrix_y[FDIndex] :
                                                        lagint[1 * LagIntOrder + i] * diff_matrix_y[cell_index, FDIndex];

                                            for (int j = 0; j < setInfo.Components; j++)
                                            {
                                                by[j] += c * cy[j];
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
                                #region d_z u_i
                                for (int i = 0; i < LagIntOrder; i++)
                                {
                                    int cell_index = lagInt_stencil_startz + i;
                                    int FD_stencil_start = diff_matrix_z.GetStencilStart(cell_index, FdOrder);
                                    int FDIndex = stencil_startz + KernelIndexZ - FD_stencil_start;
                                    // The kernel index ranges from 0 to 4 for 4th-order finite differencing
                                    //if (KernelIndexZ - i >= 0 && KernelIndexZ - i <= FdOrder)
                                    //!!!!! IMPORTRANT: TODO: is it correct herer???? KernelIndexZ - i=FDIndex???? !!!!!//
                                    if (FDIndex >= 0 && FDIndex <= FdOrder)
                                    {
                                        double c = periodicZ ? lagint[2 * LagIntOrder + i] * diff_matrix_z[FDIndex] :
                                            lagint[2 * LagIntOrder + i] * diff_matrix_z[cell_index, FDIndex];

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

        public override int[] GetCutoutCoordinates(int[] coordinates)
        {
            int startx = diff_matrix_x.GetStencilStart(coordinates[0], FdOrder);
            int starty = diff_matrix_y.GetStencilStart(coordinates[1], FdOrder);
            int startz = diff_matrix_z.GetStencilStart(coordinates[2], FdOrder);
            // The coordinates are given in the format [start, end),
            // up to but not including the "end" point in the region.
            // Because of the non-uniform grid along y for the channel flow dataset,
            // we can't simply get the end of the stencil from the "end" point.
            // We have to look at the previous point and add 1.
            int endx = periodicX ? diff_matrix_x.GetStencilEnd(coordinates[3], FdOrder) :
                diff_matrix_x.GetStencilEnd(coordinates[3] - 1) + 2;
            int endy = periodicY ? diff_matrix_y.GetStencilEnd(coordinates[4], FdOrder) :
                diff_matrix_y.GetStencilEnd(coordinates[4] - 1) + 2;
            int endz = periodicZ ? diff_matrix_z.GetStencilEnd(coordinates[5], FdOrder) :
                diff_matrix_z.GetStencilEnd(coordinates[5] - 1) + 2;
            return new int[] { startx, starty, startz, endx, endy, endz };
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
        public override HashSet<SQLUtility.PartialResult> GetThresholdUsingCutout(int[] coordiantes, double threshold, int workertype)
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
            int startz = 0, endz = 0, starty = 0, endy = 0, startx = 0, endx = 0, offset_x = 0, offset_y = 0, offset_z = 0;
            long zindex = 0;
            for (int z = coordiantes[2]; z < coordiantes[5]; z++)
            {
                for (int y = coordiantes[1]; y < coordiantes[4]; y++)
                {
                    for (int x = coordiantes[0]; x < coordiantes[3]; x++)
                    {
                        zindex = new Morton3D(z, y, x);

                        startx = diff_matrix_x.GetStencilStart(x, FdOrder);
                        starty = diff_matrix_y.GetStencilStart(y, FdOrder);
                        startz = diff_matrix_z.GetStencilStart(z, FdOrder);
                        if (periodicX)
                        {
                            endx = diff_matrix_x.GetStencilEnd(x, FdOrder);
                        }
                        else
                        {
                            offset_x = diff_matrix_x.GetOffset(x);
                            endx = diff_matrix_x.GetStencilEnd(x);
                        }

                        if (periodicY)
                        {
                            endy = diff_matrix_y.GetStencilEnd(y, FdOrder);
                        }
                        else
                        {
                            offset_y = diff_matrix_y.GetOffset(y);
                            endy = diff_matrix_y.GetStencilEnd(y);
                        }

                        if (periodicZ)
                        {
                            endz = diff_matrix_z.GetStencilEnd(z, FdOrder);
                        }
                        else
                        {
                            offset_z = diff_matrix_z.GetOffset(z);
                            endz = diff_matrix_z.GetStencilEnd(z);
                        }

                        point = new SQLUtility.PartialResult(zindex, GetResultSize(), numPointsInKernel);
                        GetResultUsingCutout(ref point, x, y, z, startx, starty, startz, endx, endy, endz, cutout_coordinates, cutout_dimensions, offset_x, offset_y, offset_z);

                        // Compute the norm.
                        double norm = 0.0f;
                        if (workertype == 30 || workertype == 31) //Q criterion
                        {
                            for (int i = 0; i < GetResultSize(); i++)
                            {
                                norm += point.result[i];
                            }
                        }
                        else
                        {
                            for (int i = 0; i < GetResultSize(); i++)
                            {
                                norm += point.result[i] * point.result[i];
                            }
                            norm = Math.Sqrt(norm);
                        }
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

        protected virtual void GetResultUsingCutout(ref SQLUtility.PartialResult point, int x, int y, int z, int startx, int starty, int startz, int endx, int endy, int endz,
            int[] cutout_coordinates, int[] cutout_dimensions, int offset_x, int offset_y, int offset_z)
        {
            for (int x_i = startx; x_i <= endx; x_i++)
            {
                //Determine the kernel index, at which this data point is for the kernel of the above taget point.
                int iKernelIndexX = x_i - x + kernelSize / 2 - offset_x;

                double coeff = periodicX ? diff_matrix_x[iKernelIndexX] :
                    diff_matrix_x[x, iKernelIndexX];

                ulong sourceIndex = (((ulong)z - (ulong)cutout_coordinates[2]) * (ulong)cutout_dimensions[2] * (ulong)cutout_dimensions[1] +
                    ((ulong)y - (ulong)cutout_coordinates[1]) * (ulong)cutout_dimensions[2] +
                    ((ulong)x_i - (ulong)cutout_coordinates[0])) * (ulong)setInfo.Components;
                point.result[0] += coeff * GetDataItem(sourceIndex);     //dux/dx
                point.result[3] += coeff * GetDataItem(sourceIndex + 1); //duy/dx
                point.result[6] += coeff * GetDataItem(sourceIndex + 2); //duz/dx
                point.numPointsProcessed++;
            }
            for (int y_i = starty; y_i <= endy; y_i++)
            {
                //Determine the kernel index, at which this data point is for the kernel of the above taget point.
                int iKernelIndexY = y_i - y + kernelSize / 2 - offset_y;

                //double coeff = diff_matrix_y[y, iKernelIndexY];
                double coeff = periodicY ? diff_matrix_y[iKernelIndexY] :
                    diff_matrix_y[y, iKernelIndexY];

                ulong sourceIndex = (((ulong)z - (ulong)cutout_coordinates[2]) * (ulong)cutout_dimensions[2] * (ulong)cutout_dimensions[1] +
                    ((ulong)y_i - (ulong)cutout_coordinates[1]) * (ulong)cutout_dimensions[2] +
                    ((ulong)x - (ulong)cutout_coordinates[0])) * (ulong)setInfo.Components;
                point.result[1] += coeff * GetDataItem(sourceIndex);     //dux/dy
                point.result[4] += coeff * GetDataItem(sourceIndex + 1); //duy/dy
                point.result[7] += coeff * GetDataItem(sourceIndex + 2); //duz/dy
                point.numPointsProcessed++;
            }
            for (int z_i = startz; z_i <= endz; z_i++)
            {
                //Determine the kernel index, at which this data point is for the kernel of the above taget point.
                int iKernelIndexZ = z_i - z + kernelSize / 2 - offset_z;

                //double coeff = diff_matrix_z[iKernelIndexZ];
                double coeff = periodicZ ? diff_matrix_z[iKernelIndexZ] :
                    diff_matrix_z[z, iKernelIndexZ];

                ulong sourceIndex = (((ulong)z_i - (ulong)cutout_coordinates[2]) * (ulong)cutout_dimensions[2] * (ulong)cutout_dimensions[1] +
                    ((ulong)y - (ulong)cutout_coordinates[1]) * (ulong)cutout_dimensions[2] +
                    ((ulong)x - (ulong)cutout_coordinates[0])) * (ulong)setInfo.Components;
                point.result[2] += coeff * GetDataItem(sourceIndex);     //dux/dz
                point.result[5] += coeff * GetDataItem(sourceIndex + 1); //duy/dz
                point.result[8] += coeff * GetDataItem(sourceIndex + 2); //duz/dz
                point.numPointsProcessed++;
            }
        }

    }

}
