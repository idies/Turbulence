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
    class GetPosition : Worker
    {
        private float dt;
        private GetVelocityWorker velocity_worker;
        private GetMHDWorker mhd_worker;

        public GetPosition (TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp,
            float dt)
            //,int correcting_pos)
        {
            this.setInfo = setInfo;
            this.spatialInterp = spatialInterp;
            this.dt = dt;
            //this.correcting_pos = correcting_pos;

            this.velocity_worker = new GetVelocityWorker(setInfo, spatialInterp, true, false);
            this.mhd_worker = new GetMHDWorker(setInfo, spatialInterp);

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

        public override SqlMetaData[]  GetRecordMetaData()
        {
            return new SqlMetaData[] {
                    new SqlMetaData("Req", SqlDbType.Int),
                    new SqlMetaData("X", SqlDbType.Real),
                    new SqlMetaData("Y", SqlDbType.Real),
                    new SqlMetaData("Z", SqlDbType.Real),
                    new SqlMetaData("AtomsRead", SqlDbType.Int)};
                    //,
                    //new SqlMetaData("Pre_X", SqlDbType.Real),
                    //new SqlMetaData("Pre_Y", SqlDbType.Real),
                    //new SqlMetaData("Pre_Z", SqlDbType.Real),
                    //new SqlMetaData("Vx", SqlDbType.Real),
                    //new SqlMetaData("Vy", SqlDbType.Real),
                    //new SqlMetaData("Vz", SqlDbType.Real) };
        }

        //public override void GetAtomsForPoint(float xp, float yp, float zp, long mask, HashSet<long> atoms)
        //{
        //    //velocity_worker.GetAtomsForPoint(xp, yp, zp, mask, atoms);
        //    if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None)
        //        throw new Exception("GetAtomsForPoint should only be called when spatial interpolation is performed!");

        //    int X, Y, Z;
        //    X = LagInterpolation.CalcNode(xp, setInfo.dx);
        //    Y = LagInterpolation.CalcNode(yp, setInfo.dx);
        //    Z = LagInterpolation.CalcNode(zp, setInfo.dx);
        //    // For Lagrange Polynomial interpolation we need a cube of data 

        //    //For 4^3 we have to check 3 points in each dimension (the corners and the middle point)
        //    //For 8^3 and larger we would only have to check the corners
        //    int[] x_values = new int[] { X - kernelSize / 2 + 1, X + kernelSize / 2 };
        //    int[] y_values = new int[] { Y - kernelSize / 2 + 1, Y + kernelSize / 2 };
        //    int[] z_values = new int[] { Z - kernelSize / 2 + 1, Z + kernelSize / 2 };

        //    long zindex;

        //    foreach (int x in x_values)
        //    {
        //        foreach (int y in y_values)
        //        {
        //            foreach (int z in z_values)
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
            if (spatialInterp == TurbulenceOptions.SpatialInterpolation.None)
                throw new Exception("GetAtomsForPoint should only be called when spatial interpolation is performed!");

            int X, Y, Z;
            X = LagInterpolation.CalcNode(request.x, setInfo.Dx);
            Y = LagInterpolation.CalcNode(request.y, setInfo.Dx);
            Z = LagInterpolation.CalcNode(request.z, setInfo.Dx);
            // For Lagrange Polynomial interpolation we need a cube of data 

            int startz = Z - kernelSize / 2 + 1, starty = Y - kernelSize / 2 + 1, startx = X - kernelSize / 2 + 1;
            int endz = Z + kernelSize / 2, endy = Y + kernelSize / 2, endx = X + kernelSize / 2;

            // we do not want a request to appear more than once in the list for an atom
            // with the below logic we are going to check distinct atoms only
            // we want to start at the start of a DB atom and then move from atom to atom
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
                        int yi = ((y % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;
                        int zi = ((z % setInfo.GridResolutionX) + setInfo.GridResolutionX) % setInfo.GridResolutionX;

                        if (setInfo.PointInRange(xi, yi, zi))
                        {
                            zindex = new Morton3D(zi, yi, xi).Key & mask;
                            if (!map.ContainsKey(zindex))
                            {
                                //map[zindex] = new List<int>(pointsPerCubeEstimate);
                                map[zindex] = new List<int>();
                            }
                            map[zindex].Add(request.request);
                            request.numberOfCubes++;
                            total_points++;
                        }
                    }
                }
            }
        }

        public float[] GetResult(SQLUtility.InputRequest input, float[] velocity, float dt)
        {
            float[] result = new float[3]; // Result value for the user
            //if (correcting_pos == 1)
            //{
                result[0] = input.x + velocity[0] * dt;
                result[1] = input.y + velocity[1] * dt;
                result[2] = input.z + velocity[2] * dt;
                //result[3] = input.x + velocity[0] * dt;
                //result[4] = input.y + velocity[1] * dt;
                //result[5] = input.z + velocity[2] * dt;
                //result[6] = velocity[0];
                //result[7] = velocity[1];
                //result[8] = velocity[2];
            //}
            //else
            //{
                //result[0] = input.x + 0.5F * (velocity[0] + input.velocity.x) * dt;
                //result[1] = input.y + 0.5F * (velocity[1] + input.velocity.y) * dt;
                //result[2] = input.z + 0.5F * (velocity[2] + input.velocity.z) * dt;
                //result[3] = input.predictor.x;
                //result[4] = input.predictor.y;
                //result[5] = input.predictor.z;
                //result[6] = velocity[0];
                //result[7] = velocity[1];
                //result[8] = velocity[2];
            //}
            return result;
        }

        public double[] GetResult(SQLUtility.MHDInputRequest input, float dt)
        {
            double[] result = new double[3]; // Result value for the user
            result[0] = input.x + input.result[0] * dt;
            result[1] = input.y + input.result[1] * dt;
            result[2] = input.z + input.result[2] * dt;
            //if (correcting_pos == 1)
            //{
                //result[0] = input.x;
                //result[1] = input.y;
                //result[2] = input.z;
                //result[3] = input.x + input.result[0] * dt;
                //result[4] = input.y + input.result[1] * dt;
                //result[5] = input.z + input.result[2] * dt;
                //result[6] = input.result[0];
                //result[7] = input.result[1];
                //result[8] = input.result[2];
            //}
            //else
            //{
            //    result[0] = input.x + 0.5F * (input.result[0] + input.velocity.x) * dt;
            //    result[1] = input.y + 0.5F * (input.result[1] + input.velocity.y) * dt;
            //    result[2] = input.z + 0.5F * (input.result[2] + input.velocity.z) * dt;
            //    result[3] = input.predictor.x;
            //    result[4] = input.predictor.y;
            //    result[5] = input.predictor.z;
            //    result[6] = input.result[0];
            //    result[7] = input.result[1];
            //    result[8] = input.result[2];
            //}
            return result;
        }

        public override double[] GetResult(TurbulenceBlob blob, SQLUtility.InputRequest input)
        {
            throw new NotImplementedException();
        }

        public override double[] GetResult(TurbulenceBlob blob, SQLUtility.MHDInputRequest input)
        {
            double[] result = new double[3]; // Result value for the user
            double[] velocity = new double[3];
            if (velocity_worker == null || mhd_worker == null)
                throw new Exception("velocity_worker or mhd_worker is null!");
            if (setInfo.EdgeRegion >= kernelSize / 2)
                velocity = velocity_worker.CalcVelocity(blob, input.x, input.y, input.z);
            else
                velocity = mhd_worker.CalcLagInterpolation(blob, input.x, input.y, input.z, ref input.lagInt);
            result[0] = velocity[0] * dt;
            result[1] = velocity[1] * dt;
            result[2] = velocity[2] * dt;
            return result;
        }

        public override int GetResultSize()
        {
            return 3;
        }
    }
}
