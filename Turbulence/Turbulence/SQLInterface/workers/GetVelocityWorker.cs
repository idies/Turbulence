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
    public class GetVelocityWorker : Worker
    {
        bool pressure;
        bool velocity;
        
        double[] lagDenominator = null;

        public GetVelocityWorker(TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp,
            bool velocity, bool pressure)
        {
            this.setInfo = setInfo;
            this.spatialInterp = spatialInterp;
            this.pressure = pressure;
            this.velocity = velocity;

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
                this.kernelSize = 0;
                // do nothing
            }
            else
            {
                throw new Exception(String.Format("Invalid Spatial Interpolation Option: {0}", spatialInterp));
            }
        }

        public override SqlMetaData[] GetRecordMetaData()
        {
            if (velocity && pressure)
            {
                return new SqlMetaData[] {
                    new SqlMetaData("Req", SqlDbType.Int),
                    new SqlMetaData("X", SqlDbType.Real),
                    new SqlMetaData("Y", SqlDbType.Real),
                    new SqlMetaData("Z", SqlDbType.Real),
                    new SqlMetaData("P", SqlDbType.Real) };
            }
            else if (velocity && !pressure)
            {
                return new SqlMetaData[] {
                    new SqlMetaData("Req", SqlDbType.Int),
                    new SqlMetaData("X", SqlDbType.Real),
                    new SqlMetaData("Y", SqlDbType.Real),
                    new SqlMetaData("Z", SqlDbType.Real)
                };
            }
            else if (!velocity && pressure)
            {
                return new SqlMetaData[] {
                    new SqlMetaData("Req", SqlDbType.Int),
                    new SqlMetaData("P", SqlDbType.Real) };
            }
            else
            {
                throw new Exception("One of pressure or velocity must be true.");
            }
        }

        public override double[] GetResult(TurbulenceBlob blob, SQLUtility.InputRequest input)
        {
            return CalcVelocity(blob, (float)input.x, (float)input.y, (float)input.z);
        }


        /// <summary>
        /// New version of the CalcVelocity function.
        /// </summary>
        /// <remarks>
        /// The Lagrangian evaluation function [LagInterpolation.EvaluateOpt] was moved
        /// into the function and some loop unrolling was performed.
        /// 
        /// This function assumes that Vx, Vy, Vz and P are stored together in a blob
        /// </remarks>
        unsafe public double[] CalcVelocity(TurbulenceBlob blob, float xp, float yp, float zp)
        {
            double[] up; // Result value for the user

            if (velocity && !pressure)
            {
                up = new double[3];
            }
            else if (velocity && pressure)
            {
                up = new double[4];
            }
            else if (!velocity && pressure)
            {
                up = new double[1];
            }
            else
            {
                throw new Exception("One of either velocity or pressure must be true.");
            }

            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None)
            {
                int xi = LagInterpolation.CalcNodeWithRound(xp, setInfo.Dx);
                int yi = LagInterpolation.CalcNodeWithRound(yp, setInfo.Dx);
                int zi = LagInterpolation.CalcNodeWithRound(zp, setInfo.Dx);

                float[] data = blob.data;
                int off0 = blob.GetLocalOffset(zi, yi, xi, 0);
                if (velocity)
                {
                    up[0] = data[off0];
                    up[1] = data[off0 + 1];
                    up[2] = data[off0 + 2];
                    if (pressure)
                        up[3] = data[off0 + 3];
                }
                else if (pressure)
                {
                    up[0] = data[off0 + 3];
                }
            }
            else
            {
                int x = LagInterpolation.CalcNode(xp, setInfo.Dx);
                int y = LagInterpolation.CalcNode(yp, setInfo.Dx);
                int z = LagInterpolation.CalcNode(zp, setInfo.Dx);

                int nOrder = -1;
                if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Lag4)
                { nOrder = 4; }
                else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Lag6)
                { nOrder = 6; }
                else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Lag8)
                { nOrder = 8; }
                else
                {
                    throw new Exception(String.Format("Invalid Spatial Interpolation Option: {0}", spatialInterp));
                }

                double[] lagInt = new double[nOrder * 3];

                LagInterpolation.InterpolantN(nOrder, xp, x, setInfo.Dx, lagDenominator, 0, lagInt);
                LagInterpolation.InterpolantN(nOrder, yp, y, setInfo.Dy, lagDenominator, 1, lagInt);
                LagInterpolation.InterpolantN(nOrder, zp, z, setInfo.Dz, lagDenominator, 2, lagInt);

                float[] data = blob.data;
                // The point (p - nOrder/2) will be less-than-or-equal to query point.
                // Thus, we need to only shift over by (nOrder/2)+1.
                int off0 = blob.GetLocalOffset(z - (nOrder / 2) + 1, y - (nOrder / 2) + 1, x - (nOrder / 2) + 1, 0);


                fixed (double* lagint = lagInt)
                {
                    fixed (float* fdata = data)
                    {
                        if (velocity) // Velocity with or without pressure
                        {

                            double a1 = 0.0, a2 = 0.0, a3 = 0.0, a4 = 0.0;
                            for (int iz = 0; iz < nOrder; iz++)
                            {
                                double b1 = 0.0, b2 = 0.0, b3 = 0.0, b4 = 0.0;
                                int off1 = off0 + iz * 72 * 72 * 4;
                                for (int iy = 0; iy < nOrder; iy++)
                                {
                                    double c1 = 0.0, c2 = 0.0, c3 = 0.0, c4 = 0.0;
                                    int off = off1 + iy * 72 * 4;
                                    for (int ix = 0; ix < nOrder; ix++)
                                    {
                                        double c = lagint[ix];
                                        c1 += c * fdata[off];
                                        c2 += c * fdata[off + 1];
                                        c3 += c * fdata[off + 2];
                                        if (pressure)
                                            c4 += c * data[off + 3];
                                        off += 4;
                                    }
                                    double b = lagint[1 * nOrder + iy];
                                    b1 += c1 * b;
                                    b2 += c2 * b;
                                    b3 += c3 * b;
                                    if (pressure)
                                        b4 += c4 * b;
                                }
                                double a = lagint[2 * nOrder + iz];
                                a1 += b1 * a;
                                a2 += b2 * a;
                                a3 += b3 * a;
                                if (pressure)
                                    a4 += b4 * a;
                            }
                            up[0] = a1;
                            up[1] = a2;
                            up[2] = a3;
                            if (pressure)
                                up[3] = a4;

                        }
                        else if (pressure) // Presure only
                        {

                            double a0 = 0.0;
                            for (int iz = 0; iz < nOrder; iz++)
                            {
                                double b0 = 0.0;
                                int off1 = off0 + iz * 72 * 72 * 4;
                                for (int iy = 0; iy < nOrder; iy++)
                                {
                                    double c0 = 0.0;
                                    int off = off1 + iy * 72 * 4;
                                    for (int ix = 0; ix < nOrder; ix++)
                                    {
                                        double c = lagint[ix];
                                        c0 += c * fdata[off + 3];
                                        off += 4;
                                    }
                                    double b = lagint[1 * nOrder + iy];
                                    b0 += c0 * b;
                                }
                                double a = lagint[2 * nOrder + iz];
                                a0 += b0 * a;
                            }

                            up[0] = a0;
                        }
                        /*
                        if (velocity) // Velocity with or without pressure
                        {
                            double a1 = 0.0, a2 = 0.0, a3 = 0.0, a4 = 0.0;
                            for (int iz = 0; iz < nOrder; iz++)
                            {
                                double b1 = 0.0, b2 = 0.0, b3 = 0.0, b4 = 0.0;
                                int off1 = off0 + iz * 72 * 72 * 4;
                                for (int iy = 0; iy < nOrder; iy++)
                                {
                                    double c1 = 0.0, c2 = 0.0, c3 = 0.0, c4 = 0.0;
                                    int off = off1 + iy * 72 * 4;
                                    for (int ix = 0; ix < nOrder; ix++)
                                    {
                                        double c = lagInt[ix];
                                        c1 += c * data[off];
                                        c2 += c * data[off + 1];
                                        c3 += c * data[off + 2];
                                        if (pressure)
                                            c4 += c * data[off + 3];
                                        off += 4;
                                    }
                                    double b = lagInt[1 * nOrder + iy];
                                    b1 += c1 * b;
                                    b2 += c2 * b;
                                    b3 += c3 * b;
                                    if (pressure)
                                        b4 += c4 * b;
                                }
                                double a = lagInt[2 * nOrder + iz];
                                a1 += b1 * a;
                                a2 += b2 * a;
                                a3 += b3 * a;
                                if (pressure)
                                    a4 += b4 * a;
                            }
                            up[0] = (float)a1;
                            up[1] = (float)a2;
                            up[2] = (float)a3;
                            if (pressure)
                                up[3] = (float)a4;
                        }*/
                        /*else if (pressure) // Presure only
                        {
                            double a0 = 0.0;
                            for (int iz = 0; iz < nOrder; iz++)
                            {
                                double b0 = 0.0;
                                int off1 = off0 + iz * 72 * 72 * 4;
                                for (int iy = 0; iy < nOrder; iy++)
                                {
                                    double c0 = 0.0;
                                    int off = off1 + iy * 72 * 4;
                                    for (int ix = 0; ix < nOrder; ix++)
                                    {
                                        double c = lagInt[ix];
                                        c0 += c * data[off + 3];
                                        off += 4;
                                    }
                                    double b = lagInt[1 * nOrder + iy];
                                    b0 += c0 * b;
                                }
                                double a = lagInt[2 * nOrder + iz];
                                a0 += b0 * a;
                            }
                            up[0] = (float)a0;
                        }*/

                        else
                        {
                            throw new Exception("Code should not have reached this point.");
                        }
                    }
                }
            }
            return up;
        }


        /// <summary>
        /// New version of the CalcVelocity function.
        /// Used for the MHD database.
        /// </summary>
        /// <remarks>
        /// The Lagrangian evaluation function [LagInterpolation.EvaluateOpt] was moved
        /// into the function and some loop unrolling was performed.
        /// 
        /// This function assumes that Vx, Vy and Vz are stored together in a blob
        /// </remarks>
        unsafe public double[] CalcVelocity(TurbulenceBlob blob, float xp, float yp, float zp, SQLUtility.MHDInputRequest input)
        {
            double[] up = new double[3]; // Result value for the user

            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None)
            {
                int xi = LagInterpolation.CalcNodeWithRound(xp, setInfo.Dx);
                int yi = LagInterpolation.CalcNodeWithRound(yp, setInfo.Dx);
                int zi = LagInterpolation.CalcNodeWithRound(zp, setInfo.Dx);

                float[] data = blob.data;
                int off0 = blob.GetLocalOffsetMHD(zi, yi, xi, 0);
                up[0] = data[off0];
                up[1] = data[off0 + 1];
                up[2] = data[off0 + 2];
            }
            else
            {
                int x = LagInterpolation.CalcNode(xp, setInfo.Dx);
                int y = LagInterpolation.CalcNode(yp, setInfo.Dx);
                int z = LagInterpolation.CalcNode(zp, setInfo.Dx);

                int nOrder = -1;
                if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Lag4)
                { nOrder = 4; }
                else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Lag6)
                { nOrder = 6; }
                else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Lag8)
                { nOrder = 8; }
                else
                {
                    throw new Exception(String.Format("Invalid Spatial Interpolation Option: {0}", spatialInterp));
                }

                // The coefficients are computed only once and cached, so that they don't have to be 
                // recomputed for each partial sum
                if (input.lagInt == null)
                {
                    input.lagInt = new double[nOrder * 3];

                    LagInterpolation.InterpolantN(nOrder, xp, x, setInfo.Dx, lagDenominator, 0, input.lagInt);
                    LagInterpolation.InterpolantN(nOrder, yp, y, setInfo.Dy, lagDenominator, 1, input.lagInt);
                    LagInterpolation.InterpolantN(nOrder, zp, z, setInfo.Dz, lagDenominator, 2, input.lagInt);
                }

                //float[] lagInt = new float[nOrder * 3];
                //LagInterpolation.InterpolantN(nOrder, xp, x, setInfo.Dx, lagDenominator, 0, lagInt);
                //LagInterpolation.InterpolantN(nOrder, yp, y, setInfo.Dx, lagDenominator, 1, lagInt);
                //LagInterpolation.InterpolantN(nOrder, zp, z, setInfo.Dx, lagDenominator, 2, lagInt);

                float[] data = blob.data;
                //int off0 = blob.GetLocalOffset(z - (nOrder / 2), y - (nOrder / 2), x - (nOrder / 2), 0);
                int startz = 0, starty = 0, startx = 0, endz = 0, endy = 0, endx = 0;
                blob.GetSubcubeStart(z - (nOrder / 2) + 1, y - (nOrder / 2) + 1, x - (nOrder / 2) + 1, ref startz, ref starty, ref startx);
                blob.GetSubcubeEnd(z + (nOrder / 2), y + (nOrder / 2), x + (nOrder / 2), ref endz, ref endy, ref endx);
                int off0 = startx * blob.GetComponents;

                //int iLagInt;
                int iLagIntx = blob.GetRealX - x + startx + nOrder / 2 - 1;
                //iLagIntx = ((iLagIntx % blob.GetGridResolution) + blob.GetGridResolution) % blob.GetGridResolution;
                if (iLagIntx >= blob.GetGridResolution)
                    iLagIntx -= blob.GetGridResolution;
                else if (iLagIntx < 0)
                    iLagIntx += blob.GetGridResolution;
                int iLagInty = blob.GetRealY - y + starty + nOrder / 2 - 1;
                //iLagInty = ((iLagInty % blob.GetGridResolution) + blob.GetGridResolution) % blob.GetGridResolution;
                if (iLagInty >= blob.GetGridResolution)
                    iLagInty -= blob.GetGridResolution;
                else if (iLagInty < 0)
                    iLagInty += blob.GetGridResolution;
                int iLagIntz = blob.GetRealZ - z + startz + nOrder / 2 - 1;
                //iLagIntz = ((iLagIntz % blob.GetGridResolution) + blob.GetGridResolution) % blob.GetGridResolution;
                if (iLagIntz >= blob.GetGridResolution)
                    iLagIntz -= blob.GetGridResolution;
                else if (iLagIntz < 0)
                    iLagIntz += blob.GetGridResolution;

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
                                    //int off = (ix + iy * blob.GetSide + iz * blob.GetSide * blob.GetSide) * blob.GetComponents;
                                    //need to determine the distance from the point, on which we are centered
                                    //iLagInt = blob.GetRealX + ix - x + nOrder / 2 - 1;
                                    //iLagInt = ((iLagInt % blob.GetGridResolution) + blob.GetGridResolution) % blob.GetGridResolution;
                                    //double c = lagint[iLagInt];
                                    double c = lagint[iLagIntx + ix - startx];
                                    c1 += c * fdata[off];
                                    c2 += c * fdata[off + 1];
                                    c3 += c * fdata[off + 2];
                                    off += blob.GetComponents;
                                }
                                //need to determine the distance from the point, on which we are centered
                                //iLagInt = blob.GetRealY + iy - y + nOrder / 2 - 1;
                                //iLagInt = ((iLagInt % blob.GetGridResolution) + blob.GetGridResolution) % blob.GetGridResolution;
                                //double b = lagint[1 * nOrder + iLagInt];
                                double b = lagint[1 * nOrder + iLagInty + iy - starty];
                                b1 += c1 * b;
                                b2 += c2 * b;
                                b3 += c3 * b;
                            }
                            //need to determine the distance from the point, on which we are centered
                            //iLagInt = blob.GetRealZ + iz - z + nOrder / 2 - 1;
                            //iLagInt = ((iLagInt % blob.GetGridResolution) + blob.GetGridResolution) % blob.GetGridResolution;
                            //double a = lagint[2 * nOrder + iLagInt];
                            double a = lagint[2 * nOrder + iLagIntz + iz - startz];
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

        public override double[] GetResult(TurbulenceBlob blob, SQLUtility.MHDInputRequest input)
        {
            float xp = input.x;
            float yp = input.y;
            float zp = input.z;
            return CalcVelocity(blob, xp, yp, zp, input);
        }

        public override int GetResultSize()
        {
            if (pressure)
                return 4;
            else
                return 3;
        }

    }

}
