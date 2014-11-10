using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Sql;
using System.Data.SqlTypes;
using System.Data.SqlClient;
using System.Runtime.InteropServices;

using System.Diagnostics;

namespace Turbulence.TurbLib
{
    /// <summary>
    /// Class wrapping the unit of data we store in the database.
    /// </summary>
    public class TurbulenceBlob
    {
        public float[] data;
        TurbDataTable table;
        int timestep;
        Morton3D key;

        int realx, realy, realz;
        int basex, basey, basez;
        int side;

        public TurbulenceBlob(TurbDataTable table)
        {
            this.timestep = -1;
            this.table = table;
            side = table.atomDim + 2 * table.EdgeRegion;
            data = new float[side * side * side * table.Components];    
        }

        public override string ToString()
        {
            return String.Format("Physical: Z={0},Y={1},X={2}; Logical: Z={3},Y={4},X={5}",
                realz, realy, realx,
                basez, basey, basex);
        }

        /// <summary>
        /// Populate the data blob with new data.
        /// </summary>
        /// <param name="timestep"></param>
        /// <param name="key"></param>
        /// <param name="rawdata"></param>
        public void Setup(int timestep, Morton3D key, byte [] rawdata)
        {
            this.timestep = timestep;
            this.key = key;
            int[] keyval = key.GetValues();
            basez = keyval[0];
            basey = keyval[1];
            basex = keyval[2];
            realz = basez - table.EdgeRegion; // Negative is OK
            realy = basey - table.EdgeRegion;
            realx = basex - table.EdgeRegion;

            unsafe
            {
                // TODO: This code is still far from optimal...
                //       Why can't we pass a reference to the float array directly?
                fixed (byte* brawdata = rawdata)
                {
                    fixed (float* fdata = data)
                    {
                        float* frawdata = (float*)brawdata;
                        for (int i = 0; i < data.Length; i++)
                        {
                            data[i] = frawdata[i];
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get the zindex key associated with the atom
        /// </summary>
        public long Key { get { return key; } }

        /// <summary>
        /// Retrieve a single component value out of the blob.
        /// </summary>
        /// <param name="z"></param>
        /// <param name="y"></param>
        /// <param name="x"></param>
        /// <param name="comp"></param>
        /// <returns></returns>
        public float 
            GetDataValue(int z, int y, int x, int comp)
        {
            return data[GetLocalOffset(z,y, x, comp)];
        }

        /// <summary>
        /// Retrieve three lines around a point, with 2*length+1 points on each line
        /// </summary>
        public int GetDataLinesAroundPoint(int z, int y, int x, int length, float[, ,] dataout)
        {
            if (length > table.EdgeRegion)
            {
                throw new ArgumentOutOfRangeException("length", "length is too large for guaranteed overlap");
            }
            if (dataout == null)
            {
                dataout = new float[2 * length + 1, 3, table.Components];
            }

            try
            {
                for (int k = 0; k < 2 * length + 1; k++)
                {
                    for (int i = 0; i < table.Components; i++)
                    {
                        dataout[k, 0, i] = data[GetLocalOffset(z, y, x - length + k, i)];
                        dataout[k, 1, i] = data[GetLocalOffset(z, y - length + k, x, i)];
                        dataout[k, 2, i] = data[GetLocalOffset(z - length + k, y, x, i)];
                    }

                }
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("Exception {0} caught.  Input: z={1},y={2},x={3},length={4} in blob {5}",
                    e.Message, z, y, x, length, this.ToString()), e);
            }
            return 0;
        }

        /// <summary>
        /// Retrieve a 3d cube cornered around a specific point.
        /// This should help fix the ambiguity with the previous "centering" function.
        /// </summary>
        public int GetDataCubeCorneredAtPoint(int z, int y, int x, int length, float[, , ,] dataout)
        {
            try
            {
                // TODO: Replace with if {z,y,x} + length is not in cube
                if (length > 2 * (table.EdgeRegion + 1))
                {
                    throw new ArgumentOutOfRangeException("length", "length is too large for guaranteed overlap");
                }
                if (dataout == null)
                {
                    dataout = new float[length, length, length, table.Components];
                }


                // Compute physical offsets of the local data array
                //int zv = ((z% table.BlobDim) - (length/2)) + table.EdgeRegion;
                //int yv = ((y% table.BlobDim) - (length/2)) + table.EdgeRegion;
                //int xv = ((x % table.BlobDim) - (length/2)) + table.EdgeRegion;

                int zv = z;
                int yv = y;
                int xv = x;

                //This code is wrong -- input values have already been floored.
                //if ((length % 2) == 0)
                //{
                // Not possible to center an even cube... weight in the positive direction
                //zv++; yv++; xv++;
                //}

                for (int k = 0; k < length; k++)
                {
                    for (int j = 0; j < length; j++)
                    {
                        for (int i = 0; i < length; i++)
                        {
                            //int off = ((zv+k) * side * side + (yv+j) * side + (xv + i)) * table.Components;
                            for (int c = 0; c < table.Components; c++)
                            {
                                try
                                {
                                    dataout[k, j, i, c] = data[GetLocalOffset(zv + k, yv + j, xv + i, c)];
                                }
                                catch (Exception e)
                                {
                                    throw new Exception(String.Format("Error writing to {0},{1},{2},{3} from {4},{5},{6},{7} [Inner Exception: {8}]",
                                        k, j, i, c, zv + k, yv + j, xv + i, c, e.ToString()));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(String.Format("{0} with {1},{2},{3} for blob {4},{5},{6} [Inner error: {7}]",
                    e.Message, z, y, x, basez, basey, basex, e.ToString()));
            }

            return 0;
        }

        /// <summary>
        /// Retrieve a 3d cube around a point
        /// </summary>
        public int GetDataCubeAroundPoint(int z, int y, int x, int length, float[, , ,] dataout)
        {
            return GetDataCubeCorneredAtPoint(z - (length / 2), y - (length / 2), x - (length / 2), length, dataout);
        }

        /// <summary>
        /// Retrieve a 3d cube around a point, as a 1d C-style array
        /// </summary>
        public int GetFlatDataCubeAroundPoint(int z, int y, int x, int length, float[] dataout)
        {
            return GetFlatDataCubeAroundPoint(z, y, x, length, dataout, 0);
        }
        
        public int GetFlatDataCubeAroundPoint(int z, int y, int x, int length, float[] dataout, int offset) {
            return GetFlatDataCubeCorneredAtPoint(z - (length / 2), y - (length / 2), x - (length / 2),
                length, dataout, offset);
        }

        public int GetFlatDataCubeCorneredAtPoint(int z, int y, int x, int length, float[] dataout)
        {
            return GetFlatDataCubeCorneredAtPoint(z, y, x, length, dataout, 0);
        }

        /// <summary>
        /// Retrieve a 3d cube around a point, as a 1d C-style array, saved at a fixed offset
        /// </summary>
        public int GetFlatDataCubeCorneredAtPoint(int z, int y, int x, int length, float[] dataout, int offset)
        {
            if (length == 0)
            {
                throw new ArgumentOutOfRangeException("length", "Length must not be 0.");
            }
            else if (length > 2 * table.EdgeRegion)
            {
                throw new ArgumentOutOfRangeException("length", "Length is too large for guaranteed edge overlap!");
            }
            if (dataout == null)
            {
                dataout = new float[offset + length * length * length * table.Components];
            }

            try
            {

                // Compute base of array to take
                int zv = z - (length / 2);
                int yv = y - (length / 2);
                int xv = x - (length / 2);

                //if ((length % 2) == 0)
                //{
                    // Not possible to center an even cube... weight in the positive direction
                //    zv++; yv++; xv++;
                //}
                for (int k = 0; k < length; k++)
                {
                    for (int j = 0; j < length; j++)
                    {
                        for (int i = 0; i < length; i++)
                        {

                            for (int c = 0; c < table.Components; c++)
                            {
                                try
                                {
                                    //int srcoff = ((zv + k) * side * side + (yv + j) * side + (xv + i)) * table.Components + c;
                                    int destoff = k * length * length * table.Components + j * length * table.Components + i * table.Components + c;
                                    dataout[destoff + offset] = data[GetLocalOffset(zv + k, yv + j, xv + i, c)];
                                }
                                catch (Exception e)
                                {
                                    throw new Exception(String.Format("Error writing to {0} ({1},{2},{3},{4}) from ({5},{6},{7},{8} [Inner Exception: {9}]",
                                        k, j, i, c, zv + k, yv + j, xv + i, c, e.ToString()));
                                }
                            }
                        }
                    }
                }
            } catch (Exception e) {
                throw new Exception(String.Format("{0} with {1},{2},{3} for blob {4},{5},{6} [Inner error: {7}]",
                    e.Message, x, y, z, basex, basey, basez, e.ToString()));
            }

            return 0;
        }


     /// <summary>
     /// Return the local offset associated with a Z,Y,X,Component value
     /// </summary>
     /// <remarks>
     /// URGENT: Replace this code with cleaner logic. 
     /// </remarks>
     public int GetLocalOffset(int z, int y, int x, int c) {

         // Wrap the coordinates into the grid space
         x = ((x % table.GridResolutionX) + table.GridResolutionX) % table.GridResolutionX;
         y = ((y % table.GridResolutionY) + table.GridResolutionY) % table.GridResolutionY;
         z = ((z % table.GridResolutionZ) + table.GridResolutionZ) % table.GridResolutionZ;
        

         // Kludge to use the negative value for points in the negative overlap
         // [ chunks along any axis ]
         if ((realz < 0) && (z >= (table.GridResolutionZ - table.EdgeRegion)))
         {
             z = z - table.GridResolutionZ;
         }
         if ((realy < 0) && (y >= (table.GridResolutionY - table.EdgeRegion)))
         {
             y = y - table.GridResolutionY;
         } 
         if ((realx < 0) && (x >= (table.GridResolutionX - table.EdgeRegion)))
         {
             x = x - table.GridResolutionX;
         }

         // Calculate the expected offset (from logical corner) and then adjust for edge
         int zoff, yoff, xoff;

         if (realz > z)
         {
             zoff = (z + table.GridResolutionZ) - realz;
         }
         else
         {
             zoff = z - realz;
         }
         if (realy > y)
         {
             yoff = (y + table.GridResolutionY) - realy;
         }
         else
         {
             yoff = y - realy;
         }
         if (realx > x)
         {
             xoff = (x + table.GridResolutionX) - realx;
         }
         else
         {
             xoff = x - realx;
         }

         return (zoff * side * side + yoff * side + xoff) * table.Components + c;
     }

     /// <summary>
     /// Return the local offset associated with a Z,Y,X,Component value
     /// </summary>
     /// <remarks>
     /// URGENT: Replace this code with cleaner logic. 
     /// </remarks>
     public int GetLocalOffsetMHD(int z, int y, int x, int c)
     {

         // Wrap the coordinates into the grid space
         x = ((x % table.GridResolutionX) + table.GridResolutionX) % table.GridResolutionX;
         y = ((y % table.GridResolutionY) + table.GridResolutionY) % table.GridResolutionY;
         z = ((z % table.GridResolutionZ) + table.GridResolutionZ) % table.GridResolutionZ;


         // Kludge to use the negative value for points in the negative overlap
         // [ chunks along any axis ]
         if ((realz < 0) && (z >= (table.GridResolutionZ - table.EdgeRegion)))
         {
             z = z - table.GridResolutionZ;
         }
         if ((realy < 0) && (y >= (table.GridResolutionY - table.EdgeRegion)))
         {
             y = y - table.GridResolutionY;
         } 
         if ((realx < 0) && (x >= (table.GridResolutionX - table.EdgeRegion)))
         {
             x = x - table.GridResolutionX;
         }

         // Calculate the expected offset (from logical corner) and then adjust for edge
         int zoff, yoff, xoff;

         if (realz > z)
         {
             zoff = (z + table.GridResolutionZ) - realz;
         }
         else
         {
             zoff = z - realz;
         }
         if (realy > y)
         {
             yoff = (y + table.GridResolutionY) - realy;
         }
         else
         {
             yoff = y - realy;
         }
         if (realx > x)
         {
             xoff = (x + table.GridResolutionX) - realx;
         }
         else
         {
             xoff = x - realx;
         }

         return (zoff * side * side + yoff * side + xoff) * table.Components + c;
         //return (zoff * side * side + yoff * side + xoff) + c * side * side * side;
     }

     /// <summary>
     /// Return the local offset associated with a Z,Y,X value
     /// </summary>
     /// <remarks>
     /// This method does not account for any Edge replication
     /// URGENT: Replace this code with cleaner logic. 
     /// </remarks>
     //public void GetSubcubeStart(int z, int y, int x, int nOrder, ref int startz, ref int starty, ref int startx)
     public void GetSubcubeStart(int z, int y, int x, ref int startz, ref int starty, ref int startx)
     {
         //int[] start = new int[3];

         // Wrap the coordinates into the grid space
         //x = ((x % table.GridResolution) + table.GridResolution) % table.GridResolution;
         //y = ((y % table.GridResolution) + table.GridResolution) % table.GridResolution;
         //z = ((z % table.GridResolution) + table.GridResolution) % table.GridResolution;
         //if (x >= table.GridResolution)
         //    x -= table.GridResolution;
         //else if (x < 0)
         //    x += table.GridResolution;
         //if (y >= table.GridResolution)
         //    y -= table.GridResolution;
         //else if (y < 0)
         //    y += table.GridResolution;
         //if (z >= table.GridResolution)
         //    z -= table.GridResolution;
         //else if (z < 0)
         //    z += table.GridResolution;


         // Kludge to use the negative value for points in the negative overlap
         // [ chunks along any axis ]
         //if ((realz < 0) && (z >= (table.GridResolution - table.EdgeRegion)))
         //    z = z - table.GridResolution;
         //if ((realy < 0) && (y >= (table.GridResolution - table.EdgeRegion)))
         //    y = y - table.GridResolution;
         //if ((realx < 0) && (x >= (table.GridResolution - table.EdgeRegion)))
         //    x = x - table.GridResolution;

         //if (realz > z + nOrder)
         //    z = (z + table.GridResolution);
         //if (realy > y + nOrder)
         //    y = (y + table.GridResolution);
         //if (realx > x + nOrder)
         //    x = (x + table.GridResolution);

         // This is to take care of the situation where we've had a wrap-around
         //if (z < 0 && realz >= table.GridResolution - nOrder / 2)
         if (z < 0)
             z = (z + table.GridResolutionZ);
         //if (y < 0 && realy >= table.GridResolution - nOrder / 2)
         if (y < 0)
             y = (y + table.GridResolutionY);
         //if (x < 0 && realx >= table.GridResolution - nOrder / 2)
         if (x < 0)
             x = (x + table.GridResolutionX);

         //Debug.Assert(z >= 0);
         //Debug.Assert(x >= 0);
         //Debug.Assert(y >= 0);
         //Debug.Assert(z <= table.GridResolution);
         //Debug.Assert(x <= table.GridResolution);
         //Debug.Assert(y <= table.GridResolution);

         // Calculate the expected offset (from logical corner) and then adjust for edge

         if (realz <= z && z <= realz + side - 1)
         {
             //start[0] = z - realz;
             startz = z - realz;
         }
         else
         {
             //start[0] = 0;
             startz = 0;
         }
         if (startz >= side || startz < 0)
             throw new Exception(String.Format("Start of the subcube [{6},{7},{8}] is outside of the range of the blob" +
                 " for cube start {0},{1},{2} and blob {3},{4},{5}",
                 x, y, z, basex, basey, basez, startx, starty, startz));

         if (realy <= y && y <= realy + side - 1)
         {
             //start[1] = y - realy;
             starty = y - realy;
         }
         else
         {
             //start[1] = 0;
             starty = 0;
         }
         if (starty >= side || starty < 0)
             throw new Exception(String.Format("Start of the subcube [{6},{7},{8}] is outside of the range of the blob" +
                 " for cube start {0},{1},{2} and blob {3},{4},{5}",
                 x, y, z, basex, basey, basez, startx, starty, startz));

         if (realx <= x && x <= realx + side - 1)
         {
             //start[2] = x - realx;
             startx = x - realx;
         }
         else
         {
             //start[2] = 0;
             startx = 0;
         }
         if (startx >= side || startx < 0)
             throw new Exception(String.Format("Start of the subcube [{6},{7},{8}] is outside of the range of the blob" +
                 " for cube start {0},{1},{2} and blob {3},{4},{5}",
                 x, y, z, basex, basey, basez, startx, starty, startz));

         //return start;
     }

     //Added by Kalin
     /// <summary>
     /// Return the local offset associated with a Z,Y,X value
     /// </summary>
     /// <remarks>
     /// This method does not account for any Edge replication
     /// URGENT: Replace this code with cleaner logic. 
     /// </remarks>
     //public void GetSubcubeEnd(int z, int y, int x, int nOrder, ref int endz, ref int endy, ref int endx)
     public void GetSubcubeEnd(int z, int y, int x, ref int endz, ref int endy, ref int endx)
     {
         //int[] end = new int[3];

         // Wrap the coordinates into the grid space
         //x = ((x % table.GridResolution) + table.GridResolution) % table.GridResolution;
         //y = ((y % table.GridResolution) + table.GridResolution) % table.GridResolution;
         //z = ((z % table.GridResolution) + table.GridResolution) % table.GridResolution;


         // Kludge to use the negative value for points in the negative overlap
         // [ chunks along any axis ]
         //if ((realz < 0) && (z >= (table.GridResolution - table.EdgeRegion)))
         //    z = z - table.GridResolution;
         //if ((realy < 0) && (y >= (table.GridResolution - table.EdgeRegion)))
         //    y = y - table.GridResolution;
         //if ((realx < 0) && (x >= (table.GridResolution - table.EdgeRegion)))
         //    x = x - table.GridResolution;

         // This is to take care of the situation where we've had a wrap-around
         //if (z >= table.GridResolution && realz <= nOrder / 2 - 1)
         if (z >= table.GridResolutionZ)
             z = (z - table.GridResolutionZ);
         //if (y >= table.GridResolution && realy <= nOrder / 2 - 1)
         if (y >= table.GridResolutionY)
             y = (y - table.GridResolutionY);
         //if (x >= table.GridResolution && realx <= nOrder / 2 - 1)
         if (x >= table.GridResolutionX)
             x = (x - table.GridResolutionX);

         //Debug.Assert(z >= 0);
         //Debug.Assert(x >= 0);
         //Debug.Assert(y >= 0);
         //Debug.Assert(z <= table.GridResolution);
         //Debug.Assert(x <= table.GridResolution);
         //Debug.Assert(y <= table.GridResolution);

         // Calculate the expected offset (from logical corner) and then adjust for edge

         if (realz <= z && z <= realz + side - 1)
         {
             //end[0] = z - realz;
             endz = z - realz;
         }
         else
         {
             //end[0] = side - 1;
             endz = side - 1;
         }
         if (endz >= side || endz < 0)
             throw new Exception(String.Format("End of the subcube [{6},{7},{8}] is outside of the range of the blob" +
                 " for cube end {0},{1},{2} and blob {3},{4},{5}",
                 x, y, z, basex, basey, basez, endx, endy, endz));

         if (realy <= y && y <= realy + side - 1)
         {
             //end[1] = y - realy;
             endy = y - realy;
         }
         else
         {
             //end[1] = side - 1;
             endy = side - 1;
         }
         if (endy >= side || endy < 0)
             throw new Exception(String.Format("End of the subcube [{6},{7},{8}] is outside of the range of the blob" +
                 " for cube end {0},{1},{2} and blob {3},{4},{5}",
                 x, y, z, basex, basey, basez, endx, endy, endz));

         if (realx <= x && x <= realx + side - 1)
         {
             //end[2] = x - realx;
             endx = x - realx;
         }
         else
         {
             //end[2] = side - 1;
             endx = side - 1;
         }
         if (endx >= side || endx < 0)
             throw new Exception(String.Format("End of the subcube [{6},{7},{8}] is outside of the range of the blob" +
                 " for cube end {0},{1},{2} and blob {3},{4},{5}",
                 x, y, z, basex, basey, basez, endx, endy, endz));

         //return end;
     }

     //Added by Kalin
     /// <summary>
     /// Return the side of the blob
     /// </summary>
     public int GetSide { get { return side; } }

     public int GetRealX { get { return realx; } }

     public int GetRealY { get { return realy; } }

     public int GetRealZ { get { return realz; } }

     public int GetBaseX { get { return basex; } }

     public int GetBaseY { get { return basey; } }

     public int GetBaseZ { get { return basez; } }

     public int GetComponents { get { return table.Components; } }

    }
}
