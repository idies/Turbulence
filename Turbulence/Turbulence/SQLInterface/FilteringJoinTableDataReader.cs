using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Collections;

namespace Turbulence.SQLInterface
{
    public class FilteringJoinTableDataReader : IDataReader
    {
        /// various meta data...
        //private static string[] colNames = { "zindex", "dindex" };
        private static string[] colNames = { "zindex" };
        //private static Type[] colTypes = { typeof(long), typeof(long) };
        private static Type[] colTypes = { typeof(long) };
        //private static string[] colTypeNames = { "long", "long" };
        private static string[] colTypeNames = { "long" };
        private bool done;  // set when done reading data

        int atomDim;
        int startx, starty, startz;
        int endx, endy, endz;
        int x, y, z;

        //int plane, line, point;
        //int firstLine, firstPoint;
        
        // Description of input data format
        long zindex;        // Morton index of the data atom currently being processed
        //long dindex;        // Diagonal index of the data atom currently being processed
        
        // The DataReader should always be open when returned to the user.
        private bool m_fOpen = true;

        /*
        * Because the user should not be able to directly create a 
        * DataReader object, the constructors are
        * marked as internal.
        */
        internal FilteringJoinTableDataReader(int startx, int starty, int startz, int endx, int endy, int endz, int atomDim)
        {
            this.done = false;
            this.atomDim = atomDim;
            this.startx = startx;
            this.starty = starty;
            this.startz = startz;
            this.endx = endx;
            this.endy = endy;
            this.endz = endz;
            this.x = startx;
            this.y = starty;
            this.z = startz;
            //this.x = endx/atomDim - startx/atomDim + 1;
            //this.y = endy/atomDim - starty/atomDim + 1;
            //this.z = endz/atomDim - startz/atomDim + 1;
            //this.plane = 0;
            //this.firstLine = 0;
            //this.line = 0;
            //this.firstPoint = 0;
            //this.point = 0;
            
            //this.dindex = -1;
        }

        public static IDataReader GetReader(int startx, int starty, int startz, int endx, int endy, int endz, int atomDim)
        {
            return new FilteringJoinTableDataReader(startx, starty, startz, endx, endy, endz, atomDim);
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
                zindex = new Morton3D(z, y, x);
                x += atomDim;
                if (x > endx)
                {
                    x = startx;
                    y += atomDim;
                    if (y > endy)
                    {
                        y = starty;
                        z += atomDim;
                        if (z > endz)
                        {
                            done = true;
                        }
                    }
                }
                #region Diagonal Iteration
                //// We want to determine the list of atoms that have to be read from the database
                //// These are the atoms that cover the data region from (startx, starty, startz) to (endx, endy, endz)
                //// We also want to order the atoms so that they are returned in the wavefront (diagonal) fashion,
                //// which is needed for the computation of the box-sums
                //// This is what the below logic achieves

                //// We start at plane=0, line=0, point=0 and this will give us Morton3D(startz, starty, startx)
                //// We don't want to visit all points, but distinct atoms
                //// This is why we multiply by atomDim
                //// The dimensions x,y,z of the data region are divided by atomDim
                //zindex = new Morton3D(startz + atomDim * (plane - line), starty + atomDim * (line - point), startx + atomDim * point);
                //// This is going to be the index by which the retrieved atoms are ordered
                //// Starts at 0 and is incremented for each successive atom
                //dindex++;

                //// Now that we've processed one atom, we move on to the next
                //// Since we iterate over the grid in diagonal fashion we want to start at the largest value for x for each diagonal line and diagonal plane
                //// When x is decremented, y and/or z is incremented and thus we are likely to move up in the zindex order as well
                //// (where x changes most rapidly, then y, then z)
                //// We could start with the lowest value for x and increment, the ordering will be just slightly different but also valid
                //point--;
                //// We check if we have processed all points on this diagonal line
                //if (point < firstPoint)
                //{
                //    // We then decrement the line to move to the next
                //    line--;
                //    // We check if we have processed all lines on this diagonal plane
                //    if (line < firstLine)
                //    {
                //        // We increment the plane to move to the next
                //        plane++;
                //        // We have x + y + z - 2 planes to process
                //        // Since we start at 0, if we have processed all of them we are done
                //        if (plane == x + y + z - 2)
                //            done = true;
                //        else
                //        {
                //            // Otherwise we update the line being processed and the first line that we need to reach
                //            // since we are starting from the top and going down
                //            firstLine = plane < z ? 0 : plane - z + 1;
                //            line = plane < x + y - 1 ? plane : x + y - 2;
                //        }
                //    }
                //    // Otherwise we update the point being processed and the first point that we need to reach
                //    // since we are starting from the top and going down
                //    firstPoint = line < y ? 0 : line - y + 1;
                //    point = line < x ? line : x - 1;
                //}
                #endregion
            }
            else
            {
                return false;
            }

            return true;
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
            //else if (i == 1)
            //    return dindex;
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
            //dindex = 0;
            
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
