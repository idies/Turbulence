using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace Turbulence.TurbLib
{
    public class GridPoints
    {
        public class GridPointsComparer : IComparer<KeyValuePair<int, double>>
        {
            public int Compare(KeyValuePair<int, double> a, KeyValuePair<int, double> b)
            {
                return a.Value.CompareTo(b.Value);
            }
        }

        public GridPoints(int count)
        {
            this.grid_points = new List<KeyValuePair<int, double>>(count);
        }

        /// <summary>
        /// Retrieves the grid values for the y dimension over the given SQL Server connection.
        /// </summary>
        /// <param name="conn">Open connection to the SQL Server and database containing the "grid_points_y" table storing the grid values for y.</param>
        public void GetGridPointsFromDB(SqlConnection conn)
        {
            SqlCommand GetGridPointsCommand = new SqlCommand(String.Format("SELECT cell_index, value FROM grid_points_y ORDER BY cell_index"), conn);
            using (SqlDataReader reader = GetGridPointsCommand.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        grid_points.Add(new KeyValuePair<int, double>(reader.GetSqlInt32(0).Value, reader.GetSqlDouble(1).Value));
                    }
                }
                else
                {
                    reader.Close();
                    throw new Exception("No rows returned, when requesting y grid poitns from database!");
                }
            }
        }

        public int GetCellIndex(double value, double center_line)
        {
            int i = grid_points.BinarySearch(new KeyValuePair<int, double>(-1, value), new GridPointsComparer());
            if (i >= 0)
            {
                // The given y value matches the value of a grid cell
                return i;
            }
            else
            {
                // There is no match, so the index returned is the complement of the nearest index in the gird
                int indexOfNearest = ~i;

                if (indexOfNearest == grid_points.Count)
                {
                    throw new Exception(String.Format("The given y value is greater than the largest allowed grid value for the y dimension: {0}",
                        value));
                }
                else if (indexOfNearest == 0)
                {
                    throw new Exception(String.Format("The given y value is smaller than the smallest allowed grid value for the y dimension: {0}",
                        value));
                }
                else
                {
                    // The given value is between (indexOfNearest - 1) and indexOfNearest
                    
                    // The cell_index (n) for the y direction is given by:
                    // y_n <= value < y_(n+1) if value <= 0
                    // y_(n-1) < value <= y_n if value > 0
                    if (value <= center_line)
                        return indexOfNearest - 1;
                    else
                        return indexOfNearest;
                }
            }
        }

        public int GetCellIndexRound(double value)
        {
            int i = grid_points.BinarySearch(new KeyValuePair<int, double>(-1, value), new GridPointsComparer());
            if (i >= 0)
            {
                // The given y value matches the value of a grid cell
                return i;
            }
            else
            {
                // There is no match, so the index returned is the complement of the nearest index in the gird
                int indexOfNearest = ~i;

                if (indexOfNearest == grid_points.Count)
                {
                    throw new Exception(String.Format("The given y value is greater than the largest allowed grid value for the y dimension: {0}",
                        value));
                }
                else if (indexOfNearest == 0)
                {
                    throw new Exception(String.Format("The given y value is smaller than the smallest allowed grid value for the y dimension: {0}",
                        value));
                }
                else
                {
                    // The given value is between (indexOfNearest - 1) and indexOfNearest
                    if (grid_points[indexOfNearest].Value - value < value - grid_points[indexOfNearest - 1].Value)
                        return indexOfNearest;
                    else
                        return indexOfNearest - 1;
                }
            }
        }
        
        public double GetGridValue(int cell_index)
        {
            // The entries in the List of key-value pairs are sorted on the cell index,
            // so we can return the value associated with the item at possition of cell index.
            return grid_points[cell_index].Value;
        }

        private List<KeyValuePair<int, double>> grid_points;
    }
}
