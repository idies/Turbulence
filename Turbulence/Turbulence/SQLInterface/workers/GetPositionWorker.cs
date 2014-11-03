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
    public class GetPositionWorker : Worker
    {
        TurbulenceOptions.TemporalInterpolation temporalInterpolation;
        long full;
        private GetMHDWorker turbulence_worker;

        public GetPositionWorker(TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation)
        {
            this.setInfo = setInfo;
            this.spatialInterp = spatialInterp;
            this.temporalInterpolation = temporalInterpolation;
            this.turbulence_worker = new GetMHDWorker(setInfo, spatialInterp);

            this.full = new Morton3D(0, 0, setInfo.GridResolutionX).Key; // == Morton3D(GridResolution-1,GridResolution-1,GridResolution-1) + 1

            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Lag4)
            { this.kernelSize = 4; }
            else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Lag6)
            { this.kernelSize = 6; }
            else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.Lag8)
            { this.kernelSize = 8; }
            else if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None)
            { this.kernelSize = 0; }
            else
            {
                throw new Exception(String.Format("Invalid Spatial Interpolation Option: {0}", spatialInterp));
            }
        }

        public override SqlMetaData[] GetRecordMetaData()
        {
            return new SqlMetaData[] {
                new SqlMetaData("Req", SqlDbType.Int),
                new SqlMetaData("X", SqlDbType.Real),
                new SqlMetaData("Y", SqlDbType.Real),
                new SqlMetaData("Z", SqlDbType.Real),
                new SqlMetaData("PRE_X", SqlDbType.Real),
                new SqlMetaData("PRE_Y", SqlDbType.Real),
                new SqlMetaData("PRE_Z", SqlDbType.Real),
                new SqlMetaData("VEL_INC_X", SqlDbType.Real),
                new SqlMetaData("VEL_INC_Y", SqlDbType.Real),
                new SqlMetaData("VEL_INC_Z", SqlDbType.Real),
                new SqlMetaData("timestep", SqlDbType.Int),
                new SqlMetaData("time", SqlDbType.Real),
                new SqlMetaData("endTime", SqlDbType.Real),
                new SqlMetaData("dt", SqlDbType.Real),
                new SqlMetaData("compute_predictor", SqlDbType.Bit),
                new SqlMetaData("done", SqlDbType.Bit)
            };
        }

        /// <summary>
        /// Determines the database atoms that overlap the kernel of computation for the given point
        /// </summary>
        /// <remarks>
        /// This methods should only need to be called when spatial interpolation is performed
        /// i.e. when kernelSize != 0
        /// </remarks>
        public void GetAtomsForPoint(SQLUtility.TrackingInputRequest request, long mask, int pointsPerCubeEstimate, Dictionary<SQLUtility.TimestepZindexKey, List<int>> map, ref int total_points)
        {
            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None)
                throw new Exception("GetAtomsForPoint should only be called when spatial interpolation is performed!");

            int X, Y, Z;
            if (request.compute_predictor)
            {
                X = LagInterpolation.CalcNode(request.pos.x, setInfo.Dx);
                Y = LagInterpolation.CalcNode(request.pos.y, setInfo.Dx);
                Z = LagInterpolation.CalcNode(request.pos.z, setInfo.Dx);
            }
            else
            {
                X = LagInterpolation.CalcNode(request.pre_pos.x, setInfo.Dx);
                Y = LagInterpolation.CalcNode(request.pre_pos.y, setInfo.Dx);
                Z = LagInterpolation.CalcNode(request.pre_pos.z, setInfo.Dx);
            }
            // For Lagrange Polynomial interpolation we need a cube of data 

            int startz = Z - kernelSize / 2 + 1, starty = Y - kernelSize / 2 + 1, startx = X - kernelSize / 2 + 1;
            int endz = Z + kernelSize / 2, endy = Y + kernelSize / 2, endx = X + kernelSize / 2;

            // we do not want a request to appear more than once in the list for an atom
            // with the below logic we are going to check distinct atoms only
            // we want to start at the start of a DB atom
            startz = startz - ((startz % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;
            starty = starty - ((starty % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;
            startx = startx - ((startx % setInfo.atomDim) + setInfo.atomDim) % setInfo.atomDim;

            long zindex;
            SQLUtility.TimestepZindexKey key = new SQLUtility.TimestepZindexKey();

            for (int z = startz; z <= endz; z += setInfo.atomDim)
            {
                for (int y = starty; y <= endy; y += setInfo.atomDim)
                {
                    for (int x = startx; x <= endx; x += setInfo.atomDim)
                    {
                        // Wrap the coordinates into the grid space
                        int xi = ((x % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
                        int yi = ((y % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
                        int zi = ((z % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;

                        if (setInfo.PointInRange(xi, yi, zi))
                        {
                            zindex = new Morton3D(zi, yi, xi).Key & mask;
                            key.SetValues(request.timeStep, zindex);
                            if (!map.ContainsKey(key))
                            {
                                //map[zindex] = new List<int>(pointsPerCubeEstimate);
                                map[key] = new List<int>();
                            }
                            //Debug.Assert(!map[zindex].Contains(request.request));
                            map[key].Add(request.request);
                            request.numberOfCubes++;
                            total_points++;
                        }
                        else
                        {
                            request.crossed_boundary = true;
                        }
                    }
                }
            }
        }

        public override float[] GetResult(TurbulenceBlob blob, SQLUtility.InputRequest input)
        {
            throw new NotImplementedException();
        }

        public override float[] GetResult(TurbulenceBlob blob, SQLUtility.MHDInputRequest input)
        {
            throw new NotImplementedException();
        }

        public void GetResult(TurbulenceBlob blob, ref SQLUtility.TrackingInputRequest point, int timestep, int basetime)
        {
            float[] velocity = new float[3];

            if (turbulence_worker == null)
                throw new Exception("turbulence_worker is null!");

            int timestepsForInterpolation;

            float dt1 = Math.Abs(point.endTime - point.time);

            if (temporalInterpolation == TurbulenceOptions.TemporalInterpolation.None)
            {
                timestepsForInterpolation = 1;
            }
            else
            {
                timestepsForInterpolation = 4;

                int timestep0 = basetime - setInfo.TimeInc;
                int timestep1 = basetime;
                int timestep2 = basetime + setInfo.TimeInc;
                int timestep3 = basetime + setInfo.TimeInc * 2;

                float time0 = (timestep0 - setInfo.TimeOff) * setInfo.Dt;
                float time1 = (timestep1 - setInfo.TimeOff) * setInfo.Dt;
                float time2 = (timestep2 - setInfo.TimeOff) * setInfo.Dt;
                float time3 = (timestep3 - setInfo.TimeOff) * setInfo.Dt;

                float delta = time2 - time1;

                if (point.compute_predictor)
                {
                    velocity = turbulence_worker.CalcLagInterpolation(blob, point.pos.x, point.pos.y, point.pos.z, ref point.lagInt);
                    if (dt1 < point.dt)
                    {
                        if (dt1 < 0.00001)
                        {
                            throw new Exception("This shouldn't happen!");
                            
                        }
                        else
                            point.dt = dt1;
                    }
                }
                else
                {
                    velocity = turbulence_worker.CalcLagInterpolation(blob, point.pre_pos.x, point.pre_pos.y, point.pre_pos.z, ref point.lagInt);
                }

                if (timestep == timestep0)
                {
                    point.vel_inc.x += -velocity[0] * point.dt * (point.time - time1) * (1 + (point.time - time1) * (-1 + (point.time - time2) / delta) / delta) / 2 / delta;
                    point.vel_inc.y += -velocity[1] * point.dt * (point.time - time1) * (1 + (point.time - time1) * (-1 + (point.time - time2) / delta) / delta) / 2 / delta;
                    point.vel_inc.z += -velocity[2] * point.dt * (point.time - time1) * (1 + (point.time - time1) * (-1 + (point.time - time2) / delta) / delta) / 2 / delta;
                    //point.result[r] += -result[r] * (time - time1) * (1 + (time - time1) * (-1 + (time - time2) / delta) / delta) / 2 / delta;
                }
                else if (timestep == timestep1)
                {
                    point.vel_inc.x += velocity[0] * point.dt * (1 + ((point.time - time1) * (point.time - time1) * (-2 + 3 * (point.time - time2) / delta) / 2 / delta / delta));
                    point.vel_inc.y += velocity[1] * point.dt * (1 + ((point.time - time1) * (point.time - time1) * (-2 + 3 * (point.time - time2) / delta) / 2 / delta / delta));
                    point.vel_inc.z += velocity[2] * point.dt * (1 + ((point.time - time1) * (point.time - time1) * (-2 + 3 * (point.time - time2) / delta) / 2 / delta / delta));
                    //point.result[r] += result[r] * (1 + ((time - time1) * (time - time1) * (-2 + 3 * (time - time2) / delta) / 2 / delta / delta));
                }
                else if (timestep == timestep2)
                {
                    point.vel_inc.x += velocity[0] * point.dt * (point.time - time1) * (1 + (point.time - time1) * (1 - 3 * (point.time - time2) / delta) / delta) / 2 / delta;
                    point.vel_inc.y += velocity[1] * point.dt * (point.time - time1) * (1 + (point.time - time1) * (1 - 3 * (point.time - time2) / delta) / delta) / 2 / delta;
                    point.vel_inc.z += velocity[2] * point.dt * (point.time - time1) * (1 + (point.time - time1) * (1 - 3 * (point.time - time2) / delta) / delta) / 2 / delta;
                    //point.result[r] += result[r] * (time - time1) * (1 + (time - time1) * (1 - 3 * (time - time2) / delta) / delta) / 2 / delta;
                }
                else if (timestep == timestep3)
                {
                    point.vel_inc.x += velocity[0] * point.dt * (point.time - time1) * (point.time - time1) * (point.time - time2) / 2 / delta / delta / delta;
                    point.vel_inc.y += velocity[1] * point.dt * (point.time - time1) * (point.time - time1) * (point.time - time2) / 2 / delta / delta / delta;
                    point.vel_inc.z += velocity[2] * point.dt * (point.time - time1) * (point.time - time1) * (point.time - time2) / 2 / delta / delta / delta;
                    //point.result[r] += result[r] * (time - time1) * (time - time1) * (time - time2) / 2 / delta / delta / delta;
                }
            }

            // Check if we are done with this point
            if (point.cubesRead == timestepsForInterpolation * point.numberOfCubes)
            {
                // we are done with this point, so the velocity increment has been computed
                // and we can compute the predictor or corrector position
                // we have to do this once all velcity increments have been summed together
                // as the positions should be added only once
                if (point.compute_predictor)
                {
                    point.pre_pos.x = point.pos.x + point.vel_inc.x;
                    point.pre_pos.y = point.pos.y + point.vel_inc.y;
                    point.pre_pos.z = point.pos.z + point.vel_inc.z;
                    point.time += point.dt;
                    if (temporalInterpolation == TurbulenceOptions.TemporalInterpolation.None)
                        point.timeStep = SQLUtility.GetNearestTimestep(point.time, setInfo);
                    else
                        point.timeStep = SQLUtility.GetFlooredTimestep(point.time, setInfo);
                    int X, Y, Z;
                    if (kernelSize == 0)
                    {
                        X = LagInterpolation.CalcNodeWithRound(point.pre_pos.x, setInfo.DxFloat);
                        Y = LagInterpolation.CalcNodeWithRound(point.pre_pos.y, setInfo.DxFloat);
                        Z = LagInterpolation.CalcNodeWithRound(point.pre_pos.z, setInfo.DxFloat);
                    }
                    else
                    {
                        X = LagInterpolation.CalcNode(point.pre_pos.x, setInfo.DxFloat);
                        Y = LagInterpolation.CalcNode(point.pre_pos.y, setInfo.DxFloat);
                        Z = LagInterpolation.CalcNode(point.pre_pos.z, setInfo.DxFloat);
                    }
                    point.zindex = new Morton3D(Z, Y, X);
                    point.compute_predictor = false;
                }
                else
                {
                    point.pos.x = (point.pos.x + point.pre_pos.x + point.vel_inc.x) * 0.5f;
                    point.pos.y = (point.pos.y + point.pre_pos.y + point.vel_inc.y) * 0.5f;
                    point.pos.z = (point.pos.z + point.pre_pos.z + point.vel_inc.z) * 0.5f;
                    point.compute_predictor = true;

                    // Check if we are done
                    if (dt1 < 0.00001)
                    {
                        point.done = true;
                        return;
                    }

                    int X, Y, Z;
                    if (kernelSize == 0)
                    {
                        X = LagInterpolation.CalcNodeWithRound(point.pos.x, setInfo.DxFloat);
                        Y = LagInterpolation.CalcNodeWithRound(point.pos.y, setInfo.DxFloat);
                        Z = LagInterpolation.CalcNodeWithRound(point.pos.z, setInfo.DxFloat);
                    }
                    else
                    {
                        X = LagInterpolation.CalcNode(point.pos.x, setInfo.DxFloat);
                        Y = LagInterpolation.CalcNode(point.pos.y, setInfo.DxFloat);
                        Z = LagInterpolation.CalcNode(point.pos.z, setInfo.DxFloat);
                    }
                    point.zindex = new Morton3D(Z, Y, X);
                }
            }
        }

        public override int GetResultSize()
        {
            return 15;
        }
        public int PickServer(long key, double timestep, int serverCount)
        {
            if (serverCount == 6)
            {
                if (timestep <= 3770.0)
                {
                    long pernode = full / 4;  // space assigned to each node
                    return (int)(key / pernode);
                }
                else
                {
                    long pernode = full / 2;  // space assigned to each node
                    return (int)(key / pernode) + 4;
                }
            }
            else if (serverCount == 1)
            {
                return 0;
            }
            else
            {
                throw new Exception("The number of servers is not supported for particle tracking!");
            }
        }
        public int GetIntLoc(double yp)
        {
            bool round = spatialInterp == TurbulenceOptions.SpatialInterpolation.None ? true : false;

            //double dx = 2.0F * (double)Math.PI / 1024.0f;
            double dx = 1.0 / (double)setInfo.GridResolutionX;
            int x;
            if (round)
            {
                x = Turbulence.SciLib.LagInterpolation.CalcNodeWithRound(yp, dx);
                //x = (int)(Math.Round(yp / dx));
                //return (((int)Math.Round(x / (2.0F * (double)Math.PI / (double)DIM)) % DIM) + DIM) % DIM;
            }
            else
            {
                x = Turbulence.SciLib.LagInterpolation.CalcNode(yp, dx);
                //x = (int)(Math.Floor(yp / dx));
                //return (((int)Math.Floor(x / (2.0F * (double)Math.PI / (double)DIM)) % DIM) + DIM) % DIM;
            }

            return ((x % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
        }

        //public bool InsideSameBlob(double time, long zindex, int blob_timestep, long blob_key)
        //{
        //    int timestep_int;
        //    if (temporalInterpolation == TurbulenceOptions.TemporalInterpolation.None)
        //        timestep_int = SQLUtility.GetNearestTimestep(time, setInfo);
        //    else
        //        timestep_int = SQLUtility.GetFlooredTimestep(time, setInfo);
        //    if (timestep_int != blob_timestep)
        //        return false;

        //    if (zindex != blob_key)
        //        return false;
        //    return true;
        //}

        /// <summary>
        /// Create a DataTable with the correct schema for particle tracking 
        /// for input to the database nodes.
        /// </summary>
        /// <returns>DataTable</returns>
        public DataTable createTrackingInputDataTable(string tableName)
        {
            DataTable dt = new DataTable(tableName);

            DataColumn reqseq = new DataColumn("reqseq");
            reqseq.DataType = typeof(int);
            dt.Columns.Add(reqseq);

            DataColumn timestep = new DataColumn("timestep");
            timestep.DataType = typeof(int);
            dt.Columns.Add(timestep);

            DataColumn zindex = new DataColumn("zindex");
            zindex.DataType = typeof(long);
            dt.Columns.Add(zindex);

            DataColumn x = new DataColumn("x");
            x.DataType = typeof(double);
            dt.Columns.Add(x);

            DataColumn y = new DataColumn("y");
            y.DataType = typeof(double);
            dt.Columns.Add(y);

            DataColumn z = new DataColumn("z");
            z.DataType = typeof(double);
            dt.Columns.Add(z);

            DataColumn pre_x = new DataColumn("pre_x");
            pre_x.DataType = typeof(double);
            dt.Columns.Add(pre_x);

            DataColumn pre_y = new DataColumn("pre_y");
            pre_y.DataType = typeof(double);
            dt.Columns.Add(pre_y);

            DataColumn pre_z = new DataColumn("pre_z");
            pre_z.DataType = typeof(double);
            dt.Columns.Add(pre_z);

            DataColumn vel_x = new DataColumn("vel_x");
            vel_x.DataType = typeof(double);
            dt.Columns.Add(vel_x);

            DataColumn vel_y = new DataColumn("vel_y");
            vel_y.DataType = typeof(double);
            dt.Columns.Add(vel_y);

            DataColumn vel_z = new DataColumn("vel_z");
            vel_z.DataType = typeof(double);
            dt.Columns.Add(vel_z);

            DataColumn time = new DataColumn("time");
            time.DataType = typeof(double);
            dt.Columns.Add(time);

            DataColumn endTime = new DataColumn("endTime");
            endTime.DataType = typeof(double);
            dt.Columns.Add(endTime);

            DataColumn flag = new DataColumn("flag");
            flag.DataType = typeof(bool);
            dt.Columns.Add(flag);

            DataColumn done = new DataColumn("done");
            done.DataType = typeof(bool);
            dt.Columns.Add(done);

            dt.BeginLoadData();

            return dt;
        }

    }

}
