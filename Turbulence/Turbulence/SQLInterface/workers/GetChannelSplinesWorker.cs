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
    public class GetChannelSplinesWorker : GetSplinesWorker
    {
        private SplineWeights[] weights_y;

        public GetChannelSplinesWorker(string dataset, TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp,
            int derivative,
            SqlConnection conn) : base (dataset, setInfo, spatialInterp, derivative)
        {
            weights_y = new SplineWeights[derivative + 1];
            for (int i = 0; i <= derivative; i++)
            {
                weights_y[i] = new SplineWeights();

                switch (spatialInterp)
                {
                    case TurbulenceOptions.SpatialInterpolation.M1Q4:
                        if (dataset.Contains("channel"))
                        {
                            weights_y[i].GetWeightsFromDB(conn, String.Format("spline_coeff_y_m1q4_d{0}", i));
                        }
                        else if (dataset.Contains("bl_zaki"))
                        {
                            weights_y[i].GetWeightsFromDB(conn, String.Format("BL_spline_coeff_y_m1q4_d{0}", i));
                        }
                        break;
                    case TurbulenceOptions.SpatialInterpolation.M2Q8:
                        if (dataset.Contains("channel"))
                        {
                            weights_y[i].GetWeightsFromDB(conn, String.Format("spline_coeff_y_m2q8_d{0}", i));
                        }
                        else if (dataset.Contains("bl_zaki"))
                        {
                            weights_y[i].GetWeightsFromDB(conn, String.Format("BL_spline_coeff_y_m2q8_d{0}", i));
                        }
                        break;
                    case TurbulenceOptions.SpatialInterpolation.M2Q14:
                        if (dataset.Contains("channel"))
                        {
                            weights_y[i].GetWeightsFromDB(conn, String.Format("spline_coeff_y_m2q14_d{0}", i));
                        }
                        else if (dataset.Contains("bl_zaki"))
                        {
                            weights_y[i].GetWeightsFromDB(conn, String.Format("BL_spline_coeff_y_m2q14_d{0}", i));
                        }
                        break;
                    default:
                        throw new Exception(String.Format("Invalid Spatial Interpolation Option: {0}", spatialInterp));
                }
            }
        }

        private void ComputeBetas(int cell_index, int derivative, double x, double[] poly_val, int offset)
        {
            if (cell_index == setInfo.GridResolutionY - 1)
            {
                poly_val[offset] = 1.0;
            }
            else
            {
                int stencil_start = GetStencilStartY(cell_index);
                for (int i = stencil_start; i <= GetStencilEndY(cell_index); i++)
                {
                    double[] weights = weights_y[derivative].GetWeights(cell_index, i);
                    for (int j = 0; j < weights.Length; j++)
                    {
                        poly_val[offset + i - stencil_start] += Math.Pow(x, j) * weights[j];
                    }
                }
            }
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

            int startz = request.cell_z - kernelSize / 2 + 1;
            int starty = GetStencilStartY(request.cell_y);
            int startx = request.cell_x - kernelSize / 2 + 1;
            int endz = request.cell_z + kernelSize / 2;
            int endy = GetStencilEndY(request.cell_y);
            int endx = request.cell_x + kernelSize / 2;

            AddAtoms(request, mask, startz, starty, startx, endz, endy, endx, map, ref total_points);
        }

        private int GetStencilStartY(int cell_y)
        {
            if (cell_y == setInfo.GridResolutionY - 1)
            {
                return cell_y;
            }
            else
            {
                // The stencil start/end are the same between the different derivative options (0, 1, 2).
                // So, we can just use weights_y[0] here.
                return weights_y[0].GetStencilStart(cell_y);
            }
        }

        private int GetStencilEndY(int cell_y)
        {
            if (cell_y == setInfo.GridResolutionY - 1)
            {
                return cell_y;
            }
            else
            {
                // The stencil start/end are the same between the different derivative options (0, 1, 2).
                // So, we can just use weights_y[0] here.
                return weights_y[0].GetStencilEnd(cell_y);
            }
        }


        /// <summary>
        /// New version of the CalcVelocity function (also used for magnetic field and vector potential)
        /// </summary>
        /// <remarks>
        /// The Lagrangian evaluation function [LagInterpolation.EvaluateOpt] was moved
        /// into the function and some loop unrolling was performed.
        /// </remarks>
        public override double[] CalcSplines(TurbulenceBlob blob, SQLUtility.MHDInputRequest input)
        {
            double[] up = new double[result_size]; // Result value for the user

            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None)
            {
                float[] data = blob.data;
                int off0 = blob.GetLocalOffsetMHD(input.cell_z, input.cell_y, input.cell_x, 0);
                for (int j = 0; j < setInfo.Components; j++)
                {
                    up[j] = data[off0 + j];
                }
            }
            else
            {
                // The coefficients are computed only once and cached, so that they don't have to be 
                // recomputed for each partial sum
                if (input.lagInt == null)
                {
                    int dimensions = 3;
                    input.lagInt = new double[dimensions * kernelSize * (derivative + 1)];

                    for (int i = 0; i <= derivative; i++)
                    {
                        double poly_variable;
                        if (input.cell_y == setInfo.GridResolutionY - 1)
                        {
                            poly_variable = 1.0;
                        }
                        else
                        {
                            poly_variable = (input.y - ((ChannelFlowDataTable)setInfo).GetGridValueY(input.cell_y)) /
                                (((ChannelFlowDataTable)setInfo).GetGridValueY(input.cell_y + 1) - ((ChannelFlowDataTable)setInfo).GetGridValueY(input.cell_y));
                        }
                        ComputeBetas(i, (input.x / setInfo.Dx) - input.cell_x, input.lagInt, (i * dimensions) * kernelSize);
                        ComputeBetas(input.cell_y, i, poly_variable, input.lagInt, (i * dimensions + 1) * kernelSize);
                        ComputeBetas(i, (input.z / setInfo.Dz) - input.cell_z, input.lagInt, (i * dimensions + 2) * kernelSize);
                    }
                }

                // Wrap the coordinates into the grid space
                int x = ((input.cell_x % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
                int z = ((input.cell_z % setInfo.GridResolutionZ) + setInfo.GridResolutionZ) % setInfo.GridResolutionZ;

                int startz = 0, starty = 0, startx = 0, endz = 0, endy = 0, endx = 0;
                blob.GetSubcubeStart(z - (kernelSize / 2) + 1, GetStencilStartY(input.cell_y), x - (kernelSize / 2) + 1, ref startz, ref starty, ref startx);
                blob.GetSubcubeEnd(z + (kernelSize / 2), GetStencilEndY(input.cell_y), x + (kernelSize / 2), ref endz, ref endy, ref endx);

                int iLagIntx = blob.GetRealX - x + startx + kernelSize / 2 - 1;
                if (iLagIntx >= setInfo.GridResolutionX)
                    iLagIntx -= setInfo.GridResolutionX;
                else if (iLagIntx < 0)
                    iLagIntx += setInfo.GridResolutionX;
                int iLagInty = blob.GetRealY + starty - GetStencilStartY(input.cell_y);
                if (iLagInty >= setInfo.GridResolutionY)
                    iLagInty -= setInfo.GridResolutionY;
                else if (iLagInty < 0)
                    iLagInty += setInfo.GridResolutionY;
                int iLagIntz = blob.GetRealZ - z + startz + kernelSize / 2 - 1;
                if (iLagIntz >= setInfo.GridResolutionZ)
                    iLagIntz -= setInfo.GridResolutionZ;
                else if (iLagIntz < 0)
                    iLagIntz += setInfo.GridResolutionZ;

                if (derivative == 0)
                {
                    ClacSplineInterpolation(blob, ref input.lagInt, startz, endz, starty, endy, startx, endx, iLagIntz, iLagInty, iLagIntx, ref up);
                }
                else if (derivative == 1)
                {
                    ClacSplineGradient(blob, ref input.lagInt, startz, endz, starty, endy, startx, endx, iLagIntz, iLagInty, iLagIntx, ref up);

                    int dimensions = 3;
                    for (int j = 0; j < setInfo.Components; j++)
                    {
                        //NOTE: We do not divide by Dy here, because for the channel flow grid this is already included.
                        up[dimensions * j] = up[dimensions * j] / setInfo.Dx;
                        up[2 + dimensions * j] = up[2 + dimensions * j] / setInfo.Dz;
                    }
                }
                else if (derivative == 2)
                {
                    ClacSplineHessian(blob, ref input.lagInt, startz, endz, starty, endy, startx, endx, iLagIntz, iLagInty, iLagIntx, ref up);
                    
                    int hessian_components = 6;
                    for (int j = 0; j < setInfo.Components; j++)
                    {
                        //NOTE: We do not divide by Dy here, because for the channel flow grid this is already included.
                        up[j * hessian_components] = up[j * hessian_components] / setInfo.Dx / setInfo.Dx;
                        up[j * hessian_components + 1] = up[j * hessian_components + 1] / setInfo.Dx;
                        up[j * hessian_components + 2] = up[j * hessian_components + 2] / setInfo.Dx / setInfo.Dz;
                        up[j * hessian_components + 3] = up[j * hessian_components + 3];
                        up[j * hessian_components + 4] = up[j * hessian_components + 4] / setInfo.Dz;
                        up[j * hessian_components + 5] = up[j * hessian_components + 5] / setInfo.Dz / setInfo.Dz;
                    }
                }
            }
            return up;
        }
    }

}
