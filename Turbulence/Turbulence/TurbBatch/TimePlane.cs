using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using Turbulence.TurbLib;
using Turbulence.TurbLib.DataTypes;

namespace Turbulence.TurbBatch
{    
	public class TimePlane
	{
		// list of queries in a time plane
		public Hashtable queries;		// list of queries pending against time plane
		public int totalPoints;		// total points in time plane
		public DateTime oldestQuery;	// age of oldest query
		public int time;
		
		// hash of points in each region and age of oldest query in region
		public Hashtable pointsInRegion;
		public Hashtable oldestInRegion;
		public Hashtable queriesInRegion;

		public TimePlane(int t)
		{
			queries = new Hashtable();
			totalPoints = 0;
			oldestQuery = DateTime.Now;
			pointsInRegion = new Hashtable();
			oldestInRegion = new Hashtable();
			queriesInRegion = new Hashtable();
			time = t;
		}

		/// <summary>
		/// remove completed query
		/// </summary>
		public void remove(BatchQuery query)
		{
			queries.Remove(query.id);
		}

		/// <summary>
		/// get a list of atoms in sorted order
		/// </summary>
		private ArrayList getAtomSort()
		{
			ArrayList ret = new ArrayList();

			foreach(object key in queriesInRegion.Keys)
			{
				Atom a = (Atom)queriesInRegion[key];

				BatchCounter c = (BatchCounter)pointsInRegion[key];
				double points = c.count;
				c = (BatchCounter)oldestInRegion[key];
				double age = BatchTools.getMilliSecondDiff(c.oldest, DateTime.Now);
				a.utility = getUtility(points, age);

				ret.Add(a);
			}

			ret.Sort();
			return ret;
		}

		/// <summary>
		/// get next batch of atoms
		/// </summary>
		public ArrayList nextBatch()
		{
			ArrayList ret = new ArrayList();

			// get a list of atoms accessed sorted by utility
			ArrayList sorted = getAtomSort();

			int batchSize = BatchTools.batchSize;
			if(sorted.Count < batchSize)
				batchSize = sorted.Count;

			// remove atoms from workload queue
			int numPoints = 0;
			Atom a = null;
			for(int i=0; i<batchSize; ++i)
			{
				a = (Atom)sorted[i];
				BatchCounter c = (BatchCounter)pointsInRegion[a.zindex];
				numPoints += c.count;

				// remove atoms from map
				pointsInRegion.Remove(a.zindex);
				oldestInRegion.Remove(a.zindex);
				queriesInRegion.Remove(a.zindex);

				ret.Add(a);
			}
			totalPoints -= numPoints;

			return ret;
		}

		/// <summary>
		/// compute utility
		/// </summary>
		private double getUtility(double points, double age)
		{
			return points * BatchTools.throughputBias + age * BatchTools.ageBias;
		}
		
		/// <summary>
		/// compute utility of current timestep
		/// </summary>
		public double getUtility()
		{
			if(pointsInRegion.Count == 0)
				return 0.0;
			return getUtility((totalPoints/(double)pointsInRegion.Count),BatchTools.getMilliSecondDiff(oldestQuery, DateTime.Now));
		}
		
		/// <summary>
		/// TODO: maintain max heap for list of queries
		/// reset the oldest query in timeplane
		/// </summary>
		public void resetOldest()
		{
			int i = 0;
			foreach(object key in queries.Keys)
			{
				BatchQuery aquery = (BatchQuery)queries[key];
				if(i == 0)
					oldestQuery = aquery.getStart();
				else
					if(aquery.getStart().CompareTo(oldestQuery) < 0)
						oldestQuery = aquery.getStart();
				++i;
			}

		}

		/// <summary>
		/// insert new query
		/// </summary>
		public void insert(BatchQuery query)
		{
			if(queries.Count == 0)
				oldestQuery = query.getStart();
			queries.Add(query.id,query);
			totalPoints += query.points.Length;

			foreach(object key in query.pointsHash.Keys)
			{
				// new atom instance
				if(!queriesInRegion.ContainsKey(key))
					queriesInRegion.Add(key,new Atom());
				Atom a = (Atom)queriesInRegion[key];
				a.zindex = (int)key;
				a.time = time;
				a.queries.Add(query);

				// new counter for atom if not exists
				if(!pointsInRegion.ContainsKey(key))
					pointsInRegion.Add(key,new BatchCounter());

				BatchCounter c = (BatchCounter)pointsInRegion[key];
				// sum number of points mapped to atom
				c.count += ((int[])query.pointsHash[key]).Length;

				if(!oldestInRegion.ContainsKey(key))
				{
					c = new BatchCounter();
					c.oldest = query.getStart();
					oldestInRegion.Add(key,c);
				}
			}
		}


		/// <summary>
		/// print state of time plane
		/// </summary>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			
			sb.Append("TimePlane "+time+", points "+this.totalPoints+", oldest "+BatchTools.formatTime(oldestQuery)+", queries: "+queries.Count+", atoms: "+pointsInRegion.Count+","+oldestInRegion.Count+"\n");
			
			// output points per region
			foreach(object key in pointsInRegion.Keys)
				sb.Append("regions: "+key+", points: "+((BatchCounter)pointsInRegion[key]).count+", oldest: "+BatchTools.formatTime(((BatchCounter)pointsInRegion[key]).oldest)+"\n");

			return sb.ToString();
		}


	}
}
