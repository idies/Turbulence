using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Threading;
using Turbulence.TurbLib;
using Turbulence.TurbLib.DataTypes;
using Turbulence.SQLInterface;

namespace Turbulence.TurbBatch
{    
	[Serializable]
	public class BatchQuery
	{
		// format
		// <dataset> <time> <spatialInterpolation> <temporalInterpolation> <points>

		public long id;			// unique id associated with query
		public string dataset;	// dataset associated with query
		public float time;		// timestep
		public int time_int;	// nearest discrete timestep
		public TurbulenceOptions.SpatialInterpolation spatialInterpolation;	// type of spatial interpolation
        public TurbulenceOptions.TemporalInterpolation temporalInterpolation;	// type of temporal interpolation
        public int worker;      // the type of query/worker being evaluated
		public Point3[] points;	// list of points to evaluate
		public Hashtable pointsHash;	// map points to atom
        public bool round;                    // rounding
        public int kernelSize;                     // size of kernel computation
		public Vector3[] result;	// result of query

		private int atomsRemaining;		// track number of remaining to process
		private Mutex atomLock;			// lock to synchronize updates to number of atoms processed
		private DateTime start;			// start time of query
		private DateTime end;			// end time of query
		private TurbDataTable table;	// data table referenced

		public BatchQuery()
		{
			id = -1;
			dataset = "";
			time = -1;
			spatialInterpolation = TurbulenceOptions.SpatialInterpolation.None;
			temporalInterpolation = TurbulenceOptions.TemporalInterpolation.None;
            worker = -1;
			points = null;
			pointsHash = new Hashtable();
            round = true;
            kernelSize = 0;
			result = null;
			atomsRemaining = 0;
			atomLock = new Mutex(false);
			start = DateTime.Now;
			end = DateTime.Now;
		}

		/// <summary>
		/// TODO: rely on SQLUtility.GetNearestTimestep for computing the nearest timestep
		/// compute the nearest discrete timestep
		/// </summary>
		/*
		private int GetNearestTimestep()
		{
			float delta = 0.0002f;
			int timeinc = 10;

			float temp = time / delta;
			return (int)Math.Round(temp / timeinc) * timeinc;
		}
		*/

		/// <summary>
		/// initialize mapping of points to atoms and set start time of query
		/// </summary>
		public void init(TurbDataTable t)
		{
			table = t;

			// initialize query
			zorderHash();
			setStart(DateTime.Now);
		}

		/// <summary>
		/// compute atom index based on morton zorder for each point and map points to atoms
		/// </summary>
		public void zorderHash()
		{
			// compute nearest discrete timestep
			time_int = SQLUtility.GetNearestTimestep(time, table);

			bool round = spatialInterpolation == TurbulenceOptions.SpatialInterpolation.None ? true : false;
			Morton3D[] zorder = new Morton3D[points.Length];
			int[] atom = new int[points.Length];
			Hashtable map = new Hashtable();

			for(int i=0; i<points.Length; ++i)
			{
				int z = GetIntLoc(points[i].z, round);
				int y = GetIntLoc(points[i].y, round);
				int x = GetIntLoc(points[i].x, round);

				zorder[i] = new Morton3D(z, y, x);
				atom[i] = (int) (zorder[i].Key / (long)(1 << 18));

				if(!map.ContainsKey(atom[i]))
					map.Add(atom[i],new ArrayList());
				ArrayList arr = (ArrayList)map[atom[i]];
				arr.Add(i);

				// Console.WriteLine("atom "+atom[i]+" ("+z+","+y+","+x+")");
			}

			// map point array to the corresponding atom
			foreach(object key in map.Keys)
			{
				// increment number of atoms accessed
				incrementAtoms();

				ArrayList arr = (ArrayList)map[key];
				int[] mapped = new int[arr.Count];
				int count = 0;
				foreach(int item in arr)
				{
					mapped[count] = item;
					++count;
				}
				pointsHash.Add(key, mapped);
			}
		}

		/// <summary>
		/// set the query start time
		/// </summary>
		public void setStart(DateTime s)
		{
			start = s;
		}

		/// <summary>
		/// set the query end time
		/// </summary>
		public void setEnd(DateTime e)
		{
			end = e;
		}

		/// <summary>
		/// get the query end time
		/// </summary>
		public DateTime getEnd()
		{
			return end;
		}

		/// <summary>
		/// get the query start time
		/// </summary>
		public DateTime getStart()
		{
			return start;
		}

		/// <summary>
		/// compute response time
		/// </summary>
		public double responseTime()
		{
			return BatchTools.getMilliSecondDiff(getStart(),getEnd());
		}


		/// <summary>
		/// indicate that query finished processing
		/// </summary>
		public bool finish()
		{
			return (atomsRemaining == 0);
		}

		/// <summary>
		/// decrement number of atoms accessed
		/// </summary>
		public void decrementAtoms()
		{
			atomLock.WaitOne();
			atomsRemaining--;
			atomLock.ReleaseMutex();
		}

		/// <summary>
		/// increment number of atoms accessed
		/// </summary>
		public void incrementAtoms()
		{
			atomLock.WaitOne();
			atomsRemaining++;
			atomLock.ReleaseMutex();
		}

		/// <summary>
		/// TODO: fix duplicate function from Database.cs
		/// Convert from radians to integral coordinates on the cube.
		/// </summary>
		/// <param name="yp">Input Coordinate</param>
		/// <param name="round">Round to nearest integer (true) or floor (false).</param>
		/// <returns>Integer value [0-DIM)</returns>
		private int GetIntLoc(float yp, bool round)
		{
			int DIM = 1024;

			double dx = (2.0 * Math.PI) / 1024.0;
			int x;
			if (round)
			{
				x = Turbulence.SciLib.LagInterpolation.CalcNodeWithRound(yp, dx);
			}
			else
			{
				x = Turbulence.SciLib.LagInterpolation.CalcNode(yp, dx);
			}

			return ((x % DIM) + DIM) % DIM;
		}

		/// <summary>
		/// print query details
		/// </summary>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			
			sb.Append("id: "+id+", data: "+dataset+", table: "+table.TableName+", time: "+time_int+", spatial: "+spatialInterpolation+", temporal: "+temporalInterpolation+", atoms: "+atomsRemaining+"\n");
			
			/*
			for(int i=0; i<points.Length; ++i)
			{
				sb.Append("("+points[i].x+","+points[i].y+","+points[i].z+")\n");
			}
			*/

			// output points mapped to atoms
			/*
			foreach(object key in pointsHash.Keys)
			{
				int[] mapped = (int[])pointsHash[key];
				sb.Append("zorder: "+key+"\n");
				for(int i=0; i<mapped.Length; ++i)
				{
					sb.Append("("+points[mapped[i]].x+","+points[mapped[i]].y+","+points[mapped[i]].z+")\n");
				}
			}

			// output points in insertion order
			sb.Append("insertion order\n");
			*/
			for(int i=0; i<points.Length; ++i)
			{
				sb.Append("("+points[i].x+","+points[i].y+","+points[i].z+")\n");
			}

			return sb.ToString();
		}
	}
}
