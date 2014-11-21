using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Turbulence.TurbLib;

namespace Turbulence.REST
{
    public class InterpolationOptions
    {
        public TurbulenceOptions.SpatialInterpolation Spatial { get; set; } 
        public TurbulenceOptions.TemporalInterpolation Temporal { get; set; }

        public InterpolationOptions(string spatial, string temporal)
        {
            switch (spatial) {
            case "Lag6":
                Spatial = TurbulenceOptions.SpatialInterpolation.Lag6;
                break;
            case "Lag8":
                Spatial = TurbulenceOptions.SpatialInterpolation.Lag8;
                break;
            case "Lag4":
                Spatial = TurbulenceOptions.SpatialInterpolation.Lag4;
                break;
            case "FD4Lag4":
                Spatial = TurbulenceOptions.SpatialInterpolation.Fd4Lag4;
                break;
            case "FD4NoInt":
                Spatial = TurbulenceOptions.SpatialInterpolation.None_Fd4;
                break;
            case "FD6NoInt":
                Spatial = TurbulenceOptions.SpatialInterpolation.None_Fd6;
                break;
            case "FD8NoInt":
                Spatial = TurbulenceOptions.SpatialInterpolation.None_Fd8;
                break;
            default:
                Spatial = TurbulenceOptions.SpatialInterpolation.None;
                break;
            }

            switch (temporal) {
                case "PCHIP":
                    Temporal = TurbulenceOptions.TemporalInterpolation.PCHIP;
                    break;
                default:
                    Temporal = TurbulenceOptions.TemporalInterpolation.None;
                    break;
            }
        }
    }
}