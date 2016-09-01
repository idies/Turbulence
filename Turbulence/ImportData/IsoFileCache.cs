using System;
using System.Collections.Generic;
using System.Text;
using System.IO;


namespace IsoImportData
{
    /// <summary>
    /// Cache a verticle stacking of slabs into memory for fast
    /// loading into the database.
    /// 
    /// Reads a 1024x1024x(8*slices) region of memory
    /// </summary>
    public class IsoFileCache
    {
        public byte [][] data;
        public bool filenotfound = false;
        string data_dir = "";
        string prefix = "vel_vector";
        protected int[] resolution = { 1024, 1024, 1024 };
        int z_slices = 128;
        int z_width = 8;  /* (resolution/z_slices) */
        int components = 6; /* junk, ux, uy, uz, p, junk */
        //protected int components = 3;
        protected long firstbox = -1;
        protected long lastbox = -1;
        protected int timestep = -1;

        int slice_count = 8;
        int base_slice;
        //protected int[] base_coordinates = { -1, -1, -1 };
        protected int[] base_coordinates = { -1, 0, 0 }; //Due to Z slicing, all files have 0 x and 0 y as base.

        public int[] Base { get { return base_coordinates; } }
        protected int pointDataSize = sizeof(float); // size of a single data point
        //long offset = 0; // offset into the file
        public int PointDataSize { get { return pointDataSize; } }
        protected int[] cache_dimensions = {1024, 1024, 1024};
        
        int zbase;
        // base of Z values currently cached
        public int ZBase { get { return zbase; } }

        public IsoFileCache(string data_dir) //, string prefix what is the prefix for??
        {
            this.zbase = -1;
            this.base_slice = -1;
            this.data_dir = data_dir;
            
            //this.prefix = prefix;
        }


        public void readFiles(int timestep, int base_slice, int slice_count)
        {
            if ((this.base_slice == base_slice) && (this.slice_count == slice_count) )
            {
                //Console.Write("base slice hit");
                return;
            }

            // Z-coord of the base region loaded into memory
            this.base_slice = base_slice;
            this.zbase = base_slice * z_width;
            this.Base[0] = this.zbase;
            this.slice_count = slice_count;           
            //base_coordinates = new Morton3D(firstbox).GetValues(); //Recently added--something is fishy.
            //components * size of float
            int filesize = resolution[1] * resolution[2] * z_width *  6 * 4; 
            
            if (data == null)
            {
                this.data = new byte[components][];
            }
            for (int i = 0; i < components; i++)
            {
                //Console.WriteLine("filesize = {0}, slice={1}, comp = {2}", filesize, slice_count, components);
                //Console.WriteLine("Creating buffer of {0} bytes...", filesize * slice_count / components);
                data[i] = new byte[filesize*slice_count/components];
            }
            //data = new byte[filesize * slice_count];
            
            int dataoffset;
            //Console.Write("In Iso Filecache.  {0}, {1}", slice_count, filesize);
            for (int sliceCount = 0; sliceCount < slice_count; sliceCount++) {
                dataoffset = filesize * sliceCount/components;
                int slice = (base_slice + sliceCount) % z_slices;

                FileStream fs;
                string filename = GetFileName(timestep, slice);
                if (File.Exists(filename))
                {
                    fs = File.OpenRead(filename);
                    //fs.Seek(0, SeekOrigin.Begin);
                    //Console.WriteLine("Reading {0}...", filename);
                    int count = filesize;
                    int componentsize = filesize / components;
                    int rc = 0;
                    int bytesRead = 0;

                    float firstval, secondval, thirdval, fourthval = 0;
                    int c = 0;
                    while (bytesRead < filesize)
                    {
                        for (int i = 0; i < components; i++)
                        {
                            /* Data format is junk, ux, uy, uz, p, junk */
                            /*                 0     1   2   3   4   5 */
                                //Read one byte at a time.  Not sure how bad of performance this will be, but the file is interleaved.
                                rc = fs.Read(data[i], c+dataoffset, 4);
                                if (rc == 0)
                                {
                                    throw new IOException(String.Format("Unexpected EOF: {0}!", filename));
                                }
                                //if (i == 5 && c < 20)//This is the first value. Try and print it.
                                if (false)
                                {
                                    firstval = BitConverter.ToSingle(data[1], c);
                                    secondval = BitConverter.ToSingle(data[2], c);
                                    thirdval = BitConverter.ToSingle(data[3], c);
                                    fourthval = BitConverter.ToSingle(data[4], c);

                                    Console.WriteLine("Floats: {0}, {1}, {2}, {3}, dataoffset is {4}, c is {5} rc = {6}", firstval, secondval, thirdval,
                                        fourthval, dataoffset, c, rc);
                                }
                                bytesRead += rc;
                                count -= rc;
                            
                          }
                            c = c + 4;
                    }
                    //Console.WriteLine("Read {0} bytes.", bytesRead);
                    fs.Close();
                    
                }
                else
                {
                    this.filenotfound = true;
                    throw new IOException(String.Format("File not found: {0}!", filename));
                    

                }
            }
        }

        /* New function used for parallel import */
     
        public void readFilestest(int timestep, long firstbox, long lastbox, int atomSize)
        {
            if ((this.firstbox == firstbox) && (this.lastbox == lastbox))
            {
                //Console.Write("Cache hit");
                return;
            }

            // Z-coord of the base region loaded into memory
            //this.zbase = base_slice * z_width;
            //this.Base[0] = this.zbase;
            // Z-coord of the base region loaded into memory
            this.firstbox = firstbox;
            this.lastbox = lastbox;
            base_coordinates = new Morton3D(firstbox).GetValues();
            int[] end_coordinates = new Morton3D(lastbox).GetValues();

            long offset = 
                    ((long)base_coordinates[2] +
                        (long)base_coordinates[1] * (long)resolution[2] +
                        (long)base_coordinates[0] * (long)resolution[1] * (long)resolution[2]) * (long)pointDataSize;

            // We need to be able to access data points in the 3d region [base_coordinates, end_coordiantes]
            // the data size for the stream includes all data in the file between those locations in space 
            // (which is more than the size of 3d atom as the entire volume is linearized)
            long end_offset = 
                    ((long)(end_coordinates[2] + atomSize) +
                        (long)(end_coordinates[1] + atomSize) * (long)resolution[2] +
                        (long)(end_coordinates[0] + atomSize) * (long)resolution[1] * (long)resolution[2]) * (long)pointDataSize;

            long dataSize = end_offset - offset;

            //components * size of float
            int xsize = end_coordinates[0] - base_coordinates[0];
            int ysize = end_coordinates[1] - base_coordinates[1];
            int zsize = end_coordinates[2] - base_coordinates[2];

            int filesize = xsize * ysize * zsize * 6 * 4;//acutally should be called buffer size or something

            if (data == null)
            {
                this.data = new byte[components][];
            }
            for (int i = 0; i < components; i++)
            {
                Console.WriteLine("filesize = {0}, slice={1}, comp = {2}", filesize, slice_count, components);
                Console.WriteLine("Creating buffer of {0} bytes...", filesize * slice_count / components);
                data[i] = new byte[filesize * slice_count / components];
            }
            //data = new byte[filesize * slice_count];

            int dataoffset;
            Console.Write("In Iso Filecache.  {0}, {1}", slice_count, filesize);
            for (int sliceCount = 0; sliceCount < slice_count; sliceCount++)
            {
                dataoffset = filesize * sliceCount / components;
                int slice = (base_slice + sliceCount) % z_slices;

                FileStream fs;
                string filename = GetFileName(timestep, slice);
                if (File.Exists(filename))
                {
                    fs = File.OpenRead(filename);
                    fs.Seek(0, SeekOrigin.Begin);
                    //Console.WriteLine("Reading {0}...", filename);
                    int count = filesize;
                    int componentsize = filesize / components;
                    int rc = 0;
                    int bytesRead = 0;


                    int c = 0;
                    while (bytesRead < filesize)
                    {
                        for (int i = 0; i < components; i++)
                        {
                            /* Data format is junk, ux, uy, uz, p, junk */
                            /*                 0     1   2   3   4   5 */
                            //Read one byte at a time.  Not sure how bad of performance this will be, but the file is interleaved.
                            rc = fs.Read(data[i], c + dataoffset, 4);
                            if (rc == 0)
                            {
                                throw new IOException(String.Format("Unexpected EOF: {0}!", filename));
                            }
                            bytesRead += rc;
                            count -= rc;

                        }
                        c = c + 4;
                    }
                    //Console.WriteLine("Read {0} bytes from file {1}", bytesRead, filename);
                    fs.Close();

                }
                else
                {
                    throw new IOException(String.Format("File not found: {0}!", filename));
                }
            }
        }

        private string GetFileName(int time, int slice)
        {
            if (z_slices > 1)
            {
                return String.Format("{0}{1}{2:00000}.{3:000}", data_dir, prefix, time, slice);
            }
            else
            {
                return String.Format("{0}{1}{2:00000}", data_dir, prefix, time);
            }
        }

        public void Close() {

        }

        public virtual void CopyDataFromByteArray(long z, long y, long x, byte[] destinationArray, int destinationIndex, int length, int component)
        {
            long sourceIndex = (z * cache_dimensions[2] * cache_dimensions[1] + y * cache_dimensions[2] + x) * pointDataSize;
            //if (sourceIndex + length > 33488928) Console.WriteLine("Source array is {0}, index: {1}, length: {2}", data[1].Length, sourceIndex, length);
            
            Array.Copy(data[component], sourceIndex, destinationArray, destinationIndex, length);
        }
        public virtual void CopyDataFromByteArrayDoubleToFloat(int z, int y, int x, byte[] destinationArray, int destinationIndex, int component)
        {
            int doublesize = sizeof(double);
            int sourceIndex = (z * cache_dimensions[2] * cache_dimensions[1] + y * cache_dimensions[2] + x) * doublesize;
            float float_value = (float)BitConverter.ToDouble(data[component], sourceIndex);
            Array.Copy(BitConverter.GetBytes(float_value), 0, destinationArray, destinationIndex, sizeof(float));
        }
    }

}


