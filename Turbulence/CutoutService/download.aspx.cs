using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using System.Runtime.InteropServices;
using HDF5DotNet;

using Turbulence.TurbLib;
using Turbulence.TurbLib.DataTypes;
using Turbulence.SQLInterface;
using TurbulenceService;
using System.Threading;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
//
namespace CutoutService
{
    class SingleGlobalInstance : IDisposable
    {
        public bool hasHandle = false;
        Mutex mutex;

        private void InitMutex()
        {
            string appGuid = ((GuidAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(GuidAttribute), false).GetValue(0)).Value.ToString();
            string mutexId = string.Format("Global\\{{{0}}}", appGuid);
            mutex = new Mutex(false, mutexId);

            var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
            var securitySettings = new MutexSecurity();
            securitySettings.AddAccessRule(allowEveryoneRule);
            mutex.SetAccessControl(securitySettings);
        }

        public SingleGlobalInstance(int timeOut)
        {
            InitMutex();
            try
            {
                if (timeOut < 0)
                    hasHandle = mutex.WaitOne(Timeout.Infinite, false);
                else
                    hasHandle = mutex.WaitOne(timeOut, false);

                if (hasHandle == false)
                    throw new TimeoutException("Timeout waiting for exclusive access on SingleInstance");
            }
            catch (AbandonedMutexException)
            {
                hasHandle = true;
            }
        }


        public void Dispose()
        {
            if (mutex != null)
            {
                if (hasHandle)
                    mutex.ReleaseMutex();
                mutex.Dispose();
            }
        }
    }
    public partial class download : System.Web.UI.Page
    {
        
        [DllImport("hdf5dll.dll")]
        public static extern unsafe int H5Pset_fapl_core(int fapl_id, ulong increment, uint backing_store);
        [DllImport("hdf5dll.dll")]
        public static extern unsafe ulong H5Fget_file_image(int file_id, void* buf_ptr, ulong buf_len);
        [DllImport("msvcrt.dll")]
        public static extern unsafe void* malloc(ulong size);
        [DllImport("msvcrt.dll")]
        public static extern unsafe void free(void* memblock);
        [DllImport("msvcrt.dll")]
        public static extern unsafe void memcpy(void* dest, void* src, ulong count);
         
        Database database = new Database("turbinfo", false);
        AuthInfo authInfo = new AuthInfo("turbinfo", false);
        Log log = new Log("turbinfo", false);

        //Maximum download size allowed
        long maxsize = 12L * 256L * 350L * 512L; //525MB
        
