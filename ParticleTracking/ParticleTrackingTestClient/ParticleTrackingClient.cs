﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ServiceModel;
using System.Threading;
using ParticleTracking;

using System.IO;
using System.ServiceModel.Description;

using TurbulenceService;
using Turbulence.SQLInterface;

namespace ParticleTrackingTestClient
{
    class ParticleTrackingClient : IParticleTrackingServiceCallback
    {
        DuplexChannelFactory<IParticleTrackingService>[] factories;
        IParticleTrackingService[] channels;
        Database database;
        ConcurrentDictionary<int, SQLUtility.TrackingInputRequest> partial_results;
        int num_particles = 10000;
        int num_done_particles = 0;
        bool round;
        int kernelSizeX;
        int kernelSizeY;
        int kernelSizeZ;
        ManualResetEvent doneEvent = new ManualResetEvent(false);

        const int MAX_PARTICLES_PER_NODE = 200;

        //for debugging:
        //--------------------------------
        //SQLUtility.TrackingInputRequest[] initial_particles;
        //ConcurrentDictionary<int, SQLUtility.TrackingInputRequest> particles_not_yet_finished;
        //--------------------------------

        public void TestParticleTracking()
        {
            try
            {
                bool development = true;
                Turbulence.TurbLib.TurbulenceOptions.SpatialInterpolation spatialInterp = Turbulence.TurbLib.TurbulenceOptions.SpatialInterpolation.Lag6;
                round = false;

                Random random = new Random();
                TimeSpan total = new TimeSpan(0);
                float dt = 0.001f;
                float time_range = 500 * dt;
                float max_time = 2.56f;
                float startTime = (float)random.NextDouble() * (max_time - time_range);
                //startTime = 2.394468f;
                float endTime = startTime + time_range;
                int num_processes = 1;
                DataInfo.DataSets dataset = DataInfo.DataSets.mhd1024;
                string tableName = "velocity08";
                database = new Database("turbinfo", false);
                database.Initialize(dataset, num_processes);

                SQLUtility.TrackingInputRequest[] particles;
                CreatInput(startTime, endTime, dt, num_particles, out particles);
                //CreatInputFromFile(startTime, endTime, dt, out particles);
                num_particles = particles.Length;

                //--------------------------------
                //initial_particles = particles;
                //particles_not_yet_finished = new ConcurrentDictionary<int, SQLUtility.TrackingInputRequest>();
                //foreach (SQLUtility.TrackingInputRequest particle in particles)
                //{
                //    particles_not_yet_finished.TryAdd(particle.request, particle);
                //}
                //--------------------------------

                DateTime runningTime, runningTimeEnd;
                runningTime = DateTime.Now;

                List<SQLUtility.TrackingInputRequest>[] particles_per_node;
                particles_per_node = new List<SQLUtility.TrackingInputRequest>[database.serverCount];                
                Turbulence.TurbLib.TurbulenceOptions.GetKernelSize(spatialInterp, ref kernelSizeX, ref kernelSizeY, database.channel_grid, (int)Turbulence.SQLInterface.Worker.Workers.GetPosition);
                kernelSizeZ = kernelSizeX;
                DistributeParticles(particles, round, kernelSizeX, kernelSizeY, kernelSizeZ, particles_per_node);

                factories = new DuplexChannelFactory<IParticleTrackingService>[database.serverCount];
                channels = new IParticleTrackingService[database.serverCount];
                
                InstanceContext instanceContext = new InstanceContext(this);
                for (int i = 0; i < database.serverCount; i++)
                {
                    NetTcpBinding binding = new NetTcpBinding();
                    // 1 hour receive timeout
                    binding.CloseTimeout = new System.TimeSpan(10, 0, 0);
                    binding.OpenTimeout = new System.TimeSpan(10, 0, 0);
                    binding.ReceiveTimeout = new System.TimeSpan(10, 0, 0);
                    binding.SendTimeout = new System.TimeSpan(10, 0, 0);
                    binding.MaxBufferPoolSize = 2147483647;
                    binding.MaxBufferSize = 2147483647;
                    binding.MaxReceivedMessageSize = 2147483647;
                    System.Xml.XmlDictionaryReaderQuotas readerQuotas = new System.Xml.XmlDictionaryReaderQuotas();
                    readerQuotas.MaxDepth = 1000000;
                    readerQuotas.MaxArrayLength = 10000000;
                    readerQuotas.MaxBytesPerRead = 2147483647;
                    readerQuotas.MaxStringContentLength = 2147483647;
                    binding.ReaderQuotas = readerQuotas;

                    //NetTcpSecurity security = new NetTcpSecurity();
                    //security.Mode = SecurityMode.None;
                    //TcpTransportSecurity transport_security = new TcpTransportSecurity();
                    //transport_security.ClientCredentialType = TcpClientCredentialType.None;
                    //transport_security.ProtectionLevel = System.Net.Security.ProtectionLevel.None;
                    //security.Transport = transport_security;
                    //MessageSecurityOverTcp message_security = new MessageSecurityOverTcp();
                    //message_security.ClientCredentialType = MessageCredentialType.None;
                    //security.Message = message_security;
                    //binding.Security = security;

                    string servername = database.servers[i];
                    if (servername.Contains("_"))
                        servername = database.servers[i].Remove(database.servers[i].IndexOf("_"));
                    EndpointAddress address = new EndpointAddress(
                        String.Format("net.tcp://{0}.10g.sdss.pha.jhu.edu:8090/ParticleTrackingService", servername));
                    factories[i] = new DuplexChannelFactory<IParticleTrackingService>(instanceContext, binding, address);
                    
                    foreach (OperationDescription op in factories[i].Endpoint.Contract.Operations)
                    {
                        DataContractSerializerOperationBehavior dataContractBehavior = op.Behaviors.Find<DataContractSerializerOperationBehavior>() as DataContractSerializerOperationBehavior;
                        if (dataContractBehavior != null)
                        {
                            dataContractBehavior.MaxItemsInObjectGraph = 2147483647;
                        }
                    }

                    factories[i].Open();
                    channels[i] = factories[i].CreateChannel();
                    channels[i].Init(database.servers[i], database.databases[i], (short)dataset, tableName, database.atomDim, (int)spatialInterp, development);
                    Console.WriteLine("called Init() on server {0}, database {1}", database.servers[i], database.databases[i]);
                }
                for (int i = 0; i < database.serverCount; i++)
                {
                    if (particles_per_node[i] != null && particles_per_node[i].Count > 0)
                    {
                        if (particles_per_node[i].Count > MAX_PARTICLES_PER_NODE)
                        {
                            for (int j = 0; j < particles_per_node[i].Count; j += MAX_PARTICLES_PER_NODE)
                            {
                                List<SQLUtility.TrackingInputRequest> part = particles_per_node[i].GetRange(j, Math.Min(MAX_PARTICLES_PER_NODE, particles_per_node[i].Count - j));
                                channels[i].DoParticleTrackingWork(part);
                            }
                        }
                        else
                        {
                            channels[i].DoParticleTrackingWork(particles_per_node[i]);
                        }
                        
                        //channels[i].DoParticleTrackingWork(particles_per_node[i]);
                    }
                }
                Console.WriteLine("Finished particle distribution. Waiting for results.");
                doneEvent.WaitOne();
                runningTimeEnd = DateTime.Now;
                Console.WriteLine("Total running time: " + (runningTimeEnd - runningTime).TotalSeconds);
                Console.WriteLine("Start time: {0}, End time: {1}, dt: {2}", startTime, endTime, dt);
                for (int i = 0; i < database.serverCount; i++)
                {
                    channels[i].Finish();
                    factories[i].Close();
                }
            }
            catch (Exception ex)
            {
                if (factories != null)
                {
                    for (int i = 0; i < factories.Length; i++)
                    {
                        if (factories[i] != null)
                            factories[i].Abort();
                    }
                }
                Console.WriteLine(ex.Message);
            }
        }

