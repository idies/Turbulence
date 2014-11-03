using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Threading;
using Turbulence.TurbLib;
using Turbulence.TurbLib.DataTypes;
using Turbulence.TurbBatch;

namespace TurbulenceService
{
    // worker thread for a given query (submit query and record the results)
    public class GetVelocityBatchWorker
    {
        public const int SLEEP_INTERVAL = 300;
        private BatchQuery query;			// query details
        private BatchWorkerQueue workerQueue;	// batch queue
        public const bool DEVEL_MODE = false;

        // public TestWorker(BatchQuery qry, TestQueue q, BatchWorkerQueue w)
        public GetVelocityBatchWorker(BatchWorkerQueue w, string dataset, float time, TurbulenceOptions.SpatialInterpolation spatialInterpolation,
            TurbulenceOptions.TemporalInterpolation temporalInterpolation, int worker, Point3[] points, bool round, int kernelSize, Vector3[] result)
        {
            workerQueue = w;
            query = new BatchQuery();

            query.dataset = dataset;
            query.time = time;
            query.spatialInterpolation = spatialInterpolation;
            query.temporalInterpolation = temporalInterpolation;
            query.worker = worker;
            query.points = points;
            query.round = round;
            query.kernelSize = kernelSize;
            query.result = result;

            // Load information about the requested dataset
            TurbDataTable table = TurbDataTable.GetMHDTableInfo("velocity08", 8);

            query.init(table);	// initialize mapping of points to atoms
        }

        /// <summary>
        /// submit query for processing and collect the results
        /// </summary>
        public void work()
        {
            // insert query into worker queue
            workerQueue.insert(query);

            while (!query.finish())
            {
                Thread.Sleep(SLEEP_INTERVAL);
            }

            // collect and output query result
            //BatchTools.appendLog("query "+query.id+" response time "+query.responseTime());
        }

    }
}