        //if (FileLoadException != null) throw FileLoadException;
        protected unsafe void Page_Load(object sender, EventArgs e)
        {
            //DateTime start = DateTime.Now;

            //The URL the user entered up to the .aspx
            String myPath = Request.FilePath.ToString();

            //The URL the user entered including everything after the .aspx
            String fullPath = Request.Url.AbsolutePath.ToString();

            // Unsafe byte stream for response - needs to be pre-declared for cleanup in Exception
            byte* filebuff = null;
            String filename = null;

            if (myPath.Equals(fullPath))
            {
                string baseUrl = Request.Url.Scheme + "://" + Request.Url.Authority + Request.ApplicationPath.TrimEnd('/') + "/";
                // the cutout.aspx page is part of the main Website
                baseUrl = baseUrl.Replace("cutout/", "");
                Response.Redirect(baseUrl + "cutout.aspx");
                Response.End();
            }

            //Treat everything after the URL of the page as a parameter
            String args_ = fullPath.Substring(fullPath.IndexOf(myPath) + myPath.Length);

            //Test validity of parameters given
            if (!System.Text.RegularExpressions.Regex.IsMatch(args_,
                @"^/[a-zA-Z0-9_]+([-.][a-zA-Z0-9_]+)*/(isotropic1024fine|isotropic1024coarse|mhd1024|channel|mixing)/[upbad]{1,4}/\d+,\d+/\d+,\d+/\d+,\d+/\d+,\d+(/\d+,\d+,\d+,\d+(/\d+)?)?/?$"))
            {
                Response.StatusCode = 400;
                Response.Write("Error: Bad request. URL should be in the format of /authToken/dataset/fields/time,timesteps/xlow,xwidth/ylow,ywidth/zlow,zwidth[/time_step,x_step,y_step,z_step/filterwidth]");
                Response.End();
            }

            String[] args = args_.Split('/');

            String authToken = args[1],
                dataset = args[2],
                fields = args[3],
                trange = args[4],
                xrange = args[5],
                yrange = args[6],
                zrange = args[7];

            int time_step = 1;
            int x_step = 1;
            int y_step = 1;
            int z_step = 1;
            if (args.Length >= 9 && !args[8].Equals(""))
            {
                String[] steps = args[8].Split(',');
                int.TryParse(steps[0], out time_step);
                int.TryParse(steps[1], out x_step);
                int.TryParse(steps[2], out y_step);
                int.TryParse(steps[3], out z_step);
            }
            int filter_width = 1;
            if (args.Length >= 10 && !args[9].Equals(""))
            {
                int.TryParse(args[9], out filter_width);
            }

            String[] t_ = trange.Split(','),
                x_ = xrange.Split(','),
                y_ = yrange.Split(','),
                z_ = zrange.Split(',');

            int tlow = int.Parse(t_[0]), twidth = int.Parse(t_[1]),
                xlow = int.Parse(x_[0]), xwidth = int.Parse(x_[1]),
                ylow = int.Parse(y_[0]), ywidth = int.Parse(y_[1]),
                zlow = int.Parse(z_[0]), zwidth = int.Parse(z_[1]);

            int thigh = tlow + twidth - 1,
                xhigh = xlow + xwidth - 1,
                yhigh = ylow + ywidth - 1,
                zhigh = zlow + zwidth - 1;

            // Number of points in the result set is a function of the step size.
            int tsize = (twidth + time_step - 1) / time_step,
                xsize = (xwidth + x_step - 1) / x_step,
                ysize = (ywidth + y_step - 1) / y_step,
                zsize = (zwidth + z_step - 1) / z_step;
            
            dataset = DataInfo.findDataSet(dataset);
            DataInfo.DataSets dataset_enum = (DataInfo.DataSets)Enum.Parse(typeof(DataInfo.DataSets), dataset);

            CheckBoundaries(dataset_enum, tlow, thigh, xlow, xhigh, ylow, yhigh, zlow, zhigh);

            //Prevent people from trying to download the entire database
            long dlsize = DetermineSize(fields, tsize, xsize, ysize, zsize);
            if (dlsize > maxsize)
            {
                Response.Write(String.Format("Maximum file size exceeded. Size of requested data: {0}, Maximum size: {1}", FormatSize(dlsize), FormatSize(maxsize)));
                Response.End();
            }

            
                try
                {


                    filename = System.IO.Path.GetTempFileName();//+ dataset + "_" + xrange + "_" + yrange + "_" + zrange + ".h5";
                    H5FileId file;
                    H5DataTypeId intTypeId;
                    /*System lock added due to threading issues with H5.Open and H5D.create*/
                    /*Tested with 16 current connections 256x256x16 with 2 timesteps on SkyDev and it produced no errors*/
                    using (new SingleGlobalInstance(1000))
                    {
                        H5.Open();

                        H5PropertyListId fapl = H5P.create(H5P.PropertyListClass.FILE_ACCESS);
                        H5Pset_fapl_core(fapl.Id, (ulong)1048576 * 16, (uint)0);

                        //Create a temp file and serve it to the user
                        file = H5F.create(filename, H5F.CreateMode.ACC_TRUNC, new H5PropertyListId(H5P.Template.DEFAULT), fapl);
                        //H5FileId file = H5F.create(filename, H5F.CreateMode.ACC_TRUNC);
                        //Write header info into the HDF5 file
                        int[] start_data = { tlow, xlow, ylow, zlow };
                        int[] size_data = { tsize, xsize, ysize, zsize };
                        long[] vec4size = { 4 };
                        H5DataSpaceId vec4 = H5S.create_simple(1, vec4size);
                        H5DataSetId start_field = H5D.create(file, "_start", H5T.H5Type.NATIVE_INT, vec4);
                        intTypeId = H5T.copy(H5T.H5Type.NATIVE_INT);
                        H5D.write<int>(start_field, intTypeId, new H5Array<int>(start_data));

                        H5DataSetId size_field = H5D.create(file, "_size", H5T.H5Type.NATIVE_INT, vec4);
                        H5D.write<int>(size_field, intTypeId, new H5Array<int>(size_data));

                        H5D.close(size_field);
                        H5D.close(start_field);
                        H5S.close(vec4);
                    }
                    //Bitfield indicating which fields are present
                    long[] one = { 1 };
                    H5DataSpaceId singleval = H5S.create_simple(1, one);
                    int[] dsenum = { (int)dataset_enum };
                    int[] contents_data = { (fields.Contains("u") ? 0x01 : 0x00) |
                                                (fields.Contains("p") ? 0x02 : 0x00) |
                                                (fields.Contains("b") ? 0x04 : 0x00) |
                                                (fields.Contains("a") ? 0x08 : 0x00) |
                                                (fields.Contains("d") ? 0x16 : 0x00) };

                    //The enum of the dataset requested
                    H5DataSetId ds_field = H5D.create(file, "_dataset", H5T.H5Type.NATIVE_INT, singleval);

                    H5D.write<int>(ds_field, intTypeId, new H5Array<int>(dsenum));

                    H5DataSetId contents_field = H5D.create(file, "_contents", H5T.H5Type.NATIVE_INT, singleval);
                    H5D.write<int>(contents_field, intTypeId, new H5Array<int>(contents_data));

                    H5D.close(contents_field);
                    H5D.close(ds_field);
                    H5S.close(singleval);
                    H5T.close(intTypeId);

                    long size = (long)xsize * (long)ysize * (long)zsize * 3 * sizeof(float);
                    long[] datasize = { zsize, ysize, xsize, 3 };
                    H5DataTypeId dataType = H5T.copy(H5T.H5Type.NATIVE_FLOAT);
                    H5DataSpaceId dataspace = H5S.create_simple(4, datasize, datasize);

                    AuthInfo.AuthToken auth = authInfo.VerifyToken(authToken, xwidth * ywidth * zwidth);
                    int num_virtual_servers = 1;
                    database.Initialize(dataset_enum, num_virtual_servers);

                    int pieces = 1, dz = (int)datasize[0];

                    //If a single buffer exceeds 2gb, then split into multiple pieces
                    //if (size > 2000000000L)
                    if (size > 256000000L)
                    {
                        pieces = (int)Math.Ceiling((float)size / 256000000L);

                        //Round up to nearest power of 2
                        pieces--;
                        pieces |= pieces >> 1;
                        pieces |= pieces >> 2;
                        pieces |= pieces >> 4;
                        pieces |= pieces >> 8;
                        pieces |= pieces >> 16;
                        pieces++;
                        dz = (int)Math.Ceiling((float)zwidth / pieces / 8) * 8;
                    }

                    DataInfo.TableNames tableName;
                    int components;
                    string field;

                    if (fields.Contains("u"))
                    {
                        components = 3;
                        field = "u";

                        tableName = DataInfo.getTableName(dataset_enum, field);
                        object rowid = null;
                        rowid = log.CreateLog(auth.Id, dataset, (int)Worker.Workers.GetRawVelocity,
                            (int)TurbulenceOptions.SpatialInterpolation.None,
                            (int)TurbulenceOptions.TemporalInterpolation.None,
                           xwidth * ywidth * zwidth, tlow * database.Dt * database.TimeInc, thigh * database.Dt * database.TimeInc, null);
                        log.UpdateRecordCount(auth.Id, tsize * xwidth * ywidth * zwidth);

                        if (time_step == 1 && x_step == 1 && y_step == 1 && z_step == 1 && filter_width == 1)
                        {
                            GetRawData_(file, dataType, dataspace, field, pieces, dz, dataset_enum, tableName, components, tlow, thigh, xlow, ylow, zlow, xwidth, ywidth, zwidth);
                        }
                        else
                        {
                            GetFilteredData_(file, dataType, dataspace, field, dataset_enum, tableName, components, tlow, thigh, xlow, ylow, zlow, xwidth, ywidth, zwidth,
                                time_step, x_step, y_step, z_step, filter_width);
                        }

                        log.UpdateLogRecord(rowid, database.Bitfield);
                        log.Reset();
                    }

                    if (fields.Contains("b"))
                    {
                        components = 3;
                        field = "b";

                        tableName = DataInfo.getTableName(dataset_enum, field);
                        object rowid = null;
                        rowid = log.CreateLog(auth.Id, dataset, (int)Worker.Workers.GetRawMagnetic,
                            (int)TurbulenceOptions.SpatialInterpolation.None,
                            (int)TurbulenceOptions.TemporalInterpolation.None,
                           xwidth * ywidth * zwidth, tlow * database.Dt * database.TimeInc, thigh * database.Dt * database.TimeInc, null);
                        log.UpdateRecordCount(auth.Id, tsize * xwidth * ywidth * zwidth);

                        if (time_step == 1 && x_step == 1 && y_step == 1 && z_step == 1 && filter_width == 1)
                        {
                            GetRawData_(file, dataType, dataspace, field, pieces, dz, dataset_enum, tableName, components, tlow, thigh, xlow, ylow, zlow, xwidth, ywidth, zwidth);
                        }
                        else
                        {
                            GetFilteredData_(file, dataType, dataspace, field, dataset_enum, tableName, components, tlow, thigh, xlow, ylow, zlow, xwidth, ywidth, zwidth,
                                time_step, x_step, y_step, z_step, filter_width);
                        }

                        log.UpdateLogRecord(rowid, database.Bitfield);
                        log.Reset();
                    }

                    if (fields.Contains("a"))
                    {
                        components = 3;
                        field = "a";

                        tableName = DataInfo.getTableName(dataset_enum, field);
                        object rowid = null;
                        rowid = log.CreateLog(auth.Id, dataset, (int)Worker.Workers.GetRawPotential,
                            (int)TurbulenceOptions.SpatialInterpolation.None,
                            (int)TurbulenceOptions.TemporalInterpolation.None,
                           xwidth * ywidth * zwidth, tlow * database.Dt * database.TimeInc, thigh * database.Dt * database.TimeInc, null);
                        log.UpdateRecordCount(auth.Id, tsize * xwidth * ywidth * zwidth);

                        if (time_step == 1 && x_step == 1 && y_step == 1 && z_step == 1 && filter_width == 1)
                        {
                            GetRawData_(file, dataType, dataspace, field, pieces, dz, dataset_enum, tableName, components, tlow, thigh, xlow, ylow, zlow, xwidth, ywidth, zwidth);
                        }
                        else
                        {
                            GetFilteredData_(file, dataType, dataspace, field, dataset_enum, tableName, components, tlow, thigh, xlow, ylow, zlow, xwidth, ywidth, zwidth,
                                time_step, x_step, y_step, z_step, filter_width);
                        }

                        log.UpdateLogRecord(rowid, database.Bitfield);
                        log.Reset();
                    }

                    size = (long)xsize * (long)ysize * (long)zsize * 1 * sizeof(float);
                    datasize[3] = 1;
                    H5S.close(dataspace);
                    dataspace = H5S.create_simple(4, datasize, datasize);

                    //If a single buffer exceeds 2gb, then split into multiple pieces
                    //if (size > 2000000000L)
                    if (size > 256000000L)
                    {
                        pieces = (int)Math.Ceiling((float)size / 256000000L);

                        //Round up to nearest power of 2
                        pieces--;
                        pieces |= pieces >> 1;
                        pieces |= pieces >> 2;
                        pieces |= pieces >> 4;
                        pieces |= pieces >> 8;
                        pieces |= pieces >> 16;
                        pieces++;
                        dz = (int)Math.Ceiling((float)zwidth / pieces / 8) * 8;
                    }

                    if (fields.Contains("p"))
                    {
                        components = 1;
                        field = "p";

                        tableName = DataInfo.getTableName(dataset_enum, field);
                        object rowid = null;
                        rowid = log.CreateLog(auth.Id, dataset, (int)Worker.Workers.GetRawPressure,
                            (int)TurbulenceOptions.SpatialInterpolation.None,
                            (int)TurbulenceOptions.TemporalInterpolation.None,
                           xwidth * ywidth * zwidth, tlow * database.Dt * database.TimeInc, thigh * database.Dt * database.TimeInc, null);
                        log.UpdateRecordCount(auth.Id, tsize * xwidth * ywidth * zwidth);

                        if (time_step == 1 && x_step == 1 && y_step == 1 && z_step == 1 && filter_width == 1)
                        {
                            GetRawData_(file, dataType, dataspace, field, pieces, dz, dataset_enum, tableName, components, tlow, thigh, xlow, ylow, zlow, xwidth, ywidth, zwidth);
                        }
                        else
                        {
                            GetFilteredData_(file, dataType, dataspace, field, dataset_enum, tableName, components, tlow, thigh, xlow, ylow, zlow, xwidth, ywidth, zwidth,
                                time_step, x_step, y_step, z_step, filter_width);
                        }

                        log.UpdateLogRecord(rowid, database.Bitfield);
                        log.Reset();
                    }

                    if (fields.Contains("d"))
                    {
                        components = 1;
                        field = "d";

                        tableName = DataInfo.getTableName(dataset_enum, field);
                        object rowid = null;
                        rowid = log.CreateLog(auth.Id, dataset, (int)Worker.Workers.GetRawDensity,
                            (int)TurbulenceOptions.SpatialInterpolation.None,
                            (int)TurbulenceOptions.TemporalInterpolation.None,
                           xwidth * ywidth * zwidth, tlow * database.Dt * database.TimeInc, thigh * database.Dt * database.TimeInc, null);
                        log.UpdateRecordCount(auth.Id, tsize * xwidth * ywidth * zwidth);

                        if (time_step == 1 && x_step == 1 && y_step == 1 && z_step == 1 && filter_width == 1)
                        {
                            GetRawData_(file, dataType, dataspace, field, pieces, dz, dataset_enum, tableName, components, tlow, thigh, xlow, ylow, zlow, xwidth, ywidth, zwidth);
                        }
                        else
                        {
                            GetFilteredData_(file, dataType, dataspace, field, dataset_enum, tableName, components, tlow, thigh, xlow, ylow, zlow, xwidth, ywidth, zwidth,
                                time_step, x_step, y_step, z_step, filter_width);
                        }

                        log.UpdateLogRecord(rowid, database.Bitfield);
                        log.Reset();
                    }

                    H5S.close(dataspace);
                    H5T.close(dataType);
                    H5F.flush(file, H5F.Scope.GLOBAL);

                    GC.Collect();
                    ulong filesize;

                    Response.Clear();
                    Response.ClearContent();
                    Response.ClearHeaders();
                    //Prevent browser from displaying the content as an html page
                    Response.ContentType = "";

                    //Make the file pop up as a download
                    Response.AppendHeader("Content-Disposition", String.Format("attachment; filename=\"{0}.h5\"", dataset));

                    unsafe
                    {
                        filesize = H5Fget_file_image(file.Id, null, 0);

                        filebuff = (byte*)malloc(filesize);
                        H5Fget_file_image(file.Id, filebuff, filesize);

                        H5F.close(file);
                        Response.AppendHeader("Content-length", filesize.ToString());
                        Response.AppendHeader("X-ServerName", System.Environment.MachineName);

                        //Send file piece by piece
                        ulong stride = 256000000L;
                        //ulong stride = 536870912;
                        /*Why do we allocate this much space for each request?*/
                        /*
                        byte[] temp = new byte[stride];
                        ulong parts = filesize / stride;
                        for (ulong p = 0; p < parts; p++)
                        {
                            fixed (byte* temp_ = temp)
                                memcpy(temp_, filebuff + stride * p, stride);
                            Response.BinaryWrite(temp);
                            Response.Flush();
                        }
                         */
                        /*No stride test */
                        //byte[] temp = new byte[stride];
                        byte[] temp = new byte[filesize];
                        fixed (byte* temp_ = temp)
                            memcpy(temp_, filebuff, filesize);
                        Response.BinaryWrite(temp);
                        Response.Flush();
                        ulong parts = 0;


                        temp = null;

                        if (filesize % stride > 0)
                        {
                            byte[] temp2 = new byte[filesize % stride];
                            fixed (byte* temp_ = temp2)
                                memcpy(temp_, filebuff + stride * parts, filesize % stride);
                            Response.BinaryWrite(temp2);
                            Response.Flush();
                            temp2 = null;
                        }
                        free(filebuff);
                    }

                    H5.Close();

                    System.IO.File.Delete(filename);

                    //System.IO.StreamWriter time_log = new System.IO.StreamWriter(@"C:\Documents and Settings\kalin\My Documents\downloadPageTime.txt", true);
                    //time_log.WriteLine(DateTime.Now - start);
                    //time_log.Close();

                }

                catch (Exception ex)
                {
                    // if we terminate prematurely, we'll leak all this RAM, and that's no good, now is it?
                    if (filebuff != null)
                    {
                        free(filebuff);
                    }
                    if (!String.IsNullOrEmpty(filename) && System.IO.File.Exists(filename))
                    {
                        System.IO.File.Delete(filename);
                    }
                    //Response.AddHeader("X-ServerName", System.Environment.MachineName);
                    Response.StatusCode = 500;
                    Response.Write("<font color=\"red\">Error! " + ex.Message.ToString() + "</font><br /><br />Details:<br />" + System.Environment.MachineName + "<br />" + ex.ToString());
                    Response.End();
                }
                //finally
                //{

                //}
                //mut.ReleaseMutex();
            
        }

