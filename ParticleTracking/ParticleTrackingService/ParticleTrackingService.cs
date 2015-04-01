using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using Turbulence.SciLib;
using Turbulence.SQLInterface;
using Turbulence.SQLInterface.workers;
using Turbulence.TurbLib;
using Turbulence.TurbLib.DataTypes;

namespace ParticleTracking
{
    //[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple)]
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class ParticleTrackingService : IParticleTrackingService
    {
        volatile bool working = false;
        string localServer;
        string localDatabase;
        string tableName;
        SqlConnection localConn;
        TurbDataTable table;
        GetPositionWorker worker;
        
        ConcurrentDictionary<int, SQLUtility.TrackingInputRequest> input;
                
        private void AddRequestToMap(ref Dictionary<SQLUtility.TimestepZindexKey, List<int>> map, SQLUtility.TrackingInputRequest request,
            GetPositionWorker worker, long mask)
        {
            long zindex = 0;
            SQLUtility.TimestepZindexKey key = new SQLUtility.TimestepZindexKey();
            HashSet<SQLUtility.TimestepZindexKey> atoms = new HashSet<SQLUtility.TimestepZindexKey>(); //NOTE: HashSet requires .Net 3.5
            int X, Y, Z;

            if (worker.spatialInterp == TurbulenceOptions.SpatialInterpolation.None)
            {
                // In this case we are computing the node on the grid, on which to center the computation, using rounding:
                if (request.compute_predictor)
                {
                    X = LagInterpolation.CalcNodeWithRound(request.pos.x, worker.setInfo.Dx);
                    Y = LagInterpolation.CalcNodeWithRound(request.pos.y, worker.setInfo.Dy);
                    Z = LagInterpolation.CalcNodeWithRound(request.pos.z, worker.setInfo.Dz);
                }
                else
                {
                    X = LagInterpolation.CalcNodeWithRound(request.pre_pos.x, worker.setInfo.Dx);
                    Y = LagInterpolation.CalcNodeWithRound(request.pre_pos.y, worker.setInfo.Dy);
                    Z = LagInterpolation.CalcNodeWithRound(request.pre_pos.z, worker.setInfo.Dz);
                }

                zindex = new Morton3D(Z, Y, X).Key & mask;
                key.SetValues(request.timeStep, zindex);


                if (table.PointInRange(X, Y, Z))
                {
                    if (!atoms.Contains(key))
                    {
                        atoms.Add(key);
                    }
                    //if (!map.ContainsKey(key))
                    //{
                    //    map[key] = new List<int>();
                    //}
                    //map[key].Add(request.request);
                    //request.numberOfCubes++;
                }
                else
                {
                    request.crossed_boundary = true;
                    // If the request is not marked for evaluation do not add it to the map.
                    if (!request.evaluate)
                    {
                        return;
                    }
                }
            }
            else
            {
                // In this case we are computing the node on the grid, on which to center the computation, using "floor":
                if (request.compute_predictor)
                {
                    X = LagInterpolation.CalcNode(request.pos.x, worker.setInfo.Dx);
                    Y = LagInterpolation.CalcNode(request.pos.y, worker.setInfo.Dy);
                    Z = LagInterpolation.CalcNode(request.pos.z, worker.setInfo.Dz);
                }
                else
                {
                    X = LagInterpolation.CalcNode(request.pre_pos.x, worker.setInfo.Dx);
                    Y = LagInterpolation.CalcNode(request.pre_pos.y, worker.setInfo.Dy);
                    Z = LagInterpolation.CalcNode(request.pre_pos.z, worker.setInfo.Dz);
                }

                // For Lagrange Polynomial interpolation we need a cube of data 
                int startz = Z - worker.KernelSize / 2 + 1, starty = Y - worker.KernelSize / 2 + 1, startx = X - worker.KernelSize / 2 + 1;
                int endz = Z + worker.KernelSize / 2, endy = Y + worker.KernelSize / 2, endx = X + worker.KernelSize / 2;

                // we do not want a request to appear more than once in the list for an atom
                // with the below logic we are going to check distinct atoms only
                // we want to start at the start of a DB atom
                startz = startz - ((startz % worker.setInfo.atomDim) + worker.setInfo.atomDim) % worker.setInfo.atomDim;
                starty = starty - ((starty % worker.setInfo.atomDim) + worker.setInfo.atomDim) % worker.setInfo.atomDim;
                startx = startx - ((startx % worker.setInfo.atomDim) + worker.setInfo.atomDim) % worker.setInfo.atomDim;

                for (int z = startz; z <= endz; z += worker.setInfo.atomDim)
                {
                    for (int y = starty; y <= endy; y += worker.setInfo.atomDim)
                    {
                        for (int x = startx; x <= endx; x += worker.setInfo.atomDim)
                        {
                            // Wrap the coordinates into the grid space
                            int xi = ((x % worker.setInfo.GridResolutionX) + worker.setInfo.GridResolutionX) % worker.setInfo.GridResolutionX;
                            int yi = ((y % worker.setInfo.GridResolutionY) + worker.setInfo.GridResolutionY) % worker.setInfo.GridResolutionY;
                            int zi = ((z % worker.setInfo.GridResolutionZ) + worker.setInfo.GridResolutionZ) % worker.setInfo.GridResolutionZ;

                            zindex = new Morton3D(zi, yi, xi).Key & mask;
                            key.SetValues(request.timeStep, zindex);

                            if (table.PointInRange(xi, yi, zi))
                            {
                                if (!atoms.Contains(key))
                                {
                                    atoms.Add(key);
                                }
                                //if (!map.ContainsKey(key))
                                //{
                                //    map[key] = new List<int>();
                                //}
                                //map[key].Add(request.request);
                                //request.numberOfCubes++;
                            }
                            else
                            {
                                request.crossed_boundary = true;
                                // If the request is not marked for evaluation do not add it to the map.
                                if (!request.evaluate)
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            foreach (SQLUtility.TimestepZindexKey atom in atoms)
            {
                if (!map.ContainsKey(atom))
                {
                    map[atom] = new List<int>();
                }
                map[atom].Add(request.request);
                request.numberOfCubes++;
            }
        }

        public void Init(string localServer, string localDatabase, short datasetID, string tableName, int atomDim, int spatialInterp, bool development)
        {
            this.localServer = localServer;
            this.localDatabase = localDatabase;

            if (localServer.Contains("_"))
                localConn = new SqlConnection(
                    String.Format("Data Source={0};Initial Catalog={1};Trusted_Connection=True;Pooling=false;", localServer.Remove(localServer.IndexOf("_")), localDatabase));
            else
                localConn = new SqlConnection(
                    String.Format("Data Source={0};Initial Catalog={1};Trusted_Connection=True;Pooling=false;", localServer, localDatabase));
            Console.WriteLine("Attempting to connect using connection string: {0}", localConn.ConnectionString);
            localConn.Open();

            // Load information about the requested dataset
            this.table = TurbDataTable.GetTableInfo(localServer, localDatabase, tableName, atomDim, localConn);
            this.tableName = String.Format("{0}.dbo.{1}", localDatabase, table.TableName);
            
            // Instantiate a worker class
            worker = new GetPositionWorker(table,
                    (TurbulenceOptions.SpatialInterpolation)spatialInterp,
                    TurbulenceOptions.TemporalInterpolation.PCHIP);

            working = false;
        }


        public void Finish()
        {
            if (localConn.State == ConnectionState.Open)
            {
                localConn.Close();
            }
        }

        private void AddParticles(List<SQLUtility.TrackingInputRequest> particles)
        {
            //Console.WriteLine("Adding particles");
            if (input == null)
            {
                input = new ConcurrentDictionary<int, SQLUtility.TrackingInputRequest>();
            }

            foreach (SQLUtility.TrackingInputRequest particle in particles)
            {
                AddOneParticle(particle);
            }
        }

        private void AddOneParticle(SQLUtility.TrackingInputRequest one_particle)
        {
            if (input == null)
            {
                input = new ConcurrentDictionary<int, SQLUtility.TrackingInputRequest>();
            }

            if (input.ContainsKey(one_particle.request))
            {
                throw new Exception("Particle already exists in input dictionary!");
            }
            else
            {
                if (!input.TryAdd(one_particle.request, one_particle))
                {
                    throw new Exception("Could not add input particle!");
                }
            }
        }

        public void DoParticleTrackingWork(List<SQLUtility.TrackingInputRequest> particles)
        {
            //Console.WriteLine("Starting to add particles!");
            AddParticles(particles);
            //Console.WriteLine("Added a list of particles");
            IParticleTrackingServiceCallback callback_channel = OperationContext.Current.GetCallbackChannel<IParticleTrackingServiceCallback>();
            Task worker = Task.Factory.StartNew(() => DoWork(callback_channel));
        }

        public void DoParticleTrackingWorkOneParticle(SQLUtility.TrackingInputRequest one_particle)
        {
            //Console.WriteLine("Adding one particle!");
            AddOneParticle(one_particle);
            //Console.WriteLine("Added one particle");
            IParticleTrackingServiceCallback callback_channel = OperationContext.Current.GetCallbackChannel<IParticleTrackingServiceCallback>();
            Task worker = Task.Factory.StartNew(() => DoWork(callback_channel));
        }

        private void DoWork(IParticleTrackingServiceCallback callback_channel)
        {
            if (!working)
            {
                Console.WriteLine("Starting to do work!");
                if (localConn.State != ConnectionState.Open)
                {
                    Console.WriteLine("local connection is not open, state is: " + localConn.State);
                }
                try
                {
                    working = true;
                    Dictionary<SQLUtility.TimestepZindexKey, List<int>> atoms_map = new Dictionary<SQLUtility.TimestepZindexKey, List<int>>(); // Contains the database atoms to be retrieved from the DB
                                                                                                                                               // and the input particles associated with each atom
                                                                                                                                               // (identified by their request/particle id).
                    long mask = ~(long)(worker.DataTable.atomDim * worker.DataTable.atomDim * worker.DataTable.atomDim - 1);
                    SqlCommand cmd;
                    SQLUtility.TimestepZindexKey key = new SQLUtility.TimestepZindexKey();
                    TurbulenceBlob blob = new TurbulenceBlob(table);
                    byte[] rawdata = new byte[table.BlobByteSize];
                    SQLUtility.TrackingInputRequest particle = new SQLUtility.TrackingInputRequest();
                    List<SQLUtility.TrackingInputRequest> done_particles = new List<SQLUtility.TrackingInputRequest>();

                    while (input.Count > 0)
                    {
                        foreach (SQLUtility.TrackingInputRequest input_particle in input.Values)
                        {
                            AddRequestToMap(ref atoms_map, input_particle, worker, mask);
                        }

                        // for each entry in map
                        // get data from DB
                        // for each particle associated with data atom evaluate result
                        // if all cubes read for this particle 
                        //      if crossed boundary or done
                        //      remove from input and add to done particles

                        //Create a table to perform query via a JOIN
                        string joinTable = SQLUtility.CreateTemporaryJoinTable(atoms_map.Keys, TurbulenceOptions.TemporalInterpolation.PCHIP, table.TimeInc, localConn, 0);

                        cmd = new SqlCommand(
                            String.Format(@"SELECT {1}.basetime, {0}.timestep, {0}.zindex, {0}.data " +
                                            "FROM {0}, {1} " +
                                            "WHERE {0}.timestep = {1}.timestep AND {0}.zindex = {1}.zindex",
                                        tableName, joinTable),
                                        localConn);
                        cmd.CommandTimeout = 3600;

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int basetime = reader.GetSqlInt32(0).Value;  // Base time
                                int timestep = reader.GetSqlInt32(1).Value;  // Timestep returned
                                long thisBlob = reader.GetSqlInt64(2).Value; // Blob returned
                                key.SetValues(basetime, thisBlob);
                                int bytesread = 0;

                                while (bytesread < table.BlobByteSize)
                                {
                                    int bytes = (int)reader.GetBytes(3, table.SqlArrayHeaderSize, rawdata, bytesread, table.BlobByteSize - bytesread);
                                    bytesread += bytes;
                                }
                                blob.Setup(timestep, new Morton3D(thisBlob), rawdata);

                                foreach (int request_id in atoms_map[key])
                                {
                                    particle = input[request_id];

                                    if (worker == null)
                                        throw new Exception("worker is NULL!");
                                    if (blob == null)
                                        throw new Exception("blob is NULL!");

                                    particle.cubesRead++;
                                    worker.GetResult(blob, ref particle, timestep, basetime);

                                    if (particle.done || (particle.crossed_boundary && particle.cubesRead == 
                                        GetPositionWorker.TIMESTEPS_TO_READ_WITH_INTERPOLATION * particle.numberOfCubes))
                                    {
                                        SQLUtility.TrackingInputRequest done_particle;
                                        if (!input.TryRemove(request_id, out done_particle))
                                        {
                                            throw new Exception("Could not remove done particle!");
                                        }
                                        done_particles.Add(done_particle);
                                    }
                                }
                            }
                        }
                        cmd = new SqlCommand(String.Format(@"DROP TABLE tempdb..{0}", joinTable), localConn);
                        try
                        {
                            cmd.CommandTimeout = 600;
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception e)
                        {
                            throw new Exception(String.Format("Error dropping temporary table.  [Inner Exception: {0}])",
                                e.ToString()));
                        }

                        foreach (int input_point in input.Keys)
                        {
                            // reset the velocity increment
                            input[input_point].vel_inc.x = 0.0f;
                            input[input_point].vel_inc.y = 0.0f;
                            input[input_point].vel_inc.z = 0.0f;
                            input[input_point].cubesRead = 0;
                            input[input_point].numberOfCubes = 0;
                            input[input_point].lagInt = null;

                            // Check for particles that have crossed during execution.
                            // These will not be added to the list to send back to the mediator otherwise
                            // and then need to be reassigned.
                            if (input[input_point].crossed_boundary && !input[input_point].evaluate)
                            {
                                SQLUtility.TrackingInputRequest done_particle;
                                if (!input.TryRemove(input_point, out done_particle))
                                {
                                    throw new Exception("Could not remove particle that has cross data boundaries on this node!");
                                }
                                done_particles.Add(done_particle);
                            }
                        }

                        if (done_particles.Count > 0)
                        {
                            callback_channel.DoneParticles(done_particles);
                            done_particles.Clear();
                        }
                        atoms_map.Clear();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw ex;
                }
                working = false;
            }
        }
    }
}
