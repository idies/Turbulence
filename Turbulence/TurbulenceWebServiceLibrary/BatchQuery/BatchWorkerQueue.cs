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
	// execute query workload in batches
	public class BatchWorkerQueue
	{
		public const int SLEEP_INTERVAL = 300;
		private Thread workerThread;		// worker thread instance
		private bool TERMINATE_WORKER;		// terminate worker
		private Mutex queueLock;			// lock accessing workload

		private long queryCount;			// total queries

		private Hashtable timeSlices;		// hash of time slices for workload

		private int concurrency;			// number of concurrent query execution threads
		private int queriesExecuting;		// number of queries executing

		public BatchWorkerQueue(int c)
		{
			workerThread = null;
			TERMINATE_WORKER = false;
			queueLock = new Mutex(false);

			queryCount = 0;

			timeSlices = new Hashtable();
			concurrency = c;
			queriesExecuting = 0;
		}

		/// <summary>
		/// return if workload queue is empty
		/// </summary>
		public bool isEmpty()
		{
			bool ret = false;

			queueLock.WaitOne();
			ret = (timeSlices.Count == 0);
			queueLock.ReleaseMutex();

			return ret;
		}


		/// <summary>
		/// decrement number of concurrent queries
		/// </summary>
		public void decrementQueries()
		{
			queueLock.WaitOne();
			--queriesExecuting;
			queueLock.ReleaseMutex();
		}

		/// <summary>
		/// increment number of concurrent queries
		/// </summary>
		public void incrementQueries()
		{
			queueLock.WaitOne();
			++queriesExecuting;
			queueLock.ReleaseMutex();
		}

		/// <summary>
		/// check if maximized concurrency
		/// </summary>
		private bool maxConcurrency()
		{
			queueLock.WaitOne();
			bool ret = (concurrency <= queriesExecuting);
			queueLock.ReleaseMutex();

			return ret;
		}

		/// <summary>
		/// count total number of points in queue
		/// </summary>
		public int totalPoints()
		{
			int ret = 0;

			queueLock.WaitOne();

			foreach(object key in timeSlices.Keys)
			{
				TimePlane t = (TimePlane)timeSlices[key];
				ret += t.totalPoints;
			}

			queueLock.ReleaseMutex();

			return ret;
		}

		/// <summary>
		/// return number of queries
		/// </summary>
		public int totalQueries()
		{
			int ret = 0;

			queueLock.WaitOne();
		
			foreach(object key in timeSlices.Keys)
			{
				TimePlane t = (TimePlane)timeSlices[key];
				ret += t.queries.Count;
			}

			queueLock.ReleaseMutex();

			return ret;
		}

		/// <summary>
		/// remove the next time slice based on priority
		/// </summary>
		public TimePlane nextTimeSlice()
		{
			queueLock.WaitOne();				

			TimePlane ret = null;
			if(isEmpty())
				return ret;

			double maxUtility = 0;
			foreach(int key in timeSlices.Keys)
			{
				TimePlane t = (TimePlane)timeSlices[key];
				if(ret == null)
				{
					ret = t;
					maxUtility = ret.getUtility();
				}
				else
				{
					double currUtility = t.getUtility();
					if(currUtility > maxUtility)
					{
						ret = t;
						maxUtility = currUtility;
					}
				}
			}

			queueLock.ReleaseMutex();

			return ret;
		}


		/// <summary>
		/// worker thread
		/// </summary>
		public void work()
		{
			while(!TERMINATE_WORKER)
			{
				if((!isEmpty()) && (!maxConcurrency()))
				{
					Thread.Sleep(SLEEP_INTERVAL);

					// retrieve the next batch of workload
					TimePlane slice = nextTimeSlice();
					// if no work remains
					if(slice == null)
					{
						Thread.Sleep(SLEEP_INTERVAL);
						continue;
					}

					queueLock.WaitOne();
					ArrayList atoms = slice.nextBatch();
					queueLock.ReleaseMutex();
					// if not work remaining in time slice
					if(atoms.Count == 0)
					{
						Thread.Sleep(SLEEP_INTERVAL);
						continue;
					}

					// submit a batch query for list of atoms
					BatchWorker worker = new BatchWorker(slice, atoms, this, queueLock, timeSlices);
					new Thread(new ThreadStart(worker.Submit)).Start();

					incrementQueries();
				}
				else
				{
					Thread.Sleep(SLEEP_INTERVAL);
				}
			}
		}


		/// <summary>
		/// insert a query into workload queue
		/// </summary>
		public void insert(BatchQuery query)
		{
			queueLock.WaitOne();

			query.id = queryCount;
			++queryCount;

			// insert query into workload queue of a specific time slice
			if(!timeSlices.ContainsKey(query.time_int))
				timeSlices.Add(query.time_int, new TimePlane(query.time_int));
			TimePlane slice = (TimePlane)timeSlices[query.time_int];
			slice.insert(query);

			// BatchTools.appendLog(slice.ToString());

			queueLock.ReleaseMutex();
		}

		/// <summary>
		/// start worker thread
		/// </summary>
		public void start()
		{
			workerThread = new Thread(new ThreadStart(this.work));
			workerThread.Start();
		}

		/// <summary>
		/// terminate the worker thread
		/// </summary>
		public void terminate()
		{
			TERMINATE_WORKER = true;
			workerThread.Join();
		}
	}
}
