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
    public class GetChannelCurl : GetChannelGradient
    {
        public GetChannelCurl(TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp,
            SqlConnection conn) :
            base (setInfo, spatialInterp, conn)
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

        protected override void GetResultUsingCutout(ref SQLUtility.PartialResult point, float[] cutout, int x, int y, int z, int startx, int starty, int startz, int endx, int endy, int endz,
            int[] cutout_coordinates, int[] cutout_dimensions, int offset_y)
        {
            for (int x_i = startx; x_i <= endx; x_i++)
            {
                //Determine the kernel index, at which this data point is for the kernel of the above taget point.
                int iKernelIndexX = x_i - x + kernelSize / 2;

                double coeff = diff_matrix_x[iKernelIndexX];
                int sourceIndex = ((z - cutout_coordinates[2]) * cutout_dimensions[2] * cutout_dimensions[1] +
                    (y - cutout_coordinates[1]) * cutout_dimensions[2] +
                    (x_i - cutout_coordinates[0])) * setInfo.Components;
                point.result[2] += coeff * cutout[sourceIndex + 1]; // computes the contribution from duy/dx to cz
                point.result[1] -= coeff * cutout[sourceIndex + 2]; // computes the contribution from duz/dx to cy
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
                point.result[0] += coeff * cutout[sourceIndex + 2]; // computes the contribution from duz/dy to cx
                point.result[2] -= coeff * cutout[sourceIndex];     // computes the contribution from dux/dy to cz
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
                point.result[1] += coeff * cutout[sourceIndex];     // computes the contribution from dux/dz to cy
                point.result[0] -= coeff * cutout[sourceIndex + 1]; // computes the contribution from duy/dz to cx  
                point.numPointsProcessed++;
            }
        }

        protected override void GetResultUsingCutout(ref SQLUtility.PartialResult point, BigArray<float> cutout, int x, int y, int z, int startx, int starty, int startz, int endx, int endy, int endz,
            int[] cutout_coordinates, int[] cutout_dimensions, int offset_y)
        {
            for (int x_i = startx; x_i <= endx; x_i++)
            {
                //Determine the kernel index, at which this data point is for the kernel of the above taget point.
                int iKernelIndexX = x_i - x + kernelSize / 2;

                double coeff = diff_matrix_x[iKernelIndexX];
                ulong sourceIndex = (((ulong)z - (ulong)cutout_coordinates[2]) * (ulong)cutout_dimensions[2] * (ulong)cutout_dimensions[1] +
                    ((ulong)y - (ulong)cutout_coordinates[1]) * (ulong)cutout_dimensions[2] +
                    ((ulong)x_i - (ulong)cutout_coordinates[0])) * (ulong)setInfo.Components;
                point.result[2] += coeff * cutout[sourceIndex + 1]; // computes the contribution from duy/dx to cz
                point.result[1] -= coeff * cutout[sourceIndex + 2]; // computes the contribution from duz/dx to cy
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
                point.result[0] += coeff * cutout[sourceIndex + 2]; // computes the contribution from duz/dy to cx
                point.result[2] -= coeff * cutout[sourceIndex];     // computes the contribution from dux/dy to cz
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
                point.result[1] += coeff * cutout[sourceIndex];     // computes the contribution from dux/dz to cy
                point.result[0] -= coeff * cutout[sourceIndex + 1]; // computes the contribution from duy/dz to cx  
                point.numPointsProcessed++;
            }
        }

        public override int GetResultSize()
        {
            return 3;
        }
    }

}
