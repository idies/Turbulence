using System;
using System.Collections.Generic;
using System.Text;
using Turbulence;
using Turbulence.TurbLib;
using Turbulence.TurbLib.DataTypes;

namespace TurbulenceService
{
    public class GetForce
    {

        /// <summary>
        /// Calculate the forcing vector at a given location based on stored wave information.
        /// </summary>
        /// <param name="forceInfo">Wave information read from either disk or database</param>
        public Vector3 getForceByTime(FourierInfo[] forceInfo, Point3 point)
        {
            float x = point.x;
            float y = point.y;
            float z = point.z;

            Vector3 result;

            float fx, fy, fz, fxim, fyim, fzim, tmpc, tmps;
            fx = fy = fz = 0;
            fxim = fyim = fzim = 0;
            for (int i = 0  ; i < 50; i++)
            {
                FourierInfo wave = forceInfo[i];
                tmpc = (float)Math.Cos(wave.kx * x + wave.ky * y + wave.kz * z);
                tmps = (float)Math.Sin(wave.kx * x + wave.ky * y + wave.kz * z);
                fx = fx + wave.fxr * tmpc - wave.fxi * tmps;
                fy = fy + wave.fyr * tmpc - wave.fyi * tmps;
                fz = fz + wave.fzr * tmpc - wave.fzi * tmps;
                fxim = fxim + wave.fxr * tmps + wave.fxi * tmpc;
                fyim = fyim + wave.fyr * tmps + wave.fyi * tmpc;
                fzim = fzim + wave.fzr * tmps + wave.fzi * tmpc;
                if (wave.kx > 0.01)
                {
                    tmpc = (float)Math.Cos(-wave.kx * x - wave.ky * y - wave.kz * z);
                    tmps = (float)Math.Sin(-wave.kx * x - wave.ky * y - wave.kz * z);
                    fx = fx + wave.fxr * tmpc + wave.fxi * tmps;
                    fy = fy + wave.fyr * tmpc + wave.fyi * tmps;
                    fz = fz + wave.fzr * tmpc + wave.fzi * tmps;
                    fxim = fxim + wave.fxr * tmps - wave.fxi * tmpc;
                    fyim = fyim + wave.fyr * tmps - wave.fyi * tmpc;
                    fzim = fzim + wave.fzr * tmps - wave.fzi * tmpc;
                }
            }
            if ((fxim > 0.001 || fxim < -0.001) || (fyim > 0.001 || fyim < -0.001) || (fyim > 0.001 || fyim < -0.001))
            {
                    throw new Exception(String.Format("Warning! fxim={0},fyim={1},fzim={2}", fxim, fyim, fzim));
            }

            result.x = fx;
            result.y = fy;
            result.z = fz;

            return result;
        }
    }
}
