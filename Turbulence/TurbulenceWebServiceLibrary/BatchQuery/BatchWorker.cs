using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Threading;
using Turbulence.TurbLib;
using Turbulence.TurbLib.DataTypes;
using Turbulence.TurbBatch;

namespace TurbulenceService
{    
	// worker thread for evaluating a batch query
	public class BatchWorker
	{
		private TimePlane slice;
		private ArrayList atoms;
		private BatchWorkerQueue queue;
		private Mutex queueLock;
        private Hashtable timeSlices;
        private const string infodb = "turbinfo";

		// public const bool DEVEL_MODE = false;

		public BatchWorker(TimePlane s, ArrayList a, BatchWorkerQueue q, Mutex l, Hashtable t)
		{
			slice = s;
			atoms = a;
			queue = q;
			queueLock = l;
			timeSlices = t;
		}

		/// <summary>
		/// execute query for queries pending against a set of atoms
		/// </summary>
		private void ExecuteQuery(ArrayList atoms)
		{
			// execute query and append the results into the corresponding queries
			// aggregate all points into a single batch
			Hashtable queryMap = new Hashtable();
			Hashtable sourceQuery = new Hashtable();
			int records = 0;
			string dataset = null;
			float time = 0;
			foreach(Atom atom in atoms)
			{
				// BatchTools.appendLog("processed atom (t"+atom.time+",z"+atom.zindex+") for queries: ");
				foreach(BatchQuery query in atom.queries)
				{
					dataset = query.dataset;
					time = query.time;

					// BatchTools.appendLog(query.id+" ");

					if(!queryMap.ContainsKey(query.id))
					{
						queryMap.Add(query.id, new ArrayList());
						sourceQuery.Add(query.id, query);
					}

					ArrayList arr = (ArrayList)queryMap[query.id];

					// append points from query
					int[] mapped = (int[])query.pointsHash[atom.zindex];
					for(int i=0; i<mapped.Length; ++i)
					{
						arr.Add(mapped[i]);
						records++;
					}
				}
				// Console.WriteLine("");	
			}

			// execute batch job
            Database database = new Database(infodb, GetVelocityBatchWorker.DEVEL_MODE);
            //DataInfo.TableNames field = DataInfo.TableNames.velocity08;
            int dataset_id = DataInfo.findDataSetInt(dataset);
            //string tableName = DataInfo.getTableName(dataset, field);
            string field = "vel"; /*Not sure about this*/
			database.selectServers(dataset_id);

			// aggregate all queries and construct boundaries
			Point3[] points = new Point3[records];
			Vector3[] result = new Vector3[records];
			BatchMeta metadata = new BatchMeta(queryMap.Count);
            int totalAdded = 0;
			foreach(object key in queryMap.Keys)
			{
				BatchQuery aquery = (BatchQuery)sourceQuery[key];
				Point3[] ret = metadata.insertQuery( aquery, (ArrayList)queryMap[key], points );

				// insert points into temp table
				// bool round = aquery.spatialInterpolation == TurbulenceOptions.SpatialInterpolation.None ? true : false;
				database.AddBulkParticlesBatch(totalAdded, ret, aquery.round, aquery.kernelSize);
                totalAdded += ret.Length;
			}
			database.DoBulkInsertBatch();

			// output each merged point and correspond metadata
		    // BatchTools.appendLog("* list of merged points");
			//for(int i=0; i<points.Length; ++i)
				//BatchTools.appendLog("("+points[i].x+","+points[i].y+","+points[i].z+"), query: "+metadata.lookupQuery(i).id+", insertion order: "+metadata.lookupLocalOrder(i));
			

            database.ExecuteGetMHDDataBatch(field, time, metadata.toBoundaryString(), result);

			database.Close();
			database = null;

			// map results back to the appropriate query
			// Console.WriteLine("* list of results");
			for(int i=0; i<result.Length; ++i)
			{
				// Console.WriteLine("("+result[i].x+","+result[i].y+","+result[i].z+"), query: "+metadata.lookupQuery(i).id+", insertion order: "+metadata.lookupLocalOrder(i));

				BatchQuery aquery = metadata.lookupQuery(i);
				int index = metadata.lookupLocalOrder(i);
				aquery.result[index] = result[i];
			}
		}

		/// <summary>
		/// thread for executing query batch
		/// </summary>
		public void Submit()
		{
			// execute queries accessing a subset of atoms
			ExecuteQuery(atoms);

			// loop through finish queries and timeplanes (ie. remove completed queries, and timeplanes)
			DateTime end = DateTime.Now;
			DateTime oldest = end;

			queueLock.WaitOne();

			foreach(Atom atom in atoms)
			{
				foreach(BatchQuery query in atom.queries)
				{
					// update end time for query
					// must be updated to before decrementAtoms()
					query.setEnd(end);

					// remove atom from query
					//BatchTools.appendLog("finished 1 atom for query "+query.id);
					query.decrementAtoms();

					// if query is finished, then remove
					if(query.finish())
					{
						// remove finished queries from time slice
						//BatchTools.appendLog("query "+query.id+" finished "+BatchTools.formatTime(end));
						slice.remove(query);

						// update oldest
						if(oldest.CompareTo(query.getStart()) > 0)
							oldest = query.getStart();
					}
				}
			}

			if(oldest.CompareTo(slice.oldestQuery) == 0)
			{
				// Console.WriteLine("reset oldest query in time slice");
				slice.resetOldest();
			}

			// remove completed time slice
			if(slice.queries.Count == 0)
			{
				//BatchTools.appendLog("finished processing time slice "+slice.time);
				timeSlices.Remove(slice.time);
			}

			queueLock.ReleaseMutex();

			queue.decrementQueries();
		}
	}
}
