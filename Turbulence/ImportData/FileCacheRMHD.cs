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
    public class FileCacheRMHD : FileCache
    {
        private byte[] largeArray;
        private int cubeSize;
        private string field;

        public FileCacheRMHD(string data_dir, int[] resolution, long headerSize, int components, string field)
            : base(data_dir, new string[] { "" }, resolution, headerSize)
        {
            this.pointDataSize = sizeof(float);
            this.components = components;
            this.field = field;
        }
        
        public override void readFilesIntoByteArray(int timestep, long firstbox, long lastbox, int atomSize)
        {
            // Files are in zindex order
            if (this.timestep == timestep &&
                (firstbox >= this.firstbox && firstbox <= this.lastbox) &&
                (lastbox >= this.firstbox && lastbox <= this.lastbox))
            {
                return;
            }

            // Z-coord of the base region loaded into memory
            this.firstbox = firstbox;
            this.lastbox = lastbox;
            this.timestep = timestep;

            cubeSize = atomSize * atomSize * atomSize;
            long dataSize = (lastbox + cubeSize - firstbox) * components * pointDataSize;

            long mask = -262144;
            long startOfFile = firstbox / 512 & mask;
            long offset = (firstbox - startOfFile * cubeSize) * components * pointDataSize;
            long numberOfAtoms = 262144;
            long fileSize = numberOfAtoms * cubeSize * components * pointDataSize;
            
            string filename = GetFileName(timestep, startOfFile, field);
            FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);

            if (largeArray == null)
            {
                largeArray = new byte[dataSize];
            }

            bool exception_flag = true;
            int attempts = 0;
            int max_attempts = 5;
            while (exception_flag && attempts < max_attempts)
            {
                try
                {
                    long totalRead = 0;
                    int dataoffset = 0;

                    while (totalRead < dataSize)
                    {
                        // Try reading 64 * 64 cubes, each of which is of size 8*8*8 and has 2 floating point (4 bytes) components
                        int bytesToRead = 64 * 64 * cubeSize * components * pointDataSize;
                        int remainingBytes = (int)(fileSize - offset);
                        if (remainingBytes < bytesToRead)
                        {
                            bytesToRead = remainingBytes;
                        } 
                        if (File.Exists(filename))
                        {
                            fs.Seek(offset, SeekOrigin.Begin);

                            int n = 0;
                            int bytesRead = 0;
                            int count = bytesToRead;
                            // Read large chunks
                            while (bytesRead < bytesToRead)
                            {
                                n = fs.Read(largeArray, dataoffset, count);
                                if (n == 0)
                                {
                                    throw new IOException(String.Format("Unexpected EOF: {0}!", filename));
                                }
                                bytesRead += n;
                                count -= n;
                                dataoffset += n;
                            }
                            offset += bytesRead;
                            totalRead += bytesRead;
                        }
                        else
                        {
                            throw new IOException(String.Format("File not found: {0}!", filename));
                        }
                        if (offset == fileSize)
                        {
                            fs.Close();
                            exception_flag = false;
                            // If we still have more data to read, move on to the next file.
                            if (totalRead < dataSize)
                            {
                                startOfFile += numberOfAtoms;
                                filename = GetFileName(timestep, startOfFile, field);
                                fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                                offset = 0;
                            }
                        }
                    }
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
            if (exception_flag)
            {
                throw new IOException("There was a problem reading the data!");
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
        /// Copies an entire data atom into the given array at the given index.
        /// </summary>
        /// <param name="zindex"></param>
        /// <param name="destinationArray"></param>
        /// <param name="destinationIndex"></param>
        /// <returns></returns>
        public override bool GetEntireAtom(long zindex, byte[] destinationArray, int destinationIndex)
        {
            long sourceIndex = (zindex - firstbox) * components * pointDataSize;
            Array.Copy(largeArray, sourceIndex, destinationArray, destinationIndex, cubeSize * components * pointDataSize);
            return true;
        }

        public override void CopyDataFromByteArrayDoubleToFloat(int z, int y, int x, byte[] destinationArray, int destinationIndex, int component)
        {
            throw new NotImplementedException();
        }

        private string GetFileName(int time, long zindex, string field)
        {
            if (zindex < 0x400000 || (zindex >= 0x800000 && zindex < 0xc00000))
            {
                return String.Format(@"C:\data\data1\rmhd\RMHD_{2}_t{0:x4}_z{1:x7}", time, zindex, field);
            }
            else
            {
                return String.Format(@"C:\data\data2\rmhd\RMHD_{2}_t{0:x4}_z{1:x7}", time, zindex, field);
            }
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


