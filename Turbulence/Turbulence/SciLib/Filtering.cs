using System;
using System.Collections.Generic;
using System.Text;

namespace Turbulence.SciLib
{
    class Filtering
    {
        public static double FilteringCoefficients(double filter_width)
        {
            return 1.0 / ((filter_width) * (filter_width) * (filter_width));
        }
    }
}
