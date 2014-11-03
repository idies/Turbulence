using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Collections;

namespace Turbulence.SQLInterface
{
    public class JoinTableDataReader : IDataReader
    {
        /// various meta data...
        private static string[] colNames = { "zindex" };
        private static Type[] colTypes = { typeof(long) };
        private static string[] colTypeNames = { "long" };
        private bool done;  // set when done reading data

        List<long> indexes; // indexes to be written to the database

        //IEnumerator<long> keysIter;
        //int Count;

        // Description of input data format
        long zindex;        // Morton index of the data cube currently being processed

        int recordsProcessed;

        // The DataReader should always be open when returned to the user.
        private bool m_fOpen = true;

        /*
        * Because the user should not be able to directly create a 
        * DataReader object, the constructors are
        * marked as internal.
        */
        internal JoinTableDataReader(ICollection<long> keys, float points_per_cube)
        {
            this.done = false;
            this.indexes = new List<long>(keys);
            //if (indexes.Count <= 100000 && points_per_cube >= 1.1F)
            if (indexes.Count <= 100000)
                this.indexes.Sort();
            
            this.recordsProcessed = 0;
        }

        public static IDataReader GetReader(ICollection<long> keys, float points_per_cube)
        {
            return new JoinTableDataReader(keys, points_per_cube);
        }

        /****
         * METHODS / PROPERTIES FROM IDataReader.
         ****/
        public int Depth
        {
            /*
             * Always return a value of zero if nesting is not supported.
             */
            get { return 0; }
        }

        public bool IsClosed
        {
            /*
             * Keep track of the reader state - some methods should be
             * disallowed if the reader is closed.
             */
            get { return !m_fOpen; }
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
                if (recordsProcessed == indexes.Count)
                    done = true;
                else
                {
                    zindex = indexes[recordsProcessed];
                    
                    recordsProcessed++;

                    //zindex = keysIter.Current;
                    //keysIter.MoveNext();
                }
            }

            return !done;
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
                return zindex;
            else
                throw new ArgumentException(string.Format("Invalid column index ({0})", i));
        }

        public int GetValues(object[] values)
        {
            int i = 0;
            for (; i < values.Length && i < colNames.Length; i++)
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

        public object this[int i]
        {
            get
            {
                return this.GetValue(i);
            }
        }

        public object this[String name]
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

        public void Reset()
        {
            done = false;
            recordsProcessed = 0;
            
            //keysIter.Reset();
            //keysIter.MoveNext();
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
