using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using TurbulenceService;
using Turbulence.TurbLib;
using System.IO;
using Turbulence.TurbLib.DataTypes;
using System.Configuration;
using System.Net.Http.Headers;
using SciServer.Logging;
using System.Web;
using Newtonsoft.Json;
using Turbulence.REST.Models;

namespace Turbulence.REST.Controllers
{
    public class TurbulenceController : ApiController
    {
        [ExceptionHandle]
        public HttpResponseMessage Post(string dataset, string operation)
        {
            Logger log = (HttpContext.Current.ApplicationInstance as MvcApplication).Log;

            IEnumerable<string> values;
            string userid = null;

            if (ControllerContext.Request.Headers.TryGetValues("X-Auth-Token", out values))
            {
                try
                {
                    string token = values.First();
                    var userAccess = Keystone.Authenticate(token);
                    userid = userAccess.User.Id;
                }
                catch (NotAuthorizedException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    NotAuthorizedException ex2 = new NotAuthorizedException("Authentication failed", ex);
                    ex2.Data["UserId"] = userid;
                    throw ex2;
                }

                string key = ConfigurationManager.AppSettings["turb_testkey"];
                var service = new TurbulenceService.TurbulenceService();

                var task = Request.Content.ReadAsStringAsync();
                task.Wait();

                Point3[] points = ParsePoints(task.Result);

                string field = "", spatialInterpolation = "None", temporalInterpolation = "None";
                float time = 0, filterwidth = 0, spacing = 0, startTime = 0, endTime = 0, dt = 0, threshold = 0;
                int x = 0, xwidth = 0, y = 0, ywidth = 0, z = 0, zwidth = 0;
                var parameters = Request.GetQueryNameValuePairs();

                foreach (KeyValuePair<string, string> pair in parameters)
                {
                    switch (pair.Key)
                    {
                        case "field": field = pair.Value; break;
                        case "spatialinterpolation": spatialInterpolation = pair.Value; break;
                        case "temporalinterpolation": temporalInterpolation = pair.Value; break;
                        case "time": time = float.Parse(pair.Value); break;
                        case "startTime": startTime = float.Parse(pair.Value); break;
                        case "endTime": endTime = float.Parse(pair.Value); break;
                        case "filterwidth": filterwidth = float.Parse(pair.Value); break;
                        case "spacing": spacing = float.Parse(pair.Value); break;
                        case "dt": dt = float.Parse(pair.Value); break;
                        case "threshold": threshold = float.Parse(pair.Value); break;
                        case "x": x = int.Parse(pair.Value); break;
                        case "y": y = int.Parse(pair.Value); break;
                        case "z": z = int.Parse(pair.Value); break;
                        case "xwidth": xwidth = int.Parse(pair.Value); break;
                        case "ywidth": ywidth = int.Parse(pair.Value); break;
                        case "zwidth": zwidth = int.Parse(pair.Value); break;
                        default: break;
                    }
                }

                var interpolation = new InterpolationOptions(spatialInterpolation, temporalInterpolation);

                TurbulenceRequest req = new TurbulenceRequest();
                req.Dataset = dataset;
                req.Operation = operation;
                req.Parameters = parameters.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
                req.Points = points.Length;

                Message msg = log.CreateCustomMessage("TURBULENCE", JsonConvert.SerializeObject(req));
                msg.UserId = userid;
                log.SendMessage(msg);

                object result;
                switch (operation.ToLower())
                {
                    case "boxfilter":
                        result = service.GetBoxFilter(key, dataset, field, time, filterwidth, points);
                        break;
                    case "boxfiltergradient":
                        result = service.GetBoxFilterGradient(key, dataset, field, time, filterwidth, spacing, points);
                        break;
                    case "getboxfiltersgs":
                        result = service.GetBoxFilterSGS(key, dataset, field, time, filterwidth, points);
                        break;
                    case "density":
                        result = service.GetDensity(key, dataset, time, interpolation.Spatial, interpolation.Temporal, points);
                        break;
                    case "densitygradient":
                        result = service.GetDensityGradient(key, dataset, time, interpolation.Spatial, interpolation.Temporal, points);
                        break;
                    case "densityhessian":
                        result = service.GetDensityHessian(key, dataset, time, interpolation.Spatial, interpolation.Temporal, points);
                        break;
                    case "force":
                        result = service.GetForce(key, dataset, time, interpolation.Spatial, interpolation.Temporal, points);
                        break;
                    case "laplacianofgradient":
                        result = service.GetLaplacianOfGradient(key, dataset, field, time, interpolation.Spatial, interpolation.Temporal, points);
                        break;
                    case "magneticfield":
                        result = service.GetMagneticField(key, dataset, time, interpolation.Spatial, interpolation.Temporal, points);
                        break;
                    case "magneticfieldgradient":
                        result = service.GetMagneticFieldGradient(key, dataset, time, interpolation.Spatial, interpolation.Temporal, points);
                        break;
                    case "magneticfieldlaplacian":
                        result = service.GetMagneticFieldLaplacian(key, dataset, time, interpolation.Spatial, interpolation.Temporal, points);
                        break;
                    case "magnetichessian":
                        result = service.GetMagneticHessian(key, dataset, time, interpolation.Spatial, interpolation.Temporal, points);
                        break;
                    case "position":
                        result = service.GetPosition(key, dataset, startTime, endTime, dt, interpolation.Spatial, points);
                        break;
                    case "pressure":
                        result = service.GetPressure(key, dataset, time, interpolation.Spatial, interpolation.Temporal, points);
                        break;
                    case "pressuregradient":
                        result = service.GetPressureGradient(key, dataset, time, interpolation.Spatial, interpolation.Temporal, points);
                        break;
                    case "pressurehessian":
                        result = service.GetPressureHessian(key, dataset, time, interpolation.Spatial, interpolation.Temporal, points);
                        break;
                    case "rawdensity":
                        result = service.GetRawDensity(key, dataset, time, x, y, z, xwidth, ywidth, zwidth);
                        break;
                    case "rawmagneticfield":
                        result = service.GetRawMagneticField(key, dataset, time, x, y, z, xwidth, ywidth, zwidth);
                        break;
                    case "rawpressure":
                        result = service.GetRawPressure(key, dataset, time, x, y, z, xwidth, ywidth, zwidth);
                        break;
                    case "rawvectorpotential":
                        result = service.GetRawVectorPotential(key, dataset, time, x, y, z, xwidth, ywidth, zwidth);
                        break;
                    case "rawvelocity":
                        result = service.GetRawVelocity(key, dataset, time, x, y, z, xwidth, ywidth, zwidth);
                        int len = ((byte[])result).Length;
                        log.SendMessage(log.CreateInfoMessage("*** GetRawVelocity result length: " + len));
                        break;
                    case "threshold":
                        result = service.GetThreshold(key, dataset, field, time, threshold, interpolation.Spatial, x, y, z, xwidth, ywidth, zwidth);
                        break;
                    case "vectorpotential":
                        result = service.GetVectorPotential(key, dataset, time, interpolation.Spatial, interpolation.Temporal, points);
                        break;
                    case "vectorpotentialgradient":
                        result = service.GetVectorPotentialGradient(key, dataset, time, interpolation.Spatial, interpolation.Temporal, points);
                        break;
                    case "vectorpotentialhessian":
                        result = service.GetVectorPotentialHessian(key, dataset, time, interpolation.Spatial, interpolation.Temporal, points);
                        break;
                    case "vectorpotentiallaplacian":
                        result = service.GetVectorPotentialLaplacian(key, dataset, time, interpolation.Spatial, interpolation.Temporal, points);
                        break;
                    case "velocity":
                        result = service.GetVelocity(key, dataset, time, interpolation.Spatial, interpolation.Temporal, points);
                        break;
                    case "velocityandpressure":
                        result = service.GetVelocityAndPressure(key, dataset, time, interpolation.Spatial, interpolation.Temporal, points);
                        break;
                    case "velocitybatch":
                        result = service.GetVelocityBatch(key, dataset, time, interpolation.Spatial, interpolation.Temporal, points);
                        break;
                    case "velocitygradient":
                        result = service.GetVelocityGradient(key, dataset, time, interpolation.Spatial, interpolation.Temporal, points);
                        break;
                    case "velocityhessian":
                        result = service.GetVelocityHessian(key, dataset, time, interpolation.Spatial, interpolation.Temporal, points);
                        break;
                    case "velocitylaplacian":
                        result = service.GetVelocityLaplacian(key, dataset, time, interpolation.Spatial, interpolation.Temporal, points);
                        break;
                    case "nullop":
                        result = service.NullOp(key, points);
                        break;
                    default:
                        throw new NotImplementedException("Operation not supported");
                }

                if (result is byte[])
                {
                   
                    HttpResponseMessage response = Request.CreateResponse();
                    Action<Stream, HttpContent, TransportContext> writeToStream = (stream, foo, bar) => {
                        byte[] buf = result as byte[];
                        //stream.Write(buf, 0, buf.Length);
                        
                        int i = 0;
                        int length = 1024*1024;
                        while (i < buf.Length) 
                        {
                            if (length > (buf.Length - i)) { length = buf.Length - i; }
                            stream.Write(buf, i, length);
                            i += length;
                            //stream.Flush();
                        }
                        stream.Close();
                    };
                    response.Content = new PushStreamContent(writeToStream, new MediaTypeHeaderValue("application/octet-stream"));
                    return response;
                }
                else
                {
                    return ControllerContext.Request.CreateResponse(HttpStatusCode.OK, result, "application/json");
                }
            }
            else
            {
                throw new NotAuthorizedException("User not authenticated");
            }
        }

        private Point3[] ParsePoints(string s)
        {
            List<Point3> list = new List<Point3>();
            StringReader reader = new StringReader(s);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] components = line.Split(new char[] { ';', ',', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                Point3 point = new Point3();
                point.x = float.Parse(components[0]);
                point.y = float.Parse(components[1]);
                point.z = float.Parse(components[2]);
                list.Add(point);
            }
            reader.Close();
            return list.ToArray();
        }
    }
}
