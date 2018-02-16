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
    public class GetChannelPositionWorker : Worker
    {
        public const int TIMESTEPS_TO_READ_NO_INTERPOLATION = 1;
        public const int TIMESTEPS_TO_READ_WITH_INTERPOLATION = 4;
        public bool periodicX;
        public bool periodicY;
        public bool periodicZ;

        TurbulenceOptions.TemporalInterpolation temporalInterpolation;
        long full;
        private GetChannelVelocity turbulence_worker;

        public GetChannelPositionWorker(string dataset, TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation,
            SqlConnection conn)
        {
            if (dataset.Contains("channel"))
            {
                periodicX = true;
                periodicY = false;
                periodicZ = true;
            }
            else if (dataset.Contains("bl_zaki"))
            {
                periodicX = false;
                periodicY = false;
                periodicZ = true;
            }

            this.setInfo = setInfo;
            this.spatialInterp = spatialInterp;
            this.temporalInterpolation = temporalInterpolation;
            this.turbulence_worker = new GetChannelVelocity(dataset, setInfo, spatialInterp, conn);

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

        public override double[] GetResult(TurbulenceBlob blob, SQLUtility.InputRequest input)
        {
            throw new NotImplementedException();
        }

        public override double[] GetResult(TurbulenceBlob blob, SQLUtility.MHDInputRequest input)
        {
            throw new NotImplementedException();
        }

        public void GetResult(TurbulenceBlob blob, ref SQLUtility.TrackingInputRequest point, int timestepRead, int basetime, float time, float endTime, float dt, ref int nextTimeStep, ref float nextTime, ref float priordt, double[] grid_points_y, string database)
        {
            double[] velocity = new double[3];

            if (turbulence_worker == null)
                throw new Exception("turbulence_worker is null!");

            int timestepsForInterpolation;

            float dt1 = Math.Abs(endTime - time);


            if (temporalInterpolation == TurbulenceOptions.TemporalInterpolation.None)
            {
                timestepsForInterpolation = TIMESTEPS_TO_READ_NO_INTERPOLATION;
            }
            else
            {
                timestepsForInterpolation = TIMESTEPS_TO_READ_WITH_INTERPOLATION;

                int timestep0 = basetime - setInfo.TimeInc;
                int timestep1 = basetime;
                int timestep2 = basetime + setInfo.TimeInc;
                int timestep3 = basetime + setInfo.TimeInc * 2;

                //float time0 = (timestep0 - setInfo.TimeOff) * setInfo.Dt;
                float time1 = (timestep1 - setInfo.TimeOff) * setInfo.Dt;
                float time2 = (timestep2 - setInfo.TimeOff) * setInfo.Dt;
                //float time3 = (timestep3 - setInfo.TimeOff) * setInfo.Dt;

                float delta = time2 - time1;

                if (point.compute_predictor)
                {
                    velocity = turbulence_worker.CalcLagInterpolation(blob, point.pos.x, point.pos.y, point.pos.z, ref point.lagInt);
                    //throw new Exception(velocity[0].ToString());

                    if (dt > 0)
                    {
                        if (dt1 < dt)
                        {
                            if (dt1 < 0.00001)
                            {
                                throw new Exception("This shouldn't happen!");

                            }
                            else
                                dt = dt1;
                        }
                    }
                    else
                    {
                        if (dt1 < -dt)
                        {
                            if (dt1 < 0.00001)
                            {
                                throw new Exception("This shouldn't happen!");

                            }
                            else
                                dt = -dt1;
                        }
                    }
                    priordt = dt;
                }
                else
                {
                    velocity = turbulence_worker.CalcLagInterpolation(blob, point.pre_pos.x, point.pre_pos.y, point.pre_pos.z, ref point.lagInt);
                    dt = priordt;
                }

                // PJ 2015: subtract 0.45 from x velocity due to moving grid (we track based on grid location, then convert at end)
                //velocity[0] -= 0.45f; //wrong, cause this gets called multiple times when spatial interpolation is involved

                if (timestepRead == timestep0)
                {
                    point.vel_inc.x += -(float)velocity[0] * dt * (time - time1) * (1 + (time - time1) * (-1 + (time - time2) / delta) / delta) / 2 / delta;
                    point.vel_inc.y += -(float)velocity[1] * dt * (time - time1) * (1 + (time - time1) * (-1 + (time - time2) / delta) / delta) / 2 / delta;
                    point.vel_inc.z += -(float)velocity[2] * dt * (time - time1) * (1 + (time - time1) * (-1 + (time - time2) / delta) / delta) / 2 / delta;
                    //point.result[r] += -result[r] * (time - time1) * (1 + (time - time1) * (-1 + (time - time2) / delta) / delta) / 2 / delta;
                }
                else if (timestepRead == timestep1)
                {
                    point.vel_inc.x += (float)velocity[0] * dt * (1 + ((time - time1) * (time - time1) * (-2 + 3 * (time - time2) / delta) / 2 / delta / delta));
                    point.vel_inc.y += (float)velocity[1] * dt * (1 + ((time - time1) * (time - time1) * (-2 + 3 * (time - time2) / delta) / 2 / delta / delta));
                    point.vel_inc.z += (float)velocity[2] * dt * (1 + ((time - time1) * (time - time1) * (-2 + 3 * (time - time2) / delta) / 2 / delta / delta));
                    //point.result[r] += result[r] * (1 + ((time - time1) * (time - time1) * (-2 + 3 * (time - time2) / delta) / 2 / delta / delta));
                }
                else if (timestepRead == timestep2)
                {
                    point.vel_inc.x += (float)velocity[0] * dt * (time - time1) * (1 + (time - time1) * (1 - 3 * (time - time2) / delta) / delta) / 2 / delta;
                    point.vel_inc.y += (float)velocity[1] * dt * (time - time1) * (1 + (time - time1) * (1 - 3 * (time - time2) / delta) / delta) / 2 / delta;
                    point.vel_inc.z += (float)velocity[2] * dt * (time - time1) * (1 + (time - time1) * (1 - 3 * (time - time2) / delta) / delta) / 2 / delta;
                    //point.result[r] += result[r] * (time - time1) * (1 + (time - time1) * (1 - 3 * (time - time2) / delta) / delta) / 2 / delta;
                }
                else if (timestepRead == timestep3)
                {
                    point.vel_inc.x += (float)velocity[0] * dt * (time - time1) * (time - time1) * (time - time2) / 2 / delta / delta / delta;
                    point.vel_inc.y += (float)velocity[1] * dt * (time - time1) * (time - time1) * (time - time2) / 2 / delta / delta / delta;
                    point.vel_inc.z += (float)velocity[2] * dt * (time - time1) * (time - time1) * (time - time2) / 2 / delta / delta / delta;
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
                    //point.pre_pos.x = point.pos.x + point.vel_inc.x;
                    //point.pre_pos.y = point.pos.y + point.vel_inc.y;
                    //point.pre_pos.z = point.pos.z + point.vel_inc.z;

                    // When tracking, grid->phys correction, subtract grid velocity
                    if (database.Contains("channel"))
                    {
                        point.vel_inc.x = point.vel_inc.x - 0.45f * dt;
                    }
                    // For channel predictor, if it goes out of domain, stick it to the wall
                    point.pre_pos.x = point.pos.x + point.vel_inc.x;                     
                    point.pre_pos.y = point.pos.y + point.vel_inc.y;
                    point.pre_pos.z = point.pos.z + point.vel_inc.z;

                    if (!periodicX)
                    {
                        point.pre_pos.x = Math.Max(Math.Min(point.pos.x, (float)setInfo.Dx * (setInfo.GridResolutionX - 1)), 0);
                    }
                    if (!periodicY)
                    {
                        point.pre_pos.y = Math.Max(Math.Min(point.pos.y, (float)grid_points_y[grid_points_y.Length-1]), (float)grid_points_y[0]);
                    }
                    if (!periodicZ)
                    {
                        point.pre_pos.z = Math.Max(Math.Min(point.pos.z, (float)setInfo.Dz * (setInfo.GridResolutionZ - 1)), 0);
                    }

                    // For channel flow, if predictor is out of domain, throw Exception
                    /*
                    if (point.pos.y > 1.0 || point.pos.y < -0.999999)
                    {
                        bool condition = point.pos.y < -1.000000000000000000000000001;
                        throw new Exception("Particle left domain on predictor step!\nx:"
                            + (point.pos.x + 0.45 * endTime).ToString() + "y:" + point.pos.y.ToString() + "z:" + point.pos.z.ToString()
                            + "\nCondition:" + condition.ToString() + "\nConnection:" + point.numberOfCubes.ToString()
                            + "\nZ-index:" + point.zindex.ToString()
                            + "\nx:" + (point.pre_pos.x + 0.45 * endTime).ToString() + "y:" + point.pre_pos.y.ToString() + "z:" + point.pre_pos.z.ToString()
                            + "\ndvx:" + (point.vel_inc.x + 0.45 * endTime).ToString() + "dvy:" + point.vel_inc.y.ToString() + "dvz:" + point.vel_inc.z.ToString()
                            + "\nvx:" + velocity[0].ToString() + "vy:" + velocity[1].ToString() + "vz:" + velocity[2].ToString());
                    }
                    */

                    int X = setInfo.CalcNodeX(point.pre_pos.x, spatialInterp);
                    int Y = setInfo.CalcNodeY(point.pre_pos.y, spatialInterp);
                    int Z = setInfo.CalcNodeZ(point.pre_pos.z, spatialInterp);
                    //if (kernelSize == 0)
                    //{
                    //X = LagInterpolation.CalcNodeWithRound(point.pre_pos.x, setInfo.Dx);
                    //Y = LagInterpolation.CalcNodeWithRound(point.pre_pos.y, setInfo.Dy);
                    //Z = LagInterpolation.CalcNodeWithRound(point.pre_pos.z, setInfo.Dz);
                    //}
                    //else
                    //{
                    //X = LagInterpolation.CalcNode(point.pre_pos.x, setInfo.Dx);
                    //Y = LagInterpolation.CalcNode(point.pre_pos.y, setInfo.Dy);
                    //Z = LagInterpolation.CalcNode(point.pre_pos.z, setInfo.Dz);
                    //}
                    /* Fix for wrap around */
                    X = ((X % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;                   
                    Z = ((Z % setInfo.GridResolutionZ) + setInfo.GridResolutionZ) % setInfo.GridResolutionZ;

                    point.zindex = new Morton3D(Z, Y, X);
                    point.compute_predictor = false;

                    nextTime = time + dt;
                    if (temporalInterpolation == TurbulenceOptions.TemporalInterpolation.None)
                        nextTimeStep = SQLUtility.GetNearestTimestep(nextTime, setInfo);
                    else
                        nextTimeStep = SQLUtility.GetFlooredTimestep(nextTime, setInfo);
                }
                else
                {
                    // When tracking, grid->phys correction, subtract grid velocity
                    if (database.Contains("channel"))
                    {
                        point.vel_inc.x = point.vel_inc.x - 0.45f * dt;
                    }
                    // Corrector; update positions
                    point.pos.x = (point.pos.x + point.pre_pos.x + point.vel_inc.x) * 0.5f;
                    point.pos.y = (point.pos.y + point.pre_pos.y + point.vel_inc.y) * 0.5f;
                    point.pos.z = (point.pos.z + point.pre_pos.z + point.vel_inc.z) * 0.5f;

                    if (!periodicX && (point.pos.x > setInfo.Dx * (setInfo.GridResolutionX - 1) || point.pos.x < 0))
                    {
                        bool condition = point.pos.x < 0;
                        throw new Exception("Particle left domain on corrector step!\nx:"
                            + (point.pos.x + 0.45 * endTime).ToString() + "y:" + point.pos.y.ToString() + "z:" + point.pos.z.ToString()
                            + "\nCondition:" + condition.ToString() + "\nConnection:" + point.numberOfCubes.ToString()
                            + "\nZ-index:" + point.zindex.ToString()
                            + "\nx:" + (point.pre_pos.x + 0.45 * endTime).ToString() + "y:" + point.pre_pos.y.ToString() + "z:" + point.pre_pos.z.ToString()
                            + "\ndvx:" + (point.vel_inc.x + 0.45 * endTime).ToString() + "dvy:" + point.vel_inc.y.ToString() + "dvz:" + point.vel_inc.z.ToString()
                            + "\nvx:" + velocity[0].ToString() + "vy:" + velocity[1].ToString() + "vz:" + velocity[2].ToString()
                            + "\nfull server:" + this.full.ToString());
                    }
                    // For channel flow, if corrector is out of domain, throw Exception
                    if (!periodicY && (point.pos.y > grid_points_y[grid_points_y.Length-1] || point.pos.y < grid_points_y[0]))
                    {
                        bool condition = point.pos.y < grid_points_y[0];
                        throw new Exception("Particle left domain on corrector step!\nx:"
                            +(point.pos.x+0.45*endTime).ToString()+"y:"+point.pos.y.ToString()+"z:"+point.pos.z.ToString()
                            +"\nCondition:"+condition.ToString()+"\nConnection:"+point.numberOfCubes.ToString()
                            +"\nZ-index:"+point.zindex.ToString()
                            + "\nx:"+(point.pre_pos.x + 0.45 * endTime).ToString() + "y:" + point.pre_pos.y.ToString() + "z:" + point.pre_pos.z.ToString()
                            +"\ndvx:"+(point.vel_inc.x + 0.45 * endTime).ToString()+"dvy:" + point.vel_inc.y.ToString()+"dvz:"+point.vel_inc.z.ToString()
                            + "\nvx:" + velocity[0].ToString() + "vy:" + velocity[1].ToString() + "vz:" + velocity[2].ToString()
                            + "\nfull server:" + this.full.ToString());
                    }
                    if (!periodicZ && (point.pos.z > setInfo.Dz * (setInfo.GridResolutionZ - 1) || point.pos.z < 0))
                    {
                        bool condition = point.pos.z < 0;
                        throw new Exception("Particle left domain on corrector step!\nx:"
                            + (point.pos.x + 0.45 * endTime).ToString() + "y:" + point.pos.y.ToString() + "z:" + point.pos.z.ToString()
                            + "\nCondition:" + condition.ToString() + "\nConnection:" + point.numberOfCubes.ToString()
                            + "\nZ-index:" + point.zindex.ToString()
                            + "\nx:" + (point.pre_pos.x + 0.45 * endTime).ToString() + "y:" + point.pre_pos.y.ToString() + "z:" + point.pre_pos.z.ToString()
                            + "\ndvx:" + (point.vel_inc.x + 0.45 * endTime).ToString() + "dvy:" + point.vel_inc.y.ToString() + "dvz:" + point.vel_inc.z.ToString()
                            + "\nvx:" + velocity[0].ToString() + "vy:" + velocity[1].ToString() + "vz:" + velocity[2].ToString()
                            + "\nfull server:" + this.full.ToString());
                    }

                    point.compute_predictor = true;

                    // Check if we are done
                    if (dt1 < 0.00001)
                    {
                        point.done = true;
                        return;
                    }

                    //int X, Y, Z;
                    int X = setInfo.CalcNodeX(point.pos.x, spatialInterp);
                    int Y = setInfo.CalcNodeY(point.pos.y, spatialInterp);
                    int Z = setInfo.CalcNodeZ(point.pos.z, spatialInterp);
                    //if (kernelSize == 0)
                    //{
                    //X = LagInterpolation.CalcNodeWithRound(point.pos.x, setInfo.Dx);
                    //Y = LagInterpolation.CalcNodeWithRound(point.pos.y, setInfo.Dy);
                    //Z = LagInterpolation.CalcNodeWithRound(point.pos.z, setInfo.Dz);
                    //}
                    //else
                    //{
                    //X = LagInterpolation.CalcNode(point.pos.x, setInfo.Dx);
                    //Y = LagInterpolation.CalcNode(point.pos.y, setInfo.Dy);
                    //Z = LagInterpolation.CalcNode(point.pos.z, setInfo.Dz);
                    //}
                    /* Fix for wrap around */
                    X = ((X % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
                    Z = ((Z % setInfo.GridResolutionZ) + setInfo.GridResolutionZ) % setInfo.GridResolutionZ;

                    point.zindex = new Morton3D(Z, Y, X);

                    nextTime = time;
                    nextTimeStep = basetime;
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

        // PJ 2016: part of bug fix, get the right stencil for channel
        public int GetStencilStartX(int x, int kernelSize)
        {
            return turbulence_worker.GetStencilStartX(x, kernelSize);
        }
        public int GetStencilStartY(int y, int kernelSize)
        {
            return turbulence_worker.GetStencilStartY(y, kernelSize);
        }
        public int GetStencilStartZ(int z, int kernelSize)
        {
            return turbulence_worker.GetStencilStartZ(z, kernelSize);
        }
        public int GetStencilEndX(int x, int kernelSize)
        {
            return turbulence_worker.GetStencilEndX(x, kernelSize);
        }
        public int GetStencilEndY(int y, int kernelSize)
        {
            return turbulence_worker.GetStencilEndY(y, kernelSize);
        }
        public int GetStencilEndZ(int z, int kernelSize)
        {
            return turbulence_worker.GetStencilEndZ(z, kernelSize);
        }

    }

}
