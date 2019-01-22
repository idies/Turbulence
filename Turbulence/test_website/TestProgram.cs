using System;
using System.Collections.Generic;
using System.Text;
using TestApp;

using System.Data.Sql;
using System.Data.SqlTypes;
using System.Data.SqlClient;
using System.Runtime.InteropServices;
using System.IO;

namespace TestApp
{
    /// <summary>
    /// 
    /// </summary>
    class TestProgram
    {

        static Random random = new Random();

        turbulence.TurbulenceService service = new turbulence.TurbulenceService();
        /// <summary>
        /// A small application to test the particle tracking and
        /// turbulence code libraries.
        /// </summary>
        /// <remarks>
        /// Currently only used to paste & debug snipits of code...
        /// TODO: Make it a useful, complete, test suite...
        /// </remarks>
        /// <param name="args">Command line arguments</param>
        public static void Main()
        {
            debugger();
            //speedtest();
            //Hex2Float();

            Console.WriteLine("Hint enter to quit.");
            Console.ReadLine();

        }

        private static void debugger()
        {
            try
            {
                turbulence.TurbulenceService service = new turbulence.TurbulenceService();
                string authToken = "uk.ac.manchester.zhao.wu-ea658424";

                string dataset = "channel";
                DateTime beginTime, stopTime;
                float dd = (float)(2.0 * Math.PI) / 4096;
                int pointsize = 1;

                float time = 0f;

                turbulence.Point3[] points = new turbulence.Point3[pointsize];
                int pp = 0;
                //for (int i = 2045; i < 2050; i++)
                //{
                //    for (int j = 2045; j < 2050; j++)
                //    {
                //        for (int k = 2045; k < 2050; k++)
                //        {
                //            points[pp] = new turbulence.Point3();
                //            points[pp].x = dd * i;// (float)(random.NextDouble() * 2.0 * 3.14);
                //            points[pp].y = dd * j;// (float)(random.NextDouble() * 2.0 * 3.14);
                //            points[pp].z = dd * k;// (float)(random.NextDouble() * 2.0 * 3.14);
                //            pp++;

                //        }
                //    }
                //    //Console.WriteLine("{0} {0} {0}", i, j, k);
                //}

                points[pp] = new turbulence.Point3();
                points[pp].x = dd * 5;// (float)(random.NextDouble() * 2.0 * 3.14);
                points[pp].y = dd * 5;// (float)(random.NextDouble() * 2.0 * 3.14);
                points[pp].z = dd * 5;// (float)(random.NextDouble() * 2.0 * 3.14);
                service.Timeout = -1;
                Console.WriteLine("start");
                beginTime = DateTime.Now;
                //turbulence.Vector3P[] result = service.GetVelocityAndPressure(authToken, dataset, time, //modife dsp012/gw01 in Line484, Database.cs
                //    turbulence.SpatialInterpolation.None, turbulence.TemporalInterpolation.None, points,"0.0.0.0");
                byte[] result = service.GetAnyCutoutWeb(authToken, dataset, "u", 2, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, null);
                stopTime = DateTime.Now;

                //result = service.GetBoxFilter(authToken, dataset, "u", time,
                //     dd * 5, points);

                Console.WriteLine("Time in {0}: {1}", dataset, stopTime- beginTime);

                beginTime = DateTime.Now;
                byte[] result1 = service.GetRawVelocity(authToken, dataset, 1, 0, 0, 0, 10, 10, 10, null);
                stopTime = DateTime.Now;

                //result = service.GetBoxFilter(authToken, dataset, "u", time,
                //     dd * 5, points);

                Console.WriteLine("Time in {0}: {1}", dataset, stopTime - beginTime);
            }
            catch (Exception E)
            {
                Console.WriteLine(E);
            }
        }

        //private static void speedtest()
        //{
        //    try
        //    {
        //        TestProgram TestProgram = new TestProgram();
        //        int pointsize = 10;
        //        turbulence.Point3[] points = new turbulence.Point3[pointsize];

        //        for (int i = 0; i < pointsize; i++)
        //        {
        //            //Console.WriteLine("{0} {0} {0}", i, j, k);

        //            points[i] = new turbulence.Point3();
        //            points[i].x = (float)(random.NextDouble() * 2.0 * 3.14);
        //            points[i].y = (float)(random.NextDouble() * 2.0 * 3.14);
        //            points[i].z = (float)(random.NextDouble() * 2.0 * 3.14);
        //        }

        //        string dataset1 = "isotropic1024coarse";
        //        string dataset2 = "isotropic1024fine";
        //        string dataset3 = "isotropic4096";
        //        TimeSpan t1 = new TimeSpan();
        //        TimeSpan t2 = new TimeSpan();
        //        TimeSpan t3 = new TimeSpan();
        //        string GetFun = "GetVelocity";
        //        for (int i = 0; i < 10; i++)
        //        {
        //            t1 += TestProgram.webTest(GetFun, dataset1, points);
        //            t2 += TestProgram.webTest(GetFun, dataset2, points);
        //            t3 += TestProgram.webTest(GetFun, dataset3, points);
        //        }
        //        Console.WriteLine("{0} in {1}: {2}", GetFun, dataset1, t1);
        //        Console.WriteLine("{0} in {1}: {2}", GetFun, dataset2, t2);
        //        Console.WriteLine("{0} in {1}: {2}", GetFun, dataset3, t3);

        //        t1 = new TimeSpan();
        //        t2 = new TimeSpan();
        //        t3 = new TimeSpan();
        //        GetFun = "GetPressure";
        //        for (int i = 0; i < 10; i++)
        //        {
        //            t1 += TestProgram.webTest(GetFun, dataset1, points);
        //            t2 += TestProgram.webTest(GetFun, dataset2, points);
        //            t3 += TestProgram.webTest(GetFun, dataset3, points);
        //        }
        //        Console.WriteLine("{0} in {1}: {2}", GetFun, dataset1, t1);
        //        Console.WriteLine("{0} in {1}: {2}", GetFun, dataset2, t2);
        //        Console.WriteLine("{0} in {1}: {2}", GetFun, dataset3, t3);

        //        t1 = new TimeSpan();
        //        t2 = new TimeSpan();
        //        t3 = new TimeSpan();
        //        GetFun = "GetVelocityGradient";
        //        for (int i = 0; i < 10; i++)
        //        {
        //            t1 += TestProgram.webTest(GetFun, dataset1, points);
        //            t2 += TestProgram.webTest(GetFun, dataset2, points);
        //            t3 += TestProgram.webTest(GetFun, dataset3, points);
        //        }
        //        Console.WriteLine("{0} in {1}: {2}", GetFun, dataset1, t1);
        //        Console.WriteLine("{0} in {1}: {2}", GetFun, dataset2, t2);
        //        Console.WriteLine("{0} in {1}: {2}", GetFun, dataset3, t3);

        //        t1 = new TimeSpan();
        //        t2 = new TimeSpan();
        //        t3 = new TimeSpan();
        //        GetFun = "GetPressureHessian";
        //        for (int i = 0; i < 10; i++)
        //        {
        //            t1 += TestProgram.webTest(GetFun, dataset1, points);
        //            t2 += TestProgram.webTest(GetFun, dataset2, points);
        //            t3 += TestProgram.webTest(GetFun, dataset3, points);
        //        }
        //        Console.WriteLine("{0} in {1}: {2}", GetFun, dataset1, t1);
        //        Console.WriteLine("{0} in {1}: {2}", GetFun, dataset2, t2);
        //        Console.WriteLine("{0} in {1}: {2}", GetFun, dataset3, t3);
        //    }
        //    catch (Exception E)
        //    {
        //        Console.WriteLine(E);
        //    }
        //}

        //private TimeSpan webTest(string GetFun, string dataset, turbulence.Point3[] points)
        //{
        //    DateTime beginTime, stopTime;

        //    float dd = (float)(2.0 * Math.PI) / 4096;

        //    beginTime = DateTime.Now;
        //    float time = 0f;
        //    service.Timeout = -1;

        //    switch (GetFun)
        //    {
        //        case ("GetVelocity"):
        //            turbulence.Vector3[] result;
        //            beginTime = DateTime.Now;
        //            result = service.GetVelocity("uk.ac.manchester.zhao.wu-ea658424", dataset, time, //modife dsp012/gw01 in Line484, Database.cs
        //                turbulence.SpatialInterpolation.None, turbulence.TemporalInterpolation.None, points);
        //            stopTime = DateTime.Now;
        //            break;
        //        case ("GetPressure"):
        //            turbulence.Pressure[] result_p;
        //            beginTime = DateTime.Now;
        //            result_p = service.GetPressure("uk.ac.manchester.zhao.wu-ea658424", dataset, time,
        //                turbulence.SpatialInterpolation.None, turbulence.TemporalInterpolation.None, points);
        //            stopTime = DateTime.Now;
        //            break;
        //        case ("GetVelocityGradient"):
        //            turbulence.VelocityGradient[] result_vel_grad;
        //            beginTime = DateTime.Now;
        //            result_vel_grad = service.GetVelocityGradient("uk.ac.manchester.zhao.wu-ea658424", dataset, time,
        //                turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.None, points);
        //            stopTime = DateTime.Now;
        //            break;
        //        case ("GetPressureHessian"):
        //            turbulence.PressureHessian[] result_pr_hes;
        //            beginTime = DateTime.Now;
        //            result_pr_hes = service.GetPressureHessian("uk.ac.manchester.zhao.wu-ea658424", dataset, time,
        //                turbulence.SpatialInterpolation.None_Fd4, turbulence.TemporalInterpolation.None, points);
        //            stopTime = DateTime.Now;
        //            break;
        //        default:
        //            beginTime = DateTime.Now;
        //            stopTime = DateTime.Now;
        //            break;
        //    }
        //    return stopTime - beginTime;
        //}

        public static void Hex2Float()
        {
            string hex = "";
            byte[] raw = new byte[hex.Length / 2];
            for (int i = 0; i < raw.Length; i++)
            {
                // THEN DEPENDING ON ENDIANNESS
                raw[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                // OR
                //raw[raw.Length - i - 1] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            float[] result = new float[raw.Length / sizeof(float)];

            for (int i = 0; i < raw.Length / sizeof(float); i++)
            {
                byte[] raw1 = new byte[sizeof(float)];
                for (int j = 0; j < sizeof(float); j++)
                {
                    // THEN DEPENDING ON ENDIANNESS
                    raw1[j] = raw[i * 4 + j];
                    // OR
                    //raw[raw.Length - i - 1] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                }
                result[i] = BitConverter.ToSingle(raw1, 0);
            }

            //byte[] bytes = BitConverter.GetBytes(0x2D1509C0);
            //Array.Reverse(bytes);
            //float myFloat = BitConverter.ToSingle(bytes, 0); // Always be correct

            var csv1 = new StringBuilder();
            for (int i = 0; i < raw.Length / sizeof(float) / 3; i++)
            {
                //Suggestion made by KyleMit
                var newLine1 = string.Format("{0},{1},{2}", result[i * 3], result[i * 3 + 1], result[i * 3 + 2]);
                csv1.AppendLine(newLine1);
            }
            File.WriteAllText("C:\\Users\\zwu27\\Documents\\vel_pr.txt", csv1.ToString());
        }
    }
}
