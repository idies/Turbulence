using System;
using System.Data;
using System.Collections.Generic;

namespace Turbulence.TurbLib.DataTypes
{

    /// <summary>
    /// Data type for single value results
    /// </summary>
    public struct Singleton
    {
        public float v;
        public Singleton(float v)
        {
            this.v = v;
        }
    }


    /// <summary>
    /// Data structure to request X,Y,Z locations
    /// </summary>
    public struct Point3
    {
        public float x, y, z;
        public Point3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    /// <summary>
    /// Data structure for the velocity vector result
    /// </summary>
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    /// <summary>
    /// Data structure for the velocity vector with pressure result
    /// </summary>
    public struct Vector3P
    {
        public float x, y, z, p;
        public Vector3P(float x, float y, float z, float p)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.p = p;
        }
    }

    /// <summary>
    /// Data structure for the pressure result
    /// </summary>
    public struct Pressure
    {
        public float p;
        public Pressure(float p)
        {
            this.p = p;
        }
    }

    /// <summary>
    /// Data structure for the pressure hessian
    /// </summary>
    public struct PressureHessian
    {
        public float d2pdxdx;
        public float d2pdxdy;
        public float d2pdxdz;
        public float d2pdydy;
        public float d2pdydz;
        public float d2pdzdz;

        public PressureHessian(float d2pdxdx, float d2pdxdy, float d2pdxdz,
            float d2pdydy, float d2pdydz, float d2pdzdz)
        {
            this.d2pdxdx = d2pdxdx;
            this.d2pdxdy = d2pdxdy;
            this.d2pdxdz = d2pdxdz;
            this.d2pdydy = d2pdydy;
            this.d2pdydz = d2pdydz;
            this.d2pdzdz = d2pdzdz;
        }
    }

    /// <summary>
    /// Data structure for the velocity hessian
    /// Also used for the vector potential, and magnetic field hessians
    /// </summary>
    public struct VelocityHessian
    {
        public float d2uxdxdx;
        public float d2uxdxdy;
        public float d2uxdxdz;
        public float d2uxdydy;
        public float d2uxdydz;
        public float d2uxdzdz;
        public float d2uydxdx;
        public float d2uydxdy;
        public float d2uydxdz;
        public float d2uydydy;
        public float d2uydydz;
        public float d2uydzdz;
        public float d2uzdxdx;
        public float d2uzdxdy;
        public float d2uzdxdz;
        public float d2uzdydy;
        public float d2uzdydz;
        public float d2uzdzdz;

        public VelocityHessian(float d2uxdxdx, float d2uxdxdy, float d2uxdxdz,
            float d2uxdydy, float d2uxdydz, float d2uxdzdz,
            float d2uydxdx, float d2uydxdy, float d2uydxdz,
            float d2uydydy, float d2uydydz, float d2uydzdz,
            float d2uzdxdx, float d2uzdxdy, float d2uzdxdz,
            float d2uzdydy, float d2uzdydz, float d2uzdzdz)
        {
            this.d2uxdxdx = d2uxdxdx;
            this.d2uxdxdy = d2uxdxdy;
            this.d2uxdxdz = d2uxdxdz;
            this.d2uxdydy = d2uxdydy;
            this.d2uxdydz = d2uxdydz;
            this.d2uxdzdz = d2uxdzdz;
            this.d2uydxdx = d2uydxdx;
            this.d2uydxdy = d2uydxdy;
            this.d2uydxdz = d2uydxdz;
            this.d2uydydy = d2uydydy;
            this.d2uydydz = d2uydydz;
            this.d2uydzdz = d2uydzdz;
            this.d2uzdxdx = d2uzdxdx;
            this.d2uzdxdy = d2uzdxdy;
            this.d2uzdxdz = d2uzdxdz;
            this.d2uzdydy = d2uzdydy;
            this.d2uzdydz = d2uzdydz;
            this.d2uzdzdz = d2uzdzdz;
        }
    }

