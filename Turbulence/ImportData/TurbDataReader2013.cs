using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using Turbulence.TurbLib;
using System.IO;

/// MSDN docs on DataReader implementations: http://msdn2.microsoft.com/en-us/library/cy623chd(VS.71).aspx

namespace ImportData
{
    /// <summary>
    /// A IDataReader interface to reading local data flow files.
    /// This is passed to a SqlBulkCopy call for fast data insertion...
    /// 
    /// Currently written to support one time step at a time --
    /// which is a reasonable unit of failure if we need to do a reinsertion.
    /// </summary>
    public class TurbDataReader : IDataReader
    {
        /// various meta data...
        private static string[] colNames = { "timestep", "zindex", "data" };
        private static Type[] colTypes = { typeof(string), typeof(long), typeof(byte[]) };
        private static string[] colTypeNames = { "int", "long", "binary(1560)" };
        byte[] data;
        private bool done;         // set when done reading data
        private bool open = true;  // Is the reader currently open? (not very useful)
        private byte[] header;

        FileCache cache;

        // Description of input data format
        //string data_dir; string[] suffix;   // Location of data on local disk
        int[] resolution;  // resolution of input data set
        int components;  // number of compontents for each point (4 for Ux,Uy,Uz,P)
        int timestep;    // timestep being dataread
        int timeoff;     // quantity to add to timestep to 
        int pointDataSize = sizeof(float); // size of a single data point
        int SqlArrayHeaderSize;    // size of the SqlArray header stored infront of every DB atom
        const int MAX_SHORTARRAY_RANK = 8; // the maximum rank (number of dimensions) a Short SqlArray can have

        // Description of how the data should be chunked...
        // NOTE: This information is the basis of what we need to keep in config files on the database!
        // TODO: How to encode this in XML?
        public struct ExportDataFormat
        {
            public ExportDataFormat(long start, long end, long inc, int length, int edge, bool isDataLittleEndian)
            {
                // If start is not a multiple of the increment we need to adjust it.
                // It should be equal to the next integer that is a multiple of inc. E.g.:
                // for start = 1300 and inc = 512 we will end up with start = 1536,
                // for start = 1536 and inc = 512 we will end up with start = 1536,
                // for start = 1537 and inc = 512 we will end up with start = 2048
                this.start = start / inc * inc;
                if (this.start < start)
                    this.start += inc;
                this.end = end;
                this.inc = inc;
                this.length = length;
                this.edge = edge;
                this.isDataLittleEndian = isDataLittleEndian;
            }
            public long start;   // Base morten address (ie, Morton(0,0,0))
            public long end;     // Last address (ie, Morton(0,511,511))
            public long inc;     // Morton increment (should be == Morton(length-1,length-1,length-1)+1 or Morton(0,0,length))
            public int length;   // Length of each side of a cube
            public int edge;     // Extra edge replication on all sides of the cube
            public bool isDataLittleEndian;
        };
        ExportDataFormat dataFormat;

        // long counter = 0;       // current address being accessed
        long datacounter = -1;    // data currently loaded

        long[] work;            // ordered workload to collate on Z dimension
        int steps;               // number of workload steps
        int step = -1;                // current step

        public static IDataReader GetReader(int timestep, int timeoff,
            int[] resolution, int components, ExportDataFormat dataFormat, FileCache cache)
        {
            return new TurbDataReader(timestep, timeoff, resolution, components, dataFormat, cache);
        }

        /*
         * Because the user should not be able to directly create a 
         * DataReader object, the constructors are marked as internal.
         */
        internal TurbDataReader(int timestep, int timeoff,
            int[] resolution, int components, ExportDataFormat dataFormat, FileCache cache)
        {
            this.done = false;
            //this.data_dir = data_dir;
            //this.suffix = suffix;
            this.resolution = resolution;
            this.components = components;
            this.timestep = timestep;
            this.timeoff = timeoff;
            this.dataFormat = dataFormat;
            // counter = -1;
            step = -1;
            GenerateSqlArrayHeader();
            data = new byte[SqlArrayHeaderSize +
                (dataFormat.length + dataFormat.edge * 2)
                * (dataFormat.length + dataFormat.edge * 2)
                * (dataFormat.length + dataFormat.edge * 2) * components * pointDataSize];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = 0;
            }
            this.steps = (int)((dataFormat.end - dataFormat.start) / dataFormat.inc + 1);
            this.work = new long[this.steps];
            for (int i = 0; i < this.steps; i++)
            {
                this.work[i] = (dataFormat.start + dataFormat.inc * i);
            }
            //IComparer myCOmparer = new MortonZOrderCompare();
            //Array.Sort(work, myCOmparer);
            //cache = new FileCache(data_dir, suffix, resolution);
            this.cache = cache;
        }