        private void CheckBoundaries(DataInfo.DataSets dataset, int tlow, int thigh, int xlow, int xhigh, int ylow, int yhigh, int zlow, int zhigh)
        {
            switch (dataset)
            {
                case DataInfo.DataSets.isotropic1024coarse:
                case DataInfo.DataSets.isotropic1024fine:
                case DataInfo.DataSets.mhd1024:
                    if (!(tlow >= 0 && thigh < 1025) ||
                       !(xlow >= 0 && xhigh < 1024) ||
                       !(ylow >= 0 && yhigh < 1024) ||
                       !(zlow >= 0 && zhigh < 1024))
                    { Response.Write("The requested region is out of bounds"); Response.End(); }
                    break;
                case DataInfo.DataSets.mixing:
                    if (!(tlow >= 0 && thigh < 1015) ||
                       !(xlow >= 0 && xhigh < 1024) ||
                       !(ylow >= 0 && yhigh < 1024) ||
                       !(zlow >= 0 && zhigh < 1024))
                    { Response.Write("The requested region is out of bounds"); Response.End(); }
                    break;
                case DataInfo.DataSets.channel:
                    if (!(tlow >= 0 && thigh < 4000) ||
                       !(xlow >= 0 && xhigh < 2048) ||
                       !(ylow >= 0 && yhigh < 512) ||
                       !(zlow >= 0 && zhigh < 1536))
                    { Response.Write("The requested region is out of bounds"); Response.End(); }
                    break;
                default:
                    throw new Exception(String.Format("Invalid dataset specified!"));
            }
        }