    /// <summary>
    /// Also used for Magnetic Field and Vector Potential Gradient
    /// Also used for LaplacianOfVelocityGradient
    /// Also used for the full SGS tensor
    /// </summary>
    public struct VelocityGradient
    {
        public float duxdx;
        public float duxdy;
        public float duxdz;
        public float duydx;
        public float duydy;
        public float duydz;
        public float duzdx;
        public float duzdy;
        public float duzdz;
        public VelocityGradient(float duxdx, float duxdy, float duxdz,
            float duydx, float duydy, float duydz,
            float duzdx, float duzdy, float duzdz)
        {
            this.duxdx = duxdx;
            this.duydx = duydx;
            this.duzdx = duzdx;
            this.duxdy = duxdy;
            this.duydy = duydy;
            this.duzdy = duzdy;
            this.duxdz = duxdz;
            this.duydz = duydz;
            this.duzdz = duzdz;
        }
    }

    /// <summary>
    /// Used for the symmetric SGS tensor.
    /// </summary>
    public struct SGSTensor
    {
        public float xx;
        public float yy;
        public float zz;
        public float xy;
        public float xz;
        public float yz;
        public SGSTensor(float xx, float yy, float zz, float xy, float xz, float yz)
        {
            this.xx = xx;
            this.yy = yy;
            this.zz = zz;
            this.xy = xy;
            this.xz = xz;
            this.yz = yz;
        }
    }

