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
    public class GetChannelVelocity : GetChannelWorker
    {        
        public GetChannelVelocity(TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp,
            SqlConnection conn)
            : base (setInfo, spatialInterp, conn)
        {
        }

        public override SqlMetaData[] GetRecordMetaData()
        {
            return new SqlMetaData[] {
                new SqlMetaData("Req", SqlDbType.Int),
                new SqlMetaData("X", SqlDbType.Real),
                new SqlMetaData("Y", SqlDbType.Real),
                new SqlMetaData("Z", SqlDbType.Real)
            };
        }

        public override float[] GetResult(TurbulenceBlob blob, SQLUtility.InputRequest input)
        {
            throw new NotImplementedException();
        }

        public override float[] GetResult(TurbulenceBlob blob, SQLUtility.MHDInputRequest input)
        {
            return CalcLagInterpolation(blob, input);
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
        unsafe public float[] CalcLagInterpolation(TurbulenceBlob blob, SQLUtility.MHDInputRequest input)
        {
            float[] up = new float[3]; // Result value for the user

            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None)
            {
                float[] data = blob.data;
                int off0 = blob.GetLocalOffsetMHD(input.cell_z, input.cell_y, input.cell_x, 0);
                up[0] = data[off0];
                up[1] = data[off0 + 1];
                up[2] = data[off0 + 2];
            }
            else
            {
                int stencil_startz, stencil_starty, stencil_startx;

                // The coefficients are computed only once and cached, so that they don't have to be 
                // recomputed for each partial sum
                if (input.lagInt == null)
                {
                    stencil_startz = weights_z.GetStencilStart(input.cell_z, kernelSize);
                    stencil_starty = weights_y.GetStencilStart(input.cell_y, kernelSize);
                    stencil_startx = weights_x.GetStencilStart(input.cell_x, kernelSize);
                    input.lagInt = new double[kernelSize * 3];

                    LagInterpolation.InterpolantBarycentricWeights(kernelSize, input.x, ((ChannelFlowDataTable)setInfo).GridValuesX(stencil_startx, kernelSize),
                        weights_x.GetWeights(), 0, input.lagInt);
                    // The y weights are non-uniform and therefore we have to provide the cell index
                    // for the retrieval of the weights as well as for getting the grid values.
                    LagInterpolation.InterpolantBarycentricWeights(kernelSize, input.y, ((ChannelFlowDataTable)setInfo).GridValuesY(stencil_starty, kernelSize),
                        weights_y.GetWeights(input.cell_y), 1, input.lagInt);
                    LagInterpolation.InterpolantBarycentricWeights(kernelSize, input.z, ((ChannelFlowDataTable)setInfo).GridValuesZ(stencil_startz, kernelSize),
                        weights_z.GetWeights(), 2, input.lagInt);
                }

                // Wrap the coordinates into the grid space
                int x = ((input.cell_x % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
                // NOTE: Shouldn't need to wrap y.
                int y = ((input.cell_y % setInfo.GridResolutionY) + setInfo.GridResolutionY) % setInfo.GridResolutionY;
                int z = ((input.cell_z % setInfo.GridResolutionZ) + setInfo.GridResolutionZ) % setInfo.GridResolutionZ;
                
                stencil_startz = weights_z.GetStencilStart(z, kernelSize);
                stencil_starty = weights_y.GetStencilStart(y, kernelSize);
                stencil_startx = weights_x.GetStencilStart(x, kernelSize);

                float[] data = blob.data;
                int startz = 0, starty = 0, startx = 0, endz = 0, endy = 0, endx = 0;
                blob.GetSubcubeStart(stencil_startz,
                    stencil_starty,
                    stencil_startx, 
                    ref startz, ref starty, ref startx);
                blob.GetSubcubeEnd(stencil_startz + kernelSize - 1, 
                    weights_y.GetStencilEnd(y), 
                    stencil_startx + kernelSize - 1, 
                    ref endz, ref endy, ref endx);
                int off0 = startx * setInfo.Components;

                int iLagIntx = blob.GetRealX + startx - stencil_startx;
                if (iLagIntx >= setInfo.GridResolutionX)
                    iLagIntx -= setInfo.GridResolutionX;
                else if (iLagIntx < 0)
                    iLagIntx += setInfo.GridResolutionX;
                
                int iLagInty = blob.GetRealY + starty - stencil_starty;
                if (iLagInty >= setInfo.GridResolutionY)
                    iLagInty -= setInfo.GridResolutionY;
                else if (iLagInty < 0)
                    iLagInty += setInfo.GridResolutionY;

                int iLagIntz = blob.GetRealZ + startz - stencil_startz;
                if (iLagIntz >= setInfo.GridResolutionZ)
                    iLagIntz -= setInfo.GridResolutionZ;
                else if (iLagIntz < 0)
                    iLagIntz += setInfo.GridResolutionZ;

                fixed (double* lagint = input.lagInt)
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
                                    //need to determine the distance from the point, on which we are centered
                                    double c = lagint[iLagIntx + ix - startx];
                                    c1 += c * fdata[off];
                                    c2 += c * fdata[off + 1];
                                    c3 += c * fdata[off + 2];
                                    off += blob.GetComponents;
                                }
                                //need to determine the distance from the point, on which we are centered
                                double b = lagint[1 * kernelSize + iLagInty + iy - starty];
                                b1 += c1 * b;
                                b2 += c2 * b;
                                b3 += c3 * b;
                            }
                            //need to determine the distance from the point, on which we are centered
                            double a = lagint[2 * kernelSize + iLagIntz + iz - startz];
                            a1 += b1 * a;
                            a2 += b2 * a;
                            a3 += b3 * a;
                        }
                        up[0] = (float)a1;
                        up[1] = (float)a2;
                        up[2] = (float)a3;
                    }
                }
            }
            return up;
        }

    }

}
