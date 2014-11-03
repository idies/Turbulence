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
    public class FileCacheMixing : FileCache
    {
        private byte[][][] largeArray;
        internal const int BLOCK_SIZE = 1073741824;
        internal const int BLOCK_SIZE_LOG2 = 30;

        //private string[] filenames;
        //private byte[][] data;

        //string data_dir = "";
        //string[] suffix = { "u", "v", "w" };
        //int headerSize = 192;
        //// the resolution as [z,y,x]
        //// the file stores a replicated point on each end
        //int[] resolution = { 513, 2049, 2049 };
        //int[] cache_dimensions;
        //int components = 3;
        //long firstbox = -1;
        //long lastbox = -1;
        //int timestep = -1;
        //float timestepFormatting = 10000.0f;
        //int pointDataSize = sizeof(double); // size of a single data point
        ////long offset = 0; // offset into the file

        //int[] base_coordinates = { -1, -1, -1 };
        // coordiantes of the lower left corner (base) of values currently cached
        //public int[] Base { get { return base_coordinates; } }

        public FileCacheMixing(string data_dir, int[] resolution, long headerSize, int components)
            : base(data_dir, new string[] { "" }, resolution, headerSize)
        {
            this.timestepFormatting = 1.0f;
            this.pointDataSize = sizeof(double);
            this.components = components;
        }
        
        public override void readFilesIntoByteArray(int timestep, long firstbox, long lastbox, int atomSize)
        {
            // Each file is read into the cache in 2 steps. 
            // First we read from (0,0,0) up to (511,1023,1023) along (z,y,x)
            // Second we read from (512,0,0) up to (1023,1023,1023) along (z,y,x)
            if (this.timestep == timestep &&
                (firstbox >= this.firstbox && firstbox <= this.lastbox) &&
                (lastbox >= this.firstbox && lastbox <= this.lastbox))
            {
                return;
            }

            base_coordinates = new Morton3D(firstbox).GetValues();
            int[] end_coordinates = new Morton3D(lastbox).GetValues();
            // We want to load all of the data along x and y in order to read from the files sequentially
            // Therefore we expand the end_coordiantes to be as big as the actual resolution.
            end_coordinates[1] = resolution[1] - atomSize;
            end_coordinates[2] = resolution[2] - atomSize;

            // Z-coord of the base region loaded into memory
            this.firstbox = firstbox;
            this.lastbox = new Morton3D(end_coordinates[0], end_coordinates[1], end_coordinates[2]);
            this.timestep = timestep;

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

            if (largeArray == null)
            {
                if (dataSize > BLOCK_SIZE)
                {
                    int numBlocks = (int)(dataSize / BLOCK_SIZE);
                    int NumElementsInLastBlock = BLOCK_SIZE;
                    if (((long)numBlocks * BLOCK_SIZE) < dataSize)
                    {
                        NumElementsInLastBlock = (int)(dataSize - (long)numBlocks * BLOCK_SIZE);
                        numBlocks += 1;
                    }

                    largeArray = new byte[components][][];
                    for (int c = 0; c < components; c++)
                    {
                        largeArray[c] = new byte[numBlocks][];
                        for (int i = 0; i < (numBlocks - 1); i++)
                        {
                            largeArray[c][i] = new byte[BLOCK_SIZE];
                        }
                        largeArray[c][numBlocks - 1] = new byte[NumElementsInLastBlock];
                    }
                }
                else
                {
                    largeArray = new byte[components][][];
                    for (int c = 0; c < components; c++)
                    {
                        largeArray[c] = new byte[1][];
                        largeArray[c][0] = new byte[dataSize];
                    }
                }
            }

            bool exception_flag = true;
            int attempts = 0;
            int max_attempts = 5;
            while (exception_flag && attempts < max_attempts)
            {
                try
                {
                    string filename = GetFileName(timestep / timestepFormatting);
                    FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                    int bytesToRead = 8 * 1024 * 1024;

                    if (File.Exists(filename))
                    {
                        for (int c = 0; c < components; c++)
                        {
                            long offset = offset0 + (long)c * resolution[2] * resolution[1] * resolution[0] * pointDataSize;
                            fs.Seek(offset, SeekOrigin.Begin);
                            long totalRead = 0;
                            while (totalRead < dataSize)
                            {
                                for (int i = 0; i < largeArray[c].Length; i++)
                                {
                                    int blockSize = largeArray[c][i].Length;
                                    int bytesReadForBlock = 0;
                                    int dataoffset = 0;
                                    while (bytesReadForBlock < blockSize)
                                    {
                                        int n = 0;
                                        int bytesRead = 0;
                                        int count = bytesToRead;
                                        // Read large chunks
                                        while (bytesRead < bytesToRead)
                                        {
                                            n = fs.Read(largeArray[c][i], dataoffset, count);
                                            if (n == 0)
                                            {
                                                throw new IOException(String.Format("Unexpected EOF: {0}!", filename));
                                            }
                                            bytesRead += n;
                                            count -= n;
                                        }
                                        count = bytesToRead;
                                        dataoffset += bytesRead;
                                        bytesReadForBlock += bytesRead;
                                        bytesRead = 0;
                                    }
                                    totalRead += bytesReadForBlock;
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new IOException(String.Format("File not found: {0}!", filename));
                    }
                    fs.Close();
                    exception_flag = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: {0}\n Inner Exception: {1}", ex, ex.InnerException);
                    int milliseconds = 2000;
                    Console.WriteLine("Retring after {0} seconds", milliseconds/1000);
                    System.Threading.Thread.Sleep(milliseconds);
                    exception_flag = true;
                    attempts++;
                }
            }
        }

        public override void ReadHeader(MemoryMappedViewStream stream)
        {
            throw new NotImplementedException();
        }

        public override void CopyDataFromByteArray(long z, long y, long x, byte[] destinationArray, int destinationIndex, int length, int component)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Copies one value from the binary double array into the destination array at the specified index.
        /// Convertes the double to a single precious float.
        /// </summary>
        /// <param name="z"></param>
        /// <param name="y"></param>
        /// <param name="x"></param>
        /// <param name="destinationArray"></param>
        /// <param name="destinationIndex"></param>
        /// <param name="component"></param>
        public override void CopyDataFromByteArrayDoubleToFloat(int z, int y, int x, byte[] destinationArray, int destinationIndex, int component)
        {
            long sourceIndex = ((long)z * cache_dimensions[2] * cache_dimensions[1] + (long)y * cache_dimensions[2] + (long)x) * sizeof(double);
            int blockNum = (int)(sourceIndex >> BLOCK_SIZE_LOG2);
            int elementNumberInBlock = (int)(sourceIndex & (BLOCK_SIZE - 1));
            float float_value = (float)BitConverter.ToDouble(largeArray[component][blockNum], elementNumberInBlock);
            Array.Copy(BitConverter.GetBytes(float_value), 0, destinationArray, destinationIndex, sizeof(float));
        }

        private string GetFileName(float time)
        {
            return String.Format(@"{0}rstrt.{1:0000}.bin", data_dir, time);
        }

        public override void Close()
        {
            for (int i = 0; i < components; i++)
            {
                if (data != null)
                {
                    data[i] = null;
                }
            }
        }

    }
}