    // We can use Vector3 instead of this.
    /*
    public struct VelocityLaplacian
    {
        public float grad2ux;
        public float grad2uy;
        public float grad2uz;

        public VelocityLaplacian(float grad2ux, float grad2uy, float grad2uz)
        {
            this.grad2ux = grad2ux;
            this.grad2uy = grad2uy;
            this.grad2uz = grad2uz;
        }
    }*/

 
    public struct ParticleTracking
    {
        public float x;
        public float y;
        public float z;
        public Point3 predictor;
        public Vector3 velocity;
        public ParticleTracking(float x, float y, float z, Point3 predictor, Vector3 velocity)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.predictor = predictor;
            this.velocity = velocity;
        }
    }

    /// <summary>
    /// Structure represting forcing information stored on disk (or in the database)
    /// </summary>
    public struct FourierInfo
    {
        public float kx;
        public float ky;
        public float kz;
        public float fxr;
        public float fxi;
        public float fyr;
        public float fyi;
        public float fzr;
        public float fzi;

        public FourierInfo(float kx, float ky, float kz, float fxr, float fxi, float fyr, float fyi, float fzr, float fzi)
        {
            this.kx = kx;
            this.ky = ky;
            this.kz = kz;
            this.fxr = fxr;
            this.fxi = fxi;
            this.fyr = fyr;
            this.fyi = fyi;
            this.fzr = fzr;
            this.fzi = fzi;
        }

    };

    /// <summary>
    /// Structure representing the data needed to advance a particle during particle tracking
    /// Used at web server for reassignment of particles to database servers
    /// When a particle crosses server boundaries during particle tracking
    /// it stores the position, predicting position, velocity, time, endTime and a flag
    /// compute_predictor indicates whether the predictor needs to be computed next 
    /// </summary>
    public struct TrackingInfo
    {
        public Point3 position;
        public Point3 predictor;
        public int timeStep;
        public float time;
        public float endTime;
        public float dt;
        public bool compute_predictor;
        public bool done;

        public TrackingInfo(Point3 position, Point3 predictor, int timeStep, float time, float endTime, float dt, bool compute_predictor, bool done)
        {
            this.position = position;
            this.predictor = predictor;
            this.timeStep = timeStep;
            this.time = time;
            this.endTime = endTime;
            this.dt = dt;
            this.compute_predictor = compute_predictor;
            this.done = done;
        }
    }

    /// <summary>
    /// Data structure for the thresholding results.
    /// </summary>
    public struct ThresholdInfo
    {
        public int x, y, z;
        public float value;

        public ThresholdInfo(int x, int y, int z, float value)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.value = value;
        }
    }

    /// <summary>
    /// Structure representing a server spatial boundaries.
    /// The start and end coordiantes for the data region are stored. Both are inclusive.
    /// </summary>
    public struct ServerBoundaries
    {
        public int startx;
        public int endx;
        public int starty;
        public int endy;
        public int startz;
        public int endz;
        public long startKey;
        public long endKey;

        public ServerBoundaries(Morton3D firstKey, Morton3D lastKey)
        {
            this.startx = firstKey.X;
            this.starty = firstKey.Y;
            this.startz = firstKey.Z;
            this.endx = lastKey.X;
            this.endy = lastKey.Y;
            this.endz = lastKey.Z;
            this.startKey = firstKey;
            this.endKey = lastKey;
        }

        public ServerBoundaries(Morton3D firstBox, Morton3D lastBox, int atomDim)
        {
            this.startx = firstBox.X;
            this.starty = firstBox.Y;
            this.startz = firstBox.Z;
            this.endx = lastBox.X + atomDim - 1;
            this.endy = lastBox.Y + atomDim - 1;
            this.endz = lastBox.Z + atomDim - 1;
            this.startKey = firstBox;
            this.endKey = new Morton3D(endz, endy, endx);
        }

        public ServerBoundaries(int startx, int endx, int starty, int endy, int startz, int endz)
        {
            this.startx = startx;
            this.endx = endx;
            this.starty = starty;
            this.endy = endy;
            this.startz = startz;
            this.endz = endz;
            this.startKey = new Morton3D(startz, starty, startx);
            this.endKey = new Morton3D(endz, endy, endx);
        }

        /// <summary>
        /// Given a structure representing the server's boundaries
        /// and the number of virtual servers
        /// produces the server boundaries for each of the virtual servers
        /// </summary>
        /// <returns></returns>
        public ServerBoundaries[] getVirtualServerBoundaries(int num_virtual_servers)
        {
            int Xresolution = endx - startx + 1;
            int Yresolution = endy - starty + 1;
            int Zresolution = endz - startz + 1;

            // We are going to store all of the spatial regions into a temporary queue
            Queue<ServerBoundaries> tempServerBoundaries = new Queue<ServerBoundaries>(num_virtual_servers);
            tempServerBoundaries.Enqueue(this);

            // In order to determine which spatial region this virtual server is responsible for
            // we split the entire spatial region stored on the server according to the partitioing scheme (z-order)
            // we have to perform log(num_virtual_servers) splits
            for (int j = 1; j < num_virtual_servers; j *= 2)
            {
                // we determine which dimension should be split first
                if (Zresolution >= Yresolution && Zresolution >= Xresolution)
                {
                    // each region should be split in half along Z
                    Zresolution /= 2;
                    Queue<ServerBoundaries> tempQueue = new Queue<ServerBoundaries>();
                    foreach (ServerBoundaries SB in tempServerBoundaries)
                    {
                        ServerBoundaries bottomHalf = new ServerBoundaries(SB.startx, SB.endx, SB.starty, SB.endy, SB.startz, SB.startz + Zresolution - 1);
                        ServerBoundaries topHalf = new ServerBoundaries(SB.startx, SB.endx, SB.starty, SB.endy, SB.startz + Zresolution, SB.endz);
                        tempQueue.Enqueue(bottomHalf);
                        tempQueue.Enqueue(topHalf);
                    }
                    tempServerBoundaries = tempQueue;
                }
                else if (Yresolution >= Zresolution && Yresolution >= Xresolution)
                {
                    // each region should be split in half along Y
                    Yresolution /= 2;
                    Queue<ServerBoundaries> tempQueue = new Queue<ServerBoundaries>();
                    foreach (ServerBoundaries SB in tempServerBoundaries)
                    {
                        ServerBoundaries frontHalf = new ServerBoundaries(SB.startx, SB.endx, SB.starty, SB.starty + Yresolution - 1, SB.startz, SB.endz);
                        ServerBoundaries rearHalf = new ServerBoundaries(SB.startx, SB.endx, SB.starty + Yresolution, SB.endy, SB.startz, SB.endz);
                        tempQueue.Enqueue(frontHalf);
                        tempQueue.Enqueue(rearHalf);
                    }
                    tempServerBoundaries = tempQueue;
                }
                else if (Xresolution >= Zresolution && Xresolution >= Yresolution)
                {
                    // each region should be split in half along X
                    Xresolution /= 2;
                    Queue<ServerBoundaries> tempQueue = new Queue<ServerBoundaries>();
                    foreach (ServerBoundaries SB in tempServerBoundaries)
                    {
                        ServerBoundaries leftHalf = new ServerBoundaries(SB.startx, SB.startx + Xresolution - 1, SB.starty, SB.endy, SB.startz, SB.endz);
                        ServerBoundaries rightHalf = new ServerBoundaries(SB.startx + Xresolution, SB.endx, SB.starty, SB.endy, SB.startz, SB.endz);
                        tempQueue.Enqueue(leftHalf);
                        tempQueue.Enqueue(rightHalf);
                    }
                    tempServerBoundaries = tempQueue;
                }
            }

            return tempServerBoundaries.ToArray();
        }
    }


    
}