using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ImportData
{
    /// <summary>
    /// Cache a verticle stacking of slabs into memory for fast
    /// loading into the database.
    /// 
    /// Reads a components * resolution * resolucion * (edge_width + cube_resolution + edge_width) region of memory
    /// </summary>
    class FileCache
    {
        public byte [][] data;

        string data_dir = "";
        string prefix = "V";
        int resolution = 512;
        int cube_resolution = 64;
        int z_slices = 4;
        int z_width = 128;  /* (resolution/z_slices) */
        int components = 3;
        int base_slice = -1;


        int zbase;
        // base of Z values currently cached
        public int ZBase { get { return zbase; } }

        public FileCache(string data_dir, string prefix, int resolution, int cube_resolution, int z_slices, int components)
        {
            zbase = -1;
            base_slice = -1;
            this.data_dir = data_dir;
            this.prefix = prefix;
            this.resolution = resolution;
            this.cube_resolution = cube_resolution;
            this.z_slices = z_slices;
            this.z_width = resolution / z_slices;
            this.components = components;
        }

        /// <summary>
        /// read data for each of the components from the files
        /// the number of the slices that are needed for a complete cube is required
        /// and the width of the edge to be replicated
        /// </summary>
        public void readFiles(int timestep, int base_slice, int edge_width)
        {
            if (this.base_slice == base_slice)
            {
                return;
            }

            // Z-coord of the base region loaded into memory
            this.base_slice = base_slice;
            //this.zbase = base_slice * z_width;

            int stripeSize = resolution * resolution * (cube_resolution + 2 * edge_width) * sizeof(float);

            if (data == null)
            {
                data = new byte[components][];
                for (int i = 0; i < data.Length; i++)
                    data[i] = new byte[stripeSize];
            }

            int slice;
            int fileOffset = 0;

            slice = base_slice * cube_resolution / z_width;

            //fileOffset comes into play only when z_width is bigger than the cube_resolution
            //therefore, it will be equal to 0 in most cases
            fileOffset = (base_slice * cube_resolution - slice * z_width);
            
            //if fileOffset is larger than 0 the bottom edge is located in the same file as the current slice
            if (fileOffset > 0)
                ReadBottomEdge(timestep, slice, edge_width, fileOffset);
            //otherwise it is located in the file containing the previous slice
            else
                ReadBottomEdge(timestep, slice - 1, edge_width, fileOffset);

            //we need to determine how many consecutive files need to be read 
            //in order to store enough data in the slab/stripe for a complete cube
            int slice_count = 1;
            int availableData = z_width - fileOffset;
            while (availableData < cube_resolution + edge_width)
            {
                slice_count++;
                availableData += z_width;
            }

            //the bottom edge has been read-in
            //therefore, the remaining data to be read-in is resolution * resolution * (cube_resolution + edge_width)
            int bytesToRead = resolution * resolution * (cube_resolution + edge_width) * sizeof(float);
            //this flag will be raised if there are more than 1 files to read
            bool flag = false;
            //if there are more than 1 files to read we can only read resolution * resolution * z_width bytes at a time
            //we also need to take into consideration fileOffset for the first file
            if (slice_count > 1)
            {
                bytesToRead = resolution * resolution * (z_width - fileOffset)  * sizeof(float);
                flag = true;
            }

            int dataOffset = resolution * resolution * edge_width * sizeof(float);

            for (int sliceCount = 0; sliceCount < slice_count; sliceCount++)
            {
                slice = (base_slice * cube_resolution / z_width + sliceCount) % z_slices;
                
                //in some cases we do not need to read the entire last file
                //we only need to read as much as it is left to complete the slab/stripe
                //during each iteration, we have read resolution * resolution * z_width bytes
                //possibly with the exception of the first iteration if fileOffset was greater than 0
                //therefore what is left to read is as below
                if (flag && (sliceCount == slice_count - 1))
                    bytesToRead = resolution * resolution * (cube_resolution + edge_width - z_width * sliceCount + fileOffset) * sizeof(float);

                //if fileOffset is greater than 0 we need to make sure we are reading from the appropriate place in the first file
                if ((sliceCount == 0) && (fileOffset > 0))
                    ReadData(timestep, slice, bytesToRead, dataOffset, fileOffset * resolution * resolution * sizeof(float));
                else
                    ReadData(timestep, slice, bytesToRead, dataOffset);

                dataOffset += bytesToRead;

                //after the first file has been read-in we need to make sure that we read-in the rest in their entirety
                bytesToRead = resolution * resolution * z_width * sizeof(float);
            }
        }

        /// <summary>
        /// Read data from a single file for all components
        /// </summary>
        private void ReadData(int timestep, int slice, int bytesToRead, int dataOffset)
        {
            FileStream fs;
            string filename = GetFileName(timestep, slice);
            if (File.Exists(filename))
            {
                fs = File.OpenRead(filename);
                long filesize = fs.Length;
                fs.Seek(0, SeekOrigin.Begin);
                Console.WriteLine("Reading {0}...", filename);
                for (int i = 0; i < data.Length; i++)
                {
                    // We need to seek to the appropriate location 
                    // in the case that no the entire file is read-in
                    fs.Seek(i * filesize / components, SeekOrigin.Begin);
                    int bytesRead = fs.Read(data[i], dataOffset, bytesToRead);
                    if (bytesRead < bytesToRead)
                    {
                        throw new IOException(String.Format("Unexpected EOF: {0}!", filename));
                    }
                    Console.WriteLine("Read {0} bytes for component {1}.", bytesRead, i);
                }
                fs.Close();
            }
            else
            {
                throw new IOException(String.Format("File not found: {0}!", filename));
            }
        }

        /// <summary>
        /// Read data from a single file for all components
        /// fileOffset specifies the position in the file, from which to start reading
        /// It is assumed that all the remaining data (beginning at fileOffset) is read-in
        /// </summary>
        private void ReadData(int timestep, int slice, int bytesToRead, int dataOffset, int fileOffset)
        {
            FileStream fs;
            string filename = GetFileName(timestep, slice);
            if (File.Exists(filename))
            {
                fs = File.OpenRead(filename);
                fs.Seek(0, SeekOrigin.Begin);
                Console.WriteLine("Reading {0}...", filename);
                for (int i = 0; i < data.Length; i++)
                {
                    fs.Seek(fileOffset, SeekOrigin.Current);
                    int bytesRead = fs.Read(data[i], dataOffset, bytesToRead);
                    if (bytesRead < bytesToRead)
                    {
                        throw new IOException(String.Format("Unexpected EOF: {0}!", filename));
                    }
                    Console.WriteLine("Read {0} bytes for component {1}.", bytesRead, i);
                }
                fs.Close();
            }
            else
            {
                throw new IOException(String.Format("File not found: {0}!", filename));
            }
        }

        /// <summary>
        /// Read data for the bottom (previous) replicated edge along z
        /// TODO: Need to consider the situation when z_width is less than edge_width
        /// TODO: This edge can be cached/reused from the top replicated edge of the previous slab/stripe of data
        /// </summary>
        private void ReadBottomEdge(int timestep, int slice, int edge_width, int fileOffset)
        {
            if (slice < 0)
                slice = z_slices - 1;
            FileStream fs;
            string filename = GetFileName(timestep, slice);
            if (File.Exists(filename))
            {
                fs = File.OpenRead(filename);
                fs.Seek(0, SeekOrigin.Begin);
                Console.WriteLine("Reading {0}...", filename);
                int count = resolution * resolution * edge_width * sizeof(float);
                for (int i = 0; i < data.Length; i++)
                {
                    fs.Seek(resolution * resolution * (z_width - fileOffset - edge_width) * sizeof(float), SeekOrigin.Current);
                    int bytesRead = fs.Read(data[i], 0, count);
                    if (bytesRead == 0)
                    {
                        throw new IOException(String.Format("Unexpected EOF: {0}!", filename));
                    }
                    Console.WriteLine("Read {0} bytes for component {1}.", bytesRead, i);
                }
                fs.Close();
            }
            else
            {
                throw new IOException(String.Format("File not found: {0}!", filename));
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
                return String.Format("{0}{1}{2:00000}.000", data_dir, prefix, time);
            }
        }

        public void Close()
        {
        }
    }
}


