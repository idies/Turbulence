using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Turbulence.TurbLib;
using Turbulence.SciLib;
using Turbulence.SQLInterface;

using System.Collections.Generic;

namespace Turbulence.SQLInterface
{
    /// <summary>
    /// Generic interface for a database procedure.
    /// 
    /// This interface supports procedures which only operate on points
    /// and their 8*8*8 bounding boxes at a single point in time.
    /// Time interpolation is supported by averaging 
    /// </summary>
    public abstract class Worker
    {
        protected const int MAX_NUMBER_THRESHOLD_POINTS = 1024 * 1024;
        //protected TurbDataTable setInfo;
        protected SqlDataRecord record;
        protected int kernelSize = -1; // This is the size of the kernel of computation

        public SqlDataRecord Record { get { return record; } }
        public TurbDataTable DataTable { get { return setInfo; } }
        public virtual TurbDataTable setInfo { get; set; }
        public TurbulenceOptions.SpatialInterpolation spatialInterp { get; protected set; }

        public abstract double[] GetResult(TurbulenceBlob blob, SQLUtility.InputRequest input);
        public abstract double[] GetResult(TurbulenceBlob blob, SQLUtility.MHDInputRequest input);
        public virtual HashSet<SQLUtility.PartialResult> GetResult(TurbulenceBlob blob, Dictionary<long, SQLUtility.PartialResult> active_points)
        {
            throw new NotImplementedException();
        }
        public virtual HashSet<SQLUtility.PartialResult> GetThresholdUsingCutout(float[] cutout, int[] cutout_coordinates, int[] coordiantes, double threshold)
        {
            throw new NotImplementedException();
        }
        public virtual HashSet<SQLUtility.PartialResult> GetThresholdUsingCutout(BigArray<float> cutout, int[] cutout_coordinates, int[] coordiantes, double threshold)
        {
            throw new NotImplementedException();
        }
        public abstract int GetResultSize();
        public abstract SqlMetaData[] GetRecordMetaData();

        public int KernelSize{ get {return kernelSize; } }
        //public virtual void GetAtomsForPoint(float xp, float yp, float zp, long mask, HashSet<long> atoms)
        //{
        //    throw new NotImplementedException();
        //}
        public virtual void GetAtomsForPoint(SQLUtility.MHDInputRequest request, long mask, int pointsPerCubeEstimate, Dictionary<long, List<int>> map, ref int total_points) 
        { 
            throw new NotImplementedException();
        }

        /// <summary>
        /// Given the coordinates of region where we want to compute the particular field
        /// returns coordinates [startx, starty, startz, endx, endy, endz]
        /// of the data cutout need to perform the entire computation.
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        public virtual int[] GetCutoutCoordinates(int[] coordinates)
        {
            int half_kernel = KernelSize / 2;
            return new int[] {coordinates[0] - half_kernel, coordinates[1] - half_kernel, coordinates[2] - half_kernel,
                                                coordinates[3] + half_kernel, coordinates[4] + half_kernel, coordinates[5] + half_kernel};
        }

        protected virtual void AddAtoms(int startz, int starty, int startx, int endz, int endy, int endx, HashSet<long> atoms, long mask)
        {
            long zindex;
            // we do not want a request to appear more than once in the list for an atom
            // with the below logic we are going to check distinct atoms only
            // we want to start at the start of a DB atom and then move from atom to atom
            startz = startz - ((startz % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;
            starty = starty - ((starty % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;
            startx = startx - ((startx % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;
            for (int z = startz; z <= endz; z += setInfo.atomDim)
            {
                for (int y = starty; y <= endy; y += setInfo.atomDim)
                {
                    for (int x = startx; x <= endx; x += setInfo.atomDim)
                    {
                        // Wrap the coordinates into the grid space
                        int xi = ((x % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
                        int yi = ((y % setInfo.GridResolutionY) + setInfo.GridResolutionY) % setInfo.GridResolutionY;
                        int zi = ((z % setInfo.GridResolutionZ) + setInfo.GridResolutionZ) % setInfo.GridResolutionZ;

                        if (setInfo.PointInRange(xi, yi, zi))
                        {
                            zindex = new Morton3D(zi, yi, xi).Key & mask;
                            if (!atoms.Contains(zindex))
                                atoms.Add(zindex);
                        }
                    }
                }
            }
        }

        /*
         * Integer value passed to SQL from the web service.
         * These values are also used for logging.
         */
        public enum Workers
        {
            Sample = 0,
            GetVelocity = 1,
            GetVelocityWithPressure = 2,
            GetVelocityGradient = 3,
            GetPressureGradient = 4,
            GetVelocityHessian = 7,
            GetPressureHessian = 8,
            GetVelocityLaplacian = 5,
            GetLaplacianOfGradient = 6,
            GetPosition = 21,
            GetPositionDBEvaluation = 22,
            GetPressure = 50,
            GetBoxFilterPressure = 90,
            GetBoxFilterVelocity = 91,
            GetBoxFilterVelocityGradient = 92,
            GetBoxFilterSGSStress = 93,
            GetForce = 100,
            NullOp = 999,
            GetVelocityOld = 888,
            GetVelocityWithPressureOld = 889,

            GetMHDVelocity = 56,
            GetMHDPressure = 57,
            GetMHDMagnetic = 58,
            GetMHDPotential = 59,
            GetRawVelocity = 60,
            GetRawPressure = 61,
            GetRawMagnetic = 62,
            GetRawPotential = 63,
            GetMHDVelocityGradient = 64,
            GetMHDMagneticGradient = 65,
            GetMHDPotentialGradient = 66,
            GetMHDPressureGradient = 67,
            GetMHDVelocityLaplacian = 68,
            GetMHDMagneticLaplacian = 69,
            GetMHDPotentialLaplacian = 70,
            GetMHDVelocityHessian = 71,
            GetMHDMagneticHessian = 72,
            GetMHDPotentialHessian = 73,
            GetMHDPressureHessian = 74,

            GetMHDBoxFilter = 75,
            GetMHDBoxFilterSV = 77,
            GetMHDBoxFilterSGS = 78,
            GetMHDBoxFilterSGS_SV = 79,
            GetMHDBoxFilterGradient = 80,

            GetCurl = 81,
            GetCurlThreshold = 82,
            GetChannelCurlThreshold = 83,
            GetVelocityThreshold = 84,
            GetMagneticThreshold = 85,
            GetPotentialThreshold = 86,
            GetPressureThreshold = 87,
            GetChannelVelocityThreshold = 88,
            GetChannelPressureThreshold = 89,
            GetQThreshold = 30,
            GetChannelQThreshold = 31,
            GetDensityThreshold = 32,

            GetChannelVelocity = 120,
            GetChannelPressure = 121,
            GetChannelVelocityGradient = 122,
            GetChannelPressureGradient = 123,
            GetChannelVelocityLaplacian = 124,
            GetChannelVelocityHessian = 125,
            GetChannelPressureHessian = 126,

            GetDensity = 150,
            GetDensityGradient = 151,
            GetDensityHessian = 152,
            GetRawDensity = 153,

            GetVelocityWorkerDirectOpt = 556,
            GetVelocityWorkerDirectWorst = 557

        }

        public static Worker GetWorker(TurbDataTable setInfo, int procedure,
            int spatialInterpOption,
            float arg,
            SqlConnection sqlcon)
        {
            TurbulenceOptions.SpatialInterpolation spatialInterp = (TurbulenceOptions.SpatialInterpolation)spatialInterpOption;
            switch ((Workers) procedure)
            {
                case Workers.Sample:
                    return new workers.SampleWorker(setInfo);
                case Workers.GetVelocity:
                    return new workers.GetVelocityWorker(setInfo, spatialInterp, true, false);
                case Workers.GetVelocityWithPressure:
                    return new workers.GetVelocityWorker(setInfo, spatialInterp, true, true);
                case Workers.GetPressure:
                    return new workers.GetVelocityWorker(setInfo, spatialInterp, false, true);
                case Workers.GetPressureHessian:
                   return new workers.PressureHessian(setInfo, spatialInterp);
                case Workers.GetVelocityHessian:
                    return new workers.VelocityHessian(setInfo, spatialInterp);
                case Workers.GetVelocityGradient:
                    return new workers.GetVelocityGradient(setInfo, spatialInterp);
                case Workers.GetPressureGradient:
                    return new workers.GetPressureGradient(setInfo, spatialInterp);
                case Workers.GetVelocityLaplacian:
                    return new workers.VelocityLaplacian(setInfo, spatialInterp);
                case Workers.GetLaplacianOfGradient:
                    return new workers.LaplacianOfGradient(setInfo, spatialInterp);
                case Workers.GetPosition:
                    return new workers.GetPosition(setInfo, spatialInterp, arg);
                    //return new workers.GetPosition(setInfo, spatialInterp, arg);
                case Workers.GetPositionDBEvaluation:
                    return new workers.GetPositionWorker(setInfo, spatialInterp, (TurbulenceOptions.TemporalInterpolation)arg);
                case Workers.GetVelocityOld:
                    return new workers.GetVelocityWorkerOld(setInfo, spatialInterp, false);
                case Workers.GetVelocityWithPressureOld:
                    return new workers.GetVelocityWorkerOld(setInfo, spatialInterp, true);

                case Workers.GetMHDVelocity:
                case Workers.GetMHDMagnetic:
                case Workers.GetMHDPotential:
                case Workers.GetVelocityThreshold:
                case Workers.GetMagneticThreshold:
                case Workers.GetPotentialThreshold:
                    if (TurbulenceOptions.SplinesOption(spatialInterp))
                    {
                        return new workers.GetSplinesWorker(setInfo, spatialInterp, 0);
                    }
                    else
                    {
                        return new workers.GetMHDWorker(setInfo, spatialInterp);
                    }
                case Workers.GetMHDPressure:
                case Workers.GetDensity:
                case Workers.GetPressureThreshold:
                case Workers.GetDensityThreshold:
                    if (TurbulenceOptions.SplinesOption(spatialInterp))
                    {
                        return new workers.GetSplinesWorker(setInfo, spatialInterp, 0);
                    }
                    else
                    {
                        return new workers.GetMHDPressure(setInfo, spatialInterp);
                    }
                case Workers.GetMHDVelocityGradient:
                case Workers.GetMHDMagneticGradient:
                case Workers.GetMHDPotentialGradient:
                    if (TurbulenceOptions.SplinesOption(spatialInterp))
                    {
                        return new workers.GetSplinesWorker(setInfo, spatialInterp, 1);
                    }
                    else
                    {
                        return new workers.GetMHDGradient(setInfo, spatialInterp);
                    }
                case Workers.GetMHDPressureGradient:
                case Workers.GetDensityGradient:
                    if (TurbulenceOptions.SplinesOption(spatialInterp))
                    {
                        return new workers.GetSplinesWorker(setInfo, spatialInterp, 1);
                    }
                    else
                    {
                        return new workers.GetMHDPressureGradient(setInfo, spatialInterp);
                    }
                case Workers.GetMHDVelocityLaplacian:
                case Workers.GetMHDMagneticLaplacian:
                case Workers.GetMHDPotentialLaplacian:
                    return new workers.GetMHDLaplacian(setInfo, spatialInterp);
                case Workers.GetMHDVelocityHessian:
                case Workers.GetMHDMagneticHessian:
                case Workers.GetMHDPotentialHessian:
                    if (TurbulenceOptions.SplinesOption(spatialInterp))
                    {
                        return new workers.GetSplinesWorker(setInfo, spatialInterp, 2);
                    }
                    else
                    {
                        return new workers.GetMHDHessian(setInfo, spatialInterp);
                    }
                case Workers.GetMHDPressureHessian:
                case Workers.GetDensityHessian:
                    if (TurbulenceOptions.SplinesOption(spatialInterp))
                    {
                        return new workers.GetSplinesWorker(setInfo, spatialInterp, 2);
                    }
                    else
                    {
                        return new workers.GetMHDPressureHessian(setInfo, spatialInterp);
                    }

                case Workers.GetMHDBoxFilter:
                    return new workers.GetMHDBoxFilter(setInfo, spatialInterp, arg);

                case Workers.GetMHDBoxFilterSGS:
                    return new workers.GetMHDBoxFilterSGS(setInfo, spatialInterp, arg);

                case Workers.GetMHDBoxFilterSV:
                    return new workers.GetMHDBoxFilterSV(setInfo, spatialInterp, arg);

                case Workers.GetMHDBoxFilterSGS_SV:
                    return new workers.GetMHDBoxFilterSGS_SV(setInfo, spatialInterp, arg);

                case Workers.GetMHDBoxFilterGradient:
                    return new workers.GetMHDBoxFilterGradient(setInfo, spatialInterpOption, arg);

                case Workers.GetCurl:
                case Workers.GetCurlThreshold:
                    return new workers.GetCurl(setInfo, spatialInterp);
                case Workers.GetChannelCurlThreshold:
                    return new workers.GetChannelCurl(setInfo, spatialInterp, sqlcon);
                case Workers.GetQThreshold:
                    return new workers.GetQ(setInfo, spatialInterp);
                case Workers.GetChannelQThreshold:
                    return new workers.GetChannelQ(setInfo, spatialInterp, sqlcon);

                case Workers.GetChannelVelocity:
                case Workers.GetChannelVelocityThreshold:
                    if (TurbulenceOptions.SplinesOption(spatialInterp))
                    {
                        return new workers.GetChannelSplinesWorker(setInfo, spatialInterp, 0, sqlcon);
                    }
                    else
                    {
                        return new workers.GetChannelVelocity(setInfo, spatialInterp, sqlcon);
                    }
                case Workers.GetChannelPressure:
                case Workers.GetChannelPressureThreshold:
                    if (TurbulenceOptions.SplinesOption(spatialInterp))
                    {
                        return new workers.GetChannelSplinesWorker(setInfo, spatialInterp, 0, sqlcon);
                    }
                    else
                    {
                        return new workers.GetChannelPressure(setInfo, spatialInterp, sqlcon);
                    }
                case Workers.GetChannelVelocityGradient:
                case Workers.GetChannelPressureGradient:
                    if (TurbulenceOptions.SplinesOption(spatialInterp))
                    {
                        return new workers.GetChannelSplinesWorker(setInfo, spatialInterp, 1, sqlcon);
                    }
                    else
                    {
                        return new workers.GetChannelGradient(setInfo, spatialInterp, sqlcon);
                    }
                case Workers.GetChannelVelocityLaplacian:
                    return new workers.GetChannelLaplacian(setInfo, spatialInterp, sqlcon);
                case Workers.GetChannelVelocityHessian:
                case Workers.GetChannelPressureHessian:
                    if (TurbulenceOptions.SplinesOption(spatialInterp))
                    {
                        return new workers.GetChannelSplinesWorker(setInfo, spatialInterp, 2, sqlcon);
                    }
                    else
                    {
                        return new workers.GetChannelHessian(setInfo, spatialInterp, sqlcon);
                    }                    

                default:
                    throw new Exception(String.Format("Unknown worker type: {0}", procedure));
            }
        }


    }

}
