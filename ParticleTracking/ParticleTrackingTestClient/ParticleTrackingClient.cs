using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ServiceModel;
using ParticleTracking;

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
        bool round;
        int kernelSizeX;
        int kernelSizeY;
        int kernelSizeZ;

        public void TestParticleTracking()
        {
            try
            {
                bool development = true;
                Turbulence.TurbLib.TurbulenceOptions.SpatialInterpolation spatialInterp = Turbulence.TurbLib.TurbulenceOptions.SpatialInterpolation.Lag6;
                round = false;

                DataInfo.DataSets dataset = DataInfo.DataSets.mhd1024;
                database = new Database("turbinfo", true);
                database.selectServers(dataset);

                SQLUtility.TrackingInputRequest[] particles;
                CreatInput(1, out particles);

                List<SQLUtility.TrackingInputRequest>[] particles_per_node;
                particles_per_node = new List<SQLUtility.TrackingInputRequest>[database.serverCount];                
                Turbulence.TurbLib.TurbulenceOptions.GetKernelSize(spatialInterp, ref kernelSizeX, ref kernelSizeY, database.channel_grid, (int)Turbulence.SQLInterface.Worker.Workers.GetPosition);
                kernelSizeZ = kernelSizeX;
                DistributeParticles(particles, round, kernelSizeX, kernelSizeY, kernelSizeZ, particles_per_node);

                factories = new DuplexChannelFactory<IParticleTrackingService>[database.serverCount];
                channels = new IParticleTrackingService[database.serverCount];

                //TODO: Consider starting a new Task to handle callbacks from each node.
                InstanceContext instanceContext = new InstanceContext(this);
                for (int i = 0; i < database.serverCount; i++)
                {
                    if (particles_per_node[i] != null && particles_per_node[i].Count > 0)
                    {
                        NetTcpBinding binding = new NetTcpBinding();
                        // 1 hour receive timeout
                        binding.ReceiveTimeout = new System.TimeSpan(1, 0, 0);
                        //TODO: change the address based on the server name
                        EndpointAddress address = new EndpointAddress("net.tcp://localhost:8090/ParticleTrackingService");
                        factories[i] = new DuplexChannelFactory<IParticleTrackingService>(instanceContext, binding, address);
                        channels[i] = factories[i].CreateChannel();
                        channels[i].Init(database.servers[i], database.databases[i], (short)dataset, "velocity08", database.atomDim, (int)spatialInterp, development);
                        Console.WriteLine("called Init() on server {0}, database {1}", database.servers[i], database.databases[i]);
                    }
                }
                for (int i = 0; i < database.serverCount; i++)
                {
                    if (particles_per_node[i] != null && particles_per_node[i].Count > 0)
                    {
                        channels[i].DoParticleTrackingWork(particles_per_node[i]);
                    }
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
            Console.WriteLine("Proxy closed!");
        }

        private void CreatInput(int num_particles, out SQLUtility.TrackingInputRequest[] particles)
        {
            particles = new SQLUtility.TrackingInputRequest[num_particles];
            Random rand = new Random();
            for (int i = 0; i < num_particles; i++)
            {
                float x, y, z;
                //x = (float)(2 * Math.PI * rand.NextDouble());
                //y = (float)(Math.PI * rand.NextDouble());
                //z = (float)(Math.PI * rand.NextDouble());
                x = 4.0000f;
                y = 3.1415f;
                z = 1.0000f;
                particles[i] = new SQLUtility.TrackingInputRequest(i, 0, 0, new Turbulence.TurbLib.DataTypes.Point3(x, y, z), new Turbulence.TurbLib.DataTypes.Point3(),
                    new Turbulence.TurbLib.DataTypes.Vector3(), 0.0f, 0.4f, 0.001f, true, 0, true);
                    //(i, new Turbulence.TurbLib.DataTypes.Point3(x, y, z), new Turbulence.TurbLib.DataTypes.Point3(), 0, 0, 0.4f, 0.001f, true, false, 0, 0);

                Console.WriteLine(String.Format("x = {0}, y = {1}, z = {2}", x, y, z));
            }
        }

        void IParticleTrackingServiceCallback.DoneParticles(List<Turbulence.SQLInterface.SQLUtility.TrackingInputRequest> done_particles)
        {
            for (int i = 0; i < done_particles.Count; i++)
            {
                if (!done_particles[i].crossed_boundary)
                {
                    if (done_particles[i].done)
                    {
                        Console.WriteLine("particle {0}: Final position: x = {1}, y = {2}, z = {3}", done_particles[i].request,
                            done_particles[i].pos.x, done_particles[i].pos.y, done_particles[i].pos.z);
                    }
                    else
                    {
                        Console.WriteLine("particle {0} at position: x = {1}, y = {2}, z = {3} not done, but not crossed either!", done_particles[i].request,
                            done_particles[i].pos.x, done_particles[i].pos.y, done_particles[i].pos.z);
                    }
                }
                else
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
                            }
                            else
                            {
                                ResetParticle(particle_to_be_reassigned);
                                AssignParticleToMultipleNodes(particle_to_be_reassigned, round, kernelSizeZ, kernelSizeY, kernelSizeX);
                            }
                        }
                    }
                }                
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
            List<int> nodes = new List<int>(database.serverCount);
            for (int i = 0; i < database.serverCount; i++)
            {
                if (TestParticleForNode(input_particle, round, kernelSizeZ, kernelSizeY, kernelSizeX, i))
                {
                    input_particle.numberOfNodes++;
                    nodes.Add(i);
                }
            }
            // NOTE: It's important to send the particle to each node only after we update the number of nodes it was assigned to.
            foreach (int node in nodes)
            {
                channels[node].DoParticleTrackingWorkOneParticle(input_particle);
            }
        }

        private bool TestParticleForNode(SQLUtility.TrackingInputRequest input_particle, bool round, int kernelSizeZ, int kernelSizeY, int kernelSizeX, int node)
        {
            int Z = database.GetIntLocZ(input_particle.pos.z, round);
            int Y = database.GetIntLocY(input_particle.pos.y, round, kernelSizeY);
            int X = database.GetIntLocX(input_particle.pos.x, round);
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

        private void DistributeParticles(SQLUtility.TrackingInputRequest[] input_particles, bool round, int kernelSizeZ, int kernelSizeY, int kernelSizeX,
            List<SQLUtility.TrackingInputRequest>[] particles_per_node)
        {
            for (int k = 0; k < input_particles.Length; k++)
            {
                for (int i = 0; i < database.serverCount; i++)
                {
                    if (TestParticleForNode(input_particles[k], round, kernelSizeZ, kernelSizeY, kernelSizeX, i))
                    {
                        input_particles[k].numberOfNodes++;
                        if (particles_per_node[i] == null)
                        {
                            particles_per_node[i] = new List<SQLUtility.TrackingInputRequest>();
                        }
                        particles_per_node[i].Add(input_particles[k]);
                    }
                }
            }
        }
    }
}
