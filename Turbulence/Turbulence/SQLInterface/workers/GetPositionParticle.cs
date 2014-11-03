using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Turbulence.TurbLib;
using Turbulence.SciLib;
using Turbulence.SQLInterface;
using System.Collections;

namespace Turbulence.SQLInterface.workers
{
    class GetPositionParticle : Worker
    {
        private TurbulenceOptions.SpatialInterpolation spatialInterp;
        private Computations compute;

        public GetPositionParticle (TurbDataTable setInfo,
            TurbulenceOptions.SpatialInterpolation spatialInterp)
        {
            this.setInfo = setInfo;
            this.spatialInterp = spatialInterp;
            this.compute = new Computations(setInfo);
        }

        public override SqlMetaData[]  GetRecordMetaData()
        {
           return new SqlMetaData[] {
                new SqlMetaData("Req", SqlDbType.Int),
                new SqlMetaData("x", SqlDbType.Real),
                new SqlMetaData("y", SqlDbType.Real),
                new SqlMetaData("z", SqlDbType.Real),
                new SqlMetaData("u1x", SqlDbType.Real),
                new SqlMetaData("u1y", SqlDbType.Real),
                new SqlMetaData("u1z", SqlDbType.Real),
                new SqlMetaData("u2x", SqlDbType.Real),
                new SqlMetaData("u2y", SqlDbType.Real),
                new SqlMetaData("u2z", SqlDbType.Real) };
        }

        public override float[] GetResult(TurbulenceBlob blob, SQLUtility.InputRequest input)
        {
            float[] vel3 = compute.CalcVelocity(blob, new float[] { input.x, input.y, input.z }, spatialInterp);
            input.x = input.x + input.dt * (23.0f / 12 * vel3[0] - 4.0f / 3 * input.u2.x + 5.0f / 12 * input.u1.x);
            input.y = input.y + input.dt * (23.0f / 12 * vel3[1] - 4.0f / 3 * input.u2.y + 5.0f / 12 * input.u1.y);
            input.z = input.z + input.dt * (23.0f / 12 * vel3[2] - 4.0f / 3 * input.u2.z + 5.0f / 12 * input.u1.z);
            input.u1 = input.u2;
            input.u2.x = vel3[0];
            input.u2.y = vel3[1];
            input.u2.z = vel3[2];

            return new float[] { input.x, input.y, input.z, input.u1.x, input.u1.y, input.u1.z, input.u2.x, input.u2.y, input.u2.z };
            // for location (input.x,input.y,input.z): Vx = v[0]; Vy = v[1]; Vz= = v[2];

            /*for (int j = 2; j < nt; j++)
            {
                vel3 = GetVelocity(authToken, dataset, StartTime + j * dt, spatialInterpolation, TurbulenceOptions.TemporalInterpolation.PCHIP, points);
                for (int i = 0; i < npnt; i++)
                {
                    points[i].x = points[i].x + dt * (23.0f / 12 * vel3[i].x - 4.0f / 3 * vel2[i].x + 5.0f / 12 * vel1[i].x);
                    points[i].y = points[i].y + dt * (23.0f / 12 * vel3[i].y - 4.0f / 3 * vel2[i].y + 5.0f / 12 * vel1[i].y);
                    points[i].z = points[i].z + dt * (23.0f / 12 * vel3[i].z - 4.0f / 3 * vel2[i].z + 5.0f / 12 * vel1[i].z);
                    vel1[i].x = vel2[i].x;
                    vel1[i].y = vel2[i].y;
                    vel1[i].z = vel2[i].z;
                    vel2[i].x = vel3[i].x;
                    vel2[i].y = vel3[i].y;
                    vel2[i].z = vel3[i].z;
                }
            }
            return points;*/

            //return new float[] { 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f };

            /*return (float[])compute.CalcPressureHessian(blob,
                new float[] { input.x, input.y, input.z }, spatialInterp).Clone();*/
        }
    }
}