        //Calculates the how large the file will be.
        public long DetermineSize(string fields, int T, int X, int Y, int Z)
        {
            int comps = 0;
            if (fields.Contains("u")) comps += 3;
            if (fields.Contains("p")) comps += 1;
            if (fields.Contains("a")) comps += 3;
            if (fields.Contains("b")) comps += 3;
            if (fields.Contains("d")) comps += 1;

            return (long)comps * (long)sizeof(float) * (long)(T) * (long)(X) * (long)(Y) * (long)(Z);
        }

        public void GetRawData_(H5FileId file, H5DataTypeId dataType, H5DataSpaceId dataspace, string field,
            int pieces, int dz,
            DataInfo.DataSets dataset_enum, DataInfo.TableNames tableName, int components, int tlow, int thigh,
            int xlow, int ylow, int zlow, int xsize, int ysize, int zsize)
        {
            H5PropertyListId H5P_DEFAULT = new H5PropertyListId(H5P.Template.DEFAULT);

            for (int t = tlow; t <= thigh; t++)
            {
                DataInfo.verifyTimeInRange(dataset_enum, (float)t * database.Dt * database.TimeInc);

                H5DataSetId datasetId = H5D.create(file, String.Format(field + "{0:00000}", t * 10), dataType, dataspace);

                for (int p = 0; p < pieces; p++)
                {
                    int chunklen = (p + 1) * dz <= zsize ? dz : zsize - p * dz;
                    if (chunklen == 0) break;

                    byte[] buffer = database.GetRawData(dataset_enum, tableName, t * database.Dt * database.TimeInc, components, xlow, ylow, zlow + p * dz, xsize, ysize, chunklen);

                    long chunksize = (long)xsize * (long)ysize * (long)chunklen * (long)components;
                    float[] buffer_ = new float[chunksize];
                    for (int f = 0; f < chunksize; f++)
                        buffer_[f] = BitConverter.ToSingle(buffer, f * sizeof(float));
                    long[] start = { p * dz, 0, 0, 0 };
                    long[] count = { chunklen, ysize, xsize, components };

                    H5DataSpaceId memspace = H5S.create_simple(4, count);

                    H5S.selectHyperslab(dataspace, H5S.SelectOperator.SET, start, count);

                    H5D.write<float>(datasetId, dataType, memspace, dataspace, H5P_DEFAULT, new H5Array<float>(buffer_));

                    //TODO: add a H5D.flush here

                    buffer_ = null;
                    buffer = null;
                    //GC.Collect();
                }
                // TODO: Check for open objects here and close everything
                H5D.close(datasetId);
            }
        }

