using System;
using System.Collections.Generic;
using System.Text;

namespace Turbulence.SciLib
{
    /// <summary>
    /// Class to contain utility functions for temporal interpolation of data
    /// </summary>
    public class TemporalInterpolation
    {
        /// <summary>
        /// Specific implementation of PCHIP using 4 data values across 4 time points.
        /// This function only interpolates one dimension at a time.
        /// </summary>
        /// <remarks>
        /// TODO: A special case function should be created for when the first time is 0.
        /// TODO: Do we need to do a special case for when the times are not evenly spaced?
        /// </remarks>
        /// <param name="time">Time requested [i.e., [0.005]</param>
        /// <param name="times">Four time values in an array (i.e., [0.002,0.004,0.006,0.008]</param>
        /// <param name="data">Value cooresponding to each of these times</param>
        public static float PCHIP ( float time, float [] times, float [] data ) {
            float a, b, c, d;
            float r;
            float delta = times[2] - times[1];
            float drv1 = ((data[2] - data[1]) / (times[2] - times[1]) + (data[1] - data[0]) / (times[1] - times[0]
            )) / 2;
            float drv2 = ((data[3] - data[2]) / (times[3] - times[2]) + (data[2] - data[1]) / (times[2] - times[1]
            )) / 2;

            a = data[1];
            b = drv1;
            c = ((data[2] - data[1]) / delta - drv1) / delta;
            d = 2 / delta / delta * ((drv1 + drv2) / 2 - (data[2] - data[1]) / (times[2] - times[1]));
            r = a + b * (time - times[1]) + c * (time - times[1]) * (time - times[1]) +
                 d * (time - times[1]) * (time - times[1]) * (time - times[2]);
            return r;
        }

        // Version of the same function to bypass array bound checking
        public static float PCHIP(float time,
            float time0, float time1, float time2, float time3,
            float data0, float data1, float data2, float data3)
        {
            float a, b, c, d;
            float r;
            float delta = time2 - time1;
            float drv1 = ((data2 - data1) / delta + (data1 - data0) / (time1 - time0)) / 2;
            float drv2 = ((data3 - data2) / (time3 - time2) + (data2 - data1) / delta) / 2;

            a = data1;
            b = drv1;
            c = ((data2 - data1) / delta - drv1) / delta;
            d = 2 / delta / delta * ((drv1 + drv2) / 2 - (data2 - data1) / delta);
            r = a + b * (time - time1) + c * (time - time1) * (time - time1) +
                 d * (time - time1) * (time - time1) * (time - time2);
            return r;
        }


        /// <summary>
        /// General implementation of PCHIP to support an arbitrary number of data dimensions.
        /// </summary>
        /// <remarks>
        /// TODO: Implement this general interpolator for use in multiple different functions.
        /// </remarks>
        /// <param name="time">time we are interpolating for</param>
        /// <param name="dataTimes">time of values we have</param>
        /// <param name="rawdata">data for each of the measured times</param>
        /// <param name="dimensions">number of dimensions to convert</param>
        /// <param name="outdata">output data, one per dimension</param>
        /*public static void PCHIP(float time, float[] dataTimes, float[][] rawdata,
            int dimensions, float[] outdata)
        {
            // TODO!
        }*/

    }
}
