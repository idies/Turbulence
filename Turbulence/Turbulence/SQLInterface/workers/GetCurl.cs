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
    public class GetCurl : GetMHDGradient
    {
        public GetCurl(TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp) :
            base(setInfo, spatialInterp)
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

        public override double[] GetResult(TurbulenceBlob blob, SQLUtility.MHDInputRequest input)
        {
            double[] curl = new double[GetResultSize()];
            double[] gradient = CalcGradient(blob, input);
            curl[0] = gradient[7] - gradient[5];
            curl[1] = gradient[2] - gradient[6];
            curl[2] = gradient[3] - gradient[1];
            return curl;
        }

        public override HashSet<SQLUtility.PartialResult> GetResult(TurbulenceBlob atom, Dictionary<long, SQLUtility.PartialResult> active_points)
        {
            HashSet<SQLUtility.PartialResult> processed_points = new HashSet<SQLUtility.PartialResult>();
            // Iterate over the entire atom
            int off = 0;
            SQLUtility.PartialResult point;
            for (int z = atom.GetBaseZ; z < atom.GetBaseZ + atom.GetSide; z++)
            {
                for (int y = atom.GetBaseY; y < atom.GetBaseY + atom.GetSide; y++)
                {
                    for (int x = atom.GetBaseX; x < atom.GetBaseX + atom.GetSide; x++)
                    {
                        // For each data point update all kernels, which this data point is part of.
                        int startz = z - kernelSize + 1 < setInfo.StartZ - kernelSize / 2 ? setInfo.StartZ - kernelSize / 2 : z - kernelSize + 1;
                        int starty = y - kernelSize + 1 < setInfo.StartY - kernelSize / 2 ? setInfo.StartY - kernelSize / 2 : y - kernelSize + 1;
                        int startx = x - kernelSize + 1 < setInfo.StartX - kernelSize / 2 ? setInfo.StartX - kernelSize / 2 : x - kernelSize + 1;

                        // Process the kernels along x first.
                        for (int x_i = startx; x_i <= x; x_i++)
                        {
                            //Determine the target point, which this data point is part of the kernel of.
                            int targetX = x_i + kernelSize / 2;
                            if (targetX > setInfo.EndX)
                            {
                                break;
                            }
                            //Determine the kernel index, at which this data point is for the kernel of the above taget point.
                            int iKernelIndexX = x - x_i;

                            double coeff = CenteredFiniteDiffCoeff[iKernelIndexX];
                            long zindex = new Morton3D(z, y, targetX);
                            if (active_points.ContainsKey(zindex))
                            {
                                point = active_points[zindex];
                            }
                            else
                            {
                                point = new SQLUtility.PartialResult(zindex, GetResultSize(), numPointsInKernel);
                                active_points.Add(zindex, point);
                            }
                            point.result[2] += coeff * atom.data[off + 1]; // computes the contribution from duy/dx to cz
                            point.result[1] -= coeff * atom.data[off + 2]; // computes the contribution from duz/dx to cy
                            point.numPointsProcessed++;
                            if (point.numPointsProcessed == point.numPointsInKernel)
                            {
                                processed_points.Add(point);
                                active_points.Remove(zindex);
                            }
                        }
                        
                        // Process the kernels along y next.
                        for (int y_i = starty; y_i <= y; y_i++)
                        {
                            //Determine the target point, which this data point is part of the kernel of.
                            int targetY = y_i + kernelSize / 2;
                            if (targetY > setInfo.EndY)
                            {
                                break;
                            }
                            //Determine the kernel index, at which this data point is for the kernel of the above taget point.
                            int iKernelIndexY = y - y_i;

                            double coeff = CenteredFiniteDiffCoeff[iKernelIndexY];
                            long zindex = new Morton3D(z, targetY, x);
                            if (active_points.ContainsKey(zindex))
                            {
                                point = active_points[zindex];
                            }
                            else
                            {
                                point = new SQLUtility.PartialResult(zindex, GetResultSize(), numPointsInKernel);
                                active_points.Add(zindex, point);
                            }
                            point.result[0] += coeff * atom.data[off + 2]; // computes the contribution from duz/dy to cx
                            point.result[2] -= coeff * atom.data[off + 0]; // computes the contribution from dux/dy to cz
                            point.numPointsProcessed++;
                            if (point.numPointsProcessed == point.numPointsInKernel)
                            {
                                processed_points.Add(point);
                                active_points.Remove(zindex);
                            }
                        }

                        // Process the kernels along z last.
                        for (int z_i = startz; z_i <= z; z_i++)
                        {
                            //Determine the target point, which this data point is part of the kernel of.
                            int targetZ = z_i + kernelSize / 2;
                            if (targetZ > setInfo.EndZ)
                            {
                                break;
                            }
                            //Determine the kernel index, at which this data point is for the kernel of the above taget point.
                            int iKernelIndexZ = z - z_i;

                            double coeff = CenteredFiniteDiffCoeff[iKernelIndexZ];
                            long zindex = new Morton3D(targetZ, y, x);
                            if (active_points.ContainsKey(zindex))
                            {
                                point = active_points[zindex];
                            }
                            else
                            {
                                point = new SQLUtility.PartialResult(zindex, GetResultSize(), numPointsInKernel);
                                active_points.Add(zindex, point);
                            }
                            point.result[1] += coeff * atom.data[off + 0]; // computes the contribution from dux/dz to cy
                            point.result[0] -= coeff * atom.data[off + 1]; // computes the contribution from duy/dz to cx
                            point.numPointsProcessed++;
                            if (point.numPointsProcessed == point.numPointsInKernel)
                            {
                                processed_points.Add(point);
                                active_points.Remove(zindex);
                            }
                        }

                        off += atom.GetComponents;
                    }
                }
            }
            return processed_points;
        }
        
        public void GetPDFUsingCutout(int[] coordiantes, int[] bins, int bin_size)
        {
            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Fd4Lag4)
            {
                throw new Exception("Invalid interpolation option specified!");
            }
            SQLUtility.PartialResult point;
            int[] cutout_dimensions = new int[] { cutout_coordinates[5] - cutout_coordinates[2],
                                                  cutout_coordinates[4] - cutout_coordinates[1],
                                                  cutout_coordinates[3] - cutout_coordinates[0] };
            int startz = 0, endz = 0, starty = 0, endy = 0, startx = 0, endx = 0, offset_y = 0;
            long zindex = 0;
            for (int z = coordiantes[2]; z < coordiantes[5]; z++)
            {
                for (int y = coordiantes[1]; y < coordiantes[4]; y++)
                {
                    for (int x = coordiantes[0]; x < coordiantes[3]; x++)
                    {
                        GetKernelStartEnd(z, y, x, cutout_coordinates, ref zindex, ref startz, ref endz, ref starty, ref endy, ref startx, ref endx);

                        point = new SQLUtility.PartialResult(zindex, GetResultSize(), numPointsInKernel);
                        GetResultUsingCutout(ref point, x, y, z, startx, starty, startz, endx, endy, endz, cutout_coordinates, cutout_dimensions, offset_y);

                        // Compute the norm.
                        double norm = 0.0f;
                        for (int i = 0; i < GetResultSize(); i++)
                        {
                            norm += point.result[i] * point.result[i];
                        }
                        norm = Math.Sqrt(norm);
                        point.norm = norm;
                        int bin = (int)norm / bin_size;
                        if (bin >= bins.Length)
                        {
                            bins[bins.Length - 1]++;
                        }
                        else
                        {
                            bins[bin]++;
                        }
                    }
                }
            }
        }

        protected override void GetResultUsingCutout(ref SQLUtility.PartialResult point, int x, int y, int z, int startx, int starty, int startz, int endx, int endy, int endz,
            int[] cutout_coordinates, int[] cutout_dimensions, int offset_y)
        {
            for (int x_i = startx; x_i <= endx; x_i++)
            {
                //Determine the kernel index, at which this data point is for the kernel of the above taget point.
                int iKernelIndexX = x_i - x + kernelSize / 2;

                double coeff = CenteredFiniteDiffCoeff[iKernelIndexX];
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
                int iKernelIndexY = y_i - y + kernelSize / 2;

                double coeff = CenteredFiniteDiffCoeff[iKernelIndexY];
                ulong sourceIndex = (((ulong)z - (ulong)cutout_coordinates[2]) * (ulong)cutout_dimensions[2] * (ulong)cutout_dimensions[1] +
                    ((ulong)y_i - (ulong)cutout_coordinates[1]) * (ulong)cutout_dimensions[2] +
                    ((ulong)x - (ulong)cutout_coordinates[0])) * (ulong)setInfo.Components;
                point.result[0] += coeff * GetDataItem(sourceIndex + 2); // computes the contribution from duz/dy to cx
                point.result[2] -= coeff * GetDataItem(sourceIndex); // computes the contribution from dux/dy to cz
                point.numPointsProcessed++;
            }
            for (int z_i = startz; z_i <= endz; z_i++)
            {
                //Determine the kernel index, at which this data point is for the kernel of the above taget point.
                int iKernelIndexZ = z_i - z + kernelSize / 2;

                double coeff = CenteredFiniteDiffCoeff[iKernelIndexZ];
                ulong sourceIndex = (((ulong)z_i - (ulong)cutout_coordinates[2]) * (ulong)cutout_dimensions[2] * (ulong)cutout_dimensions[1] +
                    ((ulong)y - (ulong)cutout_coordinates[1]) * (ulong)cutout_dimensions[2] +
                    ((ulong)x - (ulong)cutout_coordinates[0])) * (ulong)setInfo.Components;
                point.result[1] += coeff * GetDataItem(sourceIndex); // computes the contribution from dux/dz to cy
                point.result[0] -= coeff * GetDataItem(sourceIndex + 1); // computes the contribution from duy/dz to cx
                point.numPointsProcessed++;
            }
        }

        public override int GetResultSize()
        {
            return 3;
        }

        /// <summary>
        /// Partial computation of the curl of a vector field. The components of the vector field are stored in the data parameter.
        /// Here we use only a single data point from the kernel, where the kernel indexes are given as below.
        /// cx = (duz / dy) - (duy / dz)
        /// cy = (dux / dz) - (duz / dx)
        /// cz = (duy / dx) - (dux / dy)
        /// </summary>
        /// <remarks>
        /// Interpolation is not needed as the point at which the curl is computed is on a grid node.
        /// </remarks>
        unsafe public double[] CalcCurl(double[] data, int iKernelIndexX, int iKernelIndexY, int iKernelIndexZ)
        {
            double[] result = new double[GetResultSize()]; // Result value for the user
            for (int i = 0; i < GetResultSize(); i++)
                result[i] = 0.0;
            
            switch (spatialInterp)
            {
                case TurbulenceOptions.SpatialInterpolation.None_Fd4:
                case TurbulenceOptions.SpatialInterpolation.None_Fd6:
                case TurbulenceOptions.SpatialInterpolation.None_Fd8:
                    fixed (double* FDCoeff = CenteredFiniteDiffCoeff, fdata = data)
                    {
                        double coeff = FDCoeff[iKernelIndexZ];
                        result[1] += coeff * fdata[0]; // computes the contribution from dux/dz to cy
                        result[0] -= coeff * fdata[1]; // computes the contribution from duy/dz to cx

                        coeff = FDCoeff[iKernelIndexX];
                        result[2] += coeff * fdata[1]; // computes the contribution from duy/dx to cz
                        result[1] -= coeff * fdata[2]; // computes the contribution from duz/dx to cy

                        coeff = FDCoeff[iKernelIndexY];
                        result[0] += coeff * fdata[2]; // computes the contribution from duz/dy to cx
                        result[2] -= coeff * fdata[0]; // computes the contribution from dux/dy to cz
                    }
                    break;
                default:
                    throw new Exception("Invalid Spatial Interpolation Option");
            }

            return result;
        }

    }

}
