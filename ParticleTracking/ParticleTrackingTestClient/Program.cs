using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParticleTrackingTestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            ParticleTrackingClient ptclient = new ParticleTrackingClient();
            ptclient.TestParticleTracking();
            Console.ReadKey();
        }
    }
}
