using System;
using System.Data;
using System.Globalization;

namespace ImportData
{
    public class DataReader : IDataReader
    {
        /// various meta data...
        private static string[] colNames = { "timestep", "zindex", "data" };
        private static Type[] colTypes = { typeof(int), typeof(long), typeof(byte[]) };
        private static string[] colTypeNames = { "int", "long", "varbinary(MAX)" };
        byte[] data;
        private bool done;          // set when done reading data

        FileCache cache;

        // Description of input data format
        string data_dir, prefix;    // location of data on local disk
        int cube_resolution;        // resolution of the data cube
        int cube_size;              // size of the data cube
        int edge;        // length of the replicated edge
        int resolution;  // resolution of input data set
        int slices;      // number of files the Z dimension is split into
        int components;  // number of compontents for each point (3 for Vx,Vy,Vz or Bx,By,Bz or Ax,Ay,Az and 1 for P)
        int timestep;    // timestep being dataread
        Morton3D zindex;     // Morton index of the data cube currently being processed
        long firstBox;   // Morton index of the first data cube to process
        long lastBox;    // Morton index of the last data cube to process

        // The DataReader should always be open when returned to the user.
        private bool m_fOpen = true;

        /*
        * Because the user should not be able to directly create a 
        * DataReader object, the constructors are
        * marked as internal.
        */
        internal DataReader(string data_dir, string prefix, int timestep, int slices, int resolution,
            int cube_resolution, int edge, int components, long firstBox, long lastBox)
        {
            this.done = false;
            this.data_dir = data_dir;
            this.prefix = prefix;
            this.resolution = resolution;
            this.cube_resolution = cube_resolution;
            this.edge = edge;
            this.timestep = timestep;
            this.slices = slices;
            this.components = components;
            this.firstBox = firstBox;
            this.lastBox = lastBox;
            zindex = new Morton3D(-1);
            cube_size = (cube_resolution + 2 * edge) * (cube_resolution + 2 * edge) * (cube_resolution + 2 * edge) * sizeof(float);
            data = new byte[cube_size * components];
            for (int i = 0; i < data.Length; i++)
                data[i] = 0;
            cache = new FileCache(data_dir, prefix, resolution, cube_resolution, slices, components);
        }

        public static IDataReader GetReader(string data_dir, string prefix, int timestep, int slices, int resolution,
            int cube_resolution, int edge, int components, long firstBox, long lastBox)
        {
            return new DataReader(data_dir, prefix, timestep, slices, resolution, cube_resolution, edge, components, firstBox, lastBox);
        }

        /****
         * METHODS / PROPERTIES FROM IDataReader.
         ****/
        public int Depth 
        {
          /*
           * Always return a value of zero if nesting is not supported.
           */
          get { return 0;  }
        }

        public bool IsClosed
        {
            /*
             * Keep track of the reader state - some methods should be
             * disallowed if the reader is closed.
             */
            get  { return !m_fOpen; }
        }

        public int RecordsAffected 
        {
            /*
             * RecordsAffected is only applicable to batch statements
             * that include inserts/updates/deletes. The sample always
             * returns -1.
             */
            get { return -1; }
        }

        public void Close()
        {
            /*
             * Close the reader. The sample only changes the state,
             * but an actual implementation would also clean up any 
             * resources used by the operation. For example,
             * cleaning up any resources waiting for data to be
             * returned by the server.
             */
            m_fOpen = false;
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
                // Begin at the first box
                if (zindex.IsNull)
                    zindex.Key = firstBox;
                else
                {
                    // Read data along X first, until an entire row is read-in
                    // Then increment Y, until an entire sheet is read-in
                    // Finally, increment Z, until all of the data are read-in
                    if (zindex.X + cube_resolution == resolution)
                    {
                        if (zindex.Y + cube_resolution == resolution)
                            zindex = new Morton3D(zindex.Z + cube_resolution, 0, 0);
                        else
                            zindex = new Morton3D(zindex.Z, zindex.Y + cube_resolution, 0);
                    }
                    else
                        zindex = new Morton3D(zindex.Z, zindex.Y, zindex.X + cube_resolution);
                }
                // Keep reading until we have reached the last box
                if (zindex.Key <= lastBox)
                {
                    cache.readFiles(timestep, zindex.Z / cube_resolution, edge);
                    ReadData(zindex.X, zindex.Y);
                }
                else
                    done = true;
            }