        public void GetFilteredData_(H5FileId file, H5DataTypeId dataType, H5DataSpaceId dataspace, string field,
            DataInfo.DataSets dataset_enum, DataInfo.TableNames tableName, int components, int tlow, int thigh,
            int xlow, int ylow, int zlow, int xwidth, int ywidth, int zwidth, 
            int tstep, int xstep, int ystep, int zstep, int filter_width)
        {
            int xsize = (xwidth + xstep - 1) / xstep,
                ysize = (ywidth + ystep - 1) / ystep,
                zsize = (zwidth + zstep - 1) / zstep;

            H5PropertyListId H5P_DEFAULT = new H5PropertyListId(H5P.Template.DEFAULT);

            for (int t = tlow; t <= thigh; t += tstep)
            {
                DataInfo.verifyTimeInRange(dataset_enum, (float)t * database.Dt * database.TimeInc);

                byte[] buffer = database.GetFilteredData(dataset_enum, tableName, t * database.Dt * database.TimeInc, components, xlow, ylow, zlow, xwidth, ywidth, zwidth, 
                    xstep, ystep, zstep, filter_width);

                H5DataSetId datasetId = H5D.create(file, String.Format(field + "{0:00000}", t * 10), dataType, dataspace);

                long chunksize = (long)xsize * (long)ysize * (long)zsize * (long)components;
                float[] buffer_ = new float[chunksize];
                for (int f = 0; f < chunksize; f++)
                    buffer_[f] = BitConverter.ToSingle(buffer, f * sizeof(float));
                long[] start = { 0, 0, 0, 0 };
                long[] count = { zsize, ysize, xsize, components };

                H5DataSpaceId memspace = H5S.create_simple(4, count);

                H5S.selectHyperslab(dataspace, H5S.SelectOperator.SET, start, count);

                H5D.write<float>(datasetId, dataType, memspace, dataspace, H5P_DEFAULT, new H5Array<float>(buffer_));

                //TODO: add a H5D.flush here

                buffer_ = null;
                buffer = null;
                //GC.Collect();

                // TODO: Check for open objects here and close everything
                H5D.close(datasetId);
            }
        }

        public String FormatSize(long size)
        {
            String Text;
            if (size >= 10L * 1024L * 1024L * 1024L) Text = (size / 1024 / 1024 / 1024).ToString() + "GB";
            else if (size >= 10L * 1024L * 1024L) Text = (size / 1024 / 1024).ToString() + "MB";
            else if (size >= 10L * 1024L) Text = (size / 1024).ToString() + "KB";
            else Text = (size).ToString() + "B";
            return Text;
        }
    }
}