using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Turbulence.TurbLib;
using Turbulence.SciLib;
using Turbulence.SQLInterface;
using System.Collections.Generic;

using System.Diagnostics;

namespace Turbulence.SQLInterface.workers
{
    public class GetSplinesWorker : Worker
    {
        protected int derivative;
        protected int result_size;

        //TODO: Consider reading all of these from the DB. They are already included in the python library that generates the CSV files.
        //      Then the coefficients can be handled the same way as for the non-uniform y dimension of the channel grid.
        protected void ComputeBetas(int deriv, double x, double[] poly_val, int offset)
        {
            switch (spatialInterp)
            {
                case TurbulenceOptions.SpatialInterpolation.M1Q4:
                    switch (deriv)
                    {
                        case 0:
                            poly_val[offset + 0] = x * (x * (-1.0 / 2.0 * x + 1) - 1.0 / 2.0);
                            poly_val[offset + 1] = Math.Pow(x, 2.0) * ((3.0 / 2.0) * x - 5.0 / 2.0) + 1;
                            poly_val[offset + 2] = x * (x * (-3.0 / 2.0 * x + 2) + 1.0 / 2.0);
                            poly_val[offset + 3] = Math.Pow(x, 2) * ((1.0 / 2.0) * x - 1.0 / 2.0);
                            break;
                        case 1:
                            poly_val[offset + 0] = x * (-3.0 / 2.0 * x + 2) - 1.0 / 2.0;
                            poly_val[offset + 1] = x * ((9.0 / 2.0) * x - 5);
                            poly_val[offset + 2] = x * (-9.0 / 2.0 * x + 4) + 1.0 / 2.0;
                            poly_val[offset + 3] = x * ((3.0 / 2.0) * x - 1);
                            break;
                        case 2:
                            poly_val[offset + 0] = -3 * x + 2;
                            poly_val[offset + 1] = 9 * x - 5;
                            poly_val[offset + 2] = -9 * x + 4;
                            poly_val[offset + 3] = 3 * x - 1;
                            break;
                        default:
                            throw new Exception("Invalid derivative option!");
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.M1Q6:
                    switch (deriv)
                    {
                        case 0:
                            poly_val[offset + 0] = x * (x * ((1.0 / 12.0) * x - 1.0 / 6.0) + 1.0 / 12.0);
                            poly_val[offset + 1] = x * (x * (-7.0 / 12.0 * x + 5.0 / 4.0) - 2.0 / 3.0);
                            poly_val[offset + 2] = Math.Pow(x, 2) * ((4.0 / 3.0) * x - 7.0 / 3.0) + 1;
                            poly_val[offset + 3] = x * (x * (-4.0 / 3.0 * x + 5.0 / 3.0) + 2.0 / 3.0);
                            poly_val[offset + 4] = x * (x * ((7.0 / 12.0) * x - 1.0 / 2.0) - 1.0 / 12.0);
                            poly_val[offset + 5] = Math.Pow(x, 2) * (-1.0 / 12.0 * x + 1.0 / 12.0);
                            break;
                        case 1:
                            poly_val[offset + 0] = x * ((1.0 / 4.0) * x - 1.0 / 3.0) + 1.0 / 12.0;
                            poly_val[offset + 1] = x * (-7.0 / 4.0 * x + 5.0 / 2.0) - 2.0 / 3.0;
                            poly_val[offset + 2] = x * (4 * x - 14.0 / 3.0);
                            poly_val[offset + 3] = x * (-4 * x + 10.0 / 3.0) + 2.0 / 3.0;
                            poly_val[offset + 4] = x * ((7.0 / 4.0) * x - 1) - 1.0 / 12.0;
                            poly_val[offset + 5] = x * (-1.0 / 4.0 * x + 1.0 / 6.0);
                            break;
                        case 2:
                            poly_val[offset + 0] = (1.0 / 2.0) * x - 1.0 / 3.0;
                            poly_val[offset + 1] = -7.0 / 2.0 * x + 5.0 / 2.0;
                            poly_val[offset + 2] = 8 * x - 14.0 / 3.0;
                            poly_val[offset + 3] = -8 * x + 10.0 / 3.0;
                            poly_val[offset + 4] = (7.0 / 2.0) * x - 1;
                            poly_val[offset + 5] = -1.0 / 2.0 * x + 1.0 / 6.0;
                            break;
                        default:
                            throw new Exception("Invalid derivative option!");
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.M1Q8:
                    switch (deriv)
                    {
                        case 0:
                            poly_val[offset + 0] = x * (x * (-1.0 / 60.0 * x + 1.0 / 30.0) - 1.0 / 60.0);
                            poly_val[offset + 1] = x * (x * ((2.0 / 15.0) * x - 17.0 / 60.0) + 3.0 / 20.0);
                            poly_val[offset + 2] = x * (x * (-3.0 / 5.0 * x + 27.0 / 20.0) - 3.0 / 4.0);
                            poly_val[offset + 3] = Math.Pow(x, 2) * ((5.0 / 4.0) * x - 9.0 / 4.0) + 1;
                            poly_val[offset + 4] = x * (x * (-5.0 / 4.0 * x + 3.0 / 2.0) + 3.0 / 4.0);
                            poly_val[offset + 5] = x * (x * ((3.0 / 5.0) * x - 9.0 / 20.0) - 3.0 / 20.0);
                            poly_val[offset + 6] = x * (x * (-2.0 / 15.0 * x + 7.0 / 60.0) + 1.0 / 60.0);
                            poly_val[offset + 7] = Math.Pow(x, 2) * ((1.0 / 60.0) * x - 1.0 / 60.0);
                            break;
                        case 1:
                            poly_val[offset + 0] = x * (-1.0 / 20.0 * x + 1.0 / 15.0) - 1.0 / 60.0;
                            poly_val[offset + 1] = x * ((2.0 / 5.0) * x - 17.0 / 30.0) + 3.0 / 20.0;
                            poly_val[offset + 2] = x * (-9.0 / 5.0 * x + 27.0 / 10.0) - 3.0 / 4.0;
                            poly_val[offset + 3] = x * ((15.0 / 4.0) * x - 9.0 / 2.0);
                            poly_val[offset + 4] = x * (-15.0 / 4.0 * x + 3) + 3.0 / 4.0;
                            poly_val[offset + 5] = x * ((9.0 / 5.0) * x - 9.0 / 10.0) - 3.0 / 20.0;
                            poly_val[offset + 6] = x * (-2.0 / 5.0 * x + 7.0 / 30.0) + 1.0 / 60.0;
                            poly_val[offset + 7] = x * ((1.0 / 20.0) * x - 1.0 / 30.0);
                            break;
                        case 2:
                            poly_val[offset + 0] = -1.0 / 10.0 * x + 1.0 / 15.0;
                            poly_val[offset + 1] = (4.0 / 5.0) * x - 17.0 / 30.0;
                            poly_val[offset + 2] = -18.0 / 5.0 * x + 27.0 / 10.0;
                            poly_val[offset + 3] = (15.0 / 2.0) * x - 9.0 / 2.0;
                            poly_val[offset + 4] = -15.0 / 2.0 * x + 3;
                            poly_val[offset + 5] = (18.0 / 5.0) * x - 9.0 / 10.0;
                            poly_val[offset + 6] = -4.0 / 5.0 * x + 7.0 / 30.0;
                            poly_val[offset + 7] = (1.0 / 10.0) * x - 1.0 / 30.0;
                            break;
                        default:
                            throw new Exception("Invalid derivative option!");
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.M1Q10:
                    switch (deriv)
                    {
                        case 0:
                            poly_val[offset + 0] = x * (x * ((1.0 / 280.0) * x - 1.0 / 140.0) + 1.0 / 280.0);
                            poly_val[offset + 1] = x * (x * (-29.0 / 840.0 * x + 61.0 / 840.0) - 4.0 / 105.0);
                            poly_val[offset + 2] = x * (x * ((17.0 / 105.0) * x - 38.0 / 105.0) + 1.0 / 5.0);
                            poly_val[offset + 3] = x * (x * (-3.0 / 5.0 * x + 7.0 / 5.0) - 4.0 / 5.0);
                            poly_val[offset + 4] = Math.Pow(x, 2) * ((6.0 / 5.0) * x - 11.0 / 5.0) + 1;
                            poly_val[offset + 5] = x * (x * (-6.0 / 5.0 * x + 7.0 / 5.0) + 4.0 / 5.0);
                            poly_val[offset + 6] = x * (x * ((3.0 / 5.0) * x - 2.0 / 5.0) - 1.0 / 5.0);
                            poly_val[offset + 7] = x * (x * (-17.0 / 105.0 * x + 13.0 / 105.0) + 4.0 / 105.0);
                            poly_val[offset + 8] = x * (x * ((29.0 / 840.0) * x - 13.0 / 420.0) - 1.0 / 280.0);
                            poly_val[offset + 9] = Math.Pow(x, 2) * (-1.0 / 280.0 * x + 1.0 / 280.0);
                            break;
                        case 1:
                            poly_val[offset + 0] = x * ((3.0 / 280.0) * x - 1.0 / 70.0) + 1.0 / 280.0;
                            poly_val[offset + 1] = x * (-29.0 / 280.0 * x + 61.0 / 420.0) - 4.0 / 105.0;
                            poly_val[offset + 2] = x * ((17.0 / 35.0) * x - 76.0 / 105.0) + 1.0 / 5.0;
                            poly_val[offset + 3] = x * (-9.0 / 5.0 * x + 14.0 / 5.0) - 4.0 / 5.0;
                            poly_val[offset + 4] = x * ((18.0 / 5.0) * x - 22.0 / 5.0);
                            poly_val[offset + 5] = x * (-18.0 / 5.0 * x + 14.0 / 5.0) + 4.0 / 5.0;
                            poly_val[offset + 6] = x * ((9.0 / 5.0) * x - 4.0 / 5.0) - 1.0 / 5.0;
                            poly_val[offset + 7] = x * (-17.0 / 35.0 * x + 26.0 / 105.0) + 4.0 / 105.0;
                            poly_val[offset + 8] = x * ((29.0 / 280.0) * x - 13.0 / 210.0) - 1.0 / 280.0;
                            poly_val[offset + 9] = x * (-3.0 / 280.0 * x + 1.0 / 140.0);
                            break;
                        case 2:
                            poly_val[offset + 0] = (3.0 / 140.0) * x - 1.0 / 70.0;
                            poly_val[offset + 1] = -29.0 / 140.0 * x + 61.0 / 420.0;
                            poly_val[offset + 2] = (34.0 / 35.0) * x - 76.0 / 105.0;
                            poly_val[offset + 3] = -18.0 / 5.0 * x + 14.0 / 5.0;
                            poly_val[offset + 4] = (36.0 / 5.0) * x - 22.0 / 5.0;
                            poly_val[offset + 5] = -36.0 / 5.0 * x + 14.0 / 5.0;
                            poly_val[offset + 6] = (18.0 / 5.0) * x - 4.0 / 5.0;
                            poly_val[offset + 7] = -34.0 / 35.0 * x + 26.0 / 105.0;
                            poly_val[offset + 8] = (29.0 / 140.0) * x - 13.0 / 210.0;
                            poly_val[offset + 9] = -3.0 / 140.0 * x + 1.0 / 140.0;
                            break;
                        default:
                            throw new Exception("Invalid derivative option!");
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.M1Q12:
                    switch (deriv)
                    {
                        case 0:
                            poly_val[offset + 0] = x * (x * (-1.0 / 1260.0 * x + 1.0 / 630.0) - 1.0 / 1260.0);
                            poly_val[offset + 1] = x * (x * ((23.0 / 2520.0) * x - 2.0 / 105.0) + 5.0 / 504.0);
                            poly_val[offset + 2] = x * (x * (-25.0 / 504.0 * x + 55.0 / 504.0) - 5.0 / 84.0);
                            poly_val[offset + 3] = x * (x * ((5.0 / 28.0) * x - 5.0 / 12.0) + 5.0 / 21.0);
                            poly_val[offset + 4] = x * (x * (-25.0 / 42.0 * x + 10.0 / 7.0) - 5.0 / 6.0);
                            poly_val[offset + 5] = Math.Pow(x, 2) * ((7.0 / 6.0) * x - 13.0 / 6.0) + 1;
                            poly_val[offset + 6] = x * (x * (-7.0 / 6.0 * x + 4.0 / 3.0) + 5.0 / 6.0);
                            poly_val[offset + 7] = x * (x * ((25.0 / 42.0) * x - 5.0 / 14.0) - 5.0 / 21.0);
                            poly_val[offset + 8] = x * (x * (-5.0 / 28.0 * x + 5.0 / 42.0) + 5.0 / 84.0);
                            poly_val[offset + 9] = x * (x * ((25.0 / 504.0) * x - 5.0 / 126.0) - 5.0 / 504.0);
                            poly_val[offset + 10] = x * (x * (-23.0 / 2520.0 * x + 1.0 / 120.0) + 1.0 / 1260.0);
                            poly_val[offset + 11] = Math.Pow(x, 2) * ((1.0 / 1260.0) * x - 1.0 / 1260.0);
                            break;
                        case 1:
                            poly_val[offset + 0] = x * (-1.0 / 420.0 * x + 1.0 / 315.0) - 1.0 / 1260.0;
                            poly_val[offset + 1] = x * ((23.0 / 840.0) * x - 4.0 / 105.0) + 5.0 / 504.0;
                            poly_val[offset + 2] = x * (-25.0 / 168.0 * x + 55.0 / 252.0) - 5.0 / 84.0;
                            poly_val[offset + 3] = x * ((15.0 / 28.0) * x - 5.0 / 6.0) + 5.0 / 21.0;
                            poly_val[offset + 4] = x * (-25.0 / 14.0 * x + 20.0 / 7.0) - 5.0 / 6.0;
                            poly_val[offset + 5] = x * ((7.0 / 2.0) * x - 13.0 / 3.0);
                            poly_val[offset + 6] = x * (-7.0 / 2.0 * x + 8.0 / 3.0) + 5.0 / 6.0;
                            poly_val[offset + 7] = x * ((25.0 / 14.0) * x - 5.0 / 7.0) - 5.0 / 21.0;
                            poly_val[offset + 8] = x * (-15.0 / 28.0 * x + 5.0 / 21.0) + 5.0 / 84.0;
                            poly_val[offset + 9] = x * ((25.0 / 168.0) * x - 5.0 / 63.0) - 5.0 / 504.0;
                            poly_val[offset + 10] = x * (-23.0 / 840.0 * x + 1.0 / 60.0) + 1.0 / 1260.0;
                            poly_val[offset + 11] = x * ((1.0 / 420.0) * x - 1.0 / 630.0);
                            break;
                        case 2:
                            poly_val[offset + 0] = -1.0 / 210.0 * x + 1.0 / 315.0;
                            poly_val[offset + 1] = (23.0 / 420.0) * x - 4.0 / 105.0;
                            poly_val[offset + 2] = -25.0 / 84.0 * x + 55.0 / 252.0;
                            poly_val[offset + 3] = (15.0 / 14.0) * x - 5.0 / 6.0;
                            poly_val[offset + 4] = -25.0 / 7.0 * x + 20.0 / 7.0;
                            poly_val[offset + 5] = 7 * x - 13.0 / 3.0;
                            poly_val[offset + 6] = -7 * x + 8.0 / 3.0;
                            poly_val[offset + 7] = (25.0 / 7.0) * x - 5.0 / 7.0;
                            poly_val[offset + 8] = -15.0 / 14.0 * x + 5.0 / 21.0;
                            poly_val[offset + 9] = (25.0 / 84.0) * x - 5.0 / 63.0;
                            poly_val[offset + 10] = -23.0 / 420.0 * x + 1.0 / 60.0;
                            poly_val[offset + 11] = (1.0 / 210.0) * x - 1.0 / 630.0;
                            break;
                        default:
                            throw new Exception("Invalid derivative option!");
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.M1Q14:
                    switch (deriv)
                    {
                        case 0:
                            poly_val[offset + 0] = x * (x * ((1.0 / 5544.0) * x - 1.0 / 2772.0) + 1.0 / 5544.0);
                            poly_val[offset + 1] = x * (x * (-67.0 / 27720.0 * x + 139.0 / 27720.0) - 1.0 / 385.0);
                            poly_val[offset + 2] = x * (x * ((47.0 / 3080.0) * x - 51.0 / 1540.0) + 1.0 / 56.0);
                            poly_val[offset + 3] = x * (x * (-31.0 / 504.0 * x + 71.0 / 504.0) - 5.0 / 63.0);
                            poly_val[offset + 4] = x * (x * ((95.0 / 504.0) * x - 115.0 / 252.0) + 15.0 / 56.0);
                            poly_val[offset + 5] = x * (x * (-33.0 / 56.0 * x + 81.0 / 56.0) - 6.0 / 7.0);
                            poly_val[offset + 6] = Math.Pow(x, 2) * ((8.0 / 7.0) * x - 15.0 / 7.0) + 1;
                            poly_val[offset + 7] = x * (x * (-8.0 / 7.0 * x + 9.0 / 7.0) + 6.0 / 7.0);
                            poly_val[offset + 8] = x * (x * ((33.0 / 56.0) * x - 9.0 / 28.0) - 15.0 / 56.0);
                            poly_val[offset + 9] = x * (x * (-95.0 / 504.0 * x + 55.0 / 504.0) + 5.0 / 63.0);
                            poly_val[offset + 10] = x * (x * ((31.0 / 504.0) * x - 11.0 / 252.0) - 1.0 / 56.0);
                            poly_val[offset + 11] = x * (x * (-47.0 / 3080.0 * x + 39.0 / 3080.0) + 1.0 / 385.0);
                            poly_val[offset + 12] = x * (x * ((67.0 / 27720.0) * x - 31.0 / 13860.0) - 1.0 / 5544.0);
                            poly_val[offset + 13] = Math.Pow(x, 2) * (-1.0 / 5544.0 * x + 1.0 / 5544.0);
                            break;
                        case 1:
                            poly_val[offset + 0] = x * ((1.0 / 1848.0) * x - 1.0 / 1386.0) + 1.0 / 5544.0;
                            poly_val[offset + 1] = x * (-67.0 / 9240.0 * x + 139.0 / 13860.0) - 1.0 / 385.0;
                            poly_val[offset + 2] = x * ((141.0 / 3080.0) * x - 51.0 / 770.0) + 1.0 / 56.0;
                            poly_val[offset + 3] = x * (-31.0 / 168.0 * x + 71.0 / 252.0) - 5.0 / 63.0;
                            poly_val[offset + 4] = x * ((95.0 / 168.0) * x - 115.0 / 126.0) + 15.0 / 56.0;
                            poly_val[offset + 5] = x * (-99.0 / 56.0 * x + 81.0 / 28.0) - 6.0 / 7.0;
                            poly_val[offset + 6] = x * ((24.0 / 7.0) * x - 30.0 / 7.0);
                            poly_val[offset + 7] = x * (-24.0 / 7.0 * x + 18.0 / 7.0) + 6.0 / 7.0;
                            poly_val[offset + 8] = x * ((99.0 / 56.0) * x - 9.0 / 14.0) - 15.0 / 56.0;
                            poly_val[offset + 9] = x * (-95.0 / 168.0 * x + 55.0 / 252.0) + 5.0 / 63.0;
                            poly_val[offset + 10] = x * ((31.0 / 168.0) * x - 11.0 / 126.0) - 1.0 / 56.0;
                            poly_val[offset + 11] = x * (-141.0 / 3080.0 * x + 39.0 / 1540.0) + 1.0 / 385.0;
                            poly_val[offset + 12] = x * ((67.0 / 9240.0) * x - 31.0 / 6930.0) - 1.0 / 5544.0;
                            poly_val[offset + 13] = x * (-1.0 / 1848.0 * x + 1.0 / 2772.0);
                            break;
                        case 2:
                            poly_val[offset + 0] = (1.0 / 924.0) * x - 1.0 / 1386.0;
                            poly_val[offset + 1] = -67.0 / 4620.0 * x + 139.0 / 13860.0;
                            poly_val[offset + 2] = (141.0 / 1540.0) * x - 51.0 / 770.0;
                            poly_val[offset + 3] = -31.0 / 84.0 * x + 71.0 / 252.0;
                            poly_val[offset + 4] = (95.0 / 84.0) * x - 115.0 / 126.0;
                            poly_val[offset + 5] = -99.0 / 28.0 * x + 81.0 / 28.0;
                            poly_val[offset + 6] = (48.0 / 7.0) * x - 30.0 / 7.0;
                            poly_val[offset + 7] = -48.0 / 7.0 * x + 18.0 / 7.0;
                            poly_val[offset + 8] = (99.0 / 28.0) * x - 9.0 / 14.0;
                            poly_val[offset + 9] = -95.0 / 84.0 * x + 55.0 / 252.0;
                            poly_val[offset + 10] = (31.0 / 84.0) * x - 11.0 / 126.0;
                            poly_val[offset + 11] = -141.0 / 1540.0 * x + 39.0 / 1540.0;
                            poly_val[offset + 12] = (67.0 / 4620.0) * x - 31.0 / 6930.0;
                            poly_val[offset + 13] = -1.0 / 924.0 * x + 1.0 / 2772.0;
                            break;
                        default:
                            throw new Exception("Invalid derivative option!");
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.M2Q4:
                    switch (deriv)
                    {
                        case 0:
                            poly_val[offset + 0] = x * (x * (x * (x * (x - 5.0 / 2.0) + 3.0 / 2.0) + 1.0 / 2.0) - 1.0 / 2.0);
                            poly_val[offset + 1] = Math.Pow(x, 2) * (x * (x * (-3 * x + 15.0 / 2.0) - 9.0 / 2.0) - 1) + 1;
                            poly_val[offset + 2] = x * (x * (x * (x * (3 * x - 15.0 / 2.0) + 9.0 / 2.0) + 1.0 / 2.0) + 1.0 / 2.0);
                            poly_val[offset + 3] = Math.Pow(x, 3) * (x * (-x + 5.0 / 2.0) - 3.0 / 2.0);
                            break;
                        case 1:
                            poly_val[offset + 0] = x * (x * (x * (5 * x - 10) + 9.0 / 2.0) + 1) - 1.0 / 2.0;
                            poly_val[offset + 1] = x * (x * (x * (-15 * x + 30) - 27.0 / 2.0) - 2);
                            poly_val[offset + 2] = x * (x * (x * (15 * x - 30) + 27.0 / 2.0) + 1) + 1.0 / 2.0;
                            poly_val[offset + 3] = Math.Pow(x, 2) * (x * (-5 * x + 10) - 9.0 / 2.0);
                            break;
                        case 2:
                            poly_val[offset + 0] = x * (x * (20 * x - 30) + 9) + 1;
                            poly_val[offset + 1] = x * (x * (-60 * x + 90) - 27) - 2;
                            poly_val[offset + 2] = x * (x * (60 * x - 90) + 27) + 1;
                            poly_val[offset + 3] = x * (x * (-20 * x + 30) - 9);
                            break;
                        default:
                            throw new Exception("Invalid derivative option!");
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.M2Q6:
                    switch (deriv)
                    {
                        case 0:
                            poly_val[offset + 0] = x * (x * (x * (x * (-5.0 / 24.0 * x + 13.0 / 24.0) - 3.0 / 8.0) - 1.0 / 24.0) + 1.0 / 12.0);
                            poly_val[offset + 1] = x * (x * (x * (x * ((25.0 / 24.0) * x - 8.0 / 3.0) + 13.0 / 8.0) + 2.0 / 3.0) - 2.0 / 3.0);
                            poly_val[offset + 2] = Math.Pow(x, 2) * (x * (x * (-25.0 / 12.0 * x + 21.0 / 4.0) - 35.0 / 12.0) - 5.0 / 4.0) + 1;
                            poly_val[offset + 3] = x * (x * (x * (x * ((25.0 / 12.0) * x - 31.0 / 6.0) + 11.0 / 4.0) + 2.0 / 3.0) + 2.0 / 3.0);
                            poly_val[offset + 4] = x * (x * (x * (x * (-25.0 / 24.0 * x + 61.0 / 24.0) - 11.0 / 8.0) - 1.0 / 24.0) - 1.0 / 12.0);
                            poly_val[offset + 5] = Math.Pow(x, 3) * (x * ((5.0 / 24.0) * x - 1.0 / 2.0) + 7.0 / 24.0);
                            break;
                        case 1:
                            poly_val[offset + 0] = x * (x * (x * (-25.0 / 24.0 * x + 13.0 / 6.0) - 9.0 / 8.0) - 1.0 / 12.0) + 1.0 / 12.0;
                            poly_val[offset + 1] = x * (x * (x * ((125.0 / 24.0) * x - 32.0 / 3.0) + 39.0 / 8.0) + 4.0 / 3.0) - 2.0 / 3.0;
                            poly_val[offset + 2] = x * (x * (x * (-125.0 / 12.0 * x + 21) - 35.0 / 4.0) - 5.0 / 2.0);
                            poly_val[offset + 3] = x * (x * (x * ((125.0 / 12.0) * x - 62.0 / 3.0) + 33.0 / 4.0) + 4.0 / 3.0) + 2.0 / 3.0;
                            poly_val[offset + 4] = x * (x * (x * (-125.0 / 24.0 * x + 61.0 / 6.0) - 33.0 / 8.0) - 1.0 / 12.0) - 1.0 / 12.0;
                            poly_val[offset + 5] = Math.Pow(x, 2) * (x * ((25.0 / 24.0) * x - 2) + 7.0 / 8.0);
                            break;
                        case 2:
                            poly_val[offset + 0] = x * (x * (-25.0 / 6.0 * x + 13.0 / 2.0) - 9.0 / 4.0) - 1.0 / 12.0;
                            poly_val[offset + 1] = x * (x * ((125.0 / 6.0) * x - 32) + 39.0 / 4.0) + 4.0 / 3.0;
                            poly_val[offset + 2] = x * (x * (-125.0 / 3.0 * x + 63) - 35.0 / 2.0) - 5.0 / 2.0;
                            poly_val[offset + 3] = x * (x * ((125.0 / 3.0) * x - 62) + 33.0 / 2.0) + 4.0 / 3.0;
                            poly_val[offset + 4] = x * (x * (-125.0 / 6.0 * x + 61.0 / 2.0) - 33.0 / 4.0) - 1.0 / 12.0;
                            poly_val[offset + 5] = x * (x * ((25.0 / 6.0) * x - 6) + 7.0 / 4.0);
                            break;
                        default:
                            throw new Exception("Invalid derivative option!");
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.M2Q8:
                    switch (deriv)
                    {
                        case 0:
                            poly_val[offset + 0] = x * (x * (x * (x * ((2.0 / 45.0) * x - 7.0 / 60.0) + 1.0 / 12.0) + 1.0 / 180.0) - 1.0 / 60.0);
                            poly_val[offset + 1] = x * (x * (x * (x * (-23.0 / 72.0 * x + 61.0 / 72.0) - 217.0 / 360.0) - 3.0 / 40.0) + 3.0 / 20.0);
                            poly_val[offset + 2] = x * (x * (x * (x * ((39.0 / 40.0) * x - 51.0 / 20.0) + 63.0 / 40.0) + 3.0 / 4.0) - 3.0 / 4.0);
                            poly_val[offset + 3] = Math.Pow(x, 2) * (x * (x * (-59.0 / 36.0 * x + 25.0 / 6.0) - 13.0 / 6.0) - 49.0 / 36.0) + 1;
                            poly_val[offset + 4] = x * (x * (x * (x * ((59.0 / 36.0) * x - 145.0 / 36.0) + 17.0 / 9.0) + 3.0 / 4.0) + 3.0 / 4.0);
                            poly_val[offset + 5] = x * (x * (x * (x * (-39.0 / 40.0 * x + 93.0 / 40.0) - 9.0 / 8.0) - 3.0 / 40.0) - 3.0 / 20.0);
                            poly_val[offset + 6] = x * (x * (x * (x * ((23.0 / 72.0) * x - 3.0 / 4.0) + 49.0 / 120.0) + 1.0 / 180.0) + 1.0 / 60.0);
                            poly_val[offset + 7] = Math.Pow(x, 3) * (x * (-2.0 / 45.0 * x + 19.0 / 180.0) - 11.0 / 180.0);
                            break;
                        case 1:
                            poly_val[offset + 0] = x * (x * (x * ((2.0 / 9.0) * x - 7.0 / 15.0) + 1.0 / 4.0) + 1.0 / 90.0) - 1.0 / 60.0;
                            poly_val[offset + 1] = x * (x * (x * (-115.0 / 72.0 * x + 61.0 / 18.0) - 217.0 / 120.0) - 3.0 / 20.0) + 3.0 / 20.0;
                            poly_val[offset + 2] = x * (x * (x * ((39.0 / 8.0) * x - 51.0 / 5.0) + 189.0 / 40.0) + 3.0 / 2.0) - 3.0 / 4.0;
                            poly_val[offset + 3] = x * (x * (x * (-295.0 / 36.0 * x + 50.0 / 3.0) - 13.0 / 2.0) - 49.0 / 18.0);
                            poly_val[offset + 4] = x * (x * (x * ((295.0 / 36.0) * x - 145.0 / 9.0) + 17.0 / 3.0) + 3.0 / 2.0) + 3.0 / 4.0;
                            poly_val[offset + 5] = x * (x * (x * (-39.0 / 8.0 * x + 93.0 / 10.0) - 27.0 / 8.0) - 3.0 / 20.0) - 3.0 / 20.0;
                            poly_val[offset + 6] = x * (x * (x * ((115.0 / 72.0) * x - 3) + 49.0 / 40.0) + 1.0 / 90.0) + 1.0 / 60.0;
                            poly_val[offset + 7] = Math.Pow(x, 2) * (x * (-2.0 / 9.0 * x + 19.0 / 45.0) - 11.0 / 60.0);
                            break;
                        case 2:
                            poly_val[offset + 0] = x * (x * ((8.0 / 9.0) * x - 7.0 / 5.0) + 1.0 / 2.0) + 1.0 / 90.0;
                            poly_val[offset + 1] = x * (x * (-115.0 / 18.0 * x + 61.0 / 6.0) - 217.0 / 60.0) - 3.0 / 20.0;
                            poly_val[offset + 2] = x * (x * ((39.0 / 2.0) * x - 153.0 / 5.0) + 189.0 / 20.0) + 3.0 / 2.0;
                            poly_val[offset + 3] = x * (x * (-295.0 / 9.0 * x + 50) - 13) - 49.0 / 18.0;
                            poly_val[offset + 4] = x * (x * ((295.0 / 9.0) * x - 145.0 / 3.0) + 34.0 / 3.0) + 3.0 / 2.0;
                            poly_val[offset + 5] = x * (x * (-39.0 / 2.0 * x + 279.0 / 10.0) - 27.0 / 4.0) - 3.0 / 20.0;
                            poly_val[offset + 6] = x * (x * ((115.0 / 18.0) * x - 9) + 49.0 / 20.0) + 1.0 / 90.0;
                            poly_val[offset + 7] = x * (x * (-8.0 / 9.0 * x + 19.0 / 15.0) - 11.0 / 30.0);
                            break;
                        default:
                            throw new Exception("Invalid derivative option!");
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.M2Q10:
                    switch (deriv)
                    {
                        case 0:
                            poly_val[offset + 0] = x * (x * (x * (x * (-11.0 / 1120.0 * x + 29.0 / 1120.0) - 3.0 / 160.0) - 1.0 / 1120.0) + 1.0 / 280.0);
                            poly_val[offset + 1] = x * (x * (x * (x * ((907.0 / 10080.0) * x - 403.0 / 1680.0) + 589.0 / 3360.0) + 4.0 / 315.0) - 4.0 / 105.0);
                            poly_val[offset + 2] = x * (x * (x * (x * (-47.0 / 126.0 * x + 127.0 / 126.0) - 463.0 / 630.0) - 1.0 / 10.0) + 1.0 / 5.0);
                            poly_val[offset + 3] = x * (x * (x * (x * ((9.0 / 10.0) * x - 12.0 / 5.0) + 3.0 / 2.0) + 4.0 / 5.0) - 4.0 / 5.0);
                            poly_val[offset + 4] = Math.Pow(x, 2) * (x * (x * (-991.0 / 720.0 * x + 847.0 / 240.0) - 83.0 / 48.0) - 205.0 / 144.0) + 1;
                            poly_val[offset + 5] = x * (x * (x * (x * ((991.0 / 720.0) * x - 1207.0 / 360.0) + 991.0 / 720.0) + 4.0 / 5.0) + 4.0 / 5.0);
                            poly_val[offset + 6] = x * (x * (x * (x * (-9.0 / 10.0 * x + 21.0 / 10.0) - 9.0 / 10.0) - 1.0 / 10.0) - 1.0 / 5.0);
                            poly_val[offset + 7] = x * (x * (x * (x * ((47.0 / 126.0) * x - 6.0 / 7.0) + 13.0 / 30.0) + 4.0 / 315.0) + 4.0 / 105.0);
                            poly_val[offset + 8] = x * (x * (x * (x * (-907.0 / 10080.0 * x + 2117.0 / 10080.0) - 233.0 / 2016.0) - 1.0 / 1120.0) - 1.0 / 280.0);
                            poly_val[offset + 9] = Math.Pow(x, 3) * (x * ((11.0 / 1120.0) * x - 13.0 / 560.0) + 3.0 / 224.0);
                            break;
                        case 1:
                            poly_val[offset + 0] = x * (x * (x * (-11.0 / 224.0 * x + 29.0 / 280.0) - 9.0 / 160.0) - 1.0 / 560.0) + 1.0 / 280.0;
                            poly_val[offset + 1] = x * (x * (x * ((907.0 / 2016.0) * x - 403.0 / 420.0) + 589.0 / 1120.0) + 8.0 / 315.0) - 4.0 / 105.0;
                            poly_val[offset + 2] = x * (x * (x * (-235.0 / 126.0 * x + 254.0 / 63.0) - 463.0 / 210.0) - 1.0 / 5.0) + 1.0 / 5.0;
                            poly_val[offset + 3] = x * (x * (x * ((9.0 / 2.0) * x - 48.0 / 5.0) + 9.0 / 2.0) + 8.0 / 5.0) - 4.0 / 5.0;
                            poly_val[offset + 4] = x * (x * (x * (-991.0 / 144.0 * x + 847.0 / 60.0) - 83.0 / 16.0) - 205.0 / 72.0);
                            poly_val[offset + 5] = x * (x * (x * ((991.0 / 144.0) * x - 1207.0 / 90.0) + 991.0 / 240.0) + 8.0 / 5.0) + 4.0 / 5.0;
                            poly_val[offset + 6] = x * (x * (x * (-9.0 / 2.0 * x + 42.0 / 5.0) - 27.0 / 10.0) - 1.0 / 5.0) - 1.0 / 5.0;
                            poly_val[offset + 7] = x * (x * (x * ((235.0 / 126.0) * x - 24.0 / 7.0) + 13.0 / 10.0) + 8.0 / 315.0) + 4.0 / 105.0;
                            poly_val[offset + 8] = x * (x * (x * (-907.0 / 2016.0 * x + 2117.0 / 2520.0) - 233.0 / 672.0) - 1.0 / 560.0) - 1.0 / 280.0;
                            poly_val[offset + 9] = Math.Pow(x, 2) * (x * ((11.0 / 224.0) * x - 13.0 / 140.0) + 9.0 / 224.0);
                            break;
                        case 2:
                            poly_val[offset + 0] = x * (x * (-11.0 / 56.0 * x + 87.0 / 280.0) - 9.0 / 80.0) - 1.0 / 560.0;
                            poly_val[offset + 1] = x * (x * ((907.0 / 504.0) * x - 403.0 / 140.0) + 589.0 / 560.0) + 8.0 / 315.0;
                            poly_val[offset + 2] = x * (x * (-470.0 / 63.0 * x + 254.0 / 21.0) - 463.0 / 105.0) - 1.0 / 5.0;
                            poly_val[offset + 3] = x * (x * (18 * x - 144.0 / 5.0) + 9) + 8.0 / 5.0;
                            poly_val[offset + 4] = x * (x * (-991.0 / 36.0 * x + 847.0 / 20.0) - 83.0 / 8.0) - 205.0 / 72.0;
                            poly_val[offset + 5] = x * (x * ((991.0 / 36.0) * x - 1207.0 / 30.0) + 991.0 / 120.0) + 8.0 / 5.0;
                            poly_val[offset + 6] = x * (x * (-18 * x + 126.0 / 5.0) - 27.0 / 5.0) - 1.0 / 5.0;
                            poly_val[offset + 7] = x * (x * ((470.0 / 63.0) * x - 72.0 / 7.0) + 13.0 / 5.0) + 8.0 / 315.0;
                            poly_val[offset + 8] = x * (x * (-907.0 / 504.0 * x + 2117.0 / 840.0) - 233.0 / 336.0) - 1.0 / 560.0;
                            poly_val[offset + 9] = x * (x * ((11.0 / 56.0) * x - 39.0 / 140.0) + 9.0 / 112.0);
                            break;
                        default:
                            throw new Exception("Invalid derivative option!");
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.M2Q12:
                    switch (deriv)
                    {
                        case 0:
                            poly_val[offset + 0] = x * (x * (x * (x * ((1.0 / 450.0) * x - 37.0 / 6300.0) + 3.0 / 700.0) + 1.0 / 6300.0) - 1.0 / 1260.0);
                            poly_val[offset + 1] = x * (x * (x * (x * (-1247.0 / 50400.0 * x + 3329.0 / 50400.0) - 39.0 / 800.0) - 5.0 / 2016.0) + 5.0 / 504.0);
                            poly_val[offset + 2] = x * (x * (x * (x * ((85.0 / 672.0) * x - 115.0 / 336.0) + 515.0 / 2016.0) + 5.0 / 252.0) - 5.0 / 84.0);
                            poly_val[offset + 3] = x * (x * (x * (x * (-25.0 / 63.0 * x + 275.0 / 252.0) - 205.0 / 252.0) - 5.0 / 42.0) + 5.0 / 21.0);
                            poly_val[offset + 4] = x * (x * (x * (x * ((5.0 / 6.0) * x - 95.0 / 42.0) + 10.0 / 7.0) + 5.0 / 6.0) - 5.0 / 6.0);
                            poly_val[offset + 5] = Math.Pow(x, 2) * (x * (x * (-4331.0 / 3600.0 * x + 3731.0 / 1200.0) - 577.0 / 400.0) - 5269.0 / 3600.0) + 1;
                            poly_val[offset + 6] = x * (x * (x * (x * ((4331.0 / 3600.0) * x - 5231.0 / 1800.0) + 3731.0 / 3600.0) + 5.0 / 6.0) + 5.0 / 6.0);
                            poly_val[offset + 7] = x * (x * (x * (x * (-5.0 / 6.0 * x + 40.0 / 21.0) - 5.0 / 7.0) - 5.0 / 42.0) - 5.0 / 21.0);
                            poly_val[offset + 8] = x * (x * (x * (x * ((25.0 / 63.0) * x - 25.0 / 28.0) + 5.0 / 12.0) + 5.0 / 252.0) + 5.0 / 84.0);
                            poly_val[offset + 9] = x * (x * (x * (x * (-85.0 / 672.0 * x + 65.0 / 224.0) - 305.0 / 2016.0) - 5.0 / 2016.0) - 5.0 / 504.0);
                            poly_val[offset + 10] = x * (x * (x * (x * ((1247.0 / 50400.0) * x - 1453.0 / 25200.0) + 179.0 / 5600.0) + 1.0 / 6300.0) + 1.0 / 1260.0);
                            poly_val[offset + 11] = Math.Pow(x, 3) * (x * (-1.0 / 450.0 * x + 11.0 / 2100.0) - 19.0 / 6300.0);
                            break;
                        case 1:
                            poly_val[offset + 0] = x * (x * (x * ((1.0 / 90.0) * x - 37.0 / 1575.0) + 9.0 / 700.0) + 1.0 / 3150.0) - 1.0 / 1260.0;
                            poly_val[offset + 1] = x * (x * (x * (-1247.0 / 10080.0 * x + 3329.0 / 12600.0) - 117.0 / 800.0) - 5.0 / 1008.0) + 5.0 / 504.0;
                            poly_val[offset + 2] = x * (x * (x * ((425.0 / 672.0) * x - 115.0 / 84.0) + 515.0 / 672.0) + 5.0 / 126.0) - 5.0 / 84.0;
                            poly_val[offset + 3] = x * (x * (x * (-125.0 / 63.0 * x + 275.0 / 63.0) - 205.0 / 84.0) - 5.0 / 21.0) + 5.0 / 21.0;
                            poly_val[offset + 4] = x * (x * (x * ((25.0 / 6.0) * x - 190.0 / 21.0) + 30.0 / 7.0) + 5.0 / 3.0) - 5.0 / 6.0;
                            poly_val[offset + 5] = x * (x * (x * (-4331.0 / 720.0 * x + 3731.0 / 300.0) - 1731.0 / 400.0) - 5269.0 / 1800.0);
                            poly_val[offset + 6] = x * (x * (x * ((4331.0 / 720.0) * x - 5231.0 / 450.0) + 3731.0 / 1200.0) + 5.0 / 3.0) + 5.0 / 6.0;
                            poly_val[offset + 7] = x * (x * (x * (-25.0 / 6.0 * x + 160.0 / 21.0) - 15.0 / 7.0) - 5.0 / 21.0) - 5.0 / 21.0;
                            poly_val[offset + 8] = x * (x * (x * ((125.0 / 63.0) * x - 25.0 / 7.0) + 5.0 / 4.0) + 5.0 / 126.0) + 5.0 / 84.0;
                            poly_val[offset + 9] = x * (x * (x * (-425.0 / 672.0 * x + 65.0 / 56.0) - 305.0 / 672.0) - 5.0 / 1008.0) - 5.0 / 504.0;
                            poly_val[offset + 10] = x * (x * (x * ((1247.0 / 10080.0) * x - 1453.0 / 6300.0) + 537.0 / 5600.0) + 1.0 / 3150.0) + 1.0 / 1260.0;
                            poly_val[offset + 11] = Math.Pow(x, 2) * (x * (-1.0 / 90.0 * x + 11.0 / 525.0) - 19.0 / 2100.0);
                            break;
                        case 2:
                            poly_val[offset + 0] = x * (x * ((2.0 / 45.0) * x - 37.0 / 525.0) + 9.0 / 350.0) + 1.0 / 3150.0;
                            poly_val[offset + 1] = x * (x * (-1247.0 / 2520.0 * x + 3329.0 / 4200.0) - 117.0 / 400.0) - 5.0 / 1008.0;
                            poly_val[offset + 2] = x * (x * ((425.0 / 168.0) * x - 115.0 / 28.0) + 515.0 / 336.0) + 5.0 / 126.0;
                            poly_val[offset + 3] = x * (x * (-500.0 / 63.0 * x + 275.0 / 21.0) - 205.0 / 42.0) - 5.0 / 21.0;
                            poly_val[offset + 4] = x * (x * ((50.0 / 3.0) * x - 190.0 / 7.0) + 60.0 / 7.0) + 5.0 / 3.0;
                            poly_val[offset + 5] = x * (x * (-4331.0 / 180.0 * x + 3731.0 / 100.0) - 1731.0 / 200.0) - 5269.0 / 1800.0;
                            poly_val[offset + 6] = x * (x * ((4331.0 / 180.0) * x - 5231.0 / 150.0) + 3731.0 / 600.0) + 5.0 / 3.0;
                            poly_val[offset + 7] = x * (x * (-50.0 / 3.0 * x + 160.0 / 7.0) - 30.0 / 7.0) - 5.0 / 21.0;
                            poly_val[offset + 8] = x * (x * ((500.0 / 63.0) * x - 75.0 / 7.0) + 5.0 / 2.0) + 5.0 / 126.0;
                            poly_val[offset + 9] = x * (x * (-425.0 / 168.0 * x + 195.0 / 56.0) - 305.0 / 336.0) - 5.0 / 1008.0;
                            poly_val[offset + 10] = x * (x * ((1247.0 / 2520.0) * x - 1453.0 / 2100.0) + 537.0 / 2800.0) + 1.0 / 3150.0;
                            poly_val[offset + 11] = x * (x * (-2.0 / 45.0 * x + 11.0 / 175.0) - 19.0 / 1050.0);
                            break;
                        default:
                            throw new Exception("Invalid derivative option!");
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.M2Q14:
                    switch (deriv)
                    {
                        case 0:
                            poly_val[offset + 0] = x * (x * (x * (x * (-17.0 / 33264.0 * x + 5.0 / 3696.0) - 1.0 / 1008.0) - 1.0 / 33264.0) + 1.0 / 5544.0);
                            poly_val[offset + 1] = x * (x * (x * (x * ((5573.0 / 831600.0) * x - 3721.0 / 207900.0) + 1577.0 / 118800.0) + 1.0 / 1925.0) - 1.0 / 385.0);
                            poly_val[offset + 2] = x * (x * (x * (x * (-359.0 / 8800.0 * x + 6791.0 / 61600.0) - 729.0 / 8800.0) - 1.0 / 224.0) + 1.0 / 56.0);
                            poly_val[offset + 3] = x * (x * (x * (x * ((929.0 / 6048.0) * x - 425.0 / 1008.0) + 647.0 / 2016.0) + 5.0 / 189.0) - 5.0 / 63.0);
                            poly_val[offset + 4] = x * (x * (x * (x * (-175.0 / 432.0 * x + 3425.0 / 3024.0) - 2605.0 / 3024.0) - 15.0 / 112.0) + 15.0 / 56.0);
                            poly_val[offset + 5] = x * (x * (x * (x * ((87.0 / 112.0) * x - 15.0 / 7.0) + 153.0 / 112.0) + 6.0 / 7.0) - 6.0 / 7.0);
                            poly_val[offset + 6] = Math.Pow(x, 2) * (x * (x * (-27217.0 / 25200.0 * x + 23617.0 / 8400.0) - 10417.0 / 8400.0) - 5369.0 / 3600.0) + 1;
                            poly_val[offset + 7] = x * (x * (x * (x * ((27217.0 / 25200.0) * x - 32617.0 / 12600.0) + 20017.0 / 25200.0) + 6.0 / 7.0) + 6.0 / 7.0);
                            poly_val[offset + 8] = x * (x * (x * (x * (-87.0 / 112.0 * x + 195.0 / 112.0) - 9.0 / 16.0) - 15.0 / 112.0) - 15.0 / 56.0);
                            poly_val[offset + 9] = x * (x * (x * (x * ((175.0 / 432.0) * x - 25.0 / 28.0) + 55.0 / 144.0) + 5.0 / 189.0) + 5.0 / 63.0);
                            poly_val[offset + 10] = x * (x * (x * (x * (-929.0 / 6048.0 * x + 2095.0 / 6048.0) - 1031.0 / 6048.0) - 1.0 / 224.0) - 1.0 / 56.0);
                            poly_val[offset + 11] = x * (x * (x * (x * ((359.0 / 8800.0) * x - 2887.0 / 30800.0) + 279.0 / 5600.0) + 1.0 / 1925.0) + 1.0 / 385.0);
                            poly_val[offset + 12] = x * (x * (x * (x * (-5573.0 / 831600.0 * x + 4327.0 / 277200.0) - 2411.0 / 277200.0) - 1.0 / 33264.0) - 1.0 / 5544.0);
                            poly_val[offset + 13] = Math.Pow(x, 3) * (x * ((17.0 / 33264.0) * x - 5.0 / 4158.0) + 23.0 / 33264.0);
                            break;
                        case 1:
                            poly_val[offset + 0] = x * (x * (x * (-85.0 / 33264.0 * x + 5.0 / 924.0) - 1.0 / 336.0) - 1.0 / 16632.0) + 1.0 / 5544.0;
                            poly_val[offset + 1] = x * (x * (x * ((5573.0 / 166320.0) * x - 3721.0 / 51975.0) + 1577.0 / 39600.0) + 2.0 / 1925.0) - 1.0 / 385.0;
                            poly_val[offset + 2] = x * (x * (x * (-359.0 / 1760.0 * x + 6791.0 / 15400.0) - 2187.0 / 8800.0) - 1.0 / 112.0) + 1.0 / 56.0;
                            poly_val[offset + 3] = x * (x * (x * ((4645.0 / 6048.0) * x - 425.0 / 252.0) + 647.0 / 672.0) + 10.0 / 189.0) - 5.0 / 63.0;
                            poly_val[offset + 4] = x * (x * (x * (-875.0 / 432.0 * x + 3425.0 / 756.0) - 2605.0 / 1008.0) - 15.0 / 56.0) + 15.0 / 56.0;
                            poly_val[offset + 5] = x * (x * (x * ((435.0 / 112.0) * x - 60.0 / 7.0) + 459.0 / 112.0) + 12.0 / 7.0) - 6.0 / 7.0;
                            poly_val[offset + 6] = x * (x * (x * (-27217.0 / 5040.0 * x + 23617.0 / 2100.0) - 10417.0 / 2800.0) - 5369.0 / 1800.0);
                            poly_val[offset + 7] = x * (x * (x * ((27217.0 / 5040.0) * x - 32617.0 / 3150.0) + 20017.0 / 8400.0) + 12.0 / 7.0) + 6.0 / 7.0;
                            poly_val[offset + 8] = x * (x * (x * (-435.0 / 112.0 * x + 195.0 / 28.0) - 27.0 / 16.0) - 15.0 / 56.0) - 15.0 / 56.0;
                            poly_val[offset + 9] = x * (x * (x * ((875.0 / 432.0) * x - 25.0 / 7.0) + 55.0 / 48.0) + 10.0 / 189.0) + 5.0 / 63.0;
                            poly_val[offset + 10] = x * (x * (x * (-4645.0 / 6048.0 * x + 2095.0 / 1512.0) - 1031.0 / 2016.0) - 1.0 / 112.0) - 1.0 / 56.0;
                            poly_val[offset + 11] = x * (x * (x * ((359.0 / 1760.0) * x - 2887.0 / 7700.0) + 837.0 / 5600.0) + 2.0 / 1925.0) + 1.0 / 385.0;
                            poly_val[offset + 12] = x * (x * (x * (-5573.0 / 166320.0 * x + 4327.0 / 69300.0) - 2411.0 / 92400.0) - 1.0 / 16632.0) - 1.0 / 5544.0;
                            poly_val[offset + 13] = Math.Pow(x, 2) * (x * ((85.0 / 33264.0) * x - 10.0 / 2079.0) + 23.0 / 11088.0);
                            break;
                        case 2:
                            poly_val[offset + 0] = x * (x * (-85.0 / 8316.0 * x + 5.0 / 308.0) - 1.0 / 168.0) - 1.0 / 16632.0;
                            poly_val[offset + 1] = x * (x * ((5573.0 / 41580.0) * x - 3721.0 / 17325.0) + 1577.0 / 19800.0) + 2.0 / 1925.0;
                            poly_val[offset + 2] = x * (x * (-359.0 / 440.0 * x + 20373.0 / 15400.0) - 2187.0 / 4400.0) - 1.0 / 112.0;
                            poly_val[offset + 3] = x * (x * ((4645.0 / 1512.0) * x - 425.0 / 84.0) + 647.0 / 336.0) + 10.0 / 189.0;
                            poly_val[offset + 4] = x * (x * (-875.0 / 108.0 * x + 3425.0 / 252.0) - 2605.0 / 504.0) - 15.0 / 56.0;
                            poly_val[offset + 5] = x * (x * ((435.0 / 28.0) * x - 180.0 / 7.0) + 459.0 / 56.0) + 12.0 / 7.0;
                            poly_val[offset + 6] = x * (x * (-27217.0 / 1260.0 * x + 23617.0 / 700.0) - 10417.0 / 1400.0) - 5369.0 / 1800.0;
                            poly_val[offset + 7] = x * (x * ((27217.0 / 1260.0) * x - 32617.0 / 1050.0) + 20017.0 / 4200.0) + 12.0 / 7.0;
                            poly_val[offset + 8] = x * (x * (-435.0 / 28.0 * x + 585.0 / 28.0) - 27.0 / 8.0) - 15.0 / 56.0;
                            poly_val[offset + 9] = x * (x * ((875.0 / 108.0) * x - 75.0 / 7.0) + 55.0 / 24.0) + 10.0 / 189.0;
                            poly_val[offset + 10] = x * (x * (-4645.0 / 1512.0 * x + 2095.0 / 504.0) - 1031.0 / 1008.0) - 1.0 / 112.0;
                            poly_val[offset + 11] = x * (x * ((359.0 / 440.0) * x - 8661.0 / 7700.0) + 837.0 / 2800.0) + 2.0 / 1925.0;
                            poly_val[offset + 12] = x * (x * (-5573.0 / 41580.0 * x + 4327.0 / 23100.0) - 2411.0 / 46200.0) - 1.0 / 16632.0;
                            poly_val[offset + 13] = x * (x * ((85.0 / 8316.0) * x - 10.0 / 693.0) + 23.0 / 5544.0);
                            break;
                        default:
                            throw new Exception("Invalid derivative option!");
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.M3Q4:
                    throw new Exception(String.Format("Invalid Spatial Interpolation Option: {0}", spatialInterp));
                case TurbulenceOptions.SpatialInterpolation.M3Q6:
                    switch (deriv)
                    {
                        case 0:
                            poly_val[offset + 0] = x * (x * (x * (x * (x * (x * ((7.0 / 12.0) * x - 49.0 / 24.0) + 29.0 / 12.0) - 11.0 / 12.0) - 1.0 / 12.0) - 1.0 / 24.0) + 1.0 / 12.0);
                            poly_val[offset + 1] = x * (x * (x * (x * (x * (x * (-35.0 / 12.0 * x + 245.0 / 24.0) - 145.0 / 12.0) + 37.0 / 8.0) + 1.0 / 6.0) + 2.0 / 3.0) - 2.0 / 3.0);
                            poly_val[offset + 2] = Math.Pow(x, 2) * (Math.Pow(x, 2) * (x * (x * ((35.0 / 6.0) * x - 245.0 / 12.0) + 145.0 / 6.0) - 28.0 / 3.0) - 5.0 / 4.0) + 1;
                            poly_val[offset + 3] = x * (x * (x * (x * (x * (x * (-35.0 / 6.0 * x + 245.0 / 12.0) - 145.0 / 6.0) + 113.0 / 12.0) - 1.0 / 6.0) + 2.0 / 3.0) + 2.0 / 3.0);
                            poly_val[offset + 4] = x * (x * (x * (x * (x * (x * ((35.0 / 12.0) * x - 245.0 / 24.0) + 145.0 / 12.0) - 19.0 / 4.0) + 1.0 / 12.0) - 1.0 / 24.0) - 1.0 / 12.0);
                            poly_val[offset + 5] = Math.Pow(x, 4) * (x * (x * (-7.0 / 12.0 * x + 49.0 / 24.0) - 29.0 / 12.0) + 23.0 / 24.0);
                            break;
                        case 1:
                            poly_val[offset + 0] = x * (x * (x * (x * (x * ((49.0 / 12.0) * x - 49.0 / 4.0) + 145.0 / 12.0) - 11.0 / 3.0) - 1.0 / 4.0) - 1.0 / 12.0) + 1.0 / 12.0;
                            poly_val[offset + 1] = x * (x * (x * (x * (x * (-245.0 / 12.0 * x + 245.0 / 4.0) - 725.0 / 12.0) + 37.0 / 2.0) + 1.0 / 2.0) + 4.0 / 3.0) - 2.0 / 3.0;
                            poly_val[offset + 2] = x * (Math.Pow(x, 2) * (x * (x * ((245.0 / 6.0) * x - 245.0 / 2.0) + 725.0 / 6.0) - 112.0 / 3.0) - 5.0 / 2.0);
                            poly_val[offset + 3] = x * (x * (x * (x * (x * (-245.0 / 6.0 * x + 245.0 / 2.0) - 725.0 / 6.0) + 113.0 / 3.0) - 1.0 / 2.0) + 4.0 / 3.0) + 2.0 / 3.0;
                            poly_val[offset + 4] = x * (x * (x * (x * (x * ((245.0 / 12.0) * x - 245.0 / 4.0) + 725.0 / 12.0) - 19) + 1.0 / 4.0) - 1.0 / 12.0) - 1.0 / 12.0;
                            poly_val[offset + 5] = Math.Pow(x, 3) * (x * (x * (-49.0 / 12.0 * x + 49.0 / 4.0) - 145.0 / 12.0) + 23.0 / 6.0);
                            break;
                        case 2:
                            poly_val[offset + 0] = x * (x * (x * (x * ((49.0 / 2.0) * x - 245.0 / 4.0) + 145.0 / 3.0) - 11) - 1.0 / 2.0) - 1.0 / 12.0;
                            poly_val[offset + 1] = x * (x * (x * (x * (-245.0 / 2.0 * x + 1225.0 / 4.0) - 725.0 / 3.0) + 111.0 / 2.0) + 1) + 4.0 / 3.0;
                            poly_val[offset + 2] = Math.Pow(x, 2) * (x * (x * (245 * x - 1225.0 / 2.0) + 1450.0 / 3.0) - 112) - 5.0 / 2.0;
                            poly_val[offset + 3] = x * (x * (x * (x * (-245 * x + 1225.0 / 2.0) - 1450.0 / 3.0) + 113) - 1) + 4.0 / 3.0;
                            poly_val[offset + 4] = x * (x * (x * (x * ((245.0 / 2.0) * x - 1225.0 / 4.0) + 725.0 / 3.0) - 57) + 1.0 / 2.0) - 1.0 / 12.0;
                            poly_val[offset + 5] = Math.Pow(x, 2) * (x * (x * (-49.0 / 2.0 * x + 245.0 / 4.0) - 145.0 / 3.0) + 23.0 / 2.0);
                            break;
                        default:
                            throw new Exception("Invalid derivative option!");
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.M3Q8:
                    switch (deriv)
                    {
                        case 0:
                            poly_val[offset + 0] = x * (x * (x * (x * (x * (x * (-89.0 / 720.0 * x + 13.0 / 30.0) - 37.0 / 72.0) + 7.0 / 36.0) + 1.0 / 48.0) + 1.0 / 180.0) - 1.0 / 60.0);
                            poly_val[offset + 1] = x * (x * (x * (x * (x * (x * ((623.0 / 720.0) * x - 2183.0 / 720.0) + 2581.0 / 720.0) - 191.0 / 144.0) - 1.0 / 6.0) - 3.0 / 40.0) + 3.0 / 20.0);
                            poly_val[offset + 2] = x * (x * (x * (x * (x * (x * (-623.0 / 240.0 * x + 1091.0 / 120.0) - 429.0 / 40.0) + 95.0 / 24.0) + 13.0 / 48.0) + 3.0 / 4.0) - 3.0 / 4.0);
                            poly_val[offset + 3] = Math.Pow(x, 2) * (Math.Pow(x, 2) * (x * (x * ((623.0 / 144.0) * x - 727.0 / 48.0) + 2569.0 / 144.0) - 959.0 / 144.0) - 49.0 / 36.0) + 1;
                            poly_val[offset + 4] = x * (x * (x * (x * (x * (x * (-623.0 / 144.0 * x + 545.0 / 36.0) - 1283.0 / 72.0) + 61.0 / 9.0) - 13.0 / 48.0) + 3.0 / 4.0) + 3.0 / 4.0);
                            poly_val[offset + 5] = x * (x * (x * (x * (x * (x * ((623.0 / 240.0) * x - 2179.0 / 240.0) + 171.0 / 16.0) - 199.0 / 48.0) + 1.0 / 6.0) - 3.0 / 40.0) - 3.0 / 20.0);
                            poly_val[offset + 6] = x * (x * (x * (x * (x * (x * (-623.0 / 720.0 * x + 121.0 / 40.0) - 1283.0 / 360.0) + 101.0 / 72.0) - 1.0 / 48.0) + 1.0 / 180.0) + 1.0 / 60.0);
                            poly_val[offset + 7] = Math.Pow(x, 4) * (x * (x * ((89.0 / 720.0) * x - 311.0 / 720.0) + 367.0 / 720.0) - 29.0 / 144.0);
                            break;
                        case 1:
                            poly_val[offset + 0] = x * (x * (x * (x * (x * (-623.0 / 720.0 * x + 13.0 / 5.0) - 185.0 / 72.0) + 7.0 / 9.0) + 1.0 / 16.0) + 1.0 / 90.0) - 1.0 / 60.0;
                            poly_val[offset + 1] = x * (x * (x * (x * (x * ((4361.0 / 720.0) * x - 2183.0 / 120.0) + 2581.0 / 144.0) - 191.0 / 36.0) - 1.0 / 2.0) - 3.0 / 20.0) + 3.0 / 20.0;
                            poly_val[offset + 2] = x * (x * (x * (x * (x * (-4361.0 / 240.0 * x + 1091.0 / 20.0) - 429.0 / 8.0) + 95.0 / 6.0) + 13.0 / 16.0) + 3.0 / 2.0) - 3.0 / 4.0;
                            poly_val[offset + 3] = x * (Math.Pow(x, 2) * (x * (x * ((4361.0 / 144.0) * x - 727.0 / 8.0) + 12845.0 / 144.0) - 959.0 / 36.0) - 49.0 / 18.0);
                            poly_val[offset + 4] = x * (x * (x * (x * (x * (-4361.0 / 144.0 * x + 545.0 / 6.0) - 6415.0 / 72.0) + 244.0 / 9.0) - 13.0 / 16.0) + 3.0 / 2.0) + 3.0 / 4.0;
                            poly_val[offset + 5] = x * (x * (x * (x * (x * ((4361.0 / 240.0) * x - 2179.0 / 40.0) + 855.0 / 16.0) - 199.0 / 12.0) + 1.0 / 2.0) - 3.0 / 20.0) - 3.0 / 20.0;
                            poly_val[offset + 6] = x * (x * (x * (x * (x * (-4361.0 / 720.0 * x + 363.0 / 20.0) - 1283.0 / 72.0) + 101.0 / 18.0) - 1.0 / 16.0) + 1.0 / 90.0) + 1.0 / 60.0;
                            poly_val[offset + 7] = Math.Pow(x, 3) * (x * (x * ((623.0 / 720.0) * x - 311.0 / 120.0) + 367.0 / 144.0) - 29.0 / 36.0);
                            break;
                        case 2:
                            poly_val[offset + 0] = x * (x * (x * (x * (-623.0 / 120.0 * x + 13) - 185.0 / 18.0) + 7.0 / 3.0) + 1.0 / 8.0) + 1.0 / 90.0;
                            poly_val[offset + 1] = x * (x * (x * (x * ((4361.0 / 120.0) * x - 2183.0 / 24.0) + 2581.0 / 36.0) - 191.0 / 12.0) - 1) - 3.0 / 20.0;
                            poly_val[offset + 2] = x * (x * (x * (x * (-4361.0 / 40.0 * x + 1091.0 / 4.0) - 429.0 / 2.0) + 95.0 / 2.0) + 13.0 / 8.0) + 3.0 / 2.0;
                            poly_val[offset + 3] = Math.Pow(x, 2) * (x * (x * ((4361.0 / 24.0) * x - 3635.0 / 8.0) + 12845.0 / 36.0) - 959.0 / 12.0) - 49.0 / 18.0;
                            poly_val[offset + 4] = x * (x * (x * (x * (-4361.0 / 24.0 * x + 2725.0 / 6.0) - 6415.0 / 18.0) + 244.0 / 3.0) - 13.0 / 8.0) + 3.0 / 2.0;
                            poly_val[offset + 5] = x * (x * (x * (x * ((4361.0 / 40.0) * x - 2179.0 / 8.0) + 855.0 / 4.0) - 199.0 / 4.0) + 1) - 3.0 / 20.0;
                            poly_val[offset + 6] = x * (x * (x * (x * (-4361.0 / 120.0 * x + 363.0 / 4.0) - 1283.0 / 18.0) + 101.0 / 6.0) - 1.0 / 8.0) + 1.0 / 90.0;
                            poly_val[offset + 7] = Math.Pow(x, 2) * (x * (x * ((623.0 / 120.0) * x - 311.0 / 24.0) + 367.0 / 36.0) - 29.0 / 12.0);
                            break;
                        default:
                            throw new Exception("Invalid derivative option!");
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.M3Q10:
                    switch (deriv)
                    {
                        case 0:
                            poly_val[offset + 0] = x * (x * (x * (x * (x * (x * ((55.0 / 2016.0) * x - 193.0 / 2016.0) + 191.0 / 1680.0) - 31.0 / 720.0) - 7.0 / 1440.0) - 1.0 / 1120.0) + 1.0 / 280.0);
                            poly_val[offset + 1] = x * (x * (x * (x * (x * (x * (-2477.0 / 10080.0 * x + 69.0 / 80.0) - 10313.0 / 10080.0) + 481.0 / 1260.0) + 1.0 / 20.0) + 4.0 / 315.0) - 4.0 / 105.0);
                            poly_val[offset + 2] = x * (x * (x * (x * (x * (x * ((4957.0 / 5040.0) * x - 1087.0 / 315.0) + 10277.0 / 2520.0) - 31.0 / 21.0) - 169.0 / 720.0) - 1.0 / 10.0) + 1.0 / 5.0);
                            poly_val[offset + 3] = x * (x * (x * (x * (x * (x * (-551.0 / 240.0 * x + 1159.0 / 144.0) - 2273.0 / 240.0) + 811.0 / 240.0) + 61.0 / 180.0) + 4.0 / 5.0) - 4.0 / 5.0);
                            poly_val[offset + 4] = Math.Pow(x, 2) * (Math.Pow(x, 2) * (x * (x * ((31.0 / 9.0) * x - 193.0 / 16.0) + 1273.0 / 90.0) - 1837.0 / 360.0) - 205.0 / 144.0) + 1;
                            poly_val[offset + 5] = x * (x * (x * (x * (x * (x * (-31.0 / 9.0 * x + 1735.0 / 144.0) - 5077.0 / 360.0) + 419.0 / 80.0) - 61.0 / 180.0) + 4.0 / 5.0) + 4.0 / 5.0);
                            poly_val[offset + 6] = x * (x * (x * (x * (x * (x * ((551.0 / 240.0) * x - 361.0 / 45.0) + 1127.0 / 120.0) - 18.0 / 5.0) + 169.0 / 720.0) - 1.0 / 10.0) - 1.0 / 5.0);
                            poly_val[offset + 7] = x * (x * (x * (x * (x * (x * (-4957.0 / 5040.0 * x + 1923.0 / 560.0) - 20299.0 / 5040.0) + 227.0 / 144.0) - 1.0 / 20.0) + 4.0 / 315.0) + 4.0 / 105.0);
                            poly_val[offset + 8] = x * (x * (x * (x * (x * (x * ((2477.0 / 10080.0) * x - 247.0 / 288.0) + 5083.0 / 5040.0) - 667.0 / 1680.0) + 7.0 / 1440.0) - 1.0 / 1120.0) - 1.0 / 280.0);
                            poly_val[offset + 9] = Math.Pow(x, 4) * (x * (x * (-55.0 / 2016.0 * x + 2.0 / 21.0) - 377.0 / 3360.0) + 223.0 / 5040.0);
                            break;
                        case 1:
                            poly_val[offset + 0] = x * (x * (x * (x * (x * ((55.0 / 288.0) * x - 193.0 / 336.0) + 191.0 / 336.0) - 31.0 / 180.0) - 7.0 / 480.0) - 1.0 / 560.0) + 1.0 / 280.0;
                            poly_val[offset + 1] = x * (x * (x * (x * (x * (-2477.0 / 1440.0 * x + 207.0 / 40.0) - 10313.0 / 2016.0) + 481.0 / 315.0) + 3.0 / 20.0) + 8.0 / 315.0) - 4.0 / 105.0;
                            poly_val[offset + 2] = x * (x * (x * (x * (x * ((4957.0 / 720.0) * x - 2174.0 / 105.0) + 10277.0 / 504.0) - 124.0 / 21.0) - 169.0 / 240.0) - 1.0 / 5.0) + 1.0 / 5.0;
                            poly_val[offset + 3] = x * (x * (x * (x * (x * (-3857.0 / 240.0 * x + 1159.0 / 24.0) - 2273.0 / 48.0) + 811.0 / 60.0) + 61.0 / 60.0) + 8.0 / 5.0) - 4.0 / 5.0;
                            poly_val[offset + 4] = x * (Math.Pow(x, 2) * (x * (x * ((217.0 / 9.0) * x - 579.0 / 8.0) + 1273.0 / 18.0) - 1837.0 / 90.0) - 205.0 / 72.0);
                            poly_val[offset + 5] = x * (x * (x * (x * (x * (-217.0 / 9.0 * x + 1735.0 / 24.0) - 5077.0 / 72.0) + 419.0 / 20.0) - 61.0 / 60.0) + 8.0 / 5.0) + 4.0 / 5.0;
                            poly_val[offset + 6] = x * (x * (x * (x * (x * ((3857.0 / 240.0) * x - 722.0 / 15.0) + 1127.0 / 24.0) - 72.0 / 5.0) + 169.0 / 240.0) - 1.0 / 5.0) - 1.0 / 5.0;
                            poly_val[offset + 7] = x * (x * (x * (x * (x * (-4957.0 / 720.0 * x + 5769.0 / 280.0) - 20299.0 / 1008.0) + 227.0 / 36.0) - 3.0 / 20.0) + 8.0 / 315.0) + 4.0 / 105.0;
                            poly_val[offset + 8] = x * (x * (x * (x * (x * ((2477.0 / 1440.0) * x - 247.0 / 48.0) + 5083.0 / 1008.0) - 667.0 / 420.0) + 7.0 / 480.0) - 1.0 / 560.0) - 1.0 / 280.0;
                            poly_val[offset + 9] = Math.Pow(x, 3) * (x * (x * (-55.0 / 288.0 * x + 4.0 / 7.0) - 377.0 / 672.0) + 223.0 / 1260.0);
                            break;
                        case 2:
                            poly_val[offset + 0] = x * (x * (x * (x * ((55.0 / 48.0) * x - 965.0 / 336.0) + 191.0 / 84.0) - 31.0 / 60.0) - 7.0 / 240.0) - 1.0 / 560.0;
                            poly_val[offset + 1] = x * (x * (x * (x * (-2477.0 / 240.0 * x + 207.0 / 8.0) - 10313.0 / 504.0) + 481.0 / 105.0) + 3.0 / 10.0) + 8.0 / 315.0;
                            poly_val[offset + 2] = x * (x * (x * (x * ((4957.0 / 120.0) * x - 2174.0 / 21.0) + 10277.0 / 126.0) - 124.0 / 7.0) - 169.0 / 120.0) - 1.0 / 5.0;
                            poly_val[offset + 3] = x * (x * (x * (x * (-3857.0 / 40.0 * x + 5795.0 / 24.0) - 2273.0 / 12.0) + 811.0 / 20.0) + 61.0 / 30.0) + 8.0 / 5.0;
                            poly_val[offset + 4] = Math.Pow(x, 2) * (x * (x * ((434.0 / 3.0) * x - 2895.0 / 8.0) + 2546.0 / 9.0) - 1837.0 / 30.0) - 205.0 / 72.0;
                            poly_val[offset + 5] = x * (x * (x * (x * (-434.0 / 3.0 * x + 8675.0 / 24.0) - 5077.0 / 18.0) + 1257.0 / 20.0) - 61.0 / 30.0) + 8.0 / 5.0;
                            poly_val[offset + 6] = x * (x * (x * (x * ((3857.0 / 40.0) * x - 722.0 / 3.0) + 1127.0 / 6.0) - 216.0 / 5.0) + 169.0 / 120.0) - 1.0 / 5.0;
                            poly_val[offset + 7] = x * (x * (x * (x * (-4957.0 / 120.0 * x + 5769.0 / 56.0) - 20299.0 / 252.0) + 227.0 / 12.0) - 3.0 / 10.0) + 8.0 / 315.0;
                            poly_val[offset + 8] = x * (x * (x * (x * ((2477.0 / 240.0) * x - 1235.0 / 48.0) + 5083.0 / 252.0) - 667.0 / 140.0) + 7.0 / 240.0) - 1.0 / 560.0;
                            poly_val[offset + 9] = Math.Pow(x, 2) * (x * (x * (-55.0 / 48.0 * x + 20.0 / 7.0) - 377.0 / 168.0) + 223.0 / 420.0);
                            break;
                        default:
                            throw new Exception("Invalid derivative option!");
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.M3Q12:
                    switch (deriv)
                    {
                        case 0:
                            poly_val[offset + 0] = x * (x * (x * (x * (x * (x * (-5599.0 / 907200.0 * x + 983.0 / 45360.0) - 779.0 / 30240.0) + 443.0 / 45360.0) + 41.0 / 36288.0) + 1.0 / 6300.0) - 1.0 / 1260.0);
                            poly_val[offset + 1] = x * (x * (x * (x * (x * (x * ((61639.0 / 907200.0) * x - 216533.0 / 907200.0) + 28591.0 / 100800.0) - 2759.0 / 25920.0) - 1261.0 / 90720.0) - 5.0 / 2016.0) + 5.0 / 504.0);
                            poly_val[offset + 2] = x * (x * (x * (x * (x * (x * (-12343.0 / 36288.0 * x + 1291.0 / 1080.0) - 5363.0 / 3780.0) + 23711.0 / 45360.0) + 541.0 / 6720.0) + 5.0 / 252.0) - 5.0 / 84.0);
                            poly_val[offset + 3] = x * (x * (x * (x * (x * (x * ((61793.0 / 60480.0) * x - 217103.0 / 60480.0) + 85517.0 / 20160.0) - 2599.0 / 1728.0) - 4369.0 / 15120.0) - 5.0 / 42.0) + 5.0 / 21.0);
                            poly_val[offset + 4] = x * (x * (x * (x * (x * (x * (-12371.0 / 6048.0 * x + 108541.0 / 15120.0) - 7081.0 / 840.0) + 44003.0 / 15120.0) + 1669.0 / 4320.0) + 5.0 / 6.0) - 5.0 / 6.0);
                            poly_val[offset + 5] = Math.Pow(x, 2) * (Math.Pow(x, 2) * (x * (x * ((61889.0 / 21600.0) * x - 2891.0 / 288.0) + 16877.0 / 1440.0) - 17641.0 / 4320.0) - 5269.0 / 3600.0) + 1;
                            poly_val[offset + 6] = x * (x * (x * (x * (x * (x * (-61889.0 / 21600.0 * x + 108199.0 / 10800.0) - 13993.0 / 1200.0) + 9131.0 / 2160.0) - 1669.0 / 4320.0) + 5.0 / 6.0) + 5.0 / 6.0);
                            poly_val[offset + 7] = x * (x * (x * (x * (x * (x * ((12371.0 / 6048.0) * x - 215903.0 / 30240.0) + 27931.0 / 3360.0) - 95269.0 / 30240.0) + 4369.0 / 15120.0) - 5.0 / 42.0) - 5.0 / 21.0);
                            poly_val[offset + 8] = x * (x * (x * (x * (x * (x * (-61793.0 / 60480.0 * x + 8977.0 / 2520.0) - 4659.0 / 1120.0) + 175.0 / 108.0) - 541.0 / 6720.0) + 5.0 / 252.0) + 5.0 / 84.0);
                            poly_val[offset + 9] = x * (x * (x * (x * (x * (x * ((12343.0 / 36288.0) * x - 30731.0 / 25920.0) + 84037.0 / 60480.0) - 98981.0 / 181440.0) + 1261.0 / 90720.0) - 5.0 / 2016.0) - 5.0 / 504.0);
                            poly_val[offset + 10] = x * (x * (x * (x * (x * (x * (-61639.0 / 907200.0 * x + 10747.0 / 45360.0) - 1403.0 / 5040.0) + 71.0 / 648.0) - 41.0 / 36288.0) + 1.0 / 6300.0) + 1.0 / 1260.0);
                            poly_val[offset + 11] = Math.Pow(x, 4) * (x * (x * ((5599.0 / 907200.0) * x - 6511.0 / 302400.0) + 7663.0 / 302400.0) - 1811.0 / 181440.0);
                            break;
                        case 1:
                            poly_val[offset + 0] = x * (x * (x * (x * (x * (-5599.0 / 129600.0 * x + 983.0 / 7560.0) - 779.0 / 6048.0) + 443.0 / 11340.0) + 41.0 / 12096.0) + 1.0 / 3150.0) - 1.0 / 1260.0;
                            poly_val[offset + 1] = x * (x * (x * (x * (x * ((61639.0 / 129600.0) * x - 216533.0 / 151200.0) + 28591.0 / 20160.0) - 2759.0 / 6480.0) - 1261.0 / 30240.0) - 5.0 / 1008.0) + 5.0 / 504.0;
                            poly_val[offset + 2] = x * (x * (x * (x * (x * (-12343.0 / 5184.0 * x + 1291.0 / 180.0) - 5363.0 / 756.0) + 23711.0 / 11340.0) + 541.0 / 2240.0) + 5.0 / 126.0) - 5.0 / 84.0;
                            poly_val[offset + 3] = x * (x * (x * (x * (x * ((61793.0 / 8640.0) * x - 217103.0 / 10080.0) + 85517.0 / 4032.0) - 2599.0 / 432.0) - 4369.0 / 5040.0) - 5.0 / 21.0) + 5.0 / 21.0;
                            poly_val[offset + 4] = x * (x * (x * (x * (x * (-12371.0 / 864.0 * x + 108541.0 / 2520.0) - 7081.0 / 168.0) + 44003.0 / 3780.0) + 1669.0 / 1440.0) + 5.0 / 3.0) - 5.0 / 6.0;
                            poly_val[offset + 5] = x * (Math.Pow(x, 2) * (x * (x * ((433223.0 / 21600.0) * x - 2891.0 / 48.0) + 16877.0 / 288.0) - 17641.0 / 1080.0) - 5269.0 / 1800.0);
                            poly_val[offset + 6] = x * (x * (x * (x * (x * (-433223.0 / 21600.0 * x + 108199.0 / 1800.0) - 13993.0 / 240.0) + 9131.0 / 540.0) - 1669.0 / 1440.0) + 5.0 / 3.0) + 5.0 / 6.0;
                            poly_val[offset + 7] = x * (x * (x * (x * (x * ((12371.0 / 864.0) * x - 215903.0 / 5040.0) + 27931.0 / 672.0) - 95269.0 / 7560.0) + 4369.0 / 5040.0) - 5.0 / 21.0) - 5.0 / 21.0;
                            poly_val[offset + 8] = x * (x * (x * (x * (x * (-61793.0 / 8640.0 * x + 8977.0 / 420.0) - 4659.0 / 224.0) + 175.0 / 27.0) - 541.0 / 2240.0) + 5.0 / 126.0) + 5.0 / 84.0;
                            poly_val[offset + 9] = x * (x * (x * (x * (x * ((12343.0 / 5184.0) * x - 30731.0 / 4320.0) + 84037.0 / 12096.0) - 98981.0 / 45360.0) + 1261.0 / 30240.0) - 5.0 / 1008.0) - 5.0 / 504.0;
                            poly_val[offset + 10] = x * (x * (x * (x * (x * (-61639.0 / 129600.0 * x + 10747.0 / 7560.0) - 1403.0 / 1008.0) + 71.0 / 162.0) - 41.0 / 12096.0) + 1.0 / 3150.0) + 1.0 / 1260.0;
                            poly_val[offset + 11] = Math.Pow(x, 3) * (x * (x * ((5599.0 / 129600.0) * x - 6511.0 / 50400.0) + 7663.0 / 60480.0) - 1811.0 / 45360.0);
                            break;
                        case 2:
                            poly_val[offset + 0] = x * (x * (x * (x * (-5599.0 / 21600.0 * x + 983.0 / 1512.0) - 779.0 / 1512.0) + 443.0 / 3780.0) + 41.0 / 6048.0) + 1.0 / 3150.0;
                            poly_val[offset + 1] = x * (x * (x * (x * ((61639.0 / 21600.0) * x - 216533.0 / 30240.0) + 28591.0 / 5040.0) - 2759.0 / 2160.0) - 1261.0 / 15120.0) - 5.0 / 1008.0;
                            poly_val[offset + 2] = x * (x * (x * (x * (-12343.0 / 864.0 * x + 1291.0 / 36.0) - 5363.0 / 189.0) + 23711.0 / 3780.0) + 541.0 / 1120.0) + 5.0 / 126.0;
                            poly_val[offset + 3] = x * (x * (x * (x * ((61793.0 / 1440.0) * x - 217103.0 / 2016.0) + 85517.0 / 1008.0) - 2599.0 / 144.0) - 4369.0 / 2520.0) - 5.0 / 21.0;
                            poly_val[offset + 4] = x * (x * (x * (x * (-12371.0 / 144.0 * x + 108541.0 / 504.0) - 7081.0 / 42.0) + 44003.0 / 1260.0) + 1669.0 / 720.0) + 5.0 / 3.0;
                            poly_val[offset + 5] = Math.Pow(x, 2) * (x * (x * ((433223.0 / 3600.0) * x - 14455.0 / 48.0) + 16877.0 / 72.0) - 17641.0 / 360.0) - 5269.0 / 1800.0;
                            poly_val[offset + 6] = x * (x * (x * (x * (-433223.0 / 3600.0 * x + 108199.0 / 360.0) - 13993.0 / 60.0) + 9131.0 / 180.0) - 1669.0 / 720.0) + 5.0 / 3.0;
                            poly_val[offset + 7] = x * (x * (x * (x * ((12371.0 / 144.0) * x - 215903.0 / 1008.0) + 27931.0 / 168.0) - 95269.0 / 2520.0) + 4369.0 / 2520.0) - 5.0 / 21.0;
                            poly_val[offset + 8] = x * (x * (x * (x * (-61793.0 / 1440.0 * x + 8977.0 / 84.0) - 4659.0 / 56.0) + 175.0 / 9.0) - 541.0 / 1120.0) + 5.0 / 126.0;
                            poly_val[offset + 9] = x * (x * (x * (x * ((12343.0 / 864.0) * x - 30731.0 / 864.0) + 84037.0 / 3024.0) - 98981.0 / 15120.0) + 1261.0 / 15120.0) - 5.0 / 1008.0;
                            poly_val[offset + 10] = x * (x * (x * (x * (-61639.0 / 21600.0 * x + 10747.0 / 1512.0) - 1403.0 / 252.0) + 71.0 / 54.0) - 41.0 / 6048.0) + 1.0 / 3150.0;
                            poly_val[offset + 11] = Math.Pow(x, 2) * (x * (x * ((5599.0 / 21600.0) * x - 6511.0 / 10080.0) + 7663.0 / 15120.0) - 1811.0 / 15120.0);
                            break;
                        default:
                            throw new Exception("Invalid derivative option!");
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.M3Q14:
                    switch (deriv)
                    {
                        case 0:
                            poly_val[offset + 0] = x * (x * (x * (x * (x * (x * ((28331.0 / 19958400.0) * x - 24881.0 / 4989600.0) + 6577.0 / 1108800.0) - 1021.0 / 453600.0) - 479.0 / 1814400.0) - 1.0 / 33264.0) + 1.0 / 5544.0);
                            poly_val[offset + 1] = x * (x * (x * (x * (x * (x * (-52651.0 / 2851200.0 * x + 431789.0 / 6652800.0) - 171223.0 / 2217600.0) + 580429.0 / 19958400.0) + 19.0 / 5040.0) + 1.0 / 1925.0) - 1.0 / 385.0);
                            poly_val[offset + 2] = x * (x * (x * (x * (x * (x * ((15373.0 / 138600.0) * x - 216257.0 / 554400.0) + 1299.0 / 2800.0) - 23929.0 / 138600.0) - 643.0 / 25200.0) - 1.0 / 224.0) + 1.0 / 56.0);
                            poly_val[offset + 3] = x * (x * (x * (x * (x * (x * (-11549.0 / 28350.0 * x + 92891.0 / 64800.0) - 64361.0 / 37800.0) + 281189.0 / 453600.0) + 4969.0 / 45360.0) + 5.0 / 189.0) - 5.0 / 63.0);
                            poly_val[offset + 4] = x * (x * (x * (x * (x * (x * ((370289.0 / 362880.0) * x - 4021.0 / 1120.0) + 36659.0 / 8640.0) - 26755.0 / 18144.0) - 4469.0 / 13440.0) - 15.0 / 112.0) + 15.0 / 56.0);
                            poly_val[offset + 5] = x * (x * (x * (x * (x * (x * (-17663.0 / 9600.0 * x + 434419.0 / 67200.0) - 169737.0 / 22400.0) + 170129.0 / 67200.0) + 1769.0 / 4200.0) + 6.0 / 7.0) - 6.0 / 7.0);
                            poly_val[offset + 6] = Math.Pow(x, 2) * (Math.Pow(x, 2) * (x * (x * ((30941.0 / 12600.0) * x - 72299.0 / 8400.0) + 18013.0 / 1800.0) - 757.0 / 225.0) - 5369.0 / 3600.0) + 1;
                            poly_val[offset + 7] = x * (x * (x * (x * (x * (x * (-30941.0 / 12600.0 * x + 216277.0 / 25200.0) - 125161.0 / 12600.0) + 88541.0 / 25200.0) - 1769.0 / 4200.0) + 6.0 / 7.0) + 6.0 / 7.0);
                            poly_val[offset + 8] = x * (x * (x * (x * (x * (x * ((17663.0 / 9600.0) * x - 107767.0 / 16800.0) + 83193.0 / 11200.0) - 46769.0 / 16800.0) + 4469.0 / 13440.0) - 15.0 / 112.0) - 15.0 / 56.0);
                            poly_val[offset + 9] = x * (x * (x * (x * (x * (x * (-370289.0 / 362880.0 * x + 1289219.0 / 362880.0) - 166547.0 / 40320.0) + 116269.0 / 72576.0) - 4969.0 / 45360.0) + 5.0 / 189.0) + 5.0 / 63.0);
                            poly_val[offset + 10] = x * (x * (x * (x * (x * (x * ((11549.0 / 28350.0) * x - 30631.0 / 21600.0) + 41743.0 / 25200.0) - 73589.0 / 113400.0) + 643.0 / 25200.0) - 1.0 / 224.0) - 1.0 / 56.0);
                            poly_val[offset + 11] = x * (x * (x * (x * (x * (x * (-15373.0 / 138600.0 * x + 214187.0 / 554400.0) - 249.0 / 550.0) + 8969.0 / 50400.0) - 19.0 / 5040.0) + 1.0 / 1925.0) + 1.0 / 385.0);
                            poly_val[offset + 12] = x * (x * (x * (x * (x * (x * ((52651.0 / 2851200.0) * x - 321133.0 / 4989600.0) + 251417.0 / 3326400.0) - 148399.0 / 4989600.0) + 479.0 / 1814400.0) - 1.0 / 33264.0) - 1.0 / 5544.0);
                            poly_val[offset + 13] = Math.Pow(x, 4) * (x * (x * (-28331.0 / 19958400.0 * x + 3659.0 / 739200.0) - 503.0 / 86400.0) + 6533.0 / 2851200.0);
                            break;
                        case 1:
                            poly_val[offset + 0] = x * (x * (x * (x * (x * ((28331.0 / 2851200.0) * x - 24881.0 / 831600.0) + 6577.0 / 221760.0) - 1021.0 / 113400.0) - 479.0 / 604800.0) - 1.0 / 16632.0) + 1.0 / 5544.0;
                            poly_val[offset + 1] = x * (x * (x * (x * (x * (-368557.0 / 2851200.0 * x + 431789.0 / 1108800.0) - 171223.0 / 443520.0) + 580429.0 / 4989600.0) + 19.0 / 1680.0) + 2.0 / 1925.0) - 1.0 / 385.0;
                            poly_val[offset + 2] = x * (x * (x * (x * (x * ((15373.0 / 19800.0) * x - 216257.0 / 92400.0) + 1299.0 / 560.0) - 23929.0 / 34650.0) - 643.0 / 8400.0) - 1.0 / 112.0) + 1.0 / 56.0;
                            poly_val[offset + 3] = x * (x * (x * (x * (x * (-11549.0 / 4050.0 * x + 92891.0 / 10800.0) - 64361.0 / 7560.0) + 281189.0 / 113400.0) + 4969.0 / 15120.0) + 10.0 / 189.0) - 5.0 / 63.0;
                            poly_val[offset + 4] = x * (x * (x * (x * (x * ((370289.0 / 51840.0) * x - 12063.0 / 560.0) + 36659.0 / 1728.0) - 26755.0 / 4536.0) - 4469.0 / 4480.0) - 15.0 / 56.0) + 15.0 / 56.0;
                            poly_val[offset + 5] = x * (x * (x * (x * (x * (-123641.0 / 9600.0 * x + 434419.0 / 11200.0) - 169737.0 / 4480.0) + 170129.0 / 16800.0) + 1769.0 / 1400.0) + 12.0 / 7.0) - 6.0 / 7.0;
                            poly_val[offset + 6] = x * (Math.Pow(x, 2) * (x * (x * ((30941.0 / 1800.0) * x - 72299.0 / 1400.0) + 18013.0 / 360.0) - 3028.0 / 225.0) - 5369.0 / 1800.0);
                            poly_val[offset + 7] = x * (x * (x * (x * (x * (-30941.0 / 1800.0 * x + 216277.0 / 4200.0) - 125161.0 / 2520.0) + 88541.0 / 6300.0) - 1769.0 / 1400.0) + 12.0 / 7.0) + 6.0 / 7.0;
                            poly_val[offset + 8] = x * (x * (x * (x * (x * ((123641.0 / 9600.0) * x - 107767.0 / 2800.0) + 83193.0 / 2240.0) - 46769.0 / 4200.0) + 4469.0 / 4480.0) - 15.0 / 56.0) - 15.0 / 56.0;
                            poly_val[offset + 9] = x * (x * (x * (x * (x * (-370289.0 / 51840.0 * x + 1289219.0 / 60480.0) - 166547.0 / 8064.0) + 116269.0 / 18144.0) - 4969.0 / 15120.0) + 10.0 / 189.0) + 5.0 / 63.0;
                            poly_val[offset + 10] = x * (x * (x * (x * (x * ((11549.0 / 4050.0) * x - 30631.0 / 3600.0) + 41743.0 / 5040.0) - 73589.0 / 28350.0) + 643.0 / 8400.0) - 1.0 / 112.0) - 1.0 / 56.0;
                            poly_val[offset + 11] = x * (x * (x * (x * (x * (-15373.0 / 19800.0 * x + 214187.0 / 92400.0) - 249.0 / 110.0) + 8969.0 / 12600.0) - 19.0 / 1680.0) + 2.0 / 1925.0) + 1.0 / 385.0;
                            poly_val[offset + 12] = x * (x * (x * (x * (x * ((368557.0 / 2851200.0) * x - 321133.0 / 831600.0) + 251417.0 / 665280.0) - 148399.0 / 1247400.0) + 479.0 / 604800.0) - 1.0 / 16632.0) - 1.0 / 5544.0;
                            poly_val[offset + 13] = Math.Pow(x, 3) * (x * (x * (-28331.0 / 2851200.0 * x + 3659.0 / 123200.0) - 503.0 / 17280.0) + 6533.0 / 712800.0);
                            break;
                        case 2:
                            poly_val[offset + 0] = x * (x * (x * (x * ((28331.0 / 475200.0) * x - 24881.0 / 166320.0) + 6577.0 / 55440.0) - 1021.0 / 37800.0) - 479.0 / 302400.0) - 1.0 / 16632.0;
                            poly_val[offset + 1] = x * (x * (x * (x * (-368557.0 / 475200.0 * x + 431789.0 / 221760.0) - 171223.0 / 110880.0) + 580429.0 / 1663200.0) + 19.0 / 840.0) + 2.0 / 1925.0;
                            poly_val[offset + 2] = x * (x * (x * (x * ((15373.0 / 3300.0) * x - 216257.0 / 18480.0) + 1299.0 / 140.0) - 23929.0 / 11550.0) - 643.0 / 4200.0) - 1.0 / 112.0;
                            poly_val[offset + 3] = x * (x * (x * (x * (-11549.0 / 675.0 * x + 92891.0 / 2160.0) - 64361.0 / 1890.0) + 281189.0 / 37800.0) + 4969.0 / 7560.0) + 10.0 / 189.0;
                            poly_val[offset + 4] = x * (x * (x * (x * ((370289.0 / 8640.0) * x - 12063.0 / 112.0) + 36659.0 / 432.0) - 26755.0 / 1512.0) - 4469.0 / 2240.0) - 15.0 / 56.0;
                            poly_val[offset + 5] = x * (x * (x * (x * (-123641.0 / 1600.0 * x + 434419.0 / 2240.0) - 169737.0 / 1120.0) + 170129.0 / 5600.0) + 1769.0 / 700.0) + 12.0 / 7.0;
                            poly_val[offset + 6] = Math.Pow(x, 2) * (x * (x * ((30941.0 / 300.0) * x - 72299.0 / 280.0) + 18013.0 / 90.0) - 3028.0 / 75.0) - 5369.0 / 1800.0;
                            poly_val[offset + 7] = x * (x * (x * (x * (-30941.0 / 300.0 * x + 216277.0 / 840.0) - 125161.0 / 630.0) + 88541.0 / 2100.0) - 1769.0 / 700.0) + 12.0 / 7.0;
                            poly_val[offset + 8] = x * (x * (x * (x * ((123641.0 / 1600.0) * x - 107767.0 / 560.0) + 83193.0 / 560.0) - 46769.0 / 1400.0) + 4469.0 / 2240.0) - 15.0 / 56.0;
                            poly_val[offset + 9] = x * (x * (x * (x * (-370289.0 / 8640.0 * x + 1289219.0 / 12096.0) - 166547.0 / 2016.0) + 116269.0 / 6048.0) - 4969.0 / 7560.0) + 10.0 / 189.0;
                            poly_val[offset + 10] = x * (x * (x * (x * ((11549.0 / 675.0) * x - 30631.0 / 720.0) + 41743.0 / 1260.0) - 73589.0 / 9450.0) + 643.0 / 4200.0) - 1.0 / 112.0;
                            poly_val[offset + 11] = x * (x * (x * (x * (-15373.0 / 3300.0 * x + 214187.0 / 18480.0) - 498.0 / 55.0) + 8969.0 / 4200.0) - 19.0 / 840.0) + 2.0 / 1925.0;
                            poly_val[offset + 12] = x * (x * (x * (x * ((368557.0 / 475200.0) * x - 321133.0 / 166320.0) + 251417.0 / 166320.0) - 148399.0 / 415800.0) + 479.0 / 302400.0) - 1.0 / 16632.0;
                            poly_val[offset + 13] = Math.Pow(x, 2) * (x * (x * (-28331.0 / 475200.0 * x + 3659.0 / 24640.0) - 503.0 / 4320.0) + 6533.0 / 237600.0);
                            break;
                        default:
                            throw new Exception("Invalid derivative option!");
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.M4Q4:
                    throw new Exception("Invalid spatial interpolation option!");
                case TurbulenceOptions.SpatialInterpolation.M4Q6:
                    switch (deriv)
                    {
                        case 0:
                            poly_val[offset + 0] = x * (x * (x * (x * (x * (x * (x * (x * (-23.0 / 12.0 * x + 69.0 / 8.0) - 59.0 / 4.0) + 91.0 / 8.0) - 10.0 / 3.0) + 1.0 / 24.0) - 1.0 / 12.0) - 1.0 / 24.0) + 1.0 / 12.0);
                            poly_val[offset + 1] = x * (x * (x * (x * (x * (x * (x * (x * ((115.0 / 12.0) * x - 345.0 / 8.0) + 295.0 / 4.0) - 455.0 / 8.0) + 50.0 / 3.0) - 1.0 / 6.0) + 1.0 / 6.0) + 2.0 / 3.0) - 2.0 / 3.0);
                            poly_val[offset + 2] = Math.Pow(x, 2) * (Math.Pow(x, 2) * (x * (x * (x * (x * (-115.0 / 6.0 * x + 345.0 / 4.0) - 295.0 / 2.0) + 455.0 / 4.0) - 100.0 / 3.0) + 1.0 / 4.0) - 5.0 / 4.0) + 1;
                            poly_val[offset + 3] = x * (x * (x * (x * (x * (x * (x * (x * ((115.0 / 6.0) * x - 345.0 / 4.0) + 295.0 / 2.0) - 455.0 / 4.0) + 100.0 / 3.0) - 1.0 / 6.0) - 1.0 / 6.0) + 2.0 / 3.0) + 2.0 / 3.0);
                            poly_val[offset + 4] = x * (x * (x * (x * (x * (x * (x * (x * (-115.0 / 12.0 * x + 345.0 / 8.0) - 295.0 / 4.0) + 455.0 / 8.0) - 50.0 / 3.0) + 1.0 / 24.0) + 1.0 / 12.0) - 1.0 / 24.0) - 1.0 / 12.0);
                            poly_val[offset + 5] = Math.Pow(x, 5) * (x * (x * (x * ((23.0 / 12.0) * x - 69.0 / 8.0) + 59.0 / 4.0) - 91.0 / 8.0) + 10.0 / 3.0);
                            break;
                        case 1:
                            poly_val[offset + 0] = x * (x * (x * (x * (x * (x * (x * (-69.0 / 4.0 * x + 69) - 413.0 / 4.0) + 273.0 / 4.0) - 50.0 / 3.0) + 1.0 / 6.0) - 1.0 / 4.0) - 1.0 / 12.0) + 1.0 / 12.0;
                            poly_val[offset + 1] = x * (x * (x * (x * (x * (x * (x * ((345.0 / 4.0) * x - 345) + 2065.0 / 4.0) - 1365.0 / 4.0) + 250.0 / 3.0) - 2.0 / 3.0) + 1.0 / 2.0) + 4.0 / 3.0) - 2.0 / 3.0;
                            poly_val[offset + 2] = x * (Math.Pow(x, 2) * (x * (x * (x * (x * (-345.0 / 2.0 * x + 690) - 2065.0 / 2.0) + 1365.0 / 2.0) - 500.0 / 3.0) + 1) - 5.0 / 2.0);
                            poly_val[offset + 3] = x * (x * (x * (x * (x * (x * (x * ((345.0 / 2.0) * x - 690) + 2065.0 / 2.0) - 1365.0 / 2.0) + 500.0 / 3.0) - 2.0 / 3.0) - 1.0 / 2.0) + 4.0 / 3.0) + 2.0 / 3.0;
                            poly_val[offset + 4] = x * (x * (x * (x * (x * (x * (x * (-345.0 / 4.0 * x + 345) - 2065.0 / 4.0) + 1365.0 / 4.0) - 250.0 / 3.0) + 1.0 / 6.0) + 1.0 / 4.0) - 1.0 / 12.0) - 1.0 / 12.0;
                            poly_val[offset + 5] = Math.Pow(x, 4) * (x * (x * (x * ((69.0 / 4.0) * x - 69) + 413.0 / 4.0) - 273.0 / 4.0) + 50.0 / 3.0);
                            break;
                        case 2:
                            poly_val[offset + 0] = x * (x * (x * (x * (x * (x * (-138 * x + 483) - 1239.0 / 2.0) + 1365.0 / 4.0) - 200.0 / 3.0) + 1.0 / 2.0) - 1.0 / 2.0) - 1.0 / 12.0;
                            poly_val[offset + 1] = x * (x * (x * (x * (x * (x * (690 * x - 2415) + 6195.0 / 2.0) - 6825.0 / 4.0) + 1000.0 / 3.0) - 2) + 1) + 4.0 / 3.0;
                            poly_val[offset + 2] = Math.Pow(x, 2) * (x * (x * (x * (x * (-1380 * x + 4830) - 6195) + 6825.0 / 2.0) - 2000.0 / 3.0) + 3) - 5.0 / 2.0;
                            poly_val[offset + 3] = x * (x * (x * (x * (x * (x * (1380 * x - 4830) + 6195) - 6825.0 / 2.0) + 2000.0 / 3.0) - 2) - 1) + 4.0 / 3.0;
                            poly_val[offset + 4] = x * (x * (x * (x * (x * (x * (-690 * x + 2415) - 6195.0 / 2.0) + 6825.0 / 4.0) - 1000.0 / 3.0) + 1.0 / 2.0) + 1.0 / 2.0) - 1.0 / 12.0;
                            poly_val[offset + 5] = Math.Pow(x, 3) * (x * (x * (x * (138 * x - 483) + 1239.0 / 2.0) - 1365.0 / 4.0) + 200.0 / 3.0);
                            break;
                        default:
                            throw new Exception("Invalid derivative option!");
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.M4Q8:
                    switch (deriv)
                    {
                        case 0:
                            poly_val[offset + 0] = x * (x * (x * (x * (x * (x * (x * (x * ((29.0 / 72.0) * x - 29.0 / 16.0) + 2231.0 / 720.0) - 859.0 / 360.0) + 25.0 / 36.0) - 1.0 / 144.0) + 1.0 / 48.0) + 1.0 / 180.0) - 1.0 / 60.0);
                            poly_val[offset + 1] = x * (x * (x * (x * (x * (x * (x * (x * (-203.0 / 72.0 * x + 203.0 / 16.0) - 15617.0 / 720.0) + 4009.0 / 240.0) - 3509.0 / 720.0) + 1.0 / 12.0) - 1.0 / 6.0) - 3.0 / 40.0) + 3.0 / 20.0);
                            poly_val[offset + 2] = x * (x * (x * (x * (x * (x * (x * (x * ((203.0 / 24.0) * x - 609.0 / 16.0) + 15617.0 / 240.0) - 3007.0 / 60.0) + 293.0 / 20.0) - 13.0 / 48.0) + 13.0 / 48.0) + 3.0 / 4.0) - 3.0 / 4.0);
                            poly_val[offset + 3] = Math.Pow(x, 2) * (Math.Pow(x, 2) * (x * (x * (x * (x * (-1015.0 / 72.0 * x + 1015.0 / 16.0) - 15617.0 / 144.0) + 12029.0 / 144.0) - 3521.0 / 144.0) + 7.0 / 18.0) - 49.0 / 36.0) + 1;
                            poly_val[offset + 4] = x * (x * (x * (x * (x * (x * (x * (x * ((1015.0 / 72.0) * x - 1015.0 / 16.0) + 15617.0 / 144.0) - 2005.0 / 24.0) + 881.0 / 36.0) - 13.0 / 48.0) - 13.0 / 48.0) + 3.0 / 4.0) + 3.0 / 4.0);
                            poly_val[offset + 5] = x * (x * (x * (x * (x * (x * (x * (x * (-203.0 / 24.0 * x + 609.0 / 16.0) - 15617.0 / 240.0) + 12031.0 / 240.0) - 235.0 / 16.0) + 1.0 / 12.0) + 1.0 / 6.0) - 3.0 / 40.0) - 3.0 / 20.0);
                            poly_val[offset + 6] = x * (x * (x * (x * (x * (x * (x * (x * ((203.0 / 72.0) * x - 203.0 / 16.0) + 15617.0 / 720.0) - 752.0 / 45.0) + 881.0 / 180.0) - 1.0 / 144.0) - 1.0 / 48.0) + 1.0 / 180.0) + 1.0 / 60.0);
                            poly_val[offset + 7] = Math.Pow(x, 5) * (x * (x * (x * (-29.0 / 72.0 * x + 29.0 / 16.0) - 2231.0 / 720.0) + 191.0 / 80.0) - 503.0 / 720.0);
                            break;
                        case 1:
                            poly_val[offset + 0] = x * (x * (x * (x * (x * (x * (x * ((29.0 / 8.0) * x - 29.0 / 2.0) + 15617.0 / 720.0) - 859.0 / 60.0) + 125.0 / 36.0) - 1.0 / 36.0) + 1.0 / 16.0) + 1.0 / 90.0) - 1.0 / 60.0;
                            poly_val[offset + 1] = x * (x * (x * (x * (x * (x * (x * (-203.0 / 8.0 * x + 203.0 / 2.0) - 109319.0 / 720.0) + 4009.0 / 40.0) - 3509.0 / 144.0) + 1.0 / 3.0) - 1.0 / 2.0) - 3.0 / 20.0) + 3.0 / 20.0;
                            poly_val[offset + 2] = x * (x * (x * (x * (x * (x * (x * ((609.0 / 8.0) * x - 609.0 / 2.0) + 109319.0 / 240.0) - 3007.0 / 10.0) + 293.0 / 4.0) - 13.0 / 12.0) + 13.0 / 16.0) + 3.0 / 2.0) - 3.0 / 4.0;
                            poly_val[offset + 3] = x * (Math.Pow(x, 2) * (x * (x * (x * (x * (-1015.0 / 8.0 * x + 1015.0 / 2.0) - 109319.0 / 144.0) + 12029.0 / 24.0) - 17605.0 / 144.0) + 14.0 / 9.0) - 49.0 / 18.0);
                            poly_val[offset + 4] = x * (x * (x * (x * (x * (x * (x * ((1015.0 / 8.0) * x - 1015.0 / 2.0) + 109319.0 / 144.0) - 2005.0 / 4.0) + 4405.0 / 36.0) - 13.0 / 12.0) - 13.0 / 16.0) + 3.0 / 2.0) + 3.0 / 4.0;
                            poly_val[offset + 5] = x * (x * (x * (x * (x * (x * (x * (-609.0 / 8.0 * x + 609.0 / 2.0) - 109319.0 / 240.0) + 12031.0 / 40.0) - 1175.0 / 16.0) + 1.0 / 3.0) + 1.0 / 2.0) - 3.0 / 20.0) - 3.0 / 20.0;
                            poly_val[offset + 6] = x * (x * (x * (x * (x * (x * (x * ((203.0 / 8.0) * x - 203.0 / 2.0) + 109319.0 / 720.0) - 1504.0 / 15.0) + 881.0 / 36.0) - 1.0 / 36.0) - 1.0 / 16.0) + 1.0 / 90.0) + 1.0 / 60.0;
                            poly_val[offset + 7] = Math.Pow(x, 4) * (x * (x * (x * (-29.0 / 8.0 * x + 29.0 / 2.0) - 15617.0 / 720.0) + 573.0 / 40.0) - 503.0 / 144.0);
                            break;
                        case 2:
                            poly_val[offset + 0] = x * (x * (x * (x * (x * (x * (29 * x - 203.0 / 2.0) + 15617.0 / 120.0) - 859.0 / 12.0) + 125.0 / 9.0) - 1.0 / 12.0) + 1.0 / 8.0) + 1.0 / 90.0;
                            poly_val[offset + 1] = x * (x * (x * (x * (x * (x * (-203 * x + 1421.0 / 2.0) - 109319.0 / 120.0) + 4009.0 / 8.0) - 3509.0 / 36.0) + 1) - 1) - 3.0 / 20.0;
                            poly_val[offset + 2] = x * (x * (x * (x * (x * (x * (609 * x - 4263.0 / 2.0) + 109319.0 / 40.0) - 3007.0 / 2.0) + 293) - 13.0 / 4.0) + 13.0 / 8.0) + 3.0 / 2.0;
                            poly_val[offset + 3] = Math.Pow(x, 2) * (x * (x * (x * (x * (-1015 * x + 7105.0 / 2.0) - 109319.0 / 24.0) + 60145.0 / 24.0) - 17605.0 / 36.0) + 14.0 / 3.0) - 49.0 / 18.0;
                            poly_val[offset + 4] = x * (x * (x * (x * (x * (x * (1015 * x - 7105.0 / 2.0) + 109319.0 / 24.0) - 10025.0 / 4.0) + 4405.0 / 9.0) - 13.0 / 4.0) - 13.0 / 8.0) + 3.0 / 2.0;
                            poly_val[offset + 5] = x * (x * (x * (x * (x * (x * (-609 * x + 4263.0 / 2.0) - 109319.0 / 40.0) + 12031.0 / 8.0) - 1175.0 / 4.0) + 1) + 1) - 3.0 / 20.0;
                            poly_val[offset + 6] = x * (x * (x * (x * (x * (x * (203 * x - 1421.0 / 2.0) + 109319.0 / 120.0) - 1504.0 / 3.0) + 881.0 / 9.0) - 1.0 / 12.0) - 1.0 / 8.0) + 1.0 / 90.0;
                            poly_val[offset + 7] = Math.Pow(x, 3) * (x * (x * (x * (-29 * x + 203.0 / 2.0) - 15617.0 / 120.0) + 573.0 / 8.0) - 503.0 / 36.0);
                            break;
                        default:
                            throw new Exception("Invalid derivative option!");
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.M4Q10:
                    switch (deriv)
                    {
                        case 0:
                            poly_val[offset + 0] = x * (x * (x * (x * (x * (x * (x * (x * (-3569.0 / 40320.0 * x + 16061.0 / 40320.0) - 1961.0 / 2880.0) + 503.0 / 960.0) - 175.0 / 1152.0) + 7.0 / 5760.0) - 7.0 / 1440.0) - 1.0 / 1120.0) + 1.0 / 280.0);
                            poly_val[offset + 1] = x * (x * (x * (x * (x * (x * (x * (x * ((3569.0 / 4480.0) * x - 36137.0 / 10080.0) + 41179.0 / 6720.0) - 2263.0 / 480.0) + 175.0 / 128.0) - 1.0 / 60.0) + 1.0 / 20.0) + 4.0 / 315.0) - 4.0 / 105.0);
                            poly_val[offset + 2] = x * (x * (x * (x * (x * (x * (x * (x * (-3569.0 / 1120.0 * x + 144547.0 / 10080.0) - 30883.0 / 1260.0) + 13577.0 / 720.0) - 1579.0 / 288.0) + 169.0 / 1440.0) - 169.0 / 720.0) - 1.0 / 10.0) + 1.0 / 5.0);
                            poly_val[offset + 3] = x * (x * (x * (x * (x * (x * (x * (x * ((3569.0 / 480.0) * x - 24091.0 / 720.0) + 5147.0 / 90.0) - 31681.0 / 720.0) + 3697.0 / 288.0) - 61.0 / 180.0) + 61.0 / 180.0) + 4.0 / 5.0) - 4.0 / 5.0);
                            poly_val[offset + 4] = Math.Pow(x, 2) * (Math.Pow(x, 2) * (x * (x * (x * (x * (-3569.0 / 320.0 * x + 28909.0 / 576.0) - 2745.0 / 32.0) + 6337.0 / 96.0) - 6181.0 / 320.0) + 91.0 / 192.0) - 205.0 / 144.0) + 1;
                            poly_val[offset + 5] = x * (x * (x * (x * (x * (x * (x * (x * ((3569.0 / 320.0) * x - 4517.0 / 90.0) + 123523.0 / 1440.0) - 2971.0 / 45.0) + 11149.0 / 576.0) - 61.0 / 180.0) - 61.0 / 180.0) + 4.0 / 5.0) + 4.0 / 5.0);
                            poly_val[offset + 6] = x * (x * (x * (x * (x * (x * (x * (x * (-3569.0 / 480.0 * x + 48181.0 / 1440.0) - 20587.0 / 360.0) + 31697.0 / 720.0) - 3719.0 / 288.0) + 169.0 / 1440.0) + 169.0 / 720.0) - 1.0 / 10.0) - 1.0 / 5.0);
                            poly_val[offset + 7] = x * (x * (x * (x * (x * (x * (x * (x * ((3569.0 / 1120.0) * x - 72271.0 / 5040.0) + 2941.0 / 120.0) - 4529.0 / 240.0) + 177.0 / 32.0) - 1.0 / 60.0) - 1.0 / 20.0) + 4.0 / 315.0) + 4.0 / 105.0);
                            poly_val[offset + 8] = x * (x * (x * (x * (x * (x * (x * (x * (-3569.0 / 4480.0 * x + 144541.0 / 40320.0) - 123523.0 / 20160.0) + 13589.0 / 2880.0) - 1591.0 / 1152.0) + 7.0 / 5760.0) + 7.0 / 1440.0) - 1.0 / 1120.0) - 1.0 / 280.0);
                            poly_val[offset + 9] = Math.Pow(x, 5) * (x * (x * (x * ((3569.0 / 40320.0) * x - 803.0 / 2016.0) + 305.0 / 448.0) - 151.0 / 288.0) + 883.0 / 5760.0);
                            break;
                        case 1:
                            poly_val[offset + 0] = x * (x * (x * (x * (x * (x * (x * (-3569.0 / 4480.0 * x + 16061.0 / 5040.0) - 13727.0 / 2880.0) + 503.0 / 160.0) - 875.0 / 1152.0) + 7.0 / 1440.0) - 7.0 / 480.0) - 1.0 / 560.0) + 1.0 / 280.0;
                            poly_val[offset + 1] = x * (x * (x * (x * (x * (x * (x * ((32121.0 / 4480.0) * x - 36137.0 / 1260.0) + 41179.0 / 960.0) - 2263.0 / 80.0) + 875.0 / 128.0) - 1.0 / 15.0) + 3.0 / 20.0) + 8.0 / 315.0) - 4.0 / 105.0;
                            poly_val[offset + 2] = x * (x * (x * (x * (x * (x * (x * (-32121.0 / 1120.0 * x + 144547.0 / 1260.0) - 30883.0 / 180.0) + 13577.0 / 120.0) - 7895.0 / 288.0) + 169.0 / 360.0) - 169.0 / 240.0) - 1.0 / 5.0) + 1.0 / 5.0;
                            poly_val[offset + 3] = x * (x * (x * (x * (x * (x * (x * ((10707.0 / 160.0) * x - 24091.0 / 90.0) + 36029.0 / 90.0) - 31681.0 / 120.0) + 18485.0 / 288.0) - 61.0 / 45.0) + 61.0 / 60.0) + 8.0 / 5.0) - 4.0 / 5.0;
                            poly_val[offset + 4] = x * (Math.Pow(x, 2) * (x * (x * (x * (x * (-32121.0 / 320.0 * x + 28909.0 / 72.0) - 19215.0 / 32.0) + 6337.0 / 16.0) - 6181.0 / 64.0) + 91.0 / 48.0) - 205.0 / 72.0);
                            poly_val[offset + 5] = x * (x * (x * (x * (x * (x * (x * ((32121.0 / 320.0) * x - 18068.0 / 45.0) + 864661.0 / 1440.0) - 5942.0 / 15.0) + 55745.0 / 576.0) - 61.0 / 45.0) - 61.0 / 60.0) + 8.0 / 5.0) + 4.0 / 5.0;
                            poly_val[offset + 6] = x * (x * (x * (x * (x * (x * (x * (-10707.0 / 160.0 * x + 48181.0 / 180.0) - 144109.0 / 360.0) + 31697.0 / 120.0) - 18595.0 / 288.0) + 169.0 / 360.0) + 169.0 / 240.0) - 1.0 / 5.0) - 1.0 / 5.0;
                            poly_val[offset + 7] = x * (x * (x * (x * (x * (x * (x * ((32121.0 / 1120.0) * x - 72271.0 / 630.0) + 20587.0 / 120.0) - 4529.0 / 40.0) + 885.0 / 32.0) - 1.0 / 15.0) - 3.0 / 20.0) + 8.0 / 315.0) + 4.0 / 105.0;
                            poly_val[offset + 8] = x * (x * (x * (x * (x * (x * (x * (-32121.0 / 4480.0 * x + 144541.0 / 5040.0) - 123523.0 / 2880.0) + 13589.0 / 480.0) - 7955.0 / 1152.0) + 7.0 / 1440.0) + 7.0 / 480.0) - 1.0 / 560.0) - 1.0 / 280.0;
                            poly_val[offset + 9] = Math.Pow(x, 4) * (x * (x * (x * ((3569.0 / 4480.0) * x - 803.0 / 252.0) + 305.0 / 64.0) - 151.0 / 48.0) + 883.0 / 1152.0);
                            break;
                        case 2:
                            poly_val[offset + 0] = x * (x * (x * (x * (x * (x * (-3569.0 / 560.0 * x + 16061.0 / 720.0) - 13727.0 / 480.0) + 503.0 / 32.0) - 875.0 / 288.0) + 7.0 / 480.0) - 7.0 / 240.0) - 1.0 / 560.0;
                            poly_val[offset + 1] = x * (x * (x * (x * (x * (x * ((32121.0 / 560.0) * x - 36137.0 / 180.0) + 41179.0 / 160.0) - 2263.0 / 16.0) + 875.0 / 32.0) - 1.0 / 5.0) + 3.0 / 10.0) + 8.0 / 315.0;
                            poly_val[offset + 2] = x * (x * (x * (x * (x * (x * (-32121.0 / 140.0 * x + 144547.0 / 180.0) - 30883.0 / 30.0) + 13577.0 / 24.0) - 7895.0 / 72.0) + 169.0 / 120.0) - 169.0 / 120.0) - 1.0 / 5.0;
                            poly_val[offset + 3] = x * (x * (x * (x * (x * (x * ((10707.0 / 20.0) * x - 168637.0 / 90.0) + 36029.0 / 15.0) - 31681.0 / 24.0) + 18485.0 / 72.0) - 61.0 / 15.0) + 61.0 / 30.0) + 8.0 / 5.0;
                            poly_val[offset + 4] = Math.Pow(x, 2) * (x * (x * (x * (x * (-32121.0 / 40.0 * x + 202363.0 / 72.0) - 57645.0 / 16.0) + 31685.0 / 16.0) - 6181.0 / 16.0) + 91.0 / 16.0) - 205.0 / 72.0;
                            poly_val[offset + 5] = x * (x * (x * (x * (x * (x * ((32121.0 / 40.0) * x - 126476.0 / 45.0) + 864661.0 / 240.0) - 5942.0 / 3.0) + 55745.0 / 144.0) - 61.0 / 15.0) - 61.0 / 30.0) + 8.0 / 5.0;
                            poly_val[offset + 6] = x * (x * (x * (x * (x * (x * (-10707.0 / 20.0 * x + 337267.0 / 180.0) - 144109.0 / 60.0) + 31697.0 / 24.0) - 18595.0 / 72.0) + 169.0 / 120.0) + 169.0 / 120.0) - 1.0 / 5.0;
                            poly_val[offset + 7] = x * (x * (x * (x * (x * (x * ((32121.0 / 140.0) * x - 72271.0 / 90.0) + 20587.0 / 20.0) - 4529.0 / 8.0) + 885.0 / 8.0) - 1.0 / 5.0) - 3.0 / 10.0) + 8.0 / 315.0;
                            poly_val[offset + 8] = x * (x * (x * (x * (x * (x * (-32121.0 / 560.0 * x + 144541.0 / 720.0) - 123523.0 / 480.0) + 13589.0 / 96.0) - 7955.0 / 288.0) + 7.0 / 480.0) + 7.0 / 240.0) - 1.0 / 560.0;
                            poly_val[offset + 9] = Math.Pow(x, 3) * (x * (x * (x * ((3569.0 / 560.0) * x - 803.0 / 36.0) + 915.0 / 32.0) - 755.0 / 48.0) + 883.0 / 288.0);
                            break;
                        default:
                            throw new Exception("Invalid derivative option!");
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.M4Q12:
                    switch (deriv)
                    {
                        case 0:
                            poly_val[offset + 0] = x * (x * (x * (x * (x * (x * (x * (x * ((151.0 / 7560.0) * x - 16309.0 / 181440.0) + 139381.0 / 907200.0) - 10721.0 / 90720.0) + 443.0 / 12960.0) - 41.0 / 181440.0) + 41.0 / 36288.0) + 1.0 / 6300.0) - 1.0 / 1260.0);
                            poly_val[offset + 1] = x * (x * (x * (x * (x * (x * (x * (x * (-79729.0 / 362880.0 * x + 13289.0 / 13440.0) - 766583.0 / 453600.0) + 589531.0 / 453600.0) - 227249.0 / 604800.0) + 1261.0 / 362880.0) - 1261.0 / 90720.0) - 5.0 / 2016.0) + 5.0 / 504.0);
                            poly_val[offset + 2] = x * (x * (x * (x * (x * (x * (x * (x * ((132883.0 / 120960.0) * x - 897011.0 / 181440.0) + 18251.0 / 2160.0) - 877.0 / 135.0) + 136301.0 / 72576.0) - 541.0 / 20160.0) + 541.0 / 6720.0) + 5.0 / 252.0) - 5.0 / 84.0);
                            poly_val[offset + 3] = x * (x * (x * (x * (x * (x * (x * (x * (-33221.0 / 10080.0 * x + 897007.0 / 60480.0) - 306595.0 / 12096.0) + 1178419.0 / 60480.0) - 341587.0 / 60480.0) + 4369.0 / 30240.0) - 4369.0 / 15120.0) - 5.0 / 42.0) + 5.0 / 21.0);
                            poly_val[offset + 4] = x * (x * (x * (x * (x * (x * (x * (x * ((199327.0 / 30240.0) * x - 298999.0 / 10080.0) + 1532863.0 / 30240.0) - 29459.0 / 756.0) + 114389.0 / 10080.0) - 1669.0 / 4320.0) + 1669.0 / 4320.0) + 5.0 / 6.0) - 5.0 / 6.0);
                            poly_val[offset + 5] = Math.Pow(x, 2) * (Math.Pow(x, 2) * (x * (x * (x * (x * (-2953.0 / 320.0 * x + 358793.0 / 8640.0) - 255461.0 / 3600.0) + 9821.0 / 180.0) - 27589.0 / 1728.0) + 1529.0 / 2880.0) - 5269.0 / 3600.0) + 1;
                            poly_val[offset + 6] = x * (x * (x * (x * (x * (x * (x * (x * ((2953.0 / 320.0) * x - 179393.0 / 4320.0) + 191587.0 / 2700.0) - 589421.0 / 10800.0) + 692147.0 / 43200.0) - 1669.0 / 4320.0) - 1669.0 / 4320.0) + 5.0 / 6.0) + 5.0 / 6.0);
                            poly_val[offset + 7] = x * (x * (x * (x * (x * (x * (x * (x * (-199327.0 / 30240.0 * x + 149491.0 / 5040.0) - 1532659.0 / 30240.0) + 1179233.0 / 30240.0) - 275.0 / 24.0) + 4369.0 / 30240.0) + 4369.0 / 15120.0) - 5.0 / 42.0) - 5.0 / 21.0);
                            poly_val[offset + 8] = x * (x * (x * (x * (x * (x * (x * (x * ((33221.0 / 10080.0) * x - 896927.0 / 60480.0) + 11353.0 / 448.0) - 65533.0 / 3360.0) + 43279.0 / 7560.0) - 541.0 / 20160.0) - 541.0 / 6720.0) + 5.0 / 252.0) + 5.0 / 84.0);
                            poly_val[offset + 9] = x * (x * (x * (x * (x * (x * (x * (x * (-132883.0 / 120960.0 * x + 1793819.0 / 362880.0) - 109477.0 / 12960.0) + 16855.0 / 2592.0) - 691319.0 / 362880.0) + 1261.0 / 362880.0) + 1261.0 / 90720.0) - 5.0 / 2016.0) - 5.0 / 504.0);
                            poly_val[offset + 10] = x * (x * (x * (x * (x * (x * (x * (x * ((79729.0 / 362880.0) * x - 19931.0 / 20160.0) + 383179.0 / 226800.0) - 58999.0 / 45360.0) + 46027.0 / 120960.0) - 41.0 / 181440.0) - 41.0 / 36288.0) + 1.0 / 6300.0) + 1.0 / 1260.0);
                            poly_val[offset + 11] = Math.Pow(x, 5) * (x * (x * (x * (-151.0 / 7560.0 * x + 16307.0 / 181440.0) - 46447.0 / 302400.0) + 35759.0 / 302400.0) - 31351.0 / 907200.0);
                            break;
                        case 1:
                            poly_val[offset + 0] = x * (x * (x * (x * (x * (x * (x * ((151.0 / 840.0) * x - 16309.0 / 22680.0) + 139381.0 / 129600.0) - 10721.0 / 15120.0) + 443.0 / 2592.0) - 41.0 / 45360.0) + 41.0 / 12096.0) + 1.0 / 3150.0) - 1.0 / 1260.0;
                            poly_val[offset + 1] = x * (x * (x * (x * (x * (x * (x * (-79729.0 / 40320.0 * x + 13289.0 / 1680.0) - 766583.0 / 64800.0) + 589531.0 / 75600.0) - 227249.0 / 120960.0) + 1261.0 / 90720.0) - 1261.0 / 30240.0) - 5.0 / 1008.0) + 5.0 / 504.0;
                            poly_val[offset + 2] = x * (x * (x * (x * (x * (x * (x * ((132883.0 / 13440.0) * x - 897011.0 / 22680.0) + 127757.0 / 2160.0) - 1754.0 / 45.0) + 681505.0 / 72576.0) - 541.0 / 5040.0) + 541.0 / 2240.0) + 5.0 / 126.0) - 5.0 / 84.0;
                            poly_val[offset + 3] = x * (x * (x * (x * (x * (x * (x * (-33221.0 / 1120.0 * x + 897007.0 / 7560.0) - 306595.0 / 1728.0) + 1178419.0 / 10080.0) - 341587.0 / 12096.0) + 4369.0 / 7560.0) - 4369.0 / 5040.0) - 5.0 / 21.0) + 5.0 / 21.0;
                            poly_val[offset + 4] = x * (x * (x * (x * (x * (x * (x * ((199327.0 / 3360.0) * x - 298999.0 / 1260.0) + 1532863.0 / 4320.0) - 29459.0 / 126.0) + 114389.0 / 2016.0) - 1669.0 / 1080.0) + 1669.0 / 1440.0) + 5.0 / 3.0) - 5.0 / 6.0;
                            poly_val[offset + 5] = x * (Math.Pow(x, 2) * (x * (x * (x * (x * (-26577.0 / 320.0 * x + 358793.0 / 1080.0) - 1788227.0 / 3600.0) + 9821.0 / 30.0) - 137945.0 / 1728.0) + 1529.0 / 720.0) - 5269.0 / 1800.0);
                            poly_val[offset + 6] = x * (x * (x * (x * (x * (x * (x * ((26577.0 / 320.0) * x - 179393.0 / 540.0) + 1341109.0 / 2700.0) - 589421.0 / 1800.0) + 692147.0 / 8640.0) - 1669.0 / 1080.0) - 1669.0 / 1440.0) + 5.0 / 3.0) + 5.0 / 6.0;
                            poly_val[offset + 7] = x * (x * (x * (x * (x * (x * (x * (-199327.0 / 3360.0 * x + 149491.0 / 630.0) - 1532659.0 / 4320.0) + 1179233.0 / 5040.0) - 1375.0 / 24.0) + 4369.0 / 7560.0) + 4369.0 / 5040.0) - 5.0 / 21.0) - 5.0 / 21.0;
                            poly_val[offset + 8] = x * (x * (x * (x * (x * (x * (x * ((33221.0 / 1120.0) * x - 896927.0 / 7560.0) + 11353.0 / 64.0) - 65533.0 / 560.0) + 43279.0 / 1512.0) - 541.0 / 5040.0) - 541.0 / 2240.0) + 5.0 / 126.0) + 5.0 / 84.0;
                            poly_val[offset + 9] = x * (x * (x * (x * (x * (x * (x * (-132883.0 / 13440.0 * x + 1793819.0 / 45360.0) - 766339.0 / 12960.0) + 16855.0 / 432.0) - 691319.0 / 72576.0) + 1261.0 / 90720.0) + 1261.0 / 30240.0) - 5.0 / 1008.0) - 5.0 / 504.0;
                            poly_val[offset + 10] = x * (x * (x * (x * (x * (x * (x * ((79729.0 / 40320.0) * x - 19931.0 / 2520.0) + 383179.0 / 32400.0) - 58999.0 / 7560.0) + 46027.0 / 24192.0) - 41.0 / 45360.0) - 41.0 / 12096.0) + 1.0 / 3150.0) + 1.0 / 1260.0;
                            poly_val[offset + 11] = Math.Pow(x, 4) * (x * (x * (x * (-151.0 / 840.0 * x + 16307.0 / 22680.0) - 46447.0 / 43200.0) + 35759.0 / 50400.0) - 31351.0 / 181440.0);
                            break;
                        case 2:
                            poly_val[offset + 0] = x * (x * (x * (x * (x * (x * ((151.0 / 105.0) * x - 16309.0 / 3240.0) + 139381.0 / 21600.0) - 10721.0 / 3024.0) + 443.0 / 648.0) - 41.0 / 15120.0) + 41.0 / 6048.0) + 1.0 / 3150.0;
                            poly_val[offset + 1] = x * (x * (x * (x * (x * (x * (-79729.0 / 5040.0 * x + 13289.0 / 240.0) - 766583.0 / 10800.0) + 589531.0 / 15120.0) - 227249.0 / 30240.0) + 1261.0 / 30240.0) - 1261.0 / 15120.0) - 5.0 / 1008.0;
                            poly_val[offset + 2] = x * (x * (x * (x * (x * (x * ((132883.0 / 1680.0) * x - 897011.0 / 3240.0) + 127757.0 / 360.0) - 1754.0 / 9.0) + 681505.0 / 18144.0) - 541.0 / 1680.0) + 541.0 / 1120.0) + 5.0 / 126.0;
                            poly_val[offset + 3] = x * (x * (x * (x * (x * (x * (-33221.0 / 140.0 * x + 897007.0 / 1080.0) - 306595.0 / 288.0) + 1178419.0 / 2016.0) - 341587.0 / 3024.0) + 4369.0 / 2520.0) - 4369.0 / 2520.0) - 5.0 / 21.0;
                            poly_val[offset + 4] = x * (x * (x * (x * (x * (x * ((199327.0 / 420.0) * x - 298999.0 / 180.0) + 1532863.0 / 720.0) - 147295.0 / 126.0) + 114389.0 / 504.0) - 1669.0 / 360.0) + 1669.0 / 720.0) + 5.0 / 3.0;
                            poly_val[offset + 5] = Math.Pow(x, 2) * (x * (x * (x * (x * (-26577.0 / 40.0 * x + 2511551.0 / 1080.0) - 1788227.0 / 600.0) + 9821.0 / 6.0) - 137945.0 / 432.0) + 1529.0 / 240.0) - 5269.0 / 1800.0;
                            poly_val[offset + 6] = x * (x * (x * (x * (x * (x * ((26577.0 / 40.0) * x - 1255751.0 / 540.0) + 1341109.0 / 450.0) - 589421.0 / 360.0) + 692147.0 / 2160.0) - 1669.0 / 360.0) - 1669.0 / 720.0) + 5.0 / 3.0;
                            poly_val[offset + 7] = x * (x * (x * (x * (x * (x * (-199327.0 / 420.0 * x + 149491.0 / 90.0) - 1532659.0 / 720.0) + 1179233.0 / 1008.0) - 1375.0 / 6.0) + 4369.0 / 2520.0) + 4369.0 / 2520.0) - 5.0 / 21.0;
                            poly_val[offset + 8] = x * (x * (x * (x * (x * (x * ((33221.0 / 140.0) * x - 896927.0 / 1080.0) + 34059.0 / 32.0) - 65533.0 / 112.0) + 43279.0 / 378.0) - 541.0 / 1680.0) - 541.0 / 1120.0) + 5.0 / 126.0;
                            poly_val[offset + 9] = x * (x * (x * (x * (x * (x * (-132883.0 / 1680.0 * x + 1793819.0 / 6480.0) - 766339.0 / 2160.0) + 84275.0 / 432.0) - 691319.0 / 18144.0) + 1261.0 / 30240.0) + 1261.0 / 15120.0) - 5.0 / 1008.0;
                            poly_val[offset + 10] = x * (x * (x * (x * (x * (x * ((79729.0 / 5040.0) * x - 19931.0 / 360.0) + 383179.0 / 5400.0) - 58999.0 / 1512.0) + 46027.0 / 6048.0) - 41.0 / 15120.0) - 41.0 / 6048.0) + 1.0 / 3150.0;
                            poly_val[offset + 11] = Math.Pow(x, 3) * (x * (x * (x * (-151.0 / 105.0 * x + 16307.0 / 3240.0) - 46447.0 / 7200.0) + 35759.0 / 10080.0) - 31351.0 / 45360.0);
                            break;
                        default:
                            throw new Exception("Invalid derivative option!");
                    }
                    break;
                case TurbulenceOptions.SpatialInterpolation.M4Q14:
                    switch (deriv)
                    {
                        case 0:
                            poly_val[offset + 0] = x * (x * (x * (x * (x * (x * (x * (x * (-78457.0 / 17107200.0 * x + 353087.0 / 17107200.0) - 211223.0 / 5987520.0) + 324853.0 / 11975040.0) - 17057.0 / 2177280.0) + 479.0 / 10886400.0) - 479.0 / 1814400.0) - 1.0 / 33264.0) + 1.0 / 5544.0);
                            poly_val[offset + 1] = x * (x * (x * (x * (x * (x * (x * (x * ((7139707.0 / 119750400.0) * x - 3213169.0 / 11975040.0) + 4576529.0 / 9979200.0) - 21111899.0 / 59875200.0) + 12185113.0 / 119750400.0) - 19.0 / 25200.0) + 19.0 / 5040.0) + 1.0 / 1925.0) - 1.0 / 385.0);
                            poly_val[offset + 2] = x * (x * (x * (x * (x * (x * (x * (x * (-396659.0 / 1108800.0 * x + 198349.0 / 123200.0) - 305099.0 / 110880.0) + 36643.0 / 17325.0) - 9017.0 / 14784.0) + 643.0 / 100800.0) - 643.0 / 25200.0) - 1.0 / 224.0) + 1.0 / 56.0);
                            poly_val[offset + 3] = x * (x * (x * (x * (x * (x * (x * (x * ((3570011.0 / 2721600.0) * x - 8033279.0 / 1360800.0) + 13728709.0 / 1360800.0) - 1506979.0 / 194400.0) + 243283.0 / 108864.0) - 4969.0 / 136080.0) + 4969.0 / 45360.0) + 5.0 / 189.0) - 5.0 / 63.0);
                            poly_val[offset + 4] = x * (x * (x * (x * (x * (x * (x * (x * (-7140163.0 / 2177280.0 * x + 4590463.0 / 311040.0) - 114395.0 / 4536.0) + 21089681.0 / 1088640.0) - 12192451.0 / 2177280.0) + 4469.0 / 26880.0) - 4469.0 / 13440.0) - 15.0 / 112.0) + 15.0 / 56.0);
                            poly_val[offset + 5] = x * (x * (x * (x * (x * (x * (x * (x * ((793363.0 / 134400.0) * x - 595053.0 / 22400.0) + 21787.0 / 480.0) - 468581.0 / 13440.0) + 90827.0 / 8960.0) - 1769.0 / 4200.0) + 1769.0 / 4200.0) + 6.0 / 7.0) - 6.0 / 7.0);
                            poly_val[offset + 6] = Math.Pow(x, 2) * (Math.Pow(x, 2) * (x * (x * (x * (x * (-510023.0 / 64800.0 * x + 459029.0 / 12960.0) - 13724287.0 / 226800.0) + 5272181.0 / 113400.0) - 881767.0 / 64800.0) + 37037.0 / 64800.0) - 5369.0 / 3600.0) + 1;
                            poly_val[offset + 7] = x * (x * (x * (x * (x * (x * (x * (x * ((510023.0 / 64800.0) * x - 1147531.0 / 32400.0) + 182975.0 / 3024.0) - 10548199.0 / 226800.0) + 248141.0 / 18144.0) - 1769.0 / 4200.0) - 1769.0 / 4200.0) + 6.0 / 7.0) + 6.0 / 7.0);
                            poly_val[offset + 8] = x * (x * (x * (x * (x * (x * (x * (x * (-793363.0 / 134400.0 * x + 1189983.0 / 44800.0) - 1524721.0 / 33600.0) + 2345149.0 / 67200.0) - 92069.0 / 8960.0) + 4469.0 / 26880.0) + 4469.0 / 13440.0) - 15.0 / 112.0) - 15.0 / 56.0);
                            poly_val[offset + 9] = x * (x * (x * (x * (x * (x * (x * (x * ((7140163.0 / 2177280.0) * x - 16064113.0 / 1088640.0) + 2744477.0 / 108864.0) - 21115391.0 / 1088640.0) + 12416921.0 / 2177280.0) - 4969.0 / 136080.0) - 4969.0 / 45360.0) + 5.0 / 189.0) + 5.0 / 63.0);
                            poly_val[offset + 10] = x * (x * (x * (x * (x * (x * (x * (x * (-3570011.0 / 2721600.0 * x + 16063541.0 / 2721600.0) - 182969.0 / 18144.0) + 37717.0 / 4860.0) - 1239151.0 / 544320.0) + 643.0 / 100800.0) + 643.0 / 25200.0) - 1.0 / 224.0) - 1.0 / 56.0);
                            poly_val[offset + 11] = x * (x * (x * (x * (x * (x * (x * (x * ((396659.0 / 1108800.0) * x - 2833.0 / 1760.0) + 1524793.0 / 554400.0) - 1173593.0 / 554400.0) + 20827.0 / 33600.0) - 19.0 / 25200.0) - 19.0 / 5040.0) + 1.0 / 1925.0) + 1.0 / 385.0);
                            poly_val[offset + 12] = x * (x * (x * (x * (x * (x * (x * (x * (-7139707.0 / 119750400.0 * x + 32125673.0 / 119750400.0) - 196051.0 / 427680.0) + 21126353.0 / 59875200.0) - 494243.0 / 4790016.0) + 479.0 / 10886400.0) + 479.0 / 1814400.0) - 1.0 / 33264.0) - 1.0 / 5544.0);
                            poly_val[offset + 13] = Math.Pow(x, 5) * (x * (x * (x * ((78457.0 / 17107200.0) * x - 176513.0 / 8553600.0) + 43987.0 / 1247400.0) - 1625177.0 / 59875200.0) + 27131.0 / 3421440.0);
                            break;
                        case 1:
                            poly_val[offset + 0] = x * (x * (x * (x * (x * (x * (x * (-78457.0 / 1900800.0 * x + 353087.0 / 2138400.0) - 211223.0 / 855360.0) + 324853.0 / 1995840.0) - 17057.0 / 435456.0) + 479.0 / 2721600.0) - 479.0 / 604800.0) - 1.0 / 16632.0) + 1.0 / 5544.0;
                            poly_val[offset + 1] = x * (x * (x * (x * (x * (x * (x * ((7139707.0 / 13305600.0) * x - 3213169.0 / 1496880.0) + 4576529.0 / 1425600.0) - 21111899.0 / 9979200.0) + 12185113.0 / 23950080.0) - 19.0 / 6300.0) + 19.0 / 1680.0) + 2.0 / 1925.0) - 1.0 / 385.0;
                            poly_val[offset + 2] = x * (x * (x * (x * (x * (x * (x * (-396659.0 / 123200.0 * x + 198349.0 / 15400.0) - 305099.0 / 15840.0) + 73286.0 / 5775.0) - 45085.0 / 14784.0) + 643.0 / 25200.0) - 643.0 / 8400.0) - 1.0 / 112.0) + 1.0 / 56.0;
                            poly_val[offset + 3] = x * (x * (x * (x * (x * (x * (x * ((3570011.0 / 302400.0) * x - 8033279.0 / 170100.0) + 13728709.0 / 194400.0) - 1506979.0 / 32400.0) + 1216415.0 / 108864.0) - 4969.0 / 34020.0) + 4969.0 / 15120.0) + 10.0 / 189.0) - 5.0 / 63.0;
                            poly_val[offset + 4] = x * (x * (x * (x * (x * (x * (x * (-7140163.0 / 241920.0 * x + 4590463.0 / 38880.0) - 114395.0 / 648.0) + 21089681.0 / 181440.0) - 12192451.0 / 435456.0) + 4469.0 / 6720.0) - 4469.0 / 4480.0) - 15.0 / 56.0) + 15.0 / 56.0;
                            poly_val[offset + 5] = x * (x * (x * (x * (x * (x * (x * ((2380089.0 / 44800.0) * x - 595053.0 / 2800.0) + 152509.0 / 480.0) - 468581.0 / 2240.0) + 90827.0 / 1792.0) - 1769.0 / 1050.0) + 1769.0 / 1400.0) + 12.0 / 7.0) - 6.0 / 7.0;
                            poly_val[offset + 6] = x * (Math.Pow(x, 2) * (x * (x * (x * (x * (-510023.0 / 7200.0 * x + 459029.0 / 1620.0) - 13724287.0 / 32400.0) + 5272181.0 / 18900.0) - 881767.0 / 12960.0) + 37037.0 / 16200.0) - 5369.0 / 1800.0);
                            poly_val[offset + 7] = x * (x * (x * (x * (x * (x * (x * ((510023.0 / 7200.0) * x - 1147531.0 / 4050.0) + 182975.0 / 432.0) - 10548199.0 / 37800.0) + 1240705.0 / 18144.0) - 1769.0 / 1050.0) - 1769.0 / 1400.0) + 12.0 / 7.0) + 6.0 / 7.0;
                            poly_val[offset + 8] = x * (x * (x * (x * (x * (x * (x * (-2380089.0 / 44800.0 * x + 1189983.0 / 5600.0) - 1524721.0 / 4800.0) + 2345149.0 / 11200.0) - 92069.0 / 1792.0) + 4469.0 / 6720.0) + 4469.0 / 4480.0) - 15.0 / 56.0) - 15.0 / 56.0;
                            poly_val[offset + 9] = x * (x * (x * (x * (x * (x * (x * ((7140163.0 / 241920.0) * x - 16064113.0 / 136080.0) + 2744477.0 / 15552.0) - 21115391.0 / 181440.0) + 12416921.0 / 435456.0) - 4969.0 / 34020.0) - 4969.0 / 15120.0) + 10.0 / 189.0) + 5.0 / 63.0;
                            poly_val[offset + 10] = x * (x * (x * (x * (x * (x * (x * (-3570011.0 / 302400.0 * x + 16063541.0 / 340200.0) - 182969.0 / 2592.0) + 37717.0 / 810.0) - 1239151.0 / 108864.0) + 643.0 / 25200.0) + 643.0 / 8400.0) - 1.0 / 112.0) - 1.0 / 56.0;
                            poly_val[offset + 11] = x * (x * (x * (x * (x * (x * (x * ((396659.0 / 123200.0) * x - 2833.0 / 220.0) + 1524793.0 / 79200.0) - 1173593.0 / 92400.0) + 20827.0 / 6720.0) - 19.0 / 6300.0) - 19.0 / 1680.0) + 2.0 / 1925.0) + 1.0 / 385.0;
                            poly_val[offset + 12] = x * (x * (x * (x * (x * (x * (x * (-7139707.0 / 13305600.0 * x + 32125673.0 / 14968800.0) - 1372357.0 / 427680.0) + 21126353.0 / 9979200.0) - 2471215.0 / 4790016.0) + 479.0 / 2721600.0) + 479.0 / 604800.0) - 1.0 / 16632.0) - 1.0 / 5544.0;
                            poly_val[offset + 13] = Math.Pow(x, 4) * (x * (x * (x * ((78457.0 / 1900800.0) * x - 176513.0 / 1069200.0) + 43987.0 / 178200.0) - 1625177.0 / 9979200.0) + 27131.0 / 684288.0);
                            break;
                        case 2:
                            poly_val[offset + 0] = x * (x * (x * (x * (x * (x * (-78457.0 / 237600.0 * x + 2471609.0 / 2138400.0) - 211223.0 / 142560.0) + 324853.0 / 399168.0) - 17057.0 / 108864.0) + 479.0 / 907200.0) - 479.0 / 302400.0) - 1.0 / 16632.0;
                            poly_val[offset + 1] = x * (x * (x * (x * (x * (x * ((7139707.0 / 1663200.0) * x - 3213169.0 / 213840.0) + 4576529.0 / 237600.0) - 21111899.0 / 1995840.0) + 12185113.0 / 5987520.0) - 19.0 / 2100.0) + 19.0 / 840.0) + 2.0 / 1925.0;
                            poly_val[offset + 2] = x * (x * (x * (x * (x * (x * (-396659.0 / 15400.0 * x + 198349.0 / 2200.0) - 305099.0 / 2640.0) + 73286.0 / 1155.0) - 45085.0 / 3696.0) + 643.0 / 8400.0) - 643.0 / 4200.0) - 1.0 / 112.0;
                            poly_val[offset + 3] = x * (x * (x * (x * (x * (x * ((3570011.0 / 37800.0) * x - 8033279.0 / 24300.0) + 13728709.0 / 32400.0) - 1506979.0 / 6480.0) + 1216415.0 / 27216.0) - 4969.0 / 11340.0) + 4969.0 / 7560.0) + 10.0 / 189.0;
                            poly_val[offset + 4] = x * (x * (x * (x * (x * (x * (-7140163.0 / 30240.0 * x + 32133241.0 / 38880.0) - 114395.0 / 108.0) + 21089681.0 / 36288.0) - 12192451.0 / 108864.0) + 4469.0 / 2240.0) - 4469.0 / 2240.0) - 15.0 / 56.0;
                            poly_val[offset + 5] = x * (x * (x * (x * (x * (x * ((2380089.0 / 5600.0) * x - 595053.0 / 400.0) + 152509.0 / 80.0) - 468581.0 / 448.0) + 90827.0 / 448.0) - 1769.0 / 350.0) + 1769.0 / 700.0) + 12.0 / 7.0;
                            poly_val[offset + 6] = Math.Pow(x, 2) * (x * (x * (x * (x * (-510023.0 / 900.0 * x + 3213203.0 / 1620.0) - 13724287.0 / 5400.0) + 5272181.0 / 3780.0) - 881767.0 / 3240.0) + 37037.0 / 5400.0) - 5369.0 / 1800.0;
                            poly_val[offset + 7] = x * (x * (x * (x * (x * (x * ((510023.0 / 900.0) * x - 8032717.0 / 4050.0) + 182975.0 / 72.0) - 10548199.0 / 7560.0) + 1240705.0 / 4536.0) - 1769.0 / 350.0) - 1769.0 / 700.0) + 12.0 / 7.0;
                            poly_val[offset + 8] = x * (x * (x * (x * (x * (x * (-2380089.0 / 5600.0 * x + 1189983.0 / 800.0) - 1524721.0 / 800.0) + 2345149.0 / 2240.0) - 92069.0 / 448.0) + 4469.0 / 2240.0) + 4469.0 / 2240.0) - 15.0 / 56.0;
                            poly_val[offset + 9] = x * (x * (x * (x * (x * (x * ((7140163.0 / 30240.0) * x - 16064113.0 / 19440.0) + 2744477.0 / 2592.0) - 21115391.0 / 36288.0) + 12416921.0 / 108864.0) - 4969.0 / 11340.0) - 4969.0 / 7560.0) + 10.0 / 189.0;
                            poly_val[offset + 10] = x * (x * (x * (x * (x * (x * (-3570011.0 / 37800.0 * x + 16063541.0 / 48600.0) - 182969.0 / 432.0) + 37717.0 / 162.0) - 1239151.0 / 27216.0) + 643.0 / 8400.0) + 643.0 / 4200.0) - 1.0 / 112.0;
                            poly_val[offset + 11] = x * (x * (x * (x * (x * (x * ((396659.0 / 15400.0) * x - 19831.0 / 220.0) + 1524793.0 / 13200.0) - 1173593.0 / 18480.0) + 20827.0 / 1680.0) - 19.0 / 2100.0) - 19.0 / 840.0) + 2.0 / 1925.0;
                            poly_val[offset + 12] = x * (x * (x * (x * (x * (x * (-7139707.0 / 1663200.0 * x + 32125673.0 / 2138400.0) - 1372357.0 / 71280.0) + 21126353.0 / 1995840.0) - 2471215.0 / 1197504.0) + 479.0 / 907200.0) + 479.0 / 302400.0) - 1.0 / 16632.0;
                            poly_val[offset + 13] = Math.Pow(x, 3) * (x * (x * (x * ((78457.0 / 237600.0) * x - 1235591.0 / 1069200.0) + 43987.0 / 29700.0) - 1625177.0 / 1995840.0) + 27131.0 / 171072.0);
                            break;
                        default:
                            throw new Exception("Invalid derivative option!");
                    }
                    break;
                default:
                    throw new Exception(String.Format("Invalid Spatial Interpolation Option: {0}", spatialInterp));
            }
        }

        public GetSplinesWorker(TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp,
            int derivative)
        {
            this.kernelSize = 0;
            this.setInfo = setInfo;
            this.spatialInterp = spatialInterp;
            this.derivative = derivative;

            if (derivative == 0)
            {
                this.result_size = setInfo.Components;
            }
            else if (derivative == 1)
            {
                this.result_size = 3 * setInfo.Components;
            }
            else if (derivative == 2)
            {
                this.result_size = 6 * setInfo.Components;
            }
            else
            {
                throw new Exception(String.Format("Invalid Derivative Option: {0}", derivative));
            }

            switch (spatialInterp)
            {
                case TurbulenceOptions.SpatialInterpolation.M1Q4:
                case TurbulenceOptions.SpatialInterpolation.M2Q4:
                case TurbulenceOptions.SpatialInterpolation.M3Q4:
                case TurbulenceOptions.SpatialInterpolation.M4Q4:
                    this.kernelSize = 4;
                    break;
                case TurbulenceOptions.SpatialInterpolation.M1Q6:
                case TurbulenceOptions.SpatialInterpolation.M2Q6:
                case TurbulenceOptions.SpatialInterpolation.M3Q6:
                case TurbulenceOptions.SpatialInterpolation.M4Q6:
                    this.kernelSize = 6;
                    break;
                case TurbulenceOptions.SpatialInterpolation.M1Q8:
                case TurbulenceOptions.SpatialInterpolation.M2Q8:
                case TurbulenceOptions.SpatialInterpolation.M3Q8:
                case TurbulenceOptions.SpatialInterpolation.M4Q8:
                    this.kernelSize = 8;
                    break;
                case TurbulenceOptions.SpatialInterpolation.M1Q10:
                case TurbulenceOptions.SpatialInterpolation.M2Q10:
                case TurbulenceOptions.SpatialInterpolation.M3Q10:
                case TurbulenceOptions.SpatialInterpolation.M4Q10:
                    this.kernelSize = 10;
                    break;
                case TurbulenceOptions.SpatialInterpolation.M1Q12:
                case TurbulenceOptions.SpatialInterpolation.M2Q12:
                case TurbulenceOptions.SpatialInterpolation.M3Q12:
                case TurbulenceOptions.SpatialInterpolation.M4Q12:
                    this.kernelSize = 12;
                    break;
                case TurbulenceOptions.SpatialInterpolation.M1Q14:
                case TurbulenceOptions.SpatialInterpolation.M2Q14:
                case TurbulenceOptions.SpatialInterpolation.M3Q14:
                case TurbulenceOptions.SpatialInterpolation.M4Q14:
                    this.kernelSize = 14;
                    break;
                default:
                    throw new Exception(String.Format("Invalid Spatial Interpolation Option: {0}", spatialInterp));
            }
        }

        public override SqlMetaData[] GetRecordMetaData()
        {
            if (derivative == 0)
            {
                if (setInfo.Components == 3)
                {
                    return new SqlMetaData[] {
                        new SqlMetaData("Req", SqlDbType.Int),
                        new SqlMetaData("X", SqlDbType.Real),
                        new SqlMetaData("Y", SqlDbType.Real),
                        new SqlMetaData("Z", SqlDbType.Real)};
                }
                else
                {
                    return new SqlMetaData[] {
                        new SqlMetaData("Req", SqlDbType.Int),
                        new SqlMetaData("P", SqlDbType.Real)};
                }
            }
            else if (derivative == 1)
            {
                if (setInfo.Components == 3)
                {
                    return new SqlMetaData[] {
                        new SqlMetaData("Req", SqlDbType.Int),
                        new SqlMetaData("duxdx", SqlDbType.Real),
                        new SqlMetaData("duxdy", SqlDbType.Real),
                        new SqlMetaData("duxdz", SqlDbType.Real),
                        new SqlMetaData("duydx", SqlDbType.Real),
                        new SqlMetaData("duydy", SqlDbType.Real),
                        new SqlMetaData("duydz", SqlDbType.Real),
                        new SqlMetaData("duzdx", SqlDbType.Real),
                        new SqlMetaData("duzdy", SqlDbType.Real),
                        new SqlMetaData("duzdz", SqlDbType.Real) };
                }
                else
                {
                    return new SqlMetaData[] {
                        new SqlMetaData("Req", SqlDbType.Int),
                        new SqlMetaData("dpdx", SqlDbType.Real),
                        new SqlMetaData("dpdy", SqlDbType.Real),
                        new SqlMetaData("dpdz", SqlDbType.Real) };
                }
            }
            else if (derivative == 2)
            {
                if (setInfo.Components == 3)
                {
                    return new SqlMetaData[] {
                        new SqlMetaData("Req", SqlDbType.Int),
                        new SqlMetaData("d2uxdxdx", SqlDbType.Real),
                        new SqlMetaData("d2uxdxdy", SqlDbType.Real),
                        new SqlMetaData("d2uxdxdz", SqlDbType.Real),
                        new SqlMetaData("d2uxdydy", SqlDbType.Real),
                        new SqlMetaData("d2uxdydz", SqlDbType.Real),
                        new SqlMetaData("d2uxdzdz", SqlDbType.Real),
                        new SqlMetaData("d2uydxdx", SqlDbType.Real),
                        new SqlMetaData("d2uydxdy", SqlDbType.Real),
                        new SqlMetaData("d2uydxdz", SqlDbType.Real),
                        new SqlMetaData("d2uydydy", SqlDbType.Real),
                        new SqlMetaData("d2uydydz", SqlDbType.Real),
                        new SqlMetaData("d2uydzdz", SqlDbType.Real),
                        new SqlMetaData("d2uzdxdx", SqlDbType.Real),
                        new SqlMetaData("d2uzdxdy", SqlDbType.Real),
                        new SqlMetaData("d2uzdxdz", SqlDbType.Real),
                        new SqlMetaData("d2uzdydy", SqlDbType.Real),
                        new SqlMetaData("d2uzdydz", SqlDbType.Real),
                        new SqlMetaData("d2uzdzdz", SqlDbType.Real) };
                }
                else
                {
                    return new SqlMetaData[] {
                        new SqlMetaData("Req", SqlDbType.Int),
                        new SqlMetaData("d2pdxdx", SqlDbType.Real),
                        new SqlMetaData("d2pdxdy", SqlDbType.Real),
                        new SqlMetaData("d2pdxdz", SqlDbType.Real),
                        new SqlMetaData("d2pdydy", SqlDbType.Real),
                        new SqlMetaData("d2pdydz", SqlDbType.Real),
                        new SqlMetaData("d2pdzdz", SqlDbType.Real) };
                }
            }
            else
            {
                throw new Exception(String.Format("Invalid Derivative Option: {0}", derivative));
            }
        }

        /// <summary>
        /// Determines the database atoms that overlap the kernel of computation for the given point
        /// </summary>
        /// <remarks>
        /// This methods should only need to be called when spatial interpolation is performed
        /// i.e. when kernelSize != 0
        /// </remarks>
        public override void GetAtomsForPoint(SQLUtility.MHDInputRequest request, long mask, int pointsPerCubeEstimate, Dictionary<long, List<int>> map, ref int total_points)
        {
            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None)
                throw new Exception("GetAtomsForPoint should only be called when spatial interpolation is performed!");

            int startz = request.cell_z - kernelSize / 2 + 1, starty = request.cell_y - kernelSize / 2 + 1, startx = request.cell_x - kernelSize / 2 + 1;
            int endz = request.cell_z + kernelSize / 2, endy = request.cell_y + kernelSize / 2, endx = request.cell_x + kernelSize / 2;

            // we do not want a request to appear more than once in the list for an atom
            // with the below logic we are going to check distinct atoms only
            // we want to start at the start of a DB atom
            startz = startz - ((startz % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;
            starty = starty - ((starty % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;
            startx = startx - ((startx % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;

            AddAtoms(request, mask, startz, starty, startx, endz, endy, endx, map, ref total_points);
        }

        protected void AddAtoms(SQLUtility.MHDInputRequest request, long mask, int startz, int starty, int startx, int endz, int endy, int endx, Dictionary<long, List<int>> map, ref int total_points)
        {
            // we do not want a request to appear more than once in the list for an atom
            // with the below logic we are going to check distinct atoms only
            // we want to start at the start of a DB atom
            startz = startz - ((startz % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;
            starty = starty - ((starty % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;
            startx = startx - ((startx % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;

            long zindex;

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
                            if (!map.ContainsKey(zindex))
                            {
                                //map[zindex] = new List<int>(pointsPerCubeEstimate);
                                map[zindex] = new List<int>();
                            }
                            //Debug.Assert(!map[zindex].Contains(request.request));
                            map[zindex].Add(request.request);
                            request.numberOfCubes++;
                            total_points++;
                        }
                    }
                }
            }
        }

        public override double[] GetResult(TurbulenceBlob blob, SQLUtility.InputRequest input)
        {
            throw new NotImplementedException();
        }

        public override double[] GetResult(TurbulenceBlob blob, SQLUtility.MHDInputRequest input)
        {
            return CalcSplines(blob, input);
        }

        public override int GetResultSize()
        {
            return result_size;
        }


        /// <summary>
        /// New version of the CalcVelocity function (also used for magnetic field and vector potential)
        /// </summary>
        /// <remarks>
        /// The Lagrangian evaluation function [LagInterpolation.EvaluateOpt] was moved
        /// into the function and some loop unrolling was performed.
        /// </remarks>
        public virtual double[] CalcSplines(TurbulenceBlob blob, SQLUtility.MHDInputRequest input)
        {
            double[] up = new double[result_size]; // Result value for the user

            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None)
            {
                float[] data = blob.data;
                int off0 = blob.GetLocalOffsetMHD(input.cell_z, input.cell_y, input.cell_x, 0);
                for (int j = 0; j < setInfo.Components; j++)
                {
                    up[j] = data[off0 + j];
                }
            }
            else
            {
                // The coefficients are computed only once and cached, so that they don't have to be 
                // recomputed for each partial sum
                if (input.lagInt == null)
                {
                    int dimensions = 3;
                    input.lagInt = new double[dimensions * kernelSize * (derivative + 1)];

                    for (int i = 0; i <= derivative; i++)
                    {
                        ComputeBetas(i, (input.x / setInfo.Dx) - input.cell_x, input.lagInt, (i * dimensions) * kernelSize);
                        ComputeBetas(i, (input.y / setInfo.Dy) - input.cell_y, input.lagInt, (i * dimensions + 1) * kernelSize);
                        ComputeBetas(i, (input.z / setInfo.Dz) - input.cell_z, input.lagInt, (i * dimensions + 2) * kernelSize);
                    }
                }

                // Wrap the coordinates into the grid space
                int x = ((input.cell_x % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
                int y = ((input.cell_y % setInfo.GridResolutionY) + setInfo.GridResolutionY) % setInfo.GridResolutionY;
                int z = ((input.cell_z % setInfo.GridResolutionZ) + setInfo.GridResolutionZ) % setInfo.GridResolutionZ;

                int startz = 0, starty = 0, startx = 0, endz = 0, endy = 0, endx = 0;
                blob.GetSubcubeStart(z - (kernelSize / 2) + 1, y - (kernelSize / 2) + 1, x - (kernelSize / 2) + 1, ref startz, ref starty, ref startx);
                blob.GetSubcubeEnd(z + (kernelSize / 2), y + (kernelSize / 2), x + (kernelSize / 2), ref endz, ref endy, ref endx);

                int iLagIntx = blob.GetRealX - x + startx + kernelSize / 2 - 1;
                if (iLagIntx >= blob.GetGridResolution)
                    iLagIntx -= blob.GetGridResolution;
                else if (iLagIntx < 0)
                    iLagIntx += blob.GetGridResolution;
                int iLagInty = blob.GetRealY - y + starty + kernelSize / 2 - 1;
                if (iLagInty >= blob.GetGridResolution)
                    iLagInty -= blob.GetGridResolution;
                else if (iLagInty < 0)
                    iLagInty += blob.GetGridResolution;
                int iLagIntz = blob.GetRealZ - z + startz + kernelSize / 2 - 1;
                if (iLagIntz >= blob.GetGridResolution)
                    iLagIntz -= blob.GetGridResolution;
                else if (iLagIntz < 0)
                    iLagIntz += blob.GetGridResolution;

                if (derivative == 0)
                {
                    ClacSplineInterpolation(blob, ref input.lagInt, startz, endz, starty, endy, startx, endx, iLagIntz, iLagInty, iLagIntx, ref up);
                }
                else if (derivative == 1)
                {
                    ClacSplineGradient(blob, ref input.lagInt, startz, endz, starty, endy, startx, endx, iLagIntz, iLagInty, iLagIntx, ref up);

                    int dimensions = 3;
                    for (int j = 0; j < setInfo.Components; j++)
                    {
                        up[dimensions * j] = up[dimensions * j] / setInfo.Dx;
                        up[1 + dimensions * j] = up[1 + dimensions * j] / setInfo.Dy;
                        up[2 + dimensions * j] = up[2 + dimensions * j] / setInfo.Dz;
                    }
                }
                else if (derivative == 2)
                {
                    ClacSplineHessian(blob, ref input.lagInt, startz, endz, starty, endy, startx, endx, iLagIntz, iLagInty, iLagIntx, ref up);
                    
                    int hessian_components = 6;
                    for (int j = 0; j < setInfo.Components; j++)
                    {
                        up[j * hessian_components] = up[j * hessian_components] / setInfo.Dx / setInfo.Dx;
                        up[j * hessian_components + 1] = up[j * hessian_components + 1] / setInfo.Dx / setInfo.Dy;
                        up[j * hessian_components + 2] = up[j * hessian_components + 2] / setInfo.Dx / setInfo.Dz;
                        up[j * hessian_components + 3] = up[j * hessian_components + 3] / setInfo.Dy / setInfo.Dy;
                        up[j * hessian_components + 4] = up[j * hessian_components + 4] / setInfo.Dy / setInfo.Dz;
                        up[j * hessian_components + 5] = up[j * hessian_components + 5] / setInfo.Dz / setInfo.Dz;
                    }
                }
            }
            return up;
        }

        protected double GetBeta(double[] poly_val, int kernel_size, int derivative, int coordinate, int position)
        {
            int dimensions = 3;
            return poly_val[(dimensions * derivative + coordinate) * kernel_size + position];
        }

        unsafe protected void ClacSplineInterpolation(TurbulenceBlob blob, ref double[] poly_val, 
            int startz, int endz, int starty, int endy, int startx, int endx,
            int iLagIntz, int iLagInty, int iLagIntx, ref double[] up)
        {
            float[] data = blob.data;
            int off0 = startx * setInfo.Components;
            int x_coordinate = 0;
            int y_coordinate = 1;
            int z_coordinate = 2;

            fixed (double* polyVal = poly_val)
            {
                fixed (float* fdata = data)
                {
                    for (int iz = startz; iz <= endz; iz++)
                    {
                        double[] b = new double[setInfo.Components];
                        int off1 = off0 + iz * blob.GetSide * blob.GetSide * blob.GetComponents;
                        for (int iy = starty; iy <= endy; iy++)
                        {
                            double[] c = new double[setInfo.Components];
                            int off = off1 + iy * blob.GetSide * blob.GetComponents;
                            for (int ix = startx; ix <= endx; ix++)
                            {
                                double x_coeff = GetBeta(poly_val, kernelSize, derivative, x_coordinate, iLagIntx + ix - startx); //polyVal[iLagIntx + ix - startx];
                                for (int j = 0; j < setInfo.Components; j++)
                                {
                                    c[j] += x_coeff * fdata[off + j];
                                }
                                off += setInfo.Components;
                            }
                            double y_coeff = GetBeta(poly_val, kernelSize, derivative, y_coordinate, iLagInty + iy - starty); //polyVal[1 * kernelSize + iLagInty + iy - starty];
                            for (int j = 0; j < setInfo.Components; j++)
                            {
                                b[j] += c[j] * y_coeff;
                            }
                        }
                        double z_coeff = GetBeta(poly_val, kernelSize, derivative, z_coordinate, iLagIntz + iz - startz); //polyVal[2 * kernelSize + iLagIntz + iz - startz];
                        for (int j = 0; j < setInfo.Components; j++)
                        {
                            up[j] += b[j] * z_coeff;
                        }
                    }
                }
            }
        }

        unsafe protected void ClacSplineGradient(TurbulenceBlob blob, ref double[] poly_val,
            int startz, int endz, int starty, int endy, int startx, int endx,
            int iLagIntz, int iLagInty, int iLagIntx, ref double[] up)
        {
            float[] data = blob.data;
            int off0 = startx * setInfo.Components;
            int x_coordinate = 0;
            int y_coordinate = 1;
            int z_coordinate = 2;
            int interpolant = 0;
            int first_derivative = 1;
            int dimensions = 3;

            fixed (double* polyVal = poly_val)
            {
                fixed (float* fdata = data)
                {
                    for (int iz = startz; iz <= endz; iz++)
                    {
                        double[] b = new double[result_size];
                        int off1 = off0 + iz * blob.GetSide * blob.GetSide * blob.GetComponents;
                        for (int iy = starty; iy <= endy; iy++)
                        {
                            double[] c = new double[result_size];
                            int off = off1 + iy * blob.GetSide * blob.GetComponents;
                            for (int ix = startx; ix <= endx; ix++)
                            {
                                double dudx_x_coeff = GetBeta(poly_val, kernelSize, first_derivative, x_coordinate, iLagIntx + ix - startx);
                                // the x beta coefficient for dudy and dudz is the same
                                double dudy_x_coeff = GetBeta(poly_val, kernelSize, interpolant, x_coordinate, iLagIntx + ix - startx);
                                for (int j = 0; j < setInfo.Components; j++)
                                {
                                    // dudx computation
                                    c[j] += dudx_x_coeff * fdata[off + j];
                                    // dudy computation
                                    c[j + dimensions] += dudy_x_coeff * fdata[off + j];
                                    // dudz computation
                                    c[j + 2 * dimensions] += dudy_x_coeff * fdata[off + j];
                                }
                                off += setInfo.Components;
                            }
                            double dudy_y_coeff = GetBeta(poly_val, kernelSize, first_derivative, y_coordinate, iLagInty + iy - starty);
                            // the y beta coefficient for dudx and dudz is the same
                            double dudx_y_coeff = GetBeta(poly_val, kernelSize, interpolant, y_coordinate, iLagInty + iy - starty);
                            for (int j = 0; j < setInfo.Components; j++)
                            {
                                // dudx computation
                                b[j] += dudx_y_coeff * c[j];
                                // dudy computation
                                b[j + dimensions] += dudy_y_coeff * c[j + dimensions];
                                // dudz computation
                                b[j + 2 * dimensions] += dudx_y_coeff * c[j + 2 * dimensions];
                            }
                        }
                        double dudz_z_coeff = GetBeta(poly_val, kernelSize, first_derivative, z_coordinate, iLagIntz + iz - startz);
                        // the z beta coefficient for dudx and dudy is the same
                        double dudx_z_coeff = GetBeta(poly_val, kernelSize, interpolant, z_coordinate, iLagIntz + iz - startz);
                        for (int j = 0; j < setInfo.Components; j++)
                        {
                            // dudx computation
                            up[dimensions * j] += dudx_z_coeff * b[j];
                            // dudy computation
                            up[1 + dimensions * j] += dudx_z_coeff * b[j + dimensions];
                            // dudz computation
                            up[2 + dimensions * j] += dudz_z_coeff * b[j + 2 * dimensions];
                        }
                    }
                }
            }
        }

        unsafe protected void ClacSplineHessian(TurbulenceBlob blob, ref double[] poly_val,
            int startz, int endz, int starty, int endy, int startx, int endx,
            int iLagIntz, int iLagInty, int iLagIntx, ref double[] up)
        {
            float[] data = blob.data;
            int off0 = startx * setInfo.Components;
            int x_coordinate = 0;
            int y_coordinate = 1;
            int z_coordinate = 2;
            int interpolant = 0;
            int first_derivative = 1;
            int second_derivative = 2;
            int hessian_components = 6;

            fixed (double* polyVal = poly_val)
            {
                fixed (float* fdata = data)
                {
                    for (int iz = startz; iz <= endz; iz++)
                    {
                        double[] b = new double[result_size];
                        int off1 = off0 + iz * blob.GetSide * blob.GetSide * blob.GetComponents;
                        for (int iy = starty; iy <= endy; iy++)
                        {
                            double[] c = new double[result_size];
                            int off = off1 + iy * blob.GetSide * blob.GetComponents;
                            for (int ix = startx; ix <= endx; ix++)
                            {
                                double d2udxdx_x_coeff = GetBeta(poly_val, kernelSize, second_derivative, x_coordinate, iLagIntx + ix - startx);
                                // the x beta coefficient for d2udxdy and d2udxdz is the same
                                double d2udxdy_x_coeff = GetBeta(poly_val, kernelSize, first_derivative, x_coordinate, iLagIntx + ix - startx);
                                // the x beta coefficient for d2udydyz, d2udydz and d2udzdz is the same
                                double d2udydz_x_coeff = GetBeta(poly_val, kernelSize, interpolant, x_coordinate, iLagIntx + ix - startx);
                                for (int j = 0; j < setInfo.Components; j++)
                                {
                                    // d2udxdx computation
                                    c[j * hessian_components] += d2udxdx_x_coeff * fdata[off + j];
                                    // d2udxdy computation
                                    c[j * hessian_components + 1] += d2udxdy_x_coeff * fdata[off + j];
                                    // d2udxdz computation
                                    c[j * hessian_components + 2] += d2udxdy_x_coeff * fdata[off + j];
                                    // d2udydy computation
                                    c[j * hessian_components + 3] += d2udydz_x_coeff * fdata[off + j];
                                    // d2udydz computation
                                    c[j * hessian_components + 4] += d2udydz_x_coeff * fdata[off + j];
                                    // d2udzdz computation
                                    c[j * hessian_components + 5] += d2udydz_x_coeff * fdata[off + j];
                                }
                                off += setInfo.Components;
                            }
                            double d2udydy_y_coeff = GetBeta(poly_val, kernelSize, second_derivative, y_coordinate, iLagInty + iy - starty);
                            // the y beta coefficient for d2udxdy and d2udydz is the same
                            double d2udxdy_y_coeff = GetBeta(poly_val, kernelSize, first_derivative, y_coordinate, iLagInty + iy - starty);
                            // the y beta coefficient for d2udxdx, d2udxdz and d2udzdz is the same
                            double d2udxdz_y_coeff = GetBeta(poly_val, kernelSize, interpolant, y_coordinate, iLagInty + iy - starty);
                            for (int j = 0; j < setInfo.Components; j++)
                            {
                                // d2udxdx computation
                                b[j * hessian_components] += d2udxdz_y_coeff * c[j * hessian_components];
                                // d2udxdy computation
                                b[j * hessian_components + 1] += d2udxdy_y_coeff * c[j * hessian_components + 1];
                                // d2udxdz computation
                                b[j * hessian_components + 2] += d2udxdz_y_coeff * c[j * hessian_components + 2];
                                // d2udydy computation
                                b[j * hessian_components + 3] += d2udydy_y_coeff * c[j * hessian_components + 3];
                                // d2udydz computation
                                b[j * hessian_components + 4] += d2udxdy_y_coeff * c[j * hessian_components + 4];
                                // d2udzdz computation
                                b[j * hessian_components + 5] += d2udxdz_y_coeff * c[j * hessian_components + 5];
                            }
                        }
                        double d2udzdz_z_coeff = GetBeta(poly_val, kernelSize, second_derivative, z_coordinate, iLagIntz + iz - startz);
                        // the z beta coefficient for d2udxdz and d2udydz is the same
                        double d2udxdz_z_coeff = GetBeta(poly_val, kernelSize, first_derivative, z_coordinate, iLagIntz + iz - startz);
                        // the z beta coefficient for d2udxdx, d2udxdy and d2udydy is the same
                        double d2udxdy_z_coeff = GetBeta(poly_val, kernelSize, interpolant, z_coordinate, iLagIntz + iz - startz);
                        for (int j = 0; j < setInfo.Components; j++)
                        {
                            // d2udxdx computation
                            up[j * hessian_components] += d2udxdy_z_coeff * b[j * hessian_components];
                            // d2udxdy computation
                            up[j * hessian_components + 1] += d2udxdy_z_coeff * b[j * hessian_components + 1];
                            // d2udxdz computation
                            up[j * hessian_components + 2] += d2udxdz_z_coeff * b[j * hessian_components + 2];
                            // d2udydy computation
                            up[j * hessian_components + 3] += d2udxdy_z_coeff * b[j * hessian_components + 3];
                            // d2udydz computation
                            up[j * hessian_components + 4] += d2udxdz_z_coeff * b[j * hessian_components + 4];
                            // d2udzdz computation
                            up[j * hessian_components + 5] += d2udzdz_z_coeff * b[j * hessian_components + 5];
                        }
                    }
                }
            }
        }

    }

}
