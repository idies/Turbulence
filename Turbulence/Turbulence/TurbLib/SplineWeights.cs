using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace Turbulence.TurbLib
{
    public class SplineWeights
    {
        public SplineWeights()
        {
            grid_weights = new Dictionary<Tuple2<int, int>, Weights>();
        }

        public void GetWeightsFromDB(SqlConnection conn, string tableName)
        {
            int cell_index, neighbor_index, stencil_start, stencil_end;
            double[] weights;
            SqlCommand GetGridPointsCommand = new SqlCommand(String.Format("SELECT stencil_start, stencil_end, {0}.* from {0}, " +
                "(SELECT cell_index, MIN(neighbor_index) as stencil_start, MAX(neighbor_index) as stencil_end from {0} group by cell_index) as t1 " +
                "WHERE {0}.cell_index = t1.cell_index", tableName), conn);
            using (SqlDataReader reader = GetGridPointsCommand.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        stencil_start = reader.GetSqlInt32(0).Value;
                        stencil_end = reader.GetSqlInt32(1).Value;
                        cell_index = reader.GetSqlInt32(2).Value;
                        neighbor_index = reader.GetSqlInt32(3).Value;
                        weights = new double[reader.FieldCount - 4];
                        for (int i = 0; i < reader.FieldCount - 4; i++)
                        {
                            weights[i] = reader.GetSqlDouble(i + 4).Value;
                        }
                        AddWeights(cell_index, neighbor_index, stencil_start, stencil_end, weights);
                    }
                }
                else
                {
                    reader.Close();
                    throw new Exception("No rows returned, when requesting y grid poitns from database!");
                }
            }
        }

        private void AddWeights(int cell_index, int neighbor_index, int stencil_start, int stencil_end, double[] weights)
        {
            Weights uniformWeights = new Weights(neighbor_index, stencil_start, stencil_end, weights);
            grid_weights.Add(new Tuple2<int, int>(cell_index, neighbor_index), uniformWeights);
        }
        public double[] GetWeights(int cell_index, int neighbor_index)
        {
            return grid_weights[new Tuple2<int, int>(cell_index, neighbor_index)].WeightsValues;
        }
        public int GetStencilStart(int cell_index)
        {
            return grid_weights[new Tuple2<int, int>(cell_index, cell_index)].StencilStart;
        }
        public int GetStencilEnd(int cell_index)
        {
            return grid_weights[new Tuple2<int, int>(cell_index, cell_index)].StencilEnd;
        }

        public double this[int cell_index, int neighbor_index, int weight_index]
        {
            get
            {
                return grid_weights[new Tuple2<int, int>(cell_index, neighbor_index)].WeightsValues[weight_index];
            }
        }

        Dictionary<Tuple2<int,int>, Weights> grid_weights;
    }
}
