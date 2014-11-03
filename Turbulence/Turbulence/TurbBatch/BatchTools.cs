using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;

namespace Turbulence.TurbBatch
{    
	public class BatchTools
	{
		public static double throughputBias = 1.0;
		public static double ageBias = 1.0 - throughputBias;
		public static int batchSize = 15;

		public BatchTools()
		{
		}

		/// <summary>
		/// return time difference in milliseconds
		/// </summary>
		public static double getMilliSecondDiff(DateTime st, DateTime et)
		{
			TimeSpan span = et.Subtract(st);
			return span.TotalMilliseconds;
		}

		/// <summary>
		/// uniform time format
		/// </summary>
		public static string formatTime(DateTime temp)
		{
			return temp.ToString("MM/dd/yyyy hh:mm:ss.fff tt");
		}

        // append log file
        public static void appendLog(string output)
        {
            StreamWriter file = File.AppendText("C:\\batchlog.txt");
            file.WriteLine(output);
            file.Close();
        }

	}
}
