using System;
using System.Collections.Generic;
using System.Text;

namespace Turbulence.TurbLib
{
    public class TurbulenceOptions
    {
        public enum TemporalInterpolation
        {
            None = 0,
            PCHIP = 1
        }

        public enum SpatialInterpolation
        {
            None = 0,
            None_Fd4 = 40,
            None_Fd6 = 60,
            None_Fd8 = 80,
            Fd4Lag4 = 44,
            Lag4 = 4,
            Lag6 = 6,
            Lag8 = 8,
            M1Q4 = 104,
            M2Q4 = 204,
            M3Q4 = 304,
            M4Q4 = 404,
            M1Q6 = 106,
            M2Q6 = 206,
            M3Q6 = 306,
            M4Q6 = 406,
            M1Q8 = 108,
            M2Q8 = 208,
            M3Q8 = 308,
            M4Q8 = 408,
            M1Q10 = 110,
            M2Q10 = 210,
            M3Q10 = 310,
            M4Q10 = 410,
            M1Q12 = 112,
            M2Q12 = 212,
            M3Q12 = 312,
            M4Q12 = 412,
            M1Q14 = 114,
            M2Q14 = 214,
            M3Q14 = 314,
            M4Q14 = 414
        }

        public static bool SplinesOption(SpatialInterpolation spatialInterp)
        {
            if (spatialInterp == SpatialInterpolation.M1Q4 ||
                spatialInterp == SpatialInterpolation.M2Q4 ||
                spatialInterp == SpatialInterpolation.M3Q4 ||
                spatialInterp == SpatialInterpolation.M4Q4 ||
                spatialInterp == SpatialInterpolation.M1Q6 ||
                spatialInterp == SpatialInterpolation.M2Q6 ||
                spatialInterp == SpatialInterpolation.M3Q6 ||
                spatialInterp == SpatialInterpolation.M4Q6 ||
                spatialInterp == SpatialInterpolation.M1Q8 ||
                spatialInterp == SpatialInterpolation.M2Q8 ||
                spatialInterp == SpatialInterpolation.M3Q8 ||
                spatialInterp == SpatialInterpolation.M4Q8 ||
                spatialInterp == SpatialInterpolation.M1Q10 ||
                spatialInterp == SpatialInterpolation.M2Q10 ||
                spatialInterp == SpatialInterpolation.M3Q10 ||
                spatialInterp == SpatialInterpolation.M4Q10 ||
                spatialInterp == SpatialInterpolation.M1Q12 ||
                spatialInterp == SpatialInterpolation.M2Q12 ||
                spatialInterp == SpatialInterpolation.M3Q12 ||
                spatialInterp == SpatialInterpolation.M4Q12 ||
                spatialInterp == SpatialInterpolation.M1Q14 ||
                spatialInterp == SpatialInterpolation.M2Q14 ||
                spatialInterp == SpatialInterpolation.M3Q14 ||
                spatialInterp == SpatialInterpolation.M4Q14)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void GetKernelSize(SpatialInterpolation spatialInterp, ref int kernelSize, ref int kernelSizeY, bool IsChannelGrid, int worker)
        {
            switch (spatialInterp)
            {
                case TurbulenceOptions.SpatialInterpolation.Lag4:
                    kernelSize = 4;
                    kernelSizeY = 4;
                    if (IsChannelGrid)
                        kernelSizeY++;
                    break;
                case TurbulenceOptions.SpatialInterpolation.Lag6:
                    kernelSize = 6;
                    kernelSizeY = 6;
                    if (IsChannelGrid)
                        kernelSizeY++;
                    break;
                case TurbulenceOptions.SpatialInterpolation.Lag8:
                    kernelSize = 8;
                    kernelSizeY = 8;
                    if (IsChannelGrid)
                        kernelSizeY++;
                    break;
                case TurbulenceOptions.SpatialInterpolation.None:
                    kernelSize = 0;
                    kernelSizeY = 0;
                    break;
                case TurbulenceOptions.SpatialInterpolation.None_Fd4:
                    kernelSize = 5;
                    kernelSizeY = 5;
                    if (IsChannelGrid)
                    {
                        // For the channel flow DB, because of the non-uniformity of the y gird
                        // the kernel along y can have different starting position depending on which
                        // side of the center line the given point is on. In order to simplify the
                        // handling of these different cases we assume a larger kernel here and below.
                        if (worker == (int)Turbulence.SQLInterface.Worker.Workers.GetChannelVelocityHessian ||
                            worker == (int)Turbulence.SQLInterface.Worker.Workers.GetChannelPressureHessian ||
                            worker == (int)Turbulence.SQLInterface.Worker.Workers.GetChannelVelocityLaplacian)
                        {
                            kernelSizeY = 7;
                        }
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.None_Fd6:
                    kernelSize = 7;
                    kernelSizeY = 7;
                    if (IsChannelGrid)
                    {
                        if (worker == (int)Turbulence.SQLInterface.Worker.Workers.GetChannelVelocityHessian ||
                            worker == (int)Turbulence.SQLInterface.Worker.Workers.GetChannelPressureHessian ||
                            worker == (int)Turbulence.SQLInterface.Worker.Workers.GetChannelVelocityLaplacian)
                        {
                            kernelSizeY = 9;
                        }
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.None_Fd8:
                    kernelSize = 9;
                    kernelSizeY = 9;
                    if (IsChannelGrid)
                    {
                        if (worker == (int)Turbulence.SQLInterface.Worker.Workers.GetChannelVelocityHessian ||
                            worker == (int)Turbulence.SQLInterface.Worker.Workers.GetChannelPressureHessian ||
                            worker == (int)Turbulence.SQLInterface.Worker.Workers.GetChannelVelocityLaplacian)
                        {
                            kernelSizeY = 11;
                        }
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.Fd4Lag4:
                    kernelSize = 8;
                    kernelSizeY = 8;
                    if (IsChannelGrid)
                    {
                        kernelSizeY = 9;
                        if (worker == (int)Turbulence.SQLInterface.Worker.Workers.GetChannelVelocityHessian ||
                            worker == (int)Turbulence.SQLInterface.Worker.Workers.GetChannelPressureHessian ||
                            worker == (int)Turbulence.SQLInterface.Worker.Workers.GetChannelVelocityLaplacian)
                        {
                            kernelSizeY = 11;
                        }
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.M1Q4:
                case TurbulenceOptions.SpatialInterpolation.M2Q4:
                case TurbulenceOptions.SpatialInterpolation.M3Q4:
                case TurbulenceOptions.SpatialInterpolation.M4Q4:
                    kernelSize = 4;
                    kernelSizeY = 4;
                    break;
                case TurbulenceOptions.SpatialInterpolation.M1Q6:
                case TurbulenceOptions.SpatialInterpolation.M2Q6:
                case TurbulenceOptions.SpatialInterpolation.M3Q6:
                case TurbulenceOptions.SpatialInterpolation.M4Q6:
                    kernelSize = 6;
                    kernelSizeY = 6;
                    break;
                case TurbulenceOptions.SpatialInterpolation.M1Q8:
                case TurbulenceOptions.SpatialInterpolation.M2Q8:
                case TurbulenceOptions.SpatialInterpolation.M3Q8:
                case TurbulenceOptions.SpatialInterpolation.M4Q8:
                    kernelSize = 8;
                    kernelSizeY = 8;
                    break;
                case TurbulenceOptions.SpatialInterpolation.M1Q10:
                case TurbulenceOptions.SpatialInterpolation.M2Q10:
                case TurbulenceOptions.SpatialInterpolation.M3Q10:
                case TurbulenceOptions.SpatialInterpolation.M4Q10:
                    kernelSize = 10;
                    kernelSizeY = 10;
                    break;
                case TurbulenceOptions.SpatialInterpolation.M1Q12:
                case TurbulenceOptions.SpatialInterpolation.M2Q12:
                case TurbulenceOptions.SpatialInterpolation.M3Q12:
                case TurbulenceOptions.SpatialInterpolation.M4Q12:
                    kernelSize = 12;
                    kernelSizeY = 12;
                    break;
                case TurbulenceOptions.SpatialInterpolation.M1Q14:
                case TurbulenceOptions.SpatialInterpolation.M2Q14:
                case TurbulenceOptions.SpatialInterpolation.M3Q14:
                case TurbulenceOptions.SpatialInterpolation.M4Q14:
                    kernelSize = 14;
                    kernelSizeY = 14;
                    break;
            }
        }

        /*public enum ParticleTrackingScheme
        {
            //RK2AB2 = 2,
            RK2AB3 = 3,
            RK4 = 5
        }*/
    }
}
