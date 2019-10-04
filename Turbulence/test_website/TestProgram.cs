using System;

namespace JHTDB_demo
{
    using JHTDB_service;

    class TestProgram
    {
        public static void Main()
        {
            Random random = new Random();
            var service = new TurbulenceServiceSoapClient();
            var points = new Point3[100000];

            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new Point3();
                points[i].x = (float)(random.NextDouble() * 2.0 * 3.14);
                points[i].y = (float)(random.NextDouble() * 2.0 * 3.14);
                points[i].z = (float)(random.NextDouble() * 2.0 * 3.14);
            }
            Vector3[] output = service.GetVelocity("uk.ac.manchester.zhao.wu-ea658424", "isotropic1024fine", 0.0024f,
                JHTDB_service.SpatialInterpolation.Lag6, JHTDB_service.TemporalInterpolation.None, points, null);
            //for (int r = 0; r < output.Length; r++)
            //{
            //    Console.WriteLine("X={0} Y={1} Z={2}", output[r].x, output[r].y, output[r].z);
            //}
            Point3[] output1 = service.GetPosition("uk.ac.manchester.zhao.wu-ea658424", "isotropic1024coarse", 0.0f, 0.1f, 0.01f,
                JHTDB_service.SpatialInterpolation.Lag6, points, null);

        }
    }
}