        private void CreatInput(float startTime, float endTime, float dt, int num_particles, out SQLUtility.TrackingInputRequest[] particles)
        {
            float database_time = startTime / database.Dt;
            int timestep = (int)Math.Round(database_time / database.TimeInc) * database.TimeInc + database.TimeOff;
            //timestep = 0;
            particles = new SQLUtility.TrackingInputRequest[num_particles];
            Random rand = new Random();
            for (int i = 0; i < num_particles; i++)
            {
                float x, y, z;
                x = (float)(2 * Math.PI * rand.NextDouble());
                y = (float)(2 * Math.PI * rand.NextDouble());
                z = (float)(2 * Math.PI * rand.NextDouble());
                //x = 2.542306f; //2.542306, 0.001126792, 4.74914
                //y = 0.001126792f;
                //z = 4.74914f;
                int Z = database.GetIntLocZ(z, round);
                int Y = database.GetIntLocY(y, round, kernelSizeY);
                int X = database.GetIntLocX(x, round);
                long zindex = new Morton3D(Z, Y, X).Key;
                particles[i] = new SQLUtility.TrackingInputRequest(i, timestep, zindex, new Turbulence.TurbLib.DataTypes.Point3(x, y, z), new Turbulence.TurbLib.DataTypes.Point3(),
                    new Turbulence.TurbLib.DataTypes.Vector3(), startTime, endTime, dt, true, 0, false);

                //Console.WriteLine(String.Format("x = {0}, y = {1}, z = {2}", x, y, z));
            }
        }

