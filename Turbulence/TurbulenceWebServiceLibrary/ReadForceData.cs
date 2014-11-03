using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using Turbulence.TurbLib;
using Turbulence.TurbLib.DataTypes;
using System.Web.Caching;

namespace TurbulenceService
{

    public class ReadForceData
    {
        public const int WAVE_MODES = 50;
        public const int WAVE_MODES_ON_DISK = 50;

        public bool readFromDisk = true; 

        string directory;

        public ReadForceData(string directory)
        {
            this.directory = directory;
        }

        /// <summary>
        /// Read forcing data from the disk or (soon) database.
        /// </summary>
        /// <param name="timestep"></param>
        /// <param name="kx"></param>
        /// <param name="ky"></param>
        /// <param name="kz"></param>
        /// <param name="fxr"></param>
        /// <param name="fxi"></param>
        /// <param name="fyr"></param>
        /// <param name="fyi"></param>
        /// <param name="fzr"></param>
        /// <param name="fzi"></param>
        public FourierInfo [] getForceDataForTimestep(int timestep) {

            string cacheKey = String.Format("force{0}", timestep);
            

            FourierInfo [] force;
            
            force = new FourierInfo[WAVE_MODES];
   
            Queue<float> fq = readFloatsFromDisk(timestep);

            if (fq.Count != WAVE_MODES_ON_DISK * 9)
            {
                throw new Exception(String.Format("Incorrect number of values read ({0} vs {1})",
                    fq.Count, WAVE_MODES_ON_DISK * 9));
            }

            for (int i = 0; i < WAVE_MODES_ON_DISK; i++)
            {
                force[i] = new FourierInfo(fq.Dequeue(), fq.Dequeue(), fq.Dequeue(), fq.Dequeue(), 
                    fq.Dequeue(), fq.Dequeue(), fq.Dequeue(), fq.Dequeue(), fq.Dequeue());
                //Console.WriteLine("{0} {1} {2} ({3},{4}) ({5},{6}) ({7},{8})",
                    //kx[i], ky[i], kz[i], fxr[i], fxi[i], fyr[i], fyi[i], fzr[i], fzi[i]);
            }

            return force;
        }

        /// <summary>
        /// Kludge to read force files via regular expressions.
        /// </summary>
        /// <param name="timestep"></param>
        /// <returns>Array of floats in forcing data</returns>
        private Queue<float> readFloatsFromDisk(int timestep)
        {
            string filename;
            string file;
            Queue<float> fq = new Queue<float>(450);
            Regex fregex = new Regex(@"([-]|[.]|[-.])?[0-9][0-9]*[.]*[0-9]+([eE][-+]?[0-9]+)?",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            StreamReader sr;
            MatchCollection matches;

            // Read 31 values from forcingXXXXX.000
            filename = string.Format("{0}forcing{1:d5}.000", directory, timestep);
            Console.WriteLine(filename);
            sr = File.OpenText(filename);
            file = sr.ReadToEnd();
            sr.Close();
            matches = fregex.Matches(file);
            Console.WriteLine("{0} matches found", matches.Count);
            foreach (Match match in matches)
            {
                //Console.WriteLine(match.Value);
                fq.Enqueue(float.Parse(match.Value));
            }

            // Read 19 values from forcingXXXXX.127
            filename = string.Format("{0}forcing{1:d5}.127", directory, timestep);
            Console.WriteLine(filename);
            sr = File.OpenText(filename);
            file = sr.ReadToEnd();
            sr.Close();
            matches = fregex.Matches(file);
            Console.WriteLine("{0} matches found", matches.Count);
            foreach (Match match in matches)
            {
                //Console.WriteLine(match.Value);
                fq.Enqueue(float.Parse(match.Value));
            }
            return fq;
        }

    }
}
