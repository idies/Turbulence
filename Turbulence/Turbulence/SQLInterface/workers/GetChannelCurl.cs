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
    public class GetChannelCurl : GetChannelGradient
    {
        public GetChannelCurl(string dataset, TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp,
            SqlConnection conn) :
            base (dataset, setInfo, spatialInterp, conn)
        {
        }

        public override SqlMetaData[] GetRecordMetaData()
        {
            return new SqlMetaData[] {
                new SqlMetaData("Req", SqlDbType.Int),
                new SqlMetaData("cx", SqlDbType.Real),
                new SqlMetaData("cy", SqlDbType.Real),
                new SqlMetaData("cz", SqlDbType.Real) };
        }

        /// <summary>
        /// Computes the curl from the gradient.
        /// cx = (duz / dy) - (duy / dz)
        /// cy = (dux / dz) - (duz / dx)
        /// cz = (duy / dx) - (dux / dy)
        /// </summary>
        public override double[] GetResult(TurbulenceBlob blob, SQLUtility.MHDInputRequest input)
        {
            double[] curl = new double[GetResultSize()];
            double[] gradient = CalcGradient(blob, input);
            curl[0] = gradient[7] - gradient[5];
            curl[1] = gradient[2] - gradient[6];
            curl[2] = gradient[3] - gradient[1];
            return curl;
        }

        protected override void GetResultUsingCutout(ref SQLUtility.PartialResult point, int x, int y, int z, int startx, int starty, int startz, int endx, int endy, int endz,
            int[] cutout_coordinates, int[] cutout_dimensions, int offset_x, int offset_y, int offset_z)
        {
            for (int x_i = startx; x_i <= endx; x_i++)
            {
                //Determine the kernel index, at which this data point is for the kernel of the above taget point.
                int iKernelIndexX = x_i - x + kernelSize / 2 - offset_x;

                //double coeff = diff_matrix_x[iKernelIndexX];
                double coeff = periodicX ? diff_matrix_x[iKernelIndexX] : diff_matrix_x[x, iKernelIndexX];
                ulong sourceIndex = (((ulong)z - (ulong)cutout_coordinates[2]) * (ulong)cutout_dimensions[2] * (ulong)cutout_dimensions[1] +
                    ((ulong)y - (ulong)cutout_coordinates[1]) * (ulong)cutout_dimensions[2] +
                    ((ulong)x_i - (ulong)cutout_coordinates[0])) * (ulong)setInfo.Components;
                point.result[2] += coeff * GetDataItem(sourceIndex + 1); // computes the contribution from duy/dx to cz
                point.result[1] -= coeff * GetDataItem(sourceIndex + 2); // computes the contribution from duz/dx to cy
                point.numPointsProcessed++;
            }
            for (int y_i = starty; y_i <= endy; y_i++)
            {
                //Determine the kernel index, at which this data point is for the kernel of the above taget point.
                int iKernelIndexY = y_i - y + kernelSize / 2 - offset_y;

                //double coeff = diff_matrix_y[y, iKernelIndexY];
                double coeff = periodicY ? diff_matrix_y[iKernelIndexY] : diff_matrix_y[y, iKernelIndexY];

                ulong sourceIndex = (((ulong)z - (ulong)cutout_coordinates[2]) * (ulong)cutout_dimensions[2] * (ulong)cutout_dimensions[1] +
                    ((ulong)y_i - (ulong)cutout_coordinates[1]) * (ulong)cutout_dimensions[2] +
                    ((ulong)x - (ulong)cutout_coordinates[0])) * (ulong)setInfo.Components;
                point.result[0] += coeff * GetDataItem(sourceIndex + 2); // computes the contribution from duz/dy to cx
                point.result[2] -= coeff * GetDataItem(sourceIndex);     // computes the contribution from dux/dy to cz
                point.numPointsProcessed++;
            }
            for (int z_i = startz; z_i <= endz; z_i++)
            {
                //Determine the kernel index, at which this data point is for the kernel of the above taget point.
                int iKernelIndexZ = z_i - z + kernelSize / 2 - offset_z;

                //double coeff = diff_matrix_z[iKernelIndexZ];
                double coeff = periodicZ ? diff_matrix_z[iKernelIndexZ] : diff_matrix_z[z, iKernelIndexZ];

                ulong sourceIndex = (((ulong)z_i - (ulong)cutout_coordinates[2]) * (ulong)cutout_dimensions[2] * (ulong)cutout_dimensions[1] +
                    ((ulong)y - (ulong)cutout_coordinates[1]) * (ulong)cutout_dimensions[2] +
                    ((ulong)x - (ulong)cutout_coordinates[0])) * (ulong)setInfo.Components;
                point.result[1] += coeff * GetDataItem(sourceIndex);     // computes the contribution from dux/dz to cy
                point.result[0] -= coeff * GetDataItem(sourceIndex + 1); // computes the contribution from duy/dz to cx  
                point.numPointsProcessed++;
            }
        }

        public override int GetResultSize()
        {
            return 3;
        }
    }

}