        private void CreatInputFromFile(float startTime, float endTime, float dt, out SQLUtility.TrackingInputRequest[] particles)
        {
            float database_time = startTime / database.Dt;
            int timestep = (int)Math.Round(database_time / database.TimeInc) * database.TimeInc + database.TimeOff;
            //timestep = 0;

            StreamReader reader = new StreamReader(File.OpenRead(@"C:\Users\kalin\Desktop\particles.txt"));
            List<float> x_values = new List<float>();
            List<float> y_values = new List<float>();
            List<float> z_values = new List<float>();

            string[] separator = new string[] { ", " };

            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                string[] values = line.Split(separator, StringSplitOptions.None);

                float value;
                float.TryParse(values[0], out value);
                x_values.Add(value);
                float.TryParse(values[1], out value);
                y_values.Add(value);
                float.TryParse(values[2], out value);
                z_values.Add(value);
            }

            particles = new SQLUtility.TrackingInputRequest[x_values.Count];
            for (int i = 0; i < x_values.Count; i++)
            {
                int Z = database.GetIntLocZ(z_values[i], round);
                int Y = database.GetIntLocY(y_values[i], round, kernelSizeY);
                int X = database.GetIntLocX(x_values[i], round);
                long zindex = new Morton3D(Z, Y, X).Key;
                particles[i] = new SQLUtility.TrackingInputRequest(i, timestep, zindex, new Turbulence.TurbLib.DataTypes.Point3(x_values[i], y_values[i], z_values[i]), new Turbulence.TurbLib.DataTypes.Point3(),
                    new Turbulence.TurbLib.DataTypes.Vector3(), startTime, endTime, dt, true, 0, false);
            }
        }

