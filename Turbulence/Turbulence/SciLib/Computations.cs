using System;
using System.Collections.Generic;
using System.Text;
using Turbulence.TurbLib;

namespace Turbulence.SciLib
{

    /// <summary>
    /// Computations to be computed inside the database.
    /// </summary>
    /// <remarks> 
    /// TODO: Rewrite most of these routines to use 1D arrays (or stack memory).
    /// </remarks>
    public class Computations
    {
        private TurbDataTable table;

        // Variables used in these functions are allocated once in a
        // futile effort to reduce garbage collection overhead.
        private int[] node;
        float[] flatCube;
        float[] lagInt;
        float[] up;

        float [, , ,] dataCube;
        float [, , ,] dataCubeLg;
        float[, , ,] dataCube_dx;
        float[, , ,] dataCube_dy;
        float[, , ,] dataCube_dz;
        float[, , ,] dataCube_dx2;
        float[, , ,] dataCube_dy2;
        float[, , ,] dataCube_dz2;
        float[, , ,] dataCube_dxdy;
        float[, , ,] dataCube_dxdz;
        float[, , ,] dataCube_dydz;
        float[, ,] dataLine;

        public Computations(TurbDataTable turbTable)
        {
            this.table = turbTable;

            // Allocate variables once and reuse over multiple queries.
            this.node = new int[3];  // Integer X,Y,Z coordinates
            this.up = new float[4];  // At MOST we have 4 components
            this.lagInt = new float[4 * 8];  // At MOST we have 4 components * 8th Order Lagrangian
            this.flatCube = new float[10 * 10 * 10 * turbTable.Components]; // storage of 8^3 cube as a flat array
            this.dataCube = new float[10, 10, 10, turbTable.Components]; // same, as a 3D array (slow!!!)
            this.dataCubeLg = new float[10, 10, 10, turbTable.Components];
            this.dataCube_dx = new float[10, 10, 10, turbTable.Components];
            this.dataCube_dy = new float[10, 10, 10, turbTable.Components];
            this.dataCube_dz = new float[10, 10, 10, turbTable.Components];
            this.dataCube_dx2 = new float[10, 10, 10, turbTable.Components];
            this.dataCube_dy2 = new float[10, 10, 10, turbTable.Components];
            this.dataCube_dz2 = new float[10, 10, 10, turbTable.Components];
            this.dataCube_dxdy = new float[10, 10, 10, turbTable.Components];
            this.dataCube_dxdz = new float[10, 10, 10, turbTable.Components];
            this.dataCube_dydz = new float[10, 10, 10, turbTable.Components];
            this.dataLine = new float[10, 3, turbTable.Components];
        }


        public double[] CalcVelocityOld(TurbulenceBlob blob, float[] particle,
            TurbulenceOptions.SpatialInterpolation spatialOpt)
        {
            double[] up = new double[3]; // create a new array since it will be stored for PCHIP

            if (spatialOpt == TurbulenceOptions.SpatialInterpolation.None)
            {
                for (int i = 0; i < 3; i++)
                {
                    node[i] = LagInterpolation.CalcNodeWithRound(particle[i], table.Dx);
                }
                // Directly return point
                up[0] = blob.GetDataValue(node[2], node[1], node[0], 0);
                up[1] = blob.GetDataValue(node[2], node[1], node[0], 1);
                up[2] = blob.GetDataValue(node[2], node[1], node[0], 2);
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    node[i] = LagInterpolation.CalcNode(particle[i], table.Dx);
                }
                int nOrder = - 1;
                if (spatialOpt == TurbulenceOptions.SpatialInterpolation.Lag4)
                { nOrder = 4; }
                else if (spatialOpt == TurbulenceOptions.SpatialInterpolation.Lag6)
                { nOrder = 6; }
                else if (spatialOpt == TurbulenceOptions.SpatialInterpolation.Lag8)
                { nOrder = 8; }
                else
                { throw new Exception("Invalid Spatial Interpolation Option"); }
                for (int i = 0; i < 3; i++)
                {                
                    LagInterpolation.InterpolantOpt(i, particle[i], table.DxFloat,
                        node[i], lagInt, nOrder);
                }
                blob.GetFlatDataCubeCorneredAtPoint(node[2] - nOrder/2 + 1, node[1]- nOrder/2 + 1, node[0] - nOrder/2 + 1, nOrder, flatCube);
                LagInterpolation.EvaluateOpt(flatCube, up, lagInt, nOrder, table.Components, 3);
            }
            return up;
        }

        public double[] CalcVelocityWithPressureOld(TurbulenceBlob blob, float[] particle,
            TurbulenceOptions.SpatialInterpolation spatialOpt)
        {
            double[] up = new double[4]; // create a new array since it will be stored for PCHIP

            if (spatialOpt == TurbulenceOptions.SpatialInterpolation.None)
            {
                for (int i = 0; i < 3; i++)
                {
                    node[i] = LagInterpolation.CalcNodeWithRound(particle[i], table.Dx);
                }
                up[0] = blob.GetDataValue(node[2], node[1], node[0], 0);
                up[1] = blob.GetDataValue(node[2], node[1], node[0], 1);
                up[2] = blob.GetDataValue(node[2], node[1], node[0], 2);
                up[3] = blob.GetDataValue(node[2], node[1], node[0], 3);
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    node[i] = LagInterpolation.CalcNode(particle[i], table.Dx);
                }
                int nOrder = -1;
                if (spatialOpt == TurbulenceOptions.SpatialInterpolation.Lag4)
                { nOrder = 4; }
                else if (spatialOpt == TurbulenceOptions.SpatialInterpolation.Lag6)
                { nOrder = 6; }
                else if (spatialOpt == TurbulenceOptions.SpatialInterpolation.Lag8)
                { nOrder = 8; }
                else
                { throw new Exception("Invalid Spatial Interpolation Option"); }
                for (int i = 0; i < 3; i++)
                {
                    LagInterpolation.InterpolantOpt(i, particle[i], table.DxFloat,
                        node[i], lagInt, nOrder);
                }
                blob.GetFlatDataCubeCorneredAtPoint(node[2] - nOrder / 2 + 1, node[1] - nOrder / 2 + 1, node[0] - nOrder / 2 + 1, nOrder, flatCube);
               
                LagInterpolation.EvaluateOpt(flatCube, up, lagInt, nOrder, table.Components, 4);
            }
            return up;
        }



        /// <summary>
        /// Calculate the Pressure Hessian.
        /// </summary>
        /// <remarks>
        /// This method should be updated to use one dimensional ararys.
        /// </remarks>
        public double[] CalcPressureHessian(TurbulenceBlob blob, float[] particle,
            TurbulenceOptions.SpatialInterpolation spatialOpt)
        {
            float[][] lagInt = new float[3][];
            double[] d2pdxidxj = new double[6];

            int nOrder = -1;
            if (spatialOpt == TurbulenceOptions.SpatialInterpolation.None_Fd4)
            {
                nOrder = 4;
                int idim = 0;

                for (int i = 0; i < 3; i++)
                {
                    node[i] = LagInterpolation.CalcNodeWithRound(particle[i], table.Dx);
                }
                blob.GetDataCubeAroundPoint(node[2], node[1], node[0], nOrder + 1, dataCube);

                d2pdxidxj[0] = FiniteDiff.SecFiniteDiff4(table.DxFloat,
                    dataCube[2, 2, 0, idim], dataCube[2, 2, 1, idim], dataCube[2, 2, 2, idim], dataCube[2, 2, 3, idim], dataCube[2, 2, 4, idim]);                
                d2pdxidxj[1] = FiniteDiff.CrossFiniteDiff4(table.DxFloat,
                    dataCube[2, 0, 0, idim], dataCube[2, 4, 0, idim], dataCube[2, 4, 4, idim], dataCube[2, 0, 4, idim],
                    dataCube[2, 1, 1, idim], dataCube[2, 3, 1, idim], dataCube[2, 3, 3, idim], dataCube[2, 1, 3, idim]);
                d2pdxidxj[2] = FiniteDiff.CrossFiniteDiff4(table.DxFloat,
                    dataCube[0, 2, 0, idim], dataCube[4, 2, 0, idim], dataCube[4, 2, 4, idim], dataCube[0, 2, 4, idim],
                    dataCube[1, 2, 1, idim], dataCube[3, 2, 1, idim], dataCube[3, 2, 3, idim], dataCube[1, 2, 3, idim]);
                d2pdxidxj[3] = FiniteDiff.SecFiniteDiff4(table.DxFloat,
                    dataCube[2, 0, 2, idim], dataCube[2, 1, 2, idim], dataCube[2, 2, 2, idim], dataCube[2, 3, 2, idim], dataCube[2, 4, 2, idim]);
                d2pdxidxj[4] = FiniteDiff.CrossFiniteDiff4(table.DxFloat,
                    dataCube[0, 0, 2, idim], dataCube[4, 0, 2, idim], dataCube[4, 4, 2, idim], dataCube[0, 4, 2, idim],
                    dataCube[1, 1, 2, idim], dataCube[3, 1, 2, idim], dataCube[3, 3, 2, idim], dataCube[1, 3, 2, idim]);
                d2pdxidxj[5] = FiniteDiff.SecFiniteDiff4(table.DxFloat,
                    dataCube[0, 2, 2, idim], dataCube[1, 2, 2, idim], dataCube[2, 2, 2, idim], dataCube[3, 2, 2, idim], dataCube[4, 2, 2, idim]);
            }

            else if (spatialOpt == TurbulenceOptions.SpatialInterpolation.None_Fd6)
            {
                nOrder = 6;

                for (int i = 0; i < 3; i++)
                {
                    node[i] = LagInterpolation.CalcNodeWithRound(particle[i], table.Dx);
                }
                blob.GetDataCubeAroundPoint(node[2], node[1], node[0], nOrder + 1, dataCube);

                d2pdxidxj[0] = FiniteDiff.SecFiniteDiff6(table.DxFloat,
                    dataCube[3, 3, 0, 3], dataCube[3, 3, 1, 3], dataCube[3, 3, 2, 3], dataCube[3, 3, 3, 3], dataCube[3, 3, 4, 3], dataCube[3, 3, 5, 3], dataCube[3, 3, 6, 3]);
                d2pdxidxj[1] = FiniteDiff.CrossFiniteDiff6(table.DxFloat,
                    dataCube[3, 0, 0, 3], dataCube[3, 6, 0, 3], dataCube[3, 6, 6, 3], dataCube[3, 0, 6, 3],
                    dataCube[3, 1, 1, 3], dataCube[3, 5, 1, 3], dataCube[3, 5, 5, 3], dataCube[3, 1, 5, 3],
                    dataCube[3, 2, 2, 3], dataCube[3, 4, 2, 3], dataCube[3, 4, 4, 3], dataCube[3, 2, 4, 3]);
                d2pdxidxj[2] = FiniteDiff.CrossFiniteDiff6(table.DxFloat,
                    dataCube[0, 3, 0, 3], dataCube[6, 3, 0, 3], dataCube[6, 3, 6, 3], dataCube[0, 3, 6, 3],
                    dataCube[1, 3, 1, 3], dataCube[5, 3, 1, 3], dataCube[5, 3, 5, 3], dataCube[1, 3, 5, 3],
                    dataCube[2, 3, 2, 3], dataCube[4, 3, 2, 3], dataCube[4, 3, 4, 3], dataCube[2, 3, 4, 3]);
                d2pdxidxj[3] = FiniteDiff.SecFiniteDiff6(table.DxFloat,
                    dataCube[3, 0, 3, 3], dataCube[3, 1, 3, 3], dataCube[3, 2, 3, 3], dataCube[3, 3, 3, 3], dataCube[3, 4, 3, 3], dataCube[3, 5, 3, 3], dataCube[3, 6, 3, 3]);
                d2pdxidxj[4] = FiniteDiff.CrossFiniteDiff6(table.DxFloat,
                    dataCube[0, 0, 3, 3], dataCube[6, 0, 3, 3], dataCube[6, 6, 3, 3], dataCube[0, 6, 3, 3],
                    dataCube[1, 1, 3, 3], dataCube[5, 1, 3, 3], dataCube[5, 5, 3, 3], dataCube[1, 5, 3, 3],
                    dataCube[2, 2, 3, 3], dataCube[4, 2, 3, 3], dataCube[4, 4, 3, 3], dataCube[2, 4, 3, 3]);
                d2pdxidxj[5] = FiniteDiff.SecFiniteDiff6(table.DxFloat,
                    dataCube[0, 3, 3, 3], dataCube[1, 3, 3, 3], dataCube[2, 3, 3, 3], dataCube[3, 3, 3, 3], dataCube[4, 3, 3, 3], dataCube[5, 3, 3, 3], dataCube[6, 3, 3, 3]);
            }

            else if (spatialOpt == TurbulenceOptions.SpatialInterpolation.None_Fd8)
            {
                nOrder = 8;

                for (int i = 0; i < 3; i++)
                {
                    node[i] = LagInterpolation.CalcNodeWithRound(particle[i], table.Dx);
                }
                blob.GetDataCubeAroundPoint(node[2], node[1], node[0], nOrder + 1, dataCube);

                d2pdxidxj[0] = FiniteDiff.SecFiniteDiff8(table.DxFloat,
                    dataCube[4, 4, 0, 3], dataCube[4, 4, 1, 3], dataCube[4, 4, 2, 3], dataCube[4, 4, 3, 3],
                    dataCube[4, 4, 4, 3], dataCube[4, 4, 5, 3], dataCube[4, 4, 6, 3], dataCube[4, 4, 7, 3], dataCube[4, 4, 8, 3]);
                d2pdxidxj[1] = FiniteDiff.CrossFiniteDiff8(table.DxFloat,
                    dataCube[4, 0, 0, 3], dataCube[4, 8, 0, 3], dataCube[4, 8, 8, 3], dataCube[4, 0, 8, 3],
                    dataCube[4, 1, 1, 3], dataCube[4, 7, 1, 3], dataCube[4, 7, 7, 3], dataCube[4, 1, 7, 3],
                    dataCube[4, 2, 2, 3], dataCube[4, 6, 2, 3], dataCube[4, 6, 6, 3], dataCube[4, 2, 6, 3],
                    dataCube[4, 3, 3, 3], dataCube[4, 5, 3, 3], dataCube[4, 5, 5, 3], dataCube[4, 3, 5, 3]);
                d2pdxidxj[2] = FiniteDiff.CrossFiniteDiff8(table.DxFloat,
                    dataCube[0, 4, 0, 3], dataCube[8, 4, 0, 3], dataCube[8, 4, 8, 3], dataCube[0, 4, 8, 3],
                    dataCube[1, 4, 1, 3], dataCube[7, 4, 1, 3], dataCube[7, 4, 7, 3], dataCube[1, 4, 7, 3],
                    dataCube[2, 4, 2, 3], dataCube[6, 4, 2, 3], dataCube[6, 4, 6, 3], dataCube[2, 4, 6, 3],
                    dataCube[3, 4, 3, 3], dataCube[5, 4, 3, 3], dataCube[5, 4, 5, 3], dataCube[3, 4, 5, 3]);
                d2pdxidxj[3] = FiniteDiff.SecFiniteDiff8(table.DxFloat,
                    dataCube[4, 0, 4, 3], dataCube[4, 1, 4, 3], dataCube[4, 2, 4, 3], dataCube[4, 3, 4, 3],
                    dataCube[4, 4, 4, 3], dataCube[4, 5, 4, 3], dataCube[4, 6, 4, 3], dataCube[4, 7, 4, 3], dataCube[4, 8, 4, 3]);
                d2pdxidxj[4] = FiniteDiff.CrossFiniteDiff8(table.DxFloat,
                    dataCube[0, 0, 4, 3], dataCube[8, 0, 4, 3], dataCube[8, 8, 4, 3], dataCube[0, 8, 4, 3],
                    dataCube[1, 1, 4, 3], dataCube[7, 1, 4, 3], dataCube[7, 7, 4, 3], dataCube[1, 7, 4, 3],
                    dataCube[2, 2, 4, 3], dataCube[6, 2, 4, 3], dataCube[6, 6, 4, 3], dataCube[2, 6, 4, 3],
                    dataCube[3, 3, 4, 3], dataCube[5, 3, 4, 3], dataCube[5, 5, 4, 3], dataCube[3, 5, 4, 3]);
                d2pdxidxj[5] = FiniteDiff.SecFiniteDiff8(table.DxFloat,
                    dataCube[0, 4, 4, 3], dataCube[1, 4, 4, 3], dataCube[2, 4, 4, 3], dataCube[3, 4, 4, 3],
                    dataCube[4, 4, 4, 3], dataCube[5, 4, 4, 3], dataCube[6, 4, 4, 3], dataCube[7, 4, 4, 3], dataCube[8, 4, 4, 3]);
            }

            else if (spatialOpt == TurbulenceOptions.SpatialInterpolation.Fd4Lag4)
            {
                nOrder = 4;
                for (int i = 0; i < 3; i++)
                {
                    lagInt[i] = new float[nOrder];

                    node[i] = LagInterpolation.CalcNode(particle[i], table.Dx);
                    LagInterpolation.Interpolant(particle[i], table.DxFloat,
                        node[i], ref lagInt[i], nOrder);
                }

                blob.GetDataCubeCorneredAtPoint(node[2]-3, node[1]-3, node[0]-3, 8, dataCube);

                int idim = 0;
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        for (int k = 0; k < 4; k++)
                        {
                            dataCube_dx2[i, j, k, idim] = FiniteDiff.SecFiniteDiff4(table.DxFloat,
                                dataCube[i + 2, j + 2, k, idim], dataCube[i + 2, j + 2, k + 1, idim], dataCube[i + 2, j + 2, k + 2, idim],
                                dataCube[i + 2, j + 2, k + 3, idim], dataCube[i + 2, j + 2, k + 4, idim]);
                            dataCube_dxdy[i, j, k, idim] = FiniteDiff.CrossFiniteDiff4(table.DxFloat,
                                dataCube[i + 2, j, k, idim], dataCube[i + 2, j + 4, k, idim],
                                dataCube[i + 2, j + 4, k + 4, idim], dataCube[i + 2, j, k + 4, idim],
                                dataCube[i + 2, j + 1, k + 1, idim], dataCube[i + 2, j + 3, k + 1, idim],
                                dataCube[i + 2, j + 3, k + 3, idim], dataCube[i + 2, j + 1, k + 3, idim]);
                            dataCube_dxdz[i, j, k, idim] = FiniteDiff.CrossFiniteDiff4(table.DxFloat,
                                dataCube[i, j + 2, k, idim], dataCube[i + 4, j + 2, k, idim],
                                dataCube[i + 4, j + 2, k + 4, idim], dataCube[i, j + 2, k + 4, idim],
                                dataCube[i + 1, j + 2, k + 1, idim], dataCube[i + 3, j + 2, k + 1, idim],
                                dataCube[i + 3, j + 2, k + 3, idim], dataCube[i + 1, j + 2, k + 3, idim]);
                            dataCube_dy2[i, j, k, idim] = FiniteDiff.SecFiniteDiff4(table.DxFloat,
                                dataCube[i + 2, j, k + 2, idim], dataCube[i + 2, j + 1, k + 2, idim], dataCube[i + 2, j + 2, k + 2, idim],
                                dataCube[i + 2, j + 3, k + 2, idim], dataCube[i + 2, j + 4, k + 2, idim]);
                            dataCube_dydz[i, j, k, idim] = FiniteDiff.CrossFiniteDiff4(table.DxFloat,
                                dataCube[i, j, k + 2, idim], dataCube[i + 4, j, k + 2, idim],
                                dataCube[i + 4, j + 4, k + 2, idim], dataCube[i, j + 4, k + 2, idim],
                                dataCube[i + 1, j + 1, k + 2, idim], dataCube[i + 3, j + 1, k + 2, idim],
                                dataCube[i + 3, j + 3, k + 2, idim], dataCube[i + 1, j + 3, k + 2, idim]);
                            dataCube_dz2[i, j, k, idim] = FiniteDiff.SecFiniteDiff4(table.DxFloat,
                                dataCube[i, j + 2, k + 2, idim], dataCube[i + 1, j + 2, k + 2, idim], dataCube[i + 2, j + 2, k + 2, idim],
                                dataCube[i + 3, j + 2, k + 2, idim], dataCube[i + 4, j + 2, k + 2, idim]);
                        }
                    }
                }


                // d_x2 p
                LagInterpolation.EvaluatePressure(ref dataCube_dx2, ref d2pdxidxj[0], ref lagInt[0],
                    ref lagInt[1], ref lagInt[2], nOrder);

                // d_xy p
                LagInterpolation.EvaluatePressure(ref dataCube_dxdy, ref d2pdxidxj[1], ref lagInt[0],
                    ref lagInt[1], ref lagInt[2], nOrder);

                // d_xz p
                LagInterpolation.EvaluatePressure(ref dataCube_dxdz, ref d2pdxidxj[2], ref lagInt[0],
                    ref lagInt[1], ref lagInt[2], nOrder);

                // d_y2 p
                LagInterpolation.EvaluatePressure(ref dataCube_dy2, ref d2pdxidxj[3], ref lagInt[0],
                    ref lagInt[1], ref lagInt[2], nOrder);

                // d_yz p
                LagInterpolation.EvaluatePressure(ref dataCube_dydz, ref d2pdxidxj[4], ref lagInt[0],
                    ref lagInt[1], ref lagInt[2], nOrder);

                // d_z2 p
                LagInterpolation.EvaluatePressure(ref dataCube_dz2, ref d2pdxidxj[5], ref lagInt[0],
                    ref lagInt[1], ref lagInt[2], nOrder);

            }
            else
            {
                throw new Exception("Invalid Spatial Interpolation Option");
            }            

            return d2pdxidxj;
        }


        /// <summary>
        /// Calculate the Velocity Hessian.
        /// </summary>
        /// <remarks>
        /// This method should be updated to use one dimensional ararys.
        /// </remarks>
        public double[] CalcVelocityHessian(TurbulenceBlob blob, float[] particle,
            TurbulenceOptions.SpatialInterpolation spatialOpt)
        {
            float[][] lagInt = new float[3][];
            float[][] d2ukdxidxj = new float[6][];
            double[] result = new double[18];

            for (int i = 0; i < 6; i++)
            {
                d2ukdxidxj[i] = new float[3];
            }

            int nOrder = -1;
            if (spatialOpt == TurbulenceOptions.SpatialInterpolation.None_Fd4)
            {
                nOrder = 4;

                for (int i = 0; i < 3; i++)
                {
                    node[i] = LagInterpolation.CalcNodeWithRound(particle[i], table.Dx);
                }
                blob.GetDataCubeAroundPoint(node[2], node[1], node[0], nOrder + 1, dataCube);

                for (int i = 0; i < 3; i++)
                {
                    d2ukdxidxj[0][i] = FiniteDiff.SecFiniteDiff4(table.DxFloat,
                        dataCube[2, 2, 0, i], dataCube[2, 2, 1, i], dataCube[2, 2, 2, i], dataCube[2, 2, 3, i], dataCube[2, 2, 4, i]);
                    d2ukdxidxj[1][i] = FiniteDiff.CrossFiniteDiff4(table.DxFloat,
                        dataCube[2, 0, 0, i], dataCube[2, 4, 0, i], dataCube[2, 4, 4, i], dataCube[2, 0, 4, i],
                        dataCube[2, 1, 1, i], dataCube[2, 3, 1, i], dataCube[2, 3, 3, i], dataCube[2, 1, 3, i]);
                    d2ukdxidxj[2][i] = FiniteDiff.CrossFiniteDiff4(table.DxFloat,
                        dataCube[0, 2, 0, i], dataCube[4, 2, 0, i], dataCube[4, 2, 4, i], dataCube[0, 2, 4, i],
                        dataCube[1, 2, 1, i], dataCube[3, 2, 1, i], dataCube[3, 2, 3, i], dataCube[1, 2, 3, i]);
                    d2ukdxidxj[3][i] = FiniteDiff.SecFiniteDiff4(table.DxFloat,
                        dataCube[2, 0, 2, i], dataCube[2, 1, 2, i], dataCube[2, 2, 2, i], dataCube[2, 3, 2, i], dataCube[2, 4, 2, i]);
                    d2ukdxidxj[4][i] = FiniteDiff.CrossFiniteDiff4(table.DxFloat,
                        dataCube[0, 0, 2, i], dataCube[4, 0, 2, i], dataCube[4, 4, 2, i], dataCube[0, 4, 2, i],
                        dataCube[1, 1, 2, i], dataCube[3, 1, 2, i], dataCube[3, 3, 2, i], dataCube[1, 3, 2, i]);
                    d2ukdxidxj[5][i] = FiniteDiff.SecFiniteDiff4(table.DxFloat,
                        dataCube[0, 2, 2, i], dataCube[1, 2, 2, i], dataCube[2, 2, 2, i], dataCube[3, 2, 2, i], dataCube[4, 2, 2, i]);
                }
            }

            else if (spatialOpt == TurbulenceOptions.SpatialInterpolation.None_Fd6)
            {
                nOrder = 6;

                for (int i = 0; i < 3; i++)
                {
                    node[i] = LagInterpolation.CalcNodeWithRound(particle[i], table.Dx);
                }
                blob.GetDataCubeAroundPoint(node[2], node[1], node[0], nOrder + 1, dataCube);

                for (int i = 0; i < 3; i++)
                {
                    d2ukdxidxj[0][i] = FiniteDiff.SecFiniteDiff6(table.DxFloat,
                        dataCube[3, 3, 0, i], dataCube[3, 3, 1, i], dataCube[3, 3, 2, i], dataCube[3, 3, 3, i], dataCube[3, 3, 4, i], dataCube[3, 3, 5, i], dataCube[3, 3, 6, i]);
                    d2ukdxidxj[1][i] = FiniteDiff.CrossFiniteDiff6(table.DxFloat,
                        dataCube[3, 0, 0, i], dataCube[3, 6, 0, i], dataCube[3, 6, 6, i], dataCube[3, 0, 6, i],
                        dataCube[3, 1, 1, i], dataCube[3, 5, 1, i], dataCube[3, 5, 5, i], dataCube[3, 1, 5, i],
                        dataCube[3, 2, 2, i], dataCube[3, 4, 2, i], dataCube[3, 4, 4, i], dataCube[3, 2, 4, i]);
                    d2ukdxidxj[2][i] = FiniteDiff.CrossFiniteDiff6(table.DxFloat,
                        dataCube[0, 3, 0, i], dataCube[6, 3, 0, i], dataCube[6, 3, 6, i], dataCube[0, 3, 6, i],
                        dataCube[1, 3, 1, i], dataCube[5, 3, 1, i], dataCube[5, 3, 5, i], dataCube[1, 3, 5, i],
                        dataCube[2, 3, 2, i], dataCube[4, 3, 2, i], dataCube[4, 3, 4, i], dataCube[2, 3, 4, i]);
                    d2ukdxidxj[3][i] = FiniteDiff.SecFiniteDiff6(table.DxFloat,
                        dataCube[3, 0, 3, i], dataCube[3, 1, 3, i], dataCube[3, 2, 3, i], dataCube[3, 3, 3, i], dataCube[3, 4, 3, i], dataCube[3, 5, 3, i], dataCube[3, 6, 3, i]);
                    d2ukdxidxj[4][i] = FiniteDiff.CrossFiniteDiff6(table.DxFloat,
                        dataCube[0, 0, 3, i], dataCube[6, 0, 3, i], dataCube[6, 6, 3, i], dataCube[0, 6, 3, i],
                        dataCube[1, 1, 3, i], dataCube[5, 1, 3, i], dataCube[5, 5, 3, i], dataCube[1, 5, 3, i],
                        dataCube[2, 2, 3, i], dataCube[4, 2, 3, i], dataCube[4, 4, 3, i], dataCube[2, 4, 3, i]);
                    d2ukdxidxj[5][i] = FiniteDiff.SecFiniteDiff6(table.DxFloat,
                        dataCube[0, 3, 3, i], dataCube[1, 3, 3, i], dataCube[2, 3, 3, i], dataCube[3, 3, 3, i], dataCube[4, 3, 3, i], dataCube[5, 3, 3, i], dataCube[6, 3, 3, i]);
                }
            }

            else if (spatialOpt == TurbulenceOptions.SpatialInterpolation.None_Fd8)
            {
                nOrder = 8;

                for (int i = 0; i < 3; i++)
                {
                    node[i] = LagInterpolation.CalcNodeWithRound(particle[i], table.Dx);
                }
                blob.GetDataCubeAroundPoint(node[2], node[1], node[0], nOrder + 1, dataCube);

                for (int i = 0; i < 3; i++)
                {
                    d2ukdxidxj[0][i] = FiniteDiff.SecFiniteDiff8(table.DxFloat,
                        dataCube[4, 4, 0, i], dataCube[4, 4, 1, i], dataCube[4, 4, 2, i], dataCube[4, 4, 3, i],
                        dataCube[4, 4, 4, i], dataCube[4, 4, 5, i], dataCube[4, 4, 6, i], dataCube[4, 4, 7, i], dataCube[4, 4, 8, i]);
                    d2ukdxidxj[1][i] = FiniteDiff.CrossFiniteDiff8(table.DxFloat,
                        dataCube[4, 0, 0, i], dataCube[4, 8, 0, i], dataCube[4, 8, 8, i], dataCube[4, 0, 8, i],
                        dataCube[4, 1, 1, i], dataCube[4, 7, 1, i], dataCube[4, 7, 7, i], dataCube[4, 1, 7, i],
                        dataCube[4, 2, 2, i], dataCube[4, 6, 2, i], dataCube[4, 6, 6, i], dataCube[4, 2, 6, i],
                        dataCube[4, 3, 3, i], dataCube[4, 5, 3, i], dataCube[4, 5, 5, i], dataCube[4, 3, 5, i]);
                    d2ukdxidxj[2][i] = FiniteDiff.CrossFiniteDiff8(table.DxFloat,
                        dataCube[0, 4, 0, i], dataCube[8, 4, 0, i], dataCube[8, 4, 8, i], dataCube[0, 4, 8, i],
                        dataCube[1, 4, 1, i], dataCube[7, 4, 1, i], dataCube[7, 4, 7, i], dataCube[1, 4, 7, i],
                        dataCube[2, 4, 2, i], dataCube[6, 4, 2, i], dataCube[6, 4, 6, i], dataCube[2, 4, 6, i],
                        dataCube[3, 4, 3, i], dataCube[5, 4, 3, i], dataCube[5, 4, 5, i], dataCube[3, 4, 5, i]);
                    d2ukdxidxj[3][i] = FiniteDiff.SecFiniteDiff8(table.DxFloat,
                        dataCube[4, 0, 4, i], dataCube[4, 1, 4, i], dataCube[4, 2, 4, i], dataCube[4, 3, 4, i],
                        dataCube[4, 4, 4, i], dataCube[4, 5, 4, i], dataCube[4, 6, 4, i], dataCube[4, 7, 4, i], dataCube[4, 8, 4, i]);
                    d2ukdxidxj[4][i] = FiniteDiff.CrossFiniteDiff8(table.DxFloat,
                        dataCube[0, 0, 4, i], dataCube[8, 0, 4, i], dataCube[8, 8, 4, i], dataCube[0, 8, 4, i],
                        dataCube[1, 1, 4, i], dataCube[7, 1, 4, i], dataCube[7, 7, 4, i], dataCube[1, 7, 4, i],
                        dataCube[2, 2, 4, i], dataCube[6, 2, 4, i], dataCube[6, 6, 4, i], dataCube[2, 6, 4, i],
                        dataCube[3, 3, 4, i], dataCube[5, 3, 4, i], dataCube[5, 5, 4, i], dataCube[3, 5, 4, i]);
                    d2ukdxidxj[5][i] = FiniteDiff.SecFiniteDiff8(table.DxFloat,
                        dataCube[0, 4, 4, i], dataCube[1, 4, 4, i], dataCube[2, 4, 4, i], dataCube[3, 4, 4, i],
                        dataCube[4, 4, 4, i], dataCube[5, 4, 4, i], dataCube[6, 4, 4, i], dataCube[7, 4, 4, i], dataCube[8, 4, 4, i]);
                }
            }

            else if (spatialOpt == TurbulenceOptions.SpatialInterpolation.Fd4Lag4)
            {
                nOrder = 4;
                for (int i = 0; i < 3; i++)
                {
                    lagInt[i] = new float[nOrder];

                    node[i] = LagInterpolation.CalcNode(particle[i], table.Dx);
                    LagInterpolation.Interpolant(particle[i], table.DxFloat,
                        node[i], ref lagInt[i], nOrder);
                }

                blob.GetDataCubeCorneredAtPoint(node[2]-3, node[1]-3, node[0]-3, 8, dataCube);

                for (int idim = 0; idim < 3; idim++)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            for (int k = 0; k < 4; k++)
                            {
                                dataCube_dx2[i, j, k, idim] = FiniteDiff.SecFiniteDiff4(table.DxFloat,
                                    dataCube[i + 2, j + 2, k, idim], dataCube[i + 2, j + 2, k + 1, idim], dataCube[i + 2, j + 2, k + 2, idim],
                                    dataCube[i + 2, j + 2, k + 3, idim], dataCube[i + 2, j + 2, k + 4, idim]);
                                dataCube_dxdy[i, j, k, idim] = FiniteDiff.CrossFiniteDiff4(table.DxFloat,
                                    dataCube[i + 2, j, k, idim], dataCube[i + 2, j + 4, k, idim],
                                    dataCube[i + 2, j + 4, k + 4, idim], dataCube[i + 2, j, k + 4, idim],
                                    dataCube[i + 2, j + 1, k + 1, idim], dataCube[i + 2, j + 3, k + 1, idim],
                                    dataCube[i + 2, j + 3, k + 3, idim], dataCube[i + 2, j + 1, k + 3, idim]);
                                dataCube_dxdz[i, j, k, idim] = FiniteDiff.CrossFiniteDiff4(table.DxFloat,
                                    dataCube[i, j + 2, k, idim], dataCube[i + 4, j + 2, k, idim],
                                    dataCube[i + 4, j + 2, k + 4, idim], dataCube[i, j + 2, k + 4, idim],
                                    dataCube[i + 1, j + 2, k + 1, idim], dataCube[i + 3, j + 2, k + 1, idim],
                                    dataCube[i + 3, j + 2, k + 3, idim], dataCube[i + 1, j + 2, k + 3, idim]);
                                dataCube_dy2[i, j, k, idim] = FiniteDiff.SecFiniteDiff4(table.DxFloat,
                                    dataCube[i + 2, j, k + 2, idim], dataCube[i + 2, j + 1, k + 2, idim], dataCube[i + 2, j + 2, k + 2, idim],
                                    dataCube[i + 2, j + 3, k + 2, idim], dataCube[i + 2, j + 4, k + 2, idim]);
                                dataCube_dydz[i, j, k, idim] = FiniteDiff.CrossFiniteDiff4(table.DxFloat,
                                    dataCube[i, j, k + 2, idim], dataCube[i + 4, j, k + 2, idim],
                                    dataCube[i + 4, j + 4, k + 2, idim], dataCube[i, j + 4, k + 2, idim],
                                    dataCube[i + 1, j + 1, k + 2, idim], dataCube[i + 3, j + 1, k + 2, idim],
                                    dataCube[i + 3, j + 3, k + 2, idim], dataCube[i + 1, j + 3, k + 2, idim]);
                                dataCube_dz2[i, j, k, idim] = FiniteDiff.SecFiniteDiff4(table.DxFloat,
                                    dataCube[i, j + 2, k + 2, idim], dataCube[i + 1, j + 2, k + 2, idim], dataCube[i + 2, j + 2, k + 2, idim],
                                    dataCube[i + 3, j + 2, k + 2, idim], dataCube[i + 4, j + 2, k + 2, idim]);
                            }
                        }
                    }
                }


                // d_x2 u_i
                LagInterpolation.Evaluate(ref dataCube_dx2, ref d2ukdxidxj[0], ref lagInt[0],
                    ref lagInt[1], ref lagInt[2], nOrder);

                // d_xy u_i
                LagInterpolation.Evaluate(ref dataCube_dxdy, ref d2ukdxidxj[1], ref lagInt[0],
                    ref lagInt[1], ref lagInt[2], nOrder);

                // d_xz u_i
                LagInterpolation.Evaluate(ref dataCube_dxdz, ref d2ukdxidxj[2], ref lagInt[0],
                    ref lagInt[1], ref lagInt[2], nOrder);

                // d_y2 u_i
                LagInterpolation.Evaluate(ref dataCube_dy2, ref d2ukdxidxj[3], ref lagInt[0],
                    ref lagInt[1], ref lagInt[2], nOrder);

                // d_yz u_i
                LagInterpolation.Evaluate(ref dataCube_dydz, ref d2ukdxidxj[4], ref lagInt[0],
                    ref lagInt[1], ref lagInt[2], nOrder);

                // d_z2 u_i
                LagInterpolation.Evaluate(ref dataCube_dz2, ref d2ukdxidxj[5], ref lagInt[0],
                    ref lagInt[1], ref lagInt[2], nOrder);

            }
            else
            {
                throw new Exception("Invalid Spatial Interpolation Option");
            }

            for (int i = 0; i < 3; i++)
                for (int ii = 0; ii < 6; ii++)
                    result[i * 6 + ii] = d2ukdxidxj[ii][i];
            return result;
        }


        public double[] CalcVelocityGradient(TurbulenceBlob blob, float[] particle,
            TurbulenceOptions.SpatialInterpolation spatialOpt)
        {
            float[][] lagInt = new float[3][];
            //float[][] lagInt_dx = new float[3][];
            float[][] duidxj = new float[3][];

            double[] result = new double[9];

            int nOrder = -1;            
            if (spatialOpt == TurbulenceOptions.SpatialInterpolation.None_Fd4)
            {
                nOrder = 4;
                int length = nOrder / 2;

                for (int i = 0; i < 3; i++)
                {
                    node[i] = LagInterpolation.CalcNodeWithRound(particle[i], table.Dx);
                }
                blob.GetDataLinesAroundPoint(node[2], node[1], node[0], length, dataLine);

                for (int i = 0; i < 3; i++)
                {
                    for (int ii = 0; ii < 3; ii++)
                    {
                        result[i * 3 + ii] = 2.0f / 3.0f / table.DxFloat * (dataLine[3, ii, i] - dataLine[1, ii, i])
                                - 1.0f / 12.0f / table.DxFloat * (dataLine[4, ii, i] - dataLine[0, ii, i]);
                    }
                }
            }

            else if (spatialOpt == TurbulenceOptions.SpatialInterpolation.None_Fd6)
            {
                nOrder = 6;
                int length = nOrder / 2;

                for (int i = 0; i < 3; i++)
                {
                    node[i] = LagInterpolation.CalcNodeWithRound(particle[i], table.Dx);
                }
                blob.GetDataLinesAroundPoint(node[2], node[1], node[0], length, dataLine);

                for (int i = 0; i < 3; i++)
                {
                    for (int ii = 0; ii < 3; ii++)
                    {
                        result[i * 3 + ii] = 3.0f / 4.0f / table.DxFloat * (dataLine[4, ii, i] - dataLine[2, ii, i])
                                - 3.0f / 20.0f / table.DxFloat * (dataLine[5, ii, i] - dataLine[1, ii, i])
                                + 1.0f / 60.0f / table.DxFloat * (dataLine[6, ii, i] - dataLine[0, ii, i]);
                    }
                }
            }

            else if (spatialOpt == TurbulenceOptions.SpatialInterpolation.None_Fd8)
            {
                nOrder = 8;
                int length = nOrder / 2;

                for (int i = 0; i < 3; i++)
                {
                    node[i] = LagInterpolation.CalcNodeWithRound(particle[i], table.Dx);
                }
                blob.GetDataLinesAroundPoint(node[2], node[1], node[0], length, dataLine);

                for (int i = 0; i < 3; i++)
                {
                    for (int ii = 0; ii < 3; ii++)
                    {
                        result[i * 3 + ii] = 4.0f / 5.0f / table.DxFloat * (dataLine[5, ii, i] - dataLine[3, ii, i])
                                - 1.0f / 5.0f / table.DxFloat * (dataLine[6, ii, i] - dataLine[2, ii, i])
                                + 4.0f / 105.0f / table.DxFloat * (dataLine[7, ii, i] - dataLine[1, ii, i])
                                - 1.0f / 280.0f / table.DxFloat * (dataLine[8, ii, i] - dataLine[0, ii, i]);
                    }
                }
            }

            else if (spatialOpt == TurbulenceOptions.SpatialInterpolation.Fd4Lag4)
            { 
                nOrder = 4;
                for (int i = 0; i < 3; i++)
                {
                    lagInt[i] = new float[nOrder];
                    //lagInt_dx[i] = new float[nOrder];
                    duidxj[i] = new float[3];

                    node[i] = LagInterpolation.CalcNode(particle[i], table.Dx);
                    LagInterpolation.Interpolant(particle[i], table.DxFloat,
                        node[i], ref lagInt[i], nOrder);
                }
                //throw new Exception(String.Format("Temporary error, try again shortly. [{0} {1} {2}]", node[0], node[1], node[2]));
                
                    //LagInterpolation.Interpolant_dx(particle[i], table.Dx,
                    //    node[i], ref lagInt_dx[i], nOrder);

                    // 3 gets us X-3 to X4, point lies between X0 and X1
                    blob.GetDataCubeCorneredAtPoint(node[2] - 3, node[1] - 3, node[0] - 3, 8, dataCubeLg);
                    //dataCube = new float[nOrder, nOrder, nOrder, table.Components];
                    for (int idim = 0; idim < 3; idim++)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            for (int j = 0; j < 4; j++)
                            {
                                for (int k = 0; k < 4; k++)
                                {
                                    dataCube_dx[i, j, k, idim] =
                                        2.0f / 3.0f / table.DxFloat * (dataCubeLg[i + 2, j + 2, k + 3, idim] - dataCubeLg[i + 2, j + 2, k + 1, idim])
                                    - 1.0f / 12.0f / table.DxFloat * (dataCubeLg[i + 2, j + 2, k + 4, idim] - dataCubeLg[i + 2, j + 2, k, idim]);
                                    dataCube_dy[i, j, k, idim] =
                                        2.0f / 3.0f / table.DxFloat * (dataCubeLg[i + 2, j + 3, k + 2, idim] - dataCubeLg[i + 2, j + 1, k + 2, idim])
                                    - 1.0f / 12.0f / table.DxFloat * (dataCubeLg[i + 2, j + 4, k + 2, idim] - dataCubeLg[i + 2, j, k + 2, idim]);
                                    dataCube_dz[i, j, k, idim] =
                                        2.0f / 3.0f / table.DxFloat * (dataCubeLg[i + 3, j + 2, k + 2, idim] - dataCubeLg[i + 1, j + 2, k + 2, idim])
                                    - 1.0f / 12.0f / table.DxFloat * (dataCubeLg[i + 4, j + 2, k + 2, idim] - dataCubeLg[i, j + 2, k + 2, idim]);
                                }
                            }
                        }
                    }

                    // d_x u_i
                    LagInterpolation.Evaluate(ref dataCube_dx, ref duidxj[0], ref lagInt[0],
                        ref lagInt[1], ref lagInt[2], nOrder);

                    // d_y u_i
                    LagInterpolation.Evaluate(ref dataCube_dy, ref duidxj[1], ref lagInt[0],
                        ref lagInt[1], ref lagInt[2], nOrder);

                    // d_z u_i
                    LagInterpolation.Evaluate(ref dataCube_dz, ref duidxj[2], ref lagInt[0],
                        ref lagInt[1], ref lagInt[2], nOrder);

                    for (int i = 0; i < 3; i++)
                        for (int ii = 0; ii < 3; ii++)
                            result[i * 3 + ii] = duidxj[ii][i];
            }
            else
            {
                throw new Exception("Invalid Spatial Interpolation Option");
            }

            /*else
            {
                blob.GetDataCubeAroundPoint(node[2], node[1], node[0], nOrder, dataCube);

                // d_x u_i
                LagInterpolation.Evaluate(ref dataCube, ref duidxj[0], ref lagInt_dx[0],
                    ref lagInt[1], ref lagInt[2], nOrder);

                // d_y u_i
                LagInterpolation.Evaluate(ref dataCube, ref duidxj[1], ref lagInt[0],
                    ref lagInt_dx[1], ref lagInt[2], nOrder);

                // d_z u_i
                LagInterpolation.Evaluate(ref dataCube, ref duidxj[2], ref lagInt[0],
                    ref lagInt[1], ref lagInt_dx[2], nOrder);
            }*/

            return result;
        }



        public double[] CalcPressureGradient(TurbulenceBlob blob, float[] particle,
            TurbulenceOptions.SpatialInterpolation spatialOpt)
        {
            float[][] lagInt = new float[3][];
            //float[][] lagInt_dx = new float[3][];
            //float[] dpdxi = new float[3];

            double[] result = new double[3];

            int nOrder = -1;
            if (spatialOpt == TurbulenceOptions.SpatialInterpolation.None_Fd4)
            {
                nOrder = 4;
                int length = nOrder / 2;

                for (int i = 0; i < 3; i++)
                {
                    node[i] = LagInterpolation.CalcNodeWithRound(particle[i], table.Dx);
                }
                blob.GetDataLinesAroundPoint(node[2], node[1], node[0], length, dataLine);

                for (int i = 0; i < 3; i++)
                {                    
                    result[i] = 2.0f / 3.0f / table.DxFloat * (dataLine[3, i, 3] - dataLine[1, i, 3])
                        - 1.0f / 12.0f / table.DxFloat * (dataLine[4, i, 3] - dataLine[0, i, 3]);
                }
            }

            else if (spatialOpt == TurbulenceOptions.SpatialInterpolation.None_Fd6)
            {
                nOrder = 6;
                int length = nOrder / 2;

                for (int i = 0; i < 3; i++)
                {
                    node[i] = LagInterpolation.CalcNodeWithRound(particle[i], table.Dx);
                }
                blob.GetDataLinesAroundPoint(node[2], node[1], node[0], length, dataLine);

                for (int i = 0; i < 3; i++)
                {
                    result[i] = 3.0f / 4.0f / table.DxFloat * (dataLine[4, i, 3] - dataLine[2, i, 3])
                            - 3.0f / 20.0f / table.DxFloat * (dataLine[5, i, 3] - dataLine[1, i, 3])
                            + 1.0f / 60.0f / table.DxFloat * (dataLine[6, i, 3] - dataLine[0, i, 3]);
                }
            }

            else if (spatialOpt == TurbulenceOptions.SpatialInterpolation.None_Fd8)
            {
                nOrder = 8;
                int length = nOrder / 2;

                for (int i = 0; i < 3; i++)
                {
                    node[i] = LagInterpolation.CalcNodeWithRound(particle[i], table.Dx);
                }
                blob.GetDataLinesAroundPoint(node[2], node[1], node[0], length, dataLine);

                for (int i = 0; i < 3; i++)
                {
                    result[i] = 4.0f / 5.0f / table.DxFloat * (dataLine[5, i, 3] - dataLine[3, i, 3])
                            - 1.0f / 5.0f / table.DxFloat * (dataLine[6, i, 3] - dataLine[2, i, 3])
                            + 4.0f / 105.0f / table.DxFloat * (dataLine[7, i, 3] - dataLine[1, i, 3])
                            - 1.0f / 280.0f / table.DxFloat * (dataLine[8, i, 3] - dataLine[0, i, 3]);
                }
            }

            else if (spatialOpt == TurbulenceOptions.SpatialInterpolation.Fd4Lag4)
            {
                nOrder = 4;
                for (int i = 0; i < 3; i++)
                {
                    lagInt[i] = new float[nOrder];
                    //lagInt_dx[i] = new float[nOrder];
                    //duidxj[i] = new float[3];

                    node[i] = LagInterpolation.CalcNode(particle[i], table.Dx);
                    LagInterpolation.Interpolant(particle[i], table.DxFloat,
                        node[i], ref lagInt[i], nOrder);
                }
                //LagInterpolation.Interpolant_dx(particle[i], table.Dx,
                //    node[i], ref lagInt_dx[i], nOrder);

                blob.GetDataCubeCorneredAtPoint(node[2]-3, node[1]-3, node[0]-3, 8, dataCubeLg);
                //dataCube = new float[nOrder, nOrder, nOrder, table.Components];

                int idim = 3;
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        for (int k = 0; k < 4; k++)
                        {
                            dataCube_dx[i, j, k, idim] =
                                2.0f / 3.0f / table.DxFloat * (dataCubeLg[i + 2, j + 2, k + 3, idim] - dataCubeLg[i + 2, j + 2, k + 1, idim])
                            - 1.0f / 12.0f / table.DxFloat * (dataCubeLg[i + 2, j + 2, k + 4, idim] - dataCubeLg[i + 2, j + 2, k, idim]);
                            dataCube_dy[i, j, k, idim] =
                                2.0f / 3.0f / table.DxFloat * (dataCubeLg[i + 2, j + 3, k + 2, idim] - dataCubeLg[i + 2, j + 1, k + 2, idim])
                            - 1.0f / 12.0f / table.DxFloat * (dataCubeLg[i + 2, j + 4, k + 2, idim] - dataCubeLg[i + 2, j, k + 2, idim]);
                            dataCube_dz[i, j, k, idim] =
                                2.0f / 3.0f / table.DxFloat * (dataCubeLg[i + 3, j + 2, k + 2, idim] - dataCubeLg[i + 1, j + 2, k + 2, idim])
                            - 1.0f / 12.0f / table.DxFloat * (dataCubeLg[i + 4, j + 2, k + 2, idim] - dataCubeLg[i, j + 2, k + 2, idim]);
                        }
                    }
                }
                

                // d_x p
                LagInterpolation.EvaluatePressure(ref dataCube_dx, ref result[0], ref lagInt[0],
                    ref lagInt[1], ref lagInt[2], nOrder);

                // d_y p
                LagInterpolation.EvaluatePressure(ref dataCube_dy, ref result[1], ref lagInt[0],
                    ref lagInt[1], ref lagInt[2], nOrder);

                // d_z p
                LagInterpolation.EvaluatePressure(ref dataCube_dz, ref result[2], ref lagInt[0],
                    ref lagInt[1], ref lagInt[2], nOrder);

            }
            else
            {
                throw new Exception("Invalid Spatial Interpolation Option");
            }

            return result;
        }



        public double[] CalcVelocityLaplacian(TurbulenceBlob blob, float[] particle,
            TurbulenceOptions.SpatialInterpolation spatialOpt)
        {
            float[][] lagInt = new float[3][];
            float[][] grad2ui = new float[3][];
            double[] result = new double[3];

            for (int i = 0; i < 3; i++)
            {
                grad2ui[i] = new float[3];
            }
            
            int nOrder = -1;

            if (spatialOpt == TurbulenceOptions.SpatialInterpolation.None_Fd4)
            {
                nOrder = 4;
                int length = nOrder / 2;

                for (int i = 0; i < 3; i++)
                {
                    node[i] = LagInterpolation.CalcNodeWithRound(particle[i], table.Dx);
                }
                blob.GetDataLinesAroundPoint(node[2], node[1], node[0], length, dataLine);

                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        grad2ui[j][i] = FiniteDiff.SecFiniteDiff4(table.DxFloat,
                            dataLine[0, j, i], dataLine[1, j, i], dataLine[2, j, i], dataLine[3, j, i], dataLine[4, j, i]);
                    }
                }
            }
            else if (spatialOpt == TurbulenceOptions.SpatialInterpolation.None_Fd6)
            {
                nOrder = 6;
                int length = nOrder / 2;

                for (int i = 0; i < 3; i++)
                {
                    node[i] = LagInterpolation.CalcNodeWithRound(particle[i], table.Dx);
                }
                blob.GetDataLinesAroundPoint(node[2], node[1], node[0], length, dataLine);

                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        grad2ui[j][i] = FiniteDiff.SecFiniteDiff6(table.DxFloat,
                            dataLine[0, j, i], dataLine[1, j, i], dataLine[2, j, i], dataLine[3, j, i],
                            dataLine[4, j, i], dataLine[5, j, i], dataLine[6, j, i]);
                    }
                }
            }
            else if (spatialOpt == TurbulenceOptions.SpatialInterpolation.None_Fd8)
            {
                nOrder = 8;
                int length = nOrder / 2;

                for (int i = 0; i < 3; i++)
                {
                    node[i] = LagInterpolation.CalcNodeWithRound(particle[i], table.Dx);
                }
                blob.GetDataLinesAroundPoint(node[2], node[1], node[0], length, dataLine);

                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        grad2ui[j][i] = FiniteDiff.SecFiniteDiff8(table.DxFloat,
                            dataLine[0, j, i], dataLine[1, j, i], dataLine[2, j, i], dataLine[3, j, i],
                            dataLine[4, j, i], dataLine[5, j, i], dataLine[6, j, i], dataLine[7, j, i], dataLine[8, j, i]);
                    }
                }
            }
            else if (spatialOpt == TurbulenceOptions.SpatialInterpolation.Fd4Lag4)
            {
                nOrder = 4;
                for (int i = 0; i < 3; i++)
                {
                    lagInt[i] = new float[nOrder];

                    node[i] = LagInterpolation.CalcNode(particle[i], table.Dx);
                    LagInterpolation.Interpolant(particle[i], table.DxFloat,
                        node[i], ref lagInt[i], nOrder);
                }

                blob.GetDataCubeCorneredAtPoint(node[2]-3, node[1]-3, node[0]-3, 8, dataCube);

                for (int idim = 0; idim < 3; idim++)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            for (int k = 0; k < 4; k++)
                            {
                                dataCube_dx2[i, j, k, idim] = FiniteDiff.SecFiniteDiff4(table.DxFloat,
                                    dataCube[i + 2, j + 2, k, idim], dataCube[i + 2, j + 2, k + 1, idim], dataCube[i + 2, j + 2, k + 2, idim],
                                    dataCube[i + 2, j + 2, k + 3, idim], dataCube[i + 2, j + 2, k + 4, idim]);
                                dataCube_dy2[i, j, k, idim] = FiniteDiff.SecFiniteDiff4(table.DxFloat,
                                    dataCube[i + 2, j, k + 2, idim], dataCube[i + 2, j + 1, k + 2, idim], dataCube[i + 2, j + 2, k + 2, idim],
                                    dataCube[i + 2, j + 3, k + 2, idim], dataCube[i + 2, j + 4, k + 2, idim]);
                                dataCube_dz2[i, j, k, idim] = FiniteDiff.SecFiniteDiff4(table.DxFloat,
                                    dataCube[i, j + 2, k + 2, idim], dataCube[i + 1, j + 2, k + 2, idim], dataCube[i + 2, j + 2, k + 2, idim],
                                    dataCube[i + 3, j + 2, k + 2, idim], dataCube[i + 4, j + 2, k + 2, idim]);
                            }
                        }
                    }
                }
                // d_x2 u_i
                LagInterpolation.Evaluate(ref dataCube_dx2, ref grad2ui[0], ref lagInt[0],
                    ref lagInt[1], ref lagInt[2], nOrder);

                // d_y2 u_i
                LagInterpolation.Evaluate(ref dataCube_dy2, ref grad2ui[1], ref lagInt[0],
                    ref lagInt[1], ref lagInt[2], nOrder);

                // d_z2 u_i
                LagInterpolation.Evaluate(ref dataCube_dz2, ref grad2ui[2], ref lagInt[0],
                    ref lagInt[1], ref lagInt[2], nOrder);

            }
            else
            {
                throw new Exception("Invalid Spatial Interpolation Option");
            }


            for (int i = 0; i < 3; i++)
                result[i] = grad2ui[0][i] + grad2ui[1][i] + grad2ui[2][i];
            return result;
        }

        public double[] CalcLaplacianOfVelocityGradient(TurbulenceBlob blob, float[] particle,
            TurbulenceOptions.SpatialInterpolation spatialOpt)
        {
            // particle: 0-x,1-y,2-z
            // int[] node = new int[3];
            float[][] lagInt = new float[3][];
            float[][] lagInt_dx = new float[3][];
            float[][] lagInt_dx2 = new float[3][];
            float[][] lagInt_dx3 = new float[3][];
            float[][] grad2duidxj = new float[9][];

            double[] result = new double[9];

            int nOrder = -1;
            if (spatialOpt == TurbulenceOptions.SpatialInterpolation.Lag6)
            { nOrder = 6; }
            else if (spatialOpt == TurbulenceOptions.SpatialInterpolation.Lag8)
            { nOrder = 8; }
            else if (spatialOpt == TurbulenceOptions.SpatialInterpolation.None)
            {
                throw new Exception("6th- or 8th-order interpolation required");
            }
            else
            {
                throw new Exception("Invalid Spatial Interpolation Option");
            }


            for (int i = 0; i < 3; i++)
            {
                lagInt[i] = new float[nOrder];
                lagInt_dx[i] = new float[nOrder];
                lagInt_dx2[i] = new float[nOrder];
                lagInt_dx3[i] = new float[nOrder];

                node[i] = LagInterpolation.CalcNode(particle[i], table.Dx);
                LagInterpolation.Interpolant(particle[i], table.DxFloat,
                    node[i], ref lagInt[i], nOrder);
                LagInterpolation.Interpolant_dx(particle[i], table.DxFloat,
                    node[i], ref lagInt_dx[i], nOrder);
                LagInterpolation.Interpolant_dx2(particle[i], table.DxFloat,
                    node[i], ref lagInt_dx2[i], nOrder);
                LagInterpolation.Interpolant_dx3(particle[i], table.DxFloat,
                    node[i], ref lagInt_dx3[i], nOrder);
            }
            for (int i = 0; i < 9; i++)
                grad2duidxj[i] = new float[3];
            //blob.GetDataCubeAroundPoint(node[2], node[1], node[0], nOrder, dataCube);
            blob.GetDataCubeCorneredAtPoint(node[2] - nOrder / 2 + 1,
                node[1] - nOrder / 2 + 1,
                node[0] - nOrder / 2 + 1, nOrder, dataCube);
            // d_xx dx u_i
            LagInterpolation.Evaluate(ref dataCube, ref grad2duidxj[0], ref lagInt_dx3[0],
                ref lagInt[1], ref lagInt[2], nOrder);

            // d_yy dx u_i
            LagInterpolation.Evaluate(ref dataCube, ref grad2duidxj[1], ref lagInt_dx[0],
                ref lagInt_dx2[1], ref lagInt[2], nOrder);

            // d_zz dx u_i
            LagInterpolation.Evaluate(ref dataCube, ref grad2duidxj[2], ref lagInt_dx[0],
                ref lagInt[1], ref lagInt_dx2[2], nOrder);

            // d_xx dy u_i
            LagInterpolation.Evaluate(ref dataCube, ref grad2duidxj[3], ref lagInt_dx2[0],
                ref lagInt_dx[1], ref lagInt[2], nOrder);

            // d_yy dy u_i
            LagInterpolation.Evaluate(ref dataCube, ref grad2duidxj[4], ref lagInt[0],
                ref lagInt_dx3[1], ref lagInt[2], nOrder);

            // d_zz dy u_i
            LagInterpolation.Evaluate(ref dataCube, ref grad2duidxj[5], ref lagInt[0],
                ref lagInt_dx[1], ref lagInt_dx2[2], nOrder);

            // d_xx dz u_i
            LagInterpolation.Evaluate(ref dataCube, ref grad2duidxj[6], ref lagInt_dx2[0],
                ref lagInt[1], ref lagInt_dx[2], nOrder);

            // d_yy dz u_i
            LagInterpolation.Evaluate(ref dataCube, ref grad2duidxj[7], ref lagInt[0],
                ref lagInt_dx2[1], ref lagInt_dx[2], nOrder);

            // d_zz dz u_i
            LagInterpolation.Evaluate(ref dataCube, ref grad2duidxj[8], ref lagInt[0],
                ref lagInt[1], ref lagInt_dx3[2], nOrder);

            for (int i = 0; i < 3; i++)
                for (int ii = 0; ii < 3; ii++)
                    result[i * 3 + ii] = grad2duidxj[i * 3 + 0][ii] + grad2duidxj[i * 3 + 1][ii]
                        + grad2duidxj[i * 3 + 2][ii];

            return result;
        }
    }
}
