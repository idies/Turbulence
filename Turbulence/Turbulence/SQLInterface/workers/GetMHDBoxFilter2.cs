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
    public class GetMHDBoxFilter2 : Worker
    {
        TurbulenceOptions.SpatialInterpolation spatialOpt;
        
        float[] lagDenominator = null;
        int filter_width;

        public GetMHDBoxFilter2(TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialOpt,
            float filterwidth)
        {
            this.setInfo = setInfo;
            this.spatialOpt = spatialOpt;
            int fw = (int)Math.Round(filterwidth / setInfo.dx);
            this.filter_width = fw;
            this.kernelSize = filter_width;
        }

        public override SqlMetaData[] GetRecordMetaData()
        {
            return new SqlMetaData[] {
                new SqlMetaData("Req", SqlDbType.Int),
                new SqlMetaData("X", SqlDbType.Real),
                new SqlMetaData("Y", SqlDbType.Real),
                new SqlMetaData("Z", SqlDbType.Real),
                new SqlMetaData("Cubes Read", SqlDbType.Int)};
        }

        //public override void GetAtomsForPoint(float xp, float yp, float zp, long mask, HashSet<long> atoms)
        //{
        //    long zindex;
        //    int X, Y, Z;
        //    X = LagInterpolation.CalcNode(xp, setInfo.dx);
        //    Y = LagInterpolation.CalcNode(yp, setInfo.dx);
        //    Z = LagInterpolation.CalcNode(zp, setInfo.dx);

        //    //For each workload point we need to visit 8 data points

        //    int startz = Z - filter_width / 2, starty = Y - filter_width / 2, startx = X - filter_width / 2;
        //    int endz = Z + filter_width / 2, endy = Y + filter_width / 2, endx = X + filter_width / 2;
        //    for (int z = startz; z <= endz; z += endz - startz)
        //    {
        //        for (int y = starty; y <= endy; y += endy - starty)
        //        {
        //            for (int x = startx; x <= endx; x += endx - startx)
        //            {
        //                // Wrap the coordinates into the grid space
        //                int xi = ((x % setInfo.GridResolution) + setInfo.GridResolution) % setInfo.GridResolution;
        //                int yi = ((y % setInfo.GridResolution) + setInfo.GridResolution) % setInfo.GridResolution;
        //                int zi = ((z % setInfo.GridResolution) + setInfo.GridResolution) % setInfo.GridResolution;

        //                zindex = new Morton3D(zi, yi, xi).Key & mask;
        //                if (!atoms.Contains(zindex))
        //                {
        //                    atoms.Add(zindex);
        //                }
        //            }
        //        }
        //    }
        //}

        public override void GetAtomsForPoint(SQLUtility.MHDInputRequest request, long mask, int pointsPerCubeEstimate, Dictionary<long, List<int>> map, ref int total_points)
        {
            long zindex;
            int X, Y, Z;
            X = LagInterpolation.CalcNode(request.x, setInfo.dx);
            Y = LagInterpolation.CalcNode(request.y, setInfo.dx);
            Z = LagInterpolation.CalcNode(request.z, setInfo.dx);

            //For each workload point we need to visit 8 data points

            int startz = Z - filter_width / 2 - 1, starty = Y - filter_width / 2 - 1, startx = X - filter_width / 2 - 1;
            int endz = Z + filter_width / 2, endy = Y + filter_width / 2, endx = X + filter_width / 2;
            
            HashSet<long> atoms = new HashSet<long>(); //NOTE: HashSet requires .Net 3.5
            for (int z = startz; z <= endz; z += endz - startz)
            {
                for (int y = starty; y <= endy; y += endy - starty)
                {
                    for (int x = startx; x <= endx; x += endx - startx)
                    {
                        // Wrap the coordinates into the grid space
                        int xi = ((x % setInfo.GridResolution) + setInfo.GridResolution) % setInfo.GridResolution;
                        int yi = ((y % setInfo.GridResolution) + setInfo.GridResolution) % setInfo.GridResolution;
                        int zi = ((z % setInfo.GridResolution) + setInfo.GridResolution) % setInfo.GridResolution;
                        
                        if (setInfo.PointInRange(xi, yi, zi))
                        {
                            zindex = new Morton3D(zi, yi, xi).Key & mask;
                            if (!atoms.Contains(zindex))
                            {
                                atoms.Add(zindex);
                            }
                        }
                    }
                }
            }
            
            foreach (long atom in atoms)
            {
                if (!map.ContainsKey(atom))
                {
                    //map[atom] = new List<int>(pointsPerCubeEstimate);
                    map[atom] = new List<int>();
                }

                map[atom].Add(request.request);
                request.numberOfCubes++;
                total_points++;
            }
        }

        public override float[] GetResult(TurbulenceBlob blob, SQLUtility.InputRequest input)
        {
            throw new NotImplementedException();
        }

        public override float[] GetResult(TurbulenceBlob blob, SQLUtility.MHDInputRequest input)
        {
            float xp = input.x;
            float yp = input.y;
            float zp = input.z;
            return CalcBoxFilter(blob, xp, yp, zp, input);
        }

        public override int GetResultSize()
        {
            return 3;
        }


        /// <summary>
        /// New version of the CalcVelocity function (also used for magnetic field and vector potential)
        /// </summary>
        /// <remarks>
        /// The Lagrangian evaluation function [LagInterpolation.EvaluateOpt] was moved
        /// into the function and some loop unrolling was performed.
        /// </remarks>
        unsafe public float[] CalcBoxFilter(TurbulenceBlob blob, float xp, float yp, float zp, SQLUtility.MHDInputRequest input)
        {
            float[] up = new float[3]; // Result value for the user

            int X = LagInterpolation.CalcNodeWithRound(xp, setInfo.dx);
            int Y = LagInterpolation.CalcNodeWithRound(yp, setInfo.dx);
            int Z = LagInterpolation.CalcNodeWithRound(zp, setInfo.dx);
            
            float[] data = blob.data;
            //int startz = 0, starty = 0, startx = 0, endz = 0, endy = 0, endx = 0;
            //blob.GetSubcubeStart(z - (filter_width / 2), y - (filter_width / 2), x - (filter_width / 2), filter_width, ref startz, ref starty, ref startx);
            //blob.GetSubcubeEnd(z + (filter_width / 2), y + (filter_width / 2), x + (filter_width / 2), filter_width, ref endz, ref endy, ref endx);
            //int off0 = startx * blob.GetComponents;

            int startz = Z - filter_width / 2 - 1, starty = Y - filter_width / 2 - 1, startx = X - filter_width / 2 - 1;
            int endz = Z + filter_width / 2, endy = Y + filter_width / 2, endx = X + filter_width / 2;         
            // Wrap the coordinates into the grid space
            startz = ((startz % setInfo.GridResolution) + setInfo.GridResolution) % setInfo.GridResolution;
            starty = ((starty % setInfo.GridResolution) + setInfo.GridResolution) % setInfo.GridResolution;
            startx = ((startx % setInfo.GridResolution) + setInfo.GridResolution) % setInfo.GridResolution;
            endz = ((endz % setInfo.GridResolution) + setInfo.GridResolution) % setInfo.GridResolution;
            endy = ((endy % setInfo.GridResolution) + setInfo.GridResolution) % setInfo.GridResolution;
            endx = ((endx % setInfo.GridResolution) + setInfo.GridResolution) % setInfo.GridResolution;

            double c = Filtering.FilteringCoefficients(filter_width);
            double c1 = 0.0, c2 = 0.0, c3 = 0.0;

            bool throwEx = true;

            if (startz >= blob.GetBaseZ && startz <= blob.GetBaseZ + setInfo.atomDim)
            {
                if (starty >= blob.GetBaseY && starty <= blob.GetBaseY + setInfo.atomDim)
                {
                    if (startx >= blob.GetBaseX && startx <= blob.GetBaseX + setInfo.atomDim)
                    {
                        int off0 = blob.GetLocalOffsetMHD(startz, starty, startx, 0);
                        c1 -= data[off0] * (startx + 1) * (starty + 1) * (startz + 1);
                        c2 -= data[off0 + 1] * (startx + 1) * (starty + 1) * (startz + 1);
                        c3 -= data[off0 + 2] * (startx + 1) * (starty + 1) * (startz + 1);
                        throwEx = false;
                    }
                    else if (endx >= blob.GetBaseX && endx <= blob.GetBaseX + setInfo.atomDim)
                    {
                        int off0 = blob.GetLocalOffsetMHD(startz, starty, endx, 0);
                        c1 += data[off0] * (endx + 1) * (starty + 1) * (startz + 1);
                        c2 += data[off0 + 1] * (endx + 1) * (starty + 1) * (startz + 1);
                        c3 += data[off0 + 2] * (endx + 1) * (starty + 1) * (startz + 1);
                        throwEx = false;
                    }
                }
                else if (endy >= blob.GetBaseY && endy <= blob.GetBaseY + setInfo.atomDim)
                {
                    if (startx >= blob.GetBaseX && startx <= blob.GetBaseX + setInfo.atomDim)
                    {
                        int off0 = blob.GetLocalOffsetMHD(startz, endy, startx, 0);
                        c1 += data[off0] * (startx + 1) * (endy + 1) * (startz + 1);
                        c2 += data[off0 + 1] * (startx + 1) * (endy + 1) * (startz + 1);
                        c3 += data[off0 + 2] * (startx + 1) * (endy + 1) * (startz + 1);
                        throwEx = false;
                    }
                    else if (endx >= blob.GetBaseX && endx <= blob.GetBaseX + setInfo.atomDim)
                    {
                        int off0 = blob.GetLocalOffsetMHD(startz, endy, endx, 0);
                        c1 -= data[off0] * (startz + 1) * (endy + 1) * (endx + 1);
                        c2 -= data[off0 + 1] * (startz + 1) * (endy + 1) * (endx + 1);
                        c3 -= data[off0 + 2] * (startz + 1) * (endy + 1) * (endx + 1);
                        throwEx = false;
                    }
                }
            }
            else if (endz >= blob.GetBaseZ && endz <= blob.GetBaseZ + setInfo.atomDim)
            {
                if (starty >= blob.GetBaseY && starty <= blob.GetBaseY + setInfo.atomDim)
                {
                    if (startx >= blob.GetBaseX && startx <= blob.GetBaseX + setInfo.atomDim)
                    {
                        int off0 = blob.GetLocalOffsetMHD(endz, starty, startx, 0);
                        c1 += data[off0] * (endz + 1) * (starty + 1) * (startx + 1);
                        c2 += data[off0 + 1] * (endz + 1) * (starty + 1) * (startx + 1);
                        c3 += data[off0 + 2] * (endz + 1) * (starty + 1) * (startx + 1);
                        throwEx = false;
                    }
                    else if (endx >= blob.GetBaseX && endx <= blob.GetBaseX + setInfo.atomDim)
                    {
                        int off0 = blob.GetLocalOffsetMHD(endz, starty, endx, 0);
                        c1 -= data[off0] * (endz + 1) * (starty + 1) * (endx + 1);
                        c2 -= data[off0 + 1] * (endz + 1) * (starty + 1) * (endx + 1);
                        c3 -= data[off0 + 2] * (endz + 1) * (starty + 1) * (endx + 1);
                        throwEx = false;
                    }
                }
                else if (endy >= blob.GetBaseY && endy <= blob.GetBaseY + setInfo.atomDim)
                {
                    if (startx >= blob.GetBaseX && startx <= blob.GetBaseX + setInfo.atomDim)
                    {
                        int off0 = blob.GetLocalOffsetMHD(endz, endy, startx, 0);
                        c1 -= data[off0] * (endz + 1) * (endy + 1) * (startx + 1);
                        c2 -= data[off0 + 1] * (endz + 1) * (endy + 1) * (startx + 1);
                        c3 -= data[off0 + 2] * (endz + 1) * (endy + 1) * (startx + 1);
                        throwEx = false;
                    }
                    else if (endx >= blob.GetBaseX && endx <= blob.GetBaseX + setInfo.atomDim)
                    {
                        int off0 = blob.GetLocalOffsetMHD(endz, endy, endx, 0);
                        c1 += data[off0] * (endz + 1) * (endy + 1) * (endx + 1);
                        c2 += data[off0 + 1] * (endz + 1) * (endy + 1) * (endx + 1);
                        c3 += data[off0 + 2] * (endz + 1) * (endy + 1) * (endx + 1);
                        throwEx = false;
                    }
                }
            }

            if (throwEx)
            {
                throw new Exception(String.Format("Something went wrong: Atom [{0}] was retrieved for point, but none of the data points needed are in it!\n" + 
                    "Input point is[{1},{2},{3}]: ",blob.ToString(), Z, Y, X));
            }
            
            // we check wether each of the 8 data points needed are contained in the given data atom
            //fixed (float* lagint = input.lagInt, fdata = data)
            //{
            //    for (int z = startz; z <= endz; z += endz - startz)
            //    {
            //        for (int y = starty; y <= endy; y += endy - starty)
            //        {
            //            for (int x = startx; x <= endx; x += endx - startx)
            //            {
            //            }
            //        }
            //    }
            //}

            up[0] = (float)c1;
            up[1] = (float)c2;
            up[2] = (float)c3;
            
            return up;
        }

    }

}