        void IParticleTrackingServiceCallback.DoneParticles(List<Turbulence.SQLInterface.SQLUtility.TrackingInputRequest> done_particles)
        {
            try
            {
                for (int i = 0; i < done_particles.Count; i++)
                {
                    if (!done_particles[i].crossed_boundary)
                    {
                        if (done_particles[i].done)
                        {
                            //Console.WriteLine("particle {0}: Final position: x = {1}, y = {2}, z = {3}", done_particles[i].request,
                            //    done_particles[i].pos.x, done_particles[i].pos.y, done_particles[i].pos.z);
                            Interlocked.Increment(ref num_done_particles);
                            Console.WriteLine("number of done particles is " + num_done_particles);

                            //--------------------------------
                            //SQLUtility.TrackingInputRequest finished_particle;
                            //particles_not_yet_finished.TryRemove(done_particles[i].request, out finished_particle);
                            //--------------------------------

                            if (num_done_particles == num_particles)
                                doneEvent.Set();
                        }
                        else
                        {
                            Console.WriteLine("particle {0} at position: x = {1}, y = {2}, z = {3} not done, but not crossed either!", done_particles[i].request,
                                done_particles[i].pos.x, done_particles[i].pos.y, done_particles[i].pos.z);
                        }
                    }
                    else
                    {
                        //Console.WriteLine("particle {0} at position: x = {1}, y = {2}, z = {3} has crossed boundaries, evaluate is {4}", done_particles[i].request,
                        //    done_particles[i].pos.x, done_particles[i].pos.y, done_particles[i].pos.z, done_particles[i].evaluate);

                        // Check if the particle was assigned for evaluation.
                        // If so, partial results are expected from a few nodes.
                        // If not, it should be assigned to the nodes that have the data.
                        if (done_particles[i].evaluate)
                        {
                            if (partial_results == null)
                            {
                                partial_results = new ConcurrentDictionary<int, SQLUtility.TrackingInputRequest>();
                            }
                            if (!partial_results.ContainsKey(done_particles[i].request))
                            {
                                // This is the first time we are receiving partial results for this particle.
                                // Update predictor or corrector with the computed "partial" predictor or corrector from this server
                                SQLUtility.TrackingInputRequest new_partial_result = done_particles[i]; // NOTE: This here is a shallow copy. Is this OK?
                                new_partial_result.numberOfNodeResults++;
                                partial_results[done_particles[i].request] = new_partial_result;
                            }
                            else
                            {
                                // Update predictor or corrector with the velocity increment computed from this server.
                                // Each server will update the compute predictor flag after performing the evaluation.
                                // Therefore if "compute predictor" is set to true this means that the corrector position was computed
                                // and the predictor should be computed next.
                                // Vice versa, if the "compute predictor" is false the predictor was computed.
                                if (done_particles[i].compute_predictor)
                                {
                                    partial_results[done_particles[i].request].pos.x += 0.5f * done_particles[i].vel_inc.x;
                                    partial_results[done_particles[i].request].pos.y += 0.5f * done_particles[i].vel_inc.y;
                                    partial_results[done_particles[i].request].pos.z += 0.5f * done_particles[i].vel_inc.z;
                                }
                                else
                                {
                                    partial_results[done_particles[i].request].pre_pos.x += done_particles[i].vel_inc.x;
                                    partial_results[done_particles[i].request].pre_pos.y += done_particles[i].vel_inc.y;
                                    partial_results[done_particles[i].request].pre_pos.z += done_particles[i].vel_inc.z;
                                }
                                partial_results[done_particles[i].request].numberOfNodeResults++;
                                if (partial_results[done_particles[i].request].numberOfNodeResults == partial_results[done_particles[i].request].numberOfNodes)
                                {
                                    // All of the partial results have been received. The particle can be reassigned.
                                    // Unless it is actually done.
                                    SQLUtility.TrackingInputRequest particle_to_be_reassigned;
                                    if (!partial_results.TryRemove(done_particles[i].request, out particle_to_be_reassigned))
                                    {
                                        Console.WriteLine("Could not remove particle from the dictionary!");
                                    }

                                    if (done_particles[i].done)
                                    {
                                        Console.WriteLine("particle {0}: Final position: x = {1}, y = {2}, z = {3} computed from multiple nodes", particle_to_be_reassigned.request,
                                            particle_to_be_reassigned.pos.x, particle_to_be_reassigned.pos.y, particle_to_be_reassigned.pos.z);
                                        Interlocked.Increment(ref num_done_particles);
                                        Console.WriteLine("number of done particles is " + num_done_particles);

                                        //--------------------------------
                                        //SQLUtility.TrackingInputRequest finished_particle;
                                        //particles_not_yet_finished.TryRemove(done_particles[i].request, out finished_particle);

                                        //--------------------------------
                                        if (num_done_particles == num_particles)
                                            doneEvent.Set();
                                    }
                                    else
                                    {
                                        ResetParticle(particle_to_be_reassigned);
                                        AssignParticleToMultipleNodes(particle_to_be_reassigned, round, kernelSizeZ, kernelSizeY, kernelSizeX);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // The particle has crossed the boundaries as it was being advected on one of the nodes and needs to be assigned to 
                            // all of the nodes that have the appropriate data.
                            ResetParticle(done_particles[i]);
                            AssignParticleToMultipleNodes(done_particles[i], round, kernelSizeZ, kernelSizeY, kernelSizeX);
                        }
                    }
                }

                //--------------------------------
                //if (particles_not_yet_finished.Count < 100)
                //{
                //    Console.WriteLine("Particles that have not finished yet:");
                //    foreach (SQLUtility.TrackingInputRequest particle in particles_not_yet_finished.Values)
                //    {
                //        Console.WriteLine("{0}, {1}, {2}", particle.pos.x, particle.pos.y, particle.pos.z);
                //    }
                //    Console.WriteLine();
                //}
                //else
                //{
                //    Console.WriteLine("{0} particles that have not finished yet", particles_not_yet_finished.Count);
                //}
                //--------------------------------
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw ex;
            }
        }

        private void ResetParticle(SQLUtility.TrackingInputRequest particle)
        {
            particle.crossed_boundary = false;
            particle.numberOfNodes = 0;
            particle.numberOfNodeResults = 0;
            particle.vel_inc.x = 0.0f;
            particle.vel_inc.y = 0.0f;
            particle.vel_inc.z = 0.0f;
            particle.cubesRead = 0;
            particle.numberOfCubes = 0;
            particle.lagInt = null;
        }

        private void AssignParticleToMultipleNodes(SQLUtility.TrackingInputRequest input_particle, bool round, int kernelSizeZ, int kernelSizeY, int kernelSizeX)
        {
            try
            {
                List<int> nodes = new List<int>(database.serverCount);
                for (int i = 0; i < database.serverCount; i++)
                {
                    if (TestParticleForNode(input_particle, round, kernelSizeZ, kernelSizeY, kernelSizeX, i))
                    {
                        input_particle.numberOfNodes++;
                        nodes.Add(i);
                    }
                }

                // Set the particle's evaluate flag, which indicates whether a particle that spans node boundaries should be evaluated.
                if (input_particle.numberOfNodes > 1)
                {
                    input_particle.evaluate = true;
                }
                else
                {
                    input_particle.evaluate = false;
                }

                // NOTE: It's important to send the particle to each node only after we update the number of nodes it was assigned to.
                foreach (int node in nodes)
                {
                    channels[node].DoParticleTrackingWorkOneParticle(input_particle);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw ex;
            }
        }

        private bool TestParticleForNode(SQLUtility.TrackingInputRequest input_particle, bool round, int kernelSizeZ, int kernelSizeY, int kernelSizeX, int node)
        {
            try
            {
                int Z, Y, X;
                if (input_particle.compute_predictor)
                {
                    Z = database.GetIntLocZ(input_particle.pos.z, round);
                    Y = database.GetIntLocY(input_particle.pos.y, round, kernelSizeY);
                    X = database.GetIntLocX(input_particle.pos.x, round);
                }
                else
                {
                    Z = database.GetIntLocZ(input_particle.pre_pos.z, round);
                    Y = database.GetIntLocY(input_particle.pre_pos.y, round, kernelSizeY);
                    X = database.GetIntLocX(input_particle.pre_pos.x, round);
                }
                Morton3D zindex = new Morton3D(Z, Y, X);

                int startz = database.GetKernelStart(Z, kernelSizeZ), starty = database.GetKernelStart(Y, kernelSizeY), startx = database.GetKernelStart(X, kernelSizeX);
                int endz = startz + kernelSizeZ - 1, endy = starty + kernelSizeY - 1, endx = startx + kernelSizeX - 1;

                // The last two conditions have to do with wrap around
                // The beginning and end of each kernel may be outside of the grid space
                // Due to periodicity in space these locations are going to be wrapped around
                // Thus, we need to check if the points should be added to these servers
                if ((database.serverBoundaries[node].startx <= startx && startx <= database.serverBoundaries[node].endx) ||
                    (database.serverBoundaries[node].startx <= endx && endx <= database.serverBoundaries[node].endx) ||
                    (startx < database.serverBoundaries[node].startx && database.serverBoundaries[node].endx < endx) ||
                    (startx + database.GridResolutionX <= database.serverBoundaries[node].endx) ||
                    (database.serverBoundaries[node].startx <= endx - database.GridResolutionX))
                {
                    if ((database.serverBoundaries[node].starty <= starty && starty <= database.serverBoundaries[node].endy) ||
                        (database.serverBoundaries[node].starty <= endy && endy <= database.serverBoundaries[node].endy) ||
                        (starty < database.serverBoundaries[node].starty && database.serverBoundaries[node].endy < endy) ||
                            (starty + database.GridResolutionY <= database.serverBoundaries[node].endy) ||
                            (database.serverBoundaries[node].starty <= endy - database.GridResolutionY))
                    {
                        if ((database.serverBoundaries[node].startz <= startz && startz <= database.serverBoundaries[node].endz) ||
                            (database.serverBoundaries[node].startz <= endz && endz <= database.serverBoundaries[node].endz) ||
                            (startz < database.serverBoundaries[node].startz && database.serverBoundaries[node].endz < endz) ||
                            (startz + database.GridResolutionZ <= database.serverBoundaries[node].endz) ||
                            (database.serverBoundaries[node].startz <= endz - database.GridResolutionZ))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw ex;
            }
        }

        private void DistributeParticles(SQLUtility.TrackingInputRequest[] input_particles, bool round, int kernelSizeZ, int kernelSizeY, int kernelSizeX,
            List<SQLUtility.TrackingInputRequest>[] particles_per_node)
        {
            try
            {
                for (int k = 0; k < input_particles.Length; k++)
                {
                    for (int i = 0; i < database.serverCount; i++)
                    {
                        if (TestParticleForNode(input_particles[k], round, kernelSizeZ, kernelSizeY, kernelSizeX, i))
                        {
                            input_particles[k].numberOfNodes++;
                            if (input_particles[k].numberOfNodes > 1)
                            {
                                input_particles[k].evaluate = true;
                            }
                            else
                            {
                                input_particles[k].evaluate = false;
                            }

                            if (particles_per_node[i] == null)
                            {
                                particles_per_node[i] = new List<SQLUtility.TrackingInputRequest>();
                            }
                            particles_per_node[i].Add(input_particles[k]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw ex;
            }
        }
    }
}
