using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Turbulence.TurbLib.DataTypes;

namespace Turbulence.REST.Models
{
    public class TurbulenceRequest
    {
        public string Dataset { get; set; }
        public string Operation { get; set; }
        public int Points;
        //public Point3[] Points { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
    }
}