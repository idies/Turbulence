using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace ImportData
{
    /// <summary>
    /// Cache a portion of a data file into memory for fast
    /// loading into the database.
    /// </summary>
    public class FileCache
    {
        private MemoryMappedFile[] MMFile;
        private MemoryMappedViewStream[] MMFileStream;
        private string[] filenames;
        private byte[][] data;

        string data_dir = "";
        string[] suffix = { "u", "v", "w" };
        int headerSize = 36920;
        // the resolution as [z,y,x]
        // the file stores a replicated point on each end
        int[] resolution = { 513, 2049, 2049 };
        int[] cache_dimensions;
        int components = 3;
        long firstbox = -1;
        long lastbox = -1;
        int timestep = -1;
        float timestepFormatting = 10000.0f;
        int pointDataSize = sizeof(double); // size of a single data point
        //long offset = 0; // offset into the file

        int[] base_coordinates = { -1, -1, -1 };
        // coordiantes of the lower left corner (base) of values currently cached
        public int[] Base { get { return base_coordinates; } }

        public FileCache(string data_dir, string[] suffix, int[] resolution)
        {
            this.data_dir = data_dir;
            this.suffix = suffix;
            this.components = suffix.Length;
            this.resolution = resolution;
            this.filenames = new string[components];
            this.cache_dimensions = new int[3];
        }

        ~FileCache()
        {
            Close();
        }


        public void readFiles(int timestep, long firstbox, long lastbox, int atomSize)
        {
            if ((this.timestep == timestep) && (this.firstbox == firstbox) && (this.lastbox == lastbox))
            {
                return;
            }

            // Z-coord of the base region loaded into memory
            this.firstbox = firstbox;
            this.lastbox = lastbox;
            base_coordinates = new Morton3D(firstbox).GetValues();
            int[] end_coordinates = new Morton3D(lastbox).GetValues();

            long offset = (long)headerSize +
                    ((long)base_coordinates[2] +
                        (long)base_coordinates[1] * (long)resolution[2] +
                        (long)base_coordinates[0] * (long)resolution[1] * (long)resolution[2]) * (long)pointDataSize;

            // We need to be able to access data points in the 3d region [base_coordinates, end_coordiantes]
            // the data size for the stream includes all data in the file between those locations in space 
            // (which is more than the size of 3d atom as the entire volume is linearized)
            long end_offset = (long)headerSize +
                    ((long)(end_coordinates[2] + atomSize) +
                        (long)(end_coordinates[1] + atomSize) * (long)resolution[2] +
                        (long)(end_coordinates[0] + atomSize) * (long)resolution[1] * (long)resolution[2]) * (long)pointDataSize;

            long dataSize = end_offset - offset;

            if (this.timestep == timestep)
            {
                // We should already have a memory mapped file for this time step
                // we only need to adjust the file stream
                for (int i = 0; i < components; i++)
                {
                    MMFileStream[i].Close();
                    MMFileStream[i] = MMFile[i].CreateViewStream(offset, dataSize, MemoryMappedFileAccess.Read);
                }
            }
            else
            {
                // We need to create a new memory mapped file for the current time step
                this.timestep = timestep;

                if (MMFile == null)
                {
                    MMFile = new MemoryMappedFile[components];
                    MMFileStream = new MemoryMappedViewStream[components];
                }
                else
                {
                    for (int i = 0; i < components; i++)
                    {
                        MMFileStream[i].Close();
                        MMFile[i].Dispose();
                    }
                }

                for (int i = 0; i < components; i++)
                {
                    string filename = GetFileName(timestep / timestepFormatting, i);
                    if (File.Exists(filename))
                    {
                        filenames[i] = timestep + "." + suffix[i];
                        MMFile[i] = MemoryMappedFile.CreateFromFile(filename, FileMode.Open, filenames[i], 0, MemoryMappedFileAccess.Read);
                        MMFileStream[i] = MMFile[i].CreateViewStream(offset, dataSize, MemoryMappedFileAccess.Read);
                        //ReadHeader(MMFileStream[i]);

                        //byte[] buffer = new byte[sizeof(double)];
                        //MMFileStream[i].Read(buffer, 0, sizeof(double));
                        //Array.Reverse(buffer, 0, sizeof(double));
                        //Console.WriteLine("Reading header \n time = {0}", System.BitConverter.ToDouble(buffer, 0));
                    }
                }
            }
        }


        public void readFilesIntoByteArray(int timestep, long firstbox, long lastbox, int atomSize)
        {
            if ((this.timestep == timestep) && (this.firstbox == firstbox) && (this.lastbox == lastbox))
            {
                return;
            }

            // Z-coord of the base region loaded into memory
            this.firstbox = firstbox;
            this.lastbox = lastbox;
            base_coordinates = new Morton3D(firstbox).GetValues();
            int[] end_coordinates = new Morton3D(lastbox).GetValues();

            cache_dimensions[0] = end_coordinates[0] + atomSize - base_coordinates[0];
            cache_dimensions[1] = end_coordinates[1] + atomSize - base_coordinates[1]; 
            cache_dimensions[2] = end_coordinates[2] + atomSize - base_coordinates[2];

            long offset0 = (long)headerSize +
                          ((long)base_coordinates[2] +
                           (long)base_coordinates[1] * (long)resolution[2] +
                           (long)base_coordinates[0] * (long)resolution[1] * (long)resolution[2]) * 
                           (long)pointDataSize;

            // We need to be able to access data points in the 3d region [base_coordinates, end_coordiantes]
            // the data size for the stream includes all data in the file between those locations in space 
            // (which is more than the size of 3d atom as the entire volume is linearized)
            long dataSize = (long)cache_dimensions[2] * (long)cache_dimensions[1] * (long)cache_dimensions[0] * (long)pointDataSize;

            if (dataSize > int.MaxValue)
            {
                throw new Exception("Error! The size of the cache requested is bigger than the maximum size.");
            }
                        
            this.timestep = timestep;

            if (data == null)
            {
                this.data = new byte[components][];

                for (int i = 0; i < components; i++)
                {
                    data[i] = new byte[dataSize];
                }
            }

            for (int i = 0; i < components; i++)
            {
                int n = 0;
                int bytesRead = 0;
                int dataoffset = 0;
                int bytesToRead = cache_dimensions[2] * pointDataSize;
                int count = bytesToRead;

                string filename = GetFileName(timestep / timestepFormatting, i);
                if (File.Exists(filename))
                {
                    FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                    for (int z = 0; z < cache_dimensions[0]; z++)
                    {
                        long offset1 = offset0 + (long)z * (long)resolution[2] * (long)resolution[1] * (long)pointDataSize;
                        for (int y = 0; y < cache_dimensions[1]; y++)
                        {
                            long offset = offset1 + (long)y * (long)resolution[2] * (long)pointDataSize;
                            fs.Seek(offset, SeekOrigin.Begin);
                            // Read an entire row of values along x
                            while (bytesRead < bytesToRead)
                            {
                                n = fs.Read(data[i], dataoffset, count);
                                if (n == 0)
                                {
                                    throw new IOException(String.Format("Unexpected EOF: {0}!", filename));
                                }
                                bytesRead += n;
                                count -= n;
                            }
                            count = bytesToRead;
                            dataoffset += bytesRead;
                            bytesRead = 0;
                        }
                    }
                }
                else
                {
                    throw new IOException(String.Format("File not found: {0}!", filename));
                }
            }
        }


        /// <summary>
        /// Caches a 512^3 region from the file in memory
        /// starting at the position of the given atom
        /// </summary>
        /// <param name="timestep">the timestep requested</param>
        /// <param name="atom">requested 3d coordiantes encoded with the Morton index</param>
        /// <param name="atomSize">the size of a database atom</param>
        public void readFiles(int timestep, long atom, int atomSize)
        {
            if ((this.timestep == timestep) && (this.firstbox <= atom) && (this.lastbox >= atom))
            {
                return;
            }

            DateTime start = DateTime.Now;
            // Z-coord of the base region loaded into memory
            this.firstbox = atom;
            this.lastbox = firstbox + new Morton3D(0, 0, 512) - new Morton3D(0, 0, atomSize);
            base_coordinates = new Morton3D(firstbox).GetValues();
            int[] end_coordinates = new Morton3D(lastbox).GetValues();

            cache_dimensions[0] = end_coordinates[0] + atomSize - base_coordinates[0];
            cache_dimensions[1] = end_coordinates[1] + atomSize - base_coordinates[1];
            cache_dimensions[2] = end_coordinates[2] + atomSize - base_coordinates[2];

            long offset0 = (long)headerSize +
                          ((long)base_coordinates[2] +
                           (long)base_coordinates[1] * (long)resolution[2] +
                           (long)base_coordinates[0] * (long)resolution[1] * (long)resolution[2]) *
                           (long)pointDataSize;

            // We need to be able to access data points in the 3d region [base_coordinates, end_coordiantes]
            // the data size for the stream includes all data in the file between those locations in space 
            // (which is more than the size of 3d atom as the entire volume is linearized)
            long dataSize = (long)cache_dimensions[2] * (long)cache_dimensions[1] * (long)cache_dimensions[0] * (long)pointDataSize;

            if (dataSize > int.MaxValue)
            {
                throw new Exception("Error! The size of the cache requested is bigger than the maximum size.");
            }

            this.timestep = timestep;

            if (data == null)
            {
                this.data = new byte[components][];
            }

            for (int i = 0; i < components; i++)
            {
                data[i] = new byte[dataSize];
            }

            for (int i = 0; i < components; i++)
            {
                int n = 0;
                int bytesRead = 0;
                int dataoffset = 0;
                int bytesToRead = cache_dimensions[2] * pointDataSize;
                int count = bytesToRead;

                string filename = GetFileName(timestep / timestepFormatting, i);
                if (File.Exists(filename))
                {
                    FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                    for (int z = 0; z < cache_dimensions[0]; z++)
                    {
                        long offset1 = offset0 + (long)z * (long)resolution[2] * (long)resolution[1] * (long)pointDataSize;
                        for (int y = 0; y < cache_dimensions[1]; y++)
                        {
                            long offset = offset1 + (long)y * (long)resolution[2] * (long)pointDataSize;
                            fs.Seek(offset, SeekOrigin.Begin);
                            // Read an entire row of values along x
                            while (bytesRead < bytesToRead)
                            {
                                n = fs.Read(data[i], dataoffset, count);
                                if (n == 0)
                                {
                                    throw new IOException(String.Format("Unexpected EOF: {0}!", filename));
                                }
                                bytesRead += n;
                                count -= n;
                            }
                            count = bytesToRead;
                            dataoffset += bytesRead;
                            bytesRead = 0;
                        }
                    }
                    fs.Close();
                    TimeSpan span = DateTime.Now - start;
                    Console.WriteLine("Cached a 512^3 region in memory in {0}s", span.TotalSeconds);
                }
                else
                {
                    throw new IOException(String.Format("File not found: {0}!", filename));
                }
            }
        }

        public string[] FileName { 
            get { 
                return filenames; 
            } 
        }

        private void ReadHeader(MemoryMappedViewStream stream)
        {
            Console.WriteLine("IsLittleEndian = {0}", System.BitConverter.IsLittleEndian);
            byte[] buffer = new byte[sizeof(double)];
            stream.Read(buffer, 0, sizeof(double));
            Array.Reverse(buffer, 0, sizeof(double));
            double time = System.BitConverter.ToDouble(buffer, 0);
            Console.WriteLine("Reading header \n time = {0}", time);
            stream.Read(buffer, 0, sizeof(double));
            Array.Reverse(buffer, 0, sizeof(double));
            double nx = System.BitConverter.ToDouble(buffer, 0);
            Console.WriteLine("nx = {0}", nx);
            stream.Read(buffer, 0, sizeof(double));
            Array.Reverse(buffer, 0, sizeof(double));
            double ny = System.BitConverter.ToDouble(buffer, 0);
            Console.WriteLine("ny = {0}", ny);
            stream.Read(buffer, 0, sizeof(double));
            Array.Reverse(buffer, 0, sizeof(double));
            double nz = System.BitConverter.ToDouble(buffer, 0);
            Console.WriteLine("nz = {0}", nz);
            for (int i = 0; i < nx + ny + nz; i++)
            {
                stream.Read(buffer, 0, sizeof(double));
                Array.Reverse(buffer, 0, sizeof(double));
                Console.WriteLine("value = {0}", System.BitConverter.ToDouble(buffer, 0));
            }
        }
        
        public void CopyData(long sourceIndex, byte[] destinationArray, int destinationIndex, int length, int component)
        {
            MMFileStream[component].Seek(sourceIndex, SeekOrigin.Begin);
            MMFileStream[component].Read(destinationArray, destinationIndex, length);
        }

        public void CopyDataFromByteArray(long z, long y, long x, byte[] destinationArray, int destinationIndex, int length, int component)
        {
            long sourceIndex = (z * cache_dimensions[2] * cache_dimensions[1] + y * cache_dimensions[2] + x) * pointDataSize;
            Array.Copy(data[component], sourceIndex, destinationArray, destinationIndex, length);
        }

        private string GetFileName(float time, int component)
        {
            return String.Format(@"{0}n2048_d0.25_Ro0.002_{2:0000.0000}.{1}", data_dir, suffix[component], time);
        }

        public void Close()
        {
            for (int i = 0; i < components; i++)
            {
                if (MMFile != null)
                {
                    MMFileStream[i].Close();
                    MMFile[i].Dispose();
                }
                if (data != null)
                {
                    data[i] = null;
                }
            }
        }

    }
}