        private void GenerateSqlArrayHeader()
        {
            int cube_size = (dataFormat.length + dataFormat.edge) * (dataFormat.length + dataFormat.edge) * (dataFormat.length + dataFormat.edge);
            if (cube_size > 8000)
            {
                int cube_width = dataFormat.length + 2 * dataFormat.edge;
                // Header type 0 is for MaxArray and 1 for ShortArrays (<8000)
                bool HeaderType = false;
                bool ColumnMajor = false;
                // The data are in double precision floating point format
                // For single the DataType is 5, for double 6
                int DataType = 5;
                if (pointDataSize == sizeof(double))
                {
                    DataType = 6;
                }
                // We have 3-dimensional arrays of either scalar or vector fields
                int Rank = components == 1 ? 3 : 4;

                int Length = components * (cube_width * cube_width * cube_width);
                // The header consists of 8 "special" bytes (below) and the rank of the array (i.e. it's dimensions) as integer numbers
                // The Max array header is varaible length depending on the rank of the array
                int SpecialBytes = 8;
                SqlArrayHeaderSize = SpecialBytes + Rank * sizeof(int);
                header = new byte[SqlArrayHeaderSize];
                int BitsInByte = 8;
                BitArray headerBits = new BitArray(SpecialBytes * BitsInByte, false);

                // First bit indicates the type of array
                headerBits[0] = HeaderType;
                // Second bit indicates the ordering
                headerBits[1] = ColumnMajor;
                // Next 6 bits are used for padding

                //  Only 4 bits are used for the DataType and Rank
                BitArray bits = new BitArray(new int[] { DataType });
                for (int i = 0; i < 4; i++)
                    headerBits[8 + i] = bits[i];

                bits = new BitArray(new int[] { Rank });
                for (int i = 0; i < 4; i++)
                    headerBits[12 + i] = bits[i];
                // Next 16 bits are again padding to make a total of 4 bytes

                // The next 4 bytes store the length(size) of the array
                bits = new BitArray(new int[] { Length });
                for (int i = 0; i < bits.Length; i++)
                    headerBits[32 + i] = bits[i];

                headerBits.CopyTo(header, 0);

                // These are all of the "special" bytes of the header

                // Next the dimensions of the array are stored as integers
                int[] dimensions;
                if (components > 1)
                {
                    dimensions = new int[] { components, cube_width, cube_width, cube_width };
                }
                else
                {
                    dimensions = new int[] { cube_width, cube_width, cube_width };
                }
                Buffer.BlockCopy(dimensions, 0, header, 8, dimensions.Length * sizeof(int));
            }
            else
            {
                int cube_width = dataFormat.length + 2 * dataFormat.edge;
                // Header type 0 is for MaxArray and 1 for ShortArrays (<8000)
                bool HeaderType = true;
                bool ColumnMajor = false;
                // The data are in double precision floating point format
                // For single (i.e. float) the DataType is 5, for double 6
                int DataType = 5;
                if (pointDataSize == sizeof(double))
                {
                    DataType = 6;
                }
                // We have 3-dimensional arrays of either scalar or vector fields
                int Rank = components == 1 ? 3 : 4;

                // Length or size of the array
                int Length = components * (cube_width * cube_width * cube_width);

                // The header consists of 8 "special" bytes (below) and the rank of the array (i.e. it's dimensions) as short numbers
                // The Short array header is fixed at 24 bytes (even if the arrays is lower than rank 8)
                int SpecialBytes = 8;
                SqlArrayHeaderSize = SpecialBytes + MAX_SHORTARRAY_RANK * sizeof(short);
                header = new byte[SqlArrayHeaderSize];
                int BitsInByte = 8;
                BitArray headerBits = new BitArray(SpecialBytes * BitsInByte, false);

                // First bit indicates the type of array
                headerBits[0] = HeaderType;
                // Second bit indicates the ordering
                headerBits[1] = ColumnMajor;
                // Next 6 bits are used for padding

                //  Only 4 bits are used for the DataType and Rank
                BitArray bits = new BitArray(new int[] { DataType });
                for (int i = 0; i < 4; i++)
                    headerBits[8 + i] = bits[i];

                bits = new BitArray(new int[] { Rank });
                for (int i = 0; i < 4; i++)
                    headerBits[12 + i] = bits[i];
                // Next 16 bits are again padding to make a total of 4 bytes

                // The next 4 bytes store the length(size) of the array
                bits = new BitArray(new int[] { Length });
                for (int i = 0; i < bits.Length; i++)
                    headerBits[32 + i] = bits[i];

                headerBits.CopyTo(header, 0);

                // These are all of the "special" bytes of the header

                // Next the dimensions of the array are stored as short integers
                short[] dimensions;
                if (components > 1)
                {
                    dimensions = new short[] { (short)components, (short)cube_width, (short)cube_width, (short)cube_width };
                }
                else
                {
                    dimensions = new short[] { (short)cube_width, (short)cube_width, (short)cube_width };
                }
                Buffer.BlockCopy(dimensions, 0, header, 8, dimensions.Length * sizeof(short));
            }
        }

        #region old code to be removed later
        /// <summary>
        /// Read a single data point from disk.
        /// Use only for testing... very slow.
        /// </summary>
        /// <param name="time">Time step</param>
        /// <param name="z">Z index</param>
        /// <param name="y">Y index/param>
        /// <param name="x">X index</param>
        /// <returns>Velocity components and pressure</returns>
        /* public float[] ReadDataPoint(int z, int y, int x)
        {
            int off;
            int machine = z / slicewidth;
            FileStream fs = GetFileStream(machine);

            float[] result = new float[components];
            BinaryReader br = new BinaryReader(fs);

            // sizeof(single) = 4
            off = (((z % slicewidth) * resolution * resolution +
                    y * resolution + x) * components) * 4;
            fs.Seek(off, SeekOrigin.Begin);
            for (int i = 0; i < 4; i++)
            {
                result[i] = br.ReadSingle();
            }
            return result;
        } */
        #endregion

        private int IndexOfCol(string colName)
        {
            for (int n = 0; n < colNames.Length; n++)
            {
                if (colNames[n] == colName)
                {
                    return (n);
                }
            }
            return (-1);
        }
        /// <summary>
        /// Perform the real work of reading the data...
        /// </summary>
        /// <remarks>
        /// Output placed into 'data'.
        /// Try to perform continuous reads in the X dimension....
        /// </remarks>
        /// NOTE: At the moment this function does not handle wrap-around when for example edge > 0
        ///       and data are needed from the wrapped around starting position of the volume
        private void ReadData()
        {
            int[] corner = new Morton3D(datacounter).GetValues();

            // lower (base) corner
            int bz = (corner[0] - dataFormat.edge + resolution[0]) % resolution[0];
            int by = (corner[1] - dataFormat.edge + resolution[1]) % resolution[1];
            int bx = (corner[2] - dataFormat.edge + resolution[2]) % resolution[2];
            // upper (end) corner
            int ez = (corner[0] + dataFormat.length + dataFormat.edge) % resolution[0];
            int ey = (corner[1] + dataFormat.length + dataFormat.edge) % resolution[1];
            int ex = (corner[2] + dataFormat.length + dataFormat.edge) % resolution[2];

            // Read data files required for full stripe of data
            // cache.readFiles(timestep, dataFormat.start, dataFormat.end, dataFormat.length);
            // cache.readFiles(timestep, datacounter, dataFormat.length);

            //Console.WriteLine("Reading from {0},{1},{2} to {3},{4},{5}", bz, by, bx, ez, ey, ex);
            // We first store the SqlArray header at the beginning of the data atom
            Array.Copy(header, data, SqlArrayHeaderSize);
            int bufferOffset = SqlArrayHeaderSize; // current offset for writing data in local buffer
            int side = dataFormat.length + dataFormat.edge + dataFormat.edge;
            int dataread = 0;
            for (int z = bz; z != ez; z = (z + 1) % resolution[0])
            {
                // adjusted z offset based on base of file cache
                int zadj = (z + resolution[0] - cache.Base[0]) % resolution[0];
                for (int y = by; y != ey; y = (y + 1) % resolution[1])
                {
                    // adjusted y offset based on base of file cache
                    int yadj = (y + resolution[1] - cache.Base[1]) % resolution[1];
                    for (int x = bx; x != ex; x = (x + 1) % resolution[2])
                    {
                        // adjusted x offset based on base of file cache
                        int xadj = (x + resolution[2] - cache.Base[2]) % resolution[2];
                        //long fileoff = (zadj * resolution[2] * resolution[1] + yadj * resolution[2] + xadj) * cache.PointDataSize;
                        for (int i = 0; i < components; i++)
                        {
                            // NOTE: At the moment this function does not handle wrap-around when for example edge > 0
                            //       and data are needed from the wrapped around starting position of the volume
                            //cache.CopyData(fileoff, data, bufferOffset, pointDataSize, i);
                            if (pointDataSize == cache.PointDataSize)
                            {
                                cache.CopyDataFromByteArray(zadj, yadj, xadj, data, bufferOffset, pointDataSize, i);
                            }
                            else
                            {
                                cache.CopyDataFromByteArrayDoubleToFloat(zadj, yadj, xadj, data, bufferOffset, i);
                            }
                            if (BitConverter.IsLittleEndian != dataFormat.isDataLittleEndian)
                                Array.Reverse(data, bufferOffset, pointDataSize);
                            //Console.WriteLine("value = {0}", System.BitConverter.ToSingle(data, bufferOffset));
                            dataread += pointDataSize;
                            bufferOffset += pointDataSize;
                        }
                    }
                }
            }

            //Console.WriteLine("Read {0} bytes of data at {1} (offset={2})", dataread, new Morton3D(datacounter).ToPrettyString(), bufferOffset);

        }

        public bool NextResult()
        {
            // This method seems to be needed only when reading the results of
            // batch SQL statements.
            throw new NotSupportedException("NextResult not supported.");
        }
        public bool Read()
        {
            // Return true if it is possible to advance and if you are still positioned
            // on a valid row. 
            if (!done)
            {
                if (step < 0)
                {
                    step = 0;
                }
                else
                {
                    step++; ;
                }
                if (step < steps)
                {
                    datacounter = work[step];
                    ReadData();
                }
                else
                {
                    done = true;
                }
            }
            return !done;
        }
        public int GetOrdinal(string colName)
        {
            int colIndex = IndexOfCol(colName);
            if (colIndex == -1)
            {
                throw new IndexOutOfRangeException("Could not find specified column");
            }
            return (colIndex);
        }
        public string GetName(int i)
        {
            if (i < colNames.Length)
            {
                return (colNames[i]);
            }
            throw new IndexOutOfRangeException(string.Format("Specified column index is invalid ({0})", i));
        }
        public short GetInt16(int i)
        {
            return (short)GetInt32(i);
        }
        public int GetInt32(int i)
        {
            return (int)GetInt64(i);
        }
        public long GetInt64(int i)
        {
            if (i == 0)
                return timestep + timeoff;
            if (i == 1)
                return datacounter;
            else
                throw new ArgumentException("Invalid argument");
        }
        public object GetValue(int i)
        {
            if (i == 0)
                return timestep + timeoff;
            else if (i == 1)
                return datacounter;
            else if (i == 2)
                return data.Clone();
            else
                throw new ArgumentException(string.Format("Invalid column index ({0})", i));
        }
        public long GetBytes(int i, long dataIndex, byte[] buffer, int bufferIndex, int length)
        {
            if (i == 2)
            {
                if (buffer != null)
                {
                    // TODO: Do we need to sanity check the dataread length?
                    if (dataIndex > data.Length)
                    {
                        throw new ArgumentOutOfRangeException("dataIndex", "Data index out of range");
                    }
                    if (data.Length < (dataIndex + length))
                    {
                        length = (int)(data.Length - dataIndex);
                    }
                    Array.Copy(data, dataIndex, buffer, bufferIndex, length);
                }
                return data.Length;
            }
            else
            {
                // TODO: Throw an error instead?
                return 0;
            }
        }
        public string GetString(int i)
        {
            // TODO: Return each field as a string!
            throw new NotImplementedException("TurbDataReader.GetString");
        }
        public object this[string colName]
        {
            get
            {
                int colIndex = IndexOfCol(colName);
                if (colIndex == -1)
                {
                    throw new ArgumentException(string.Format("Invalid column name ({0})", colName), "colName");
                }
                return this.GetValue(colIndex);
            }
        }
        public int GetValues(object[] values)
        {
            // TODO: Return all the current values as an array
            // NOTE: public int GetValues( object[] values )
            int i = 0;
            for (; i < values.Length && i < 3; i++)
            {
                values[i] = this[i];
            }
            return i;
        }

        public DataTable GetSchemaTable()
        {
            throw new NotImplementedException("TurbDataReac.GetSchemaTable");
        }
        public object this[int i]
        {
            get
            {
                return (this.GetValue(i));
            }
        }
        public Type GetFieldType(int i)
        {
            return colTypes[i];
        }
        public string GetDataTypeName(int i)
        {
            // TODO: Replace with the return of actual DataBase types!
            // return (GetFieldType(i).Name);
            return colTypeNames[i];
        }

        public void Close()
        {
            open = false;
            //cache.Close();
            // CloseAllCachedFileStreams();
        }

        public void Dispose()
        {
        }

        public bool IsClosed
        {
            get { return !open; }
        }
        /*
         * Do these functions matter?
         * */

        public int RecordsAffected
        {
            /*
             * RecordsAffected is only applicable to batch statements
             * that include inserts/updates/deletes.
             * TODO: Does that apply here???
             */
            get { return -1; }
        }
        public int Depth
        {
            get { return 0; }
        }

        public bool IsDBNull(int i)
        {
            return false;
        }
        public int FieldCount
        {
            get
            {
                if (!done)
                {
                    return colNames.Length;
                }
                return 0;
            }
        }


        /*
         * All of the unused functions still need to be defined to implement the interface.
         */

        public bool GetBoolean(int i)
        {
            throw new NotImplementedException("TurbDataReader.GetBoolean");
        }
        public byte GetByte(int i)
        {
            throw new NotImplementedException("TurbDataReader.GetByte");
        }
        public char GetChar(int i)
        {
            throw new NotImplementedException("TurbDataReader.GetChar");
        }
        public long GetChars(int i, long fieldOffset, char[] buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException("TurbDataReader.GetChars");
        }
        public DateTime GetDateTime(int i)
        {
            throw new NotImplementedException("TurbDataReader.GetDateTime");
        }
        public decimal GetDecimal(int i)
        {
            throw new NotImplementedException("TurbDataReader.GetDecimal");
        }
        public double GetDouble(int i)
        {
            throw new NotImplementedException("TurbDataReader.GetDouble");
        }
        public float GetFloat(int i)
        {
            throw new NotImplementedException("TurbDataReader.GetFloat");
        }
        public Guid GetGuid(int i)
        {
            throw new NotImplementedException("TurbDataReader.GetGuid");
        }
        public IDataReader GetData(int i)
        {
            throw new NotImplementedException("TurbDataReader.GetData");
        }


        public class MortonZOrderCompare : IComparer
        {
            int IComparer.Compare(Object x, Object y)
            {
                int[] mx = new Morton3D((long)x).GetValues();
                int[] my = new Morton3D((long)y).GetValues();

                return (mx[0] - my[0]);
            }
        }

    }
}