            return !done;
        }

        private void ReadData(int x_corner, int y_corner)
        {
            int destinationIndex = 0;

            for (int i = 0; i < components; i++)
            {
                int bytesAssigned = 0;
                int count = 0;
                int sourceIndex_x = (x_corner - edge + resolution) % resolution;
                int sourceIndex_y = (y_corner - edge + resolution) % resolution;
                int sourceIndex_z = 0;

                while (bytesAssigned < cube_size)
                {
                    // Assign bytes for the left replicated edge along x for the appropriate component.
                    Array.Copy(cache.data[i], (sourceIndex_x + resolution * sourceIndex_y + resolution * resolution * sourceIndex_z) * sizeof(float),
                        data, destinationIndex * sizeof(float), edge * sizeof(float));
                    // Adjust the array index by the length of the edge.
                    sourceIndex_x += edge;
                    sourceIndex_x = sourceIndex_x % resolution;
                    destinationIndex += edge;
                    // Assign bytes for the data cube along x for the appropriate component.
                    Array.Copy(cache.data[i], (sourceIndex_x + resolution * sourceIndex_y + resolution * resolution * sourceIndex_z) * sizeof(float),
                        data, destinationIndex * sizeof(float), cube_resolution * sizeof(float));
                    // Adjust the array index by the resolution of the data cube.
                    sourceIndex_x += cube_resolution;
                    sourceIndex_x = sourceIndex_x % resolution;
                    destinationIndex += cube_resolution;
                    // Assign bytes for the right replicated edge along x for the appropriate component.
                    Array.Copy(cache.data[i], (sourceIndex_x + resolution * sourceIndex_y + resolution * resolution * sourceIndex_z) * sizeof(float),
                        data, destinationIndex * sizeof(float), edge * sizeof(float));
                    // Adjust the array index, so that we skip over the rest and start from the beginning for the next row (along y).
                    sourceIndex_x += resolution - cube_resolution - edge;
                    sourceIndex_x = sourceIndex_x % resolution;
                    destinationIndex += edge;
                    sourceIndex_y += 1;
                    sourceIndex_y = sourceIndex_y % resolution;
                    count++;
                    // Once we have assigned enough bytes for an entire sheet, move to the next (along z).
                    // We also need to adjust the y-index to skip over the data outside of the cube and start from the beginning.
                    if (count == cube_resolution + 2 * edge)
                    {
                        sourceIndex_z++;
                        sourceIndex_y += resolution - cube_resolution - 2 * edge;
                        sourceIndex_y = sourceIndex_y % resolution;
                        count = 0;
                    }
                    bytesAssigned += (cube_resolution + 2 * edge) * sizeof(float);
                }
            }
        }

        public DataTable GetSchemaTable()
        {
            //$
            throw new NotSupportedException();
        }

        /****
         * METHODS / PROPERTIES FROM IDataRecord.
         ****/
        public int FieldCount
        {
            // Return the count of the number of columns.
            get
            {
                if (!done)
                {
                    return colNames.Length;
                }
                return 0;
            }
        }

        public String GetName(int i)
        {
            return colNames[i];
        }

        public String GetDataTypeName(int i)
        {
            /*
            * Usually this would return the name of the type
            * as used on the back end, for example 'smallint' or 'varchar'.
            * The sample returns the simple name of the .NET Framework type.
            */
            return colTypeNames[i];
        }
        
        public Type GetFieldType(int i)
        {
            // Return the actual Type class for the data type.
            return colTypes[i];
        }

        public Object GetValue(int i)
        {
            if (i == 0)
                return timestep;
            else if (i == 1)
                return zindex.Key;
            else if (i == 2)
                return data.Clone();
            else
                throw new ArgumentException(string.Format("Invalid column index ({0})", i));
        }

        public int GetValues(object[] values)
        {
            int i = 0;
            for ( ; i < values.Length && i < 3; i++)
            {
                values[i] = this[i];
            }
            return i;
        }

