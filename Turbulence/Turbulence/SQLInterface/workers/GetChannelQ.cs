﻿using System;
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
    public class GetChannelQ : GetChannelGradient
    {
        public GetChannelQ(TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp,
            SqlConnection conn) :
            base (setInfo, spatialInterp, conn)
        {
        }

        public override SqlMetaData[] GetRecordMetaData()
        {
            return new SqlMetaData[] {
                new SqlMetaData("Req", SqlDbType.Int),
                new SqlMetaData("Q", SqlDbType.Real)};
        }

        /// <summary>
        /// Q = duxdx * duydy + duxdx * duzdz + duydy * duzdz − duxdy * duydx − duxdz * duzdx − duydz * duzdy
        /// </summary>
        public override float[] GetResult(TurbulenceBlob blob, SQLUtility.MHDInputRequest input)
        {
            float[] q = new float[GetResultSize()];
            float[] gradient = CalcGradient(blob, input);
            q[0] = gradient[0] * gradient[4] + gradient[0] * gradient[8] + gradient[4] * gradient[8]
                - gradient[1] * gradient[3] - gradient[2] * gradient[6] - gradient[5] * gradient[7];
            return q;
        }

        protected override void GetResultUsingCutout(ref SQLUtility.PartialResult point, float[] cutout, int x, int y, int z, int startx, int starty, int startz, int endx, int endy, int endz,
            int[] cutout_coordinates, int[] cutout_dimensions, int offset_y)
        {
            double[] gradient = new double[9];

            for (int x_i = startx; x_i <= endx; x_i++)
            {
                //Determine the kernel index, at which this data point is for the kernel of the above taget point.
                int iKernelIndexX = x_i - x + kernelSize / 2;

                double coeff = diff_matrix_x[iKernelIndexX];
                int sourceIndex = ((z - cutout_coordinates[2]) * cutout_dimensions[2] * cutout_dimensions[1] +
                    (y - cutout_coordinates[1]) * cutout_dimensions[2] +
                    (x_i - cutout_coordinates[0])) * setInfo.Components;
                gradient[0] += coeff * cutout[sourceIndex];     //dux/dx
                gradient[3] += coeff * cutout[sourceIndex + 1]; //duy/dx
                gradient[6] += coeff * cutout[sourceIndex + 2]; //duz/dx
                point.numPointsProcessed++;
            }
            for (int y_i = starty; y_i <= endy; y_i++)
            {
                //Determine the kernel index, at which this data point is for the kernel of the above taget point.
                int iKernelIndexY = y_i - y + kernelSize / 2 - offset_y;

                double coeff = diff_matrix_y[y, iKernelIndexY];
                int sourceIndex = ((z - cutout_coordinates[2]) * cutout_dimensions[2] * cutout_dimensions[1] +
                    (y_i - cutout_coordinates[1]) * cutout_dimensions[2] +
                    (x - cutout_coordinates[0])) * setInfo.Components;
                gradient[1] += coeff * cutout[sourceIndex];     //dux/dy
                gradient[4] += coeff * cutout[sourceIndex + 1]; //duy/dy
                gradient[7] += coeff * cutout[sourceIndex + 2]; //duz/dy
                point.numPointsProcessed++;
            }
            for (int z_i = startz; z_i <= endz; z_i++)
            {
                //Determine the kernel index, at which this data point is for the kernel of the above taget point.
                int iKernelIndexZ = z_i - z + kernelSize / 2;

                double coeff = diff_matrix_z[iKernelIndexZ];
                int sourceIndex = ((z_i - cutout_coordinates[2]) * cutout_dimensions[2] * cutout_dimensions[1] +
                    (y - cutout_coordinates[1]) * cutout_dimensions[2] +
                    (x - cutout_coordinates[0])) * setInfo.Components;
                gradient[2] += coeff * cutout[sourceIndex];     //dux/dz
                gradient[5] += coeff * cutout[sourceIndex + 1]; //duy/dz
                gradient[8] += coeff * cutout[sourceIndex + 2]; //duz/dz
                point.numPointsProcessed++;
            }

            //Q = duxdx * duydy + duxdx * duzdz + duydy * duzdz − duxdy * duydx − duxdz * duzdx − duydz * duzdy
            point.result[0] = gradient[0] * gradient[4] + gradient[0] * gradient[8] + gradient[4] * gradient[8]
                - gradient[1] * gradient[3] - gradient[2] * gradient[6] - gradient[5] * gradient[7];
        }

        protected override void GetResultUsingCutout(ref SQLUtility.PartialResult point, BigArray<float> cutout, int x, int y, int z, int startx, int starty, int startz, int endx, int endy, int endz,
            int[] cutout_coordinates, int[] cutout_dimensions, int offset_y)
        {
            double[] gradient = new double[9];

            for (int x_i = startx; x_i <= endx; x_i++)
            {
                //Determine the kernel index, at which this data point is for the kernel of the above taget point.
                int iKernelIndexX = x_i - x + kernelSize / 2;

                double coeff = diff_matrix_x[iKernelIndexX];
                ulong sourceIndex = (((ulong)z - (ulong)cutout_coordinates[2]) * (ulong)cutout_dimensions[2] * (ulong)cutout_dimensions[1] +
                    ((ulong)y - (ulong)cutout_coordinates[1]) * (ulong)cutout_dimensions[2] +
                    ((ulong)x_i - (ulong)cutout_coordinates[0])) * (ulong)setInfo.Components;
                gradient[0] += coeff * cutout[sourceIndex];     //dux/dx
                gradient[3] += coeff * cutout[sourceIndex + 1]; //duy/dx
                gradient[6] += coeff * cutout[sourceIndex + 2]; //duz/dx
                point.numPointsProcessed++;
            }
            for (int y_i = starty; y_i <= endy; y_i++)
            {
                //Determine the kernel index, at which this data point is for the kernel of the above taget point.
                int iKernelIndexY = y_i - y + kernelSize / 2 - offset_y;

                double coeff = diff_matrix_y[iKernelIndexY];
                ulong sourceIndex = (((ulong)z - (ulong)cutout_coordinates[2]) * (ulong)cutout_dimensions[2] * (ulong)cutout_dimensions[1] +
                    ((ulong)y_i - (ulong)cutout_coordinates[1]) * (ulong)cutout_dimensions[2] +
                    ((ulong)x - (ulong)cutout_coordinates[0])) * (ulong)setInfo.Components;
                gradient[1] += coeff * cutout[sourceIndex];     //dux/dy
                gradient[4] += coeff * cutout[sourceIndex + 1]; //duy/dy
                gradient[7] += coeff * cutout[sourceIndex + 2]; //duz/dy
                point.numPointsProcessed++;
            }
            for (int z_i = startz; z_i <= endz; z_i++)
            {
                //Determine the kernel index, at which this data point is for the kernel of the above taget point.
                int iKernelIndexZ = z_i - z + kernelSize / 2;

                double coeff = diff_matrix_z[iKernelIndexZ];
                ulong sourceIndex = (((ulong)z_i - (ulong)cutout_coordinates[2]) * (ulong)cutout_dimensions[2] * (ulong)cutout_dimensions[1] +
                    ((ulong)y - (ulong)cutout_coordinates[1]) * (ulong)cutout_dimensions[2] +
                    ((ulong)x - (ulong)cutout_coordinates[0])) * (ulong)setInfo.Components;
                gradient[2] += coeff * cutout[sourceIndex];     //dux/dz
                gradient[5] += coeff * cutout[sourceIndex + 1]; //duy/dz
                gradient[8] += coeff * cutout[sourceIndex + 2]; //duz/dz
                point.numPointsProcessed++;
            }

            //Q = duxdx * duydy + duxdx * duzdz + duydy * duzdz − duxdy * duydx − duxdz * duzdx − duydz * duzdy
            point.result[0] = gradient[0] * gradient[4] + gradient[0] * gradient[8] + gradient[4] * gradient[8]
                - gradient[1] * gradient[3] - gradient[2] * gradient[6] - gradient[5] * gradient[7];
        }

        public override int GetResultSize()
        {
            return 1;
        }
    }

}