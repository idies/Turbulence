using System;
using System.ServiceModel;

namespace ParticleTrackingHost
{
    class Program
    {
        static void Main(string[] args)
        {
            using (ServiceHost host = new ServiceHost(typeof(ParticleTracking.ParticleTrackingService)))
            {
                host.Open();
                Console.WriteLine("Particle tracking host started @ " + DateTime.Now.ToString());
                Console.ReadLine();
            }
        }
    }
}