        public int GetOrdinal(string name)
        {
            // Look for the ordinal of the column with the same name and return it.
            for (int i = 0; i < colNames.Length; i++)
            {
                if (name.CompareTo(colNames[i]) == 0)
                {
                    return i;
                }
            }
            
            // Throw an exception if the ordinal cannot be found.
            throw new IndexOutOfRangeException("Could not find specified column in results");
        }

        public object this [ int i ]
        {
            get
            {
                return this.GetValue(i);
            }
        }

        public object this [ String name ]
        { 
            // Look up the ordinal and return 
            // the value at that position.
            get { return this[GetOrdinal(name)]; }
        }

        public bool GetBoolean(int i)
        {
            /*
             * Force the cast to return the type. InvalidCastException
             * should be thrown if the data is not already of the correct type.
             */
            throw new NotImplementedException("DataReader.GetBoolean");
        }

        public byte GetByte(int i)
        {
            /*
             * Force the cast to return the type. InvalidCastException
             * should be thrown if the data is not already of the correct type.
             */
            throw new NotImplementedException("DataReader.GetByte");
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            // The sample does not support this method.
            throw new NotSupportedException("GetBytes not supported.");
        }

        public char GetChar(int i)
        {
            /*
             * Force the cast to return the type. InvalidCastException
             * should be thrown if the data is not already of the correct type.
             */
            throw new NotImplementedException("DataReader.GetChar");
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            // The sample does not support this method.
            throw new NotSupportedException("GetChars not supported.");
        }

        public Guid GetGuid(int i)
        {
            /*
             * Force the cast to return the type. InvalidCastException
             * should be thrown if the data is not already of the correct type.
             */
            throw new NotImplementedException("DataReader.GetGuid");
        }

        public Int16 GetInt16(int i)
        {
            /*
             * Force the cast to return the type. InvalidCastException
             * should be thrown if the data is not already of the correct type.
             */
            throw new NotImplementedException("DataReader.GetInt16");
        }

        public Int32 GetInt32(int i)
        {
            /*
             * Force the cast to return the type. InvalidCastException
             * should be thrown if the data is not already of the correct type.
             */
            throw new NotImplementedException("DataReader.GetInt32");
        }

        public Int64 GetInt64(int i)
        {
            /*
             * Force the cast to return the type. InvalidCastException
             * should be thrown if the data is not already of the correct type.
             */
            throw new NotImplementedException("DataReader.GetInt64");
        }

        public float GetFloat(int i)
        {
            /*
             * Force the cast to return the type. InvalidCastException
             * should be thrown if the data is not already of the correct type.
             */
            throw new NotImplementedException("DataReader.GetFloat");
        }

        public double GetDouble(int i)
        {
            /*
             * Force the cast to return the type. InvalidCastException
             * should be thrown if the data is not already of the correct type.
             */
            throw new NotImplementedException("DataReader.GetDouble");
        }

        public String GetString(int i)
        {
            /*
             * Force the cast to return the type. InvalidCastException
             * should be thrown if the data is not already of the correct type.
             */
            throw new NotImplementedException("DataReader.GetString");
        }

        public Decimal GetDecimal(int i)
        {
            /*
             * Force the cast to return the type. InvalidCastException
             * should be thrown if the data is not already of the correct type.
             */
            throw new NotImplementedException("DataReader.GetDecimal");
        }

        public DateTime GetDateTime(int i)
        {
            /*
             * Force the cast to return the type. InvalidCastException
             * should be thrown if the data is not already of the correct type.
             */
            throw new NotImplementedException("DataReader.GetDateTime");
        }

        public IDataReader GetData(int i)
        {
            /*
             * Force the cast to return the type. InvalidCastException
             * should be thrown if the data is not already of the correct type.
             */
            throw new NotImplementedException("DataReader.GetData");
        }

        public bool IsDBNull(int i)
        {
            return false;
        }

        /*
         * Implementation specific methods.
         */
        private int _cultureAwareCompare(string strA, string strB)
        {
            return CultureInfo.CurrentCulture.CompareInfo.Compare(strA, strB, CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase);
        }

        void IDisposable.Dispose() 
        {
            this.Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing) 
        {
            if (disposing) 
            {
                try 
                {
                    this.Close();
                }
                catch (Exception e) 
                {
                    throw new SystemException("An exception of type " + e.GetType() + 
                        " was encountered while closing the TemplateDataReader.");
                }
            }
        }        
    }
}