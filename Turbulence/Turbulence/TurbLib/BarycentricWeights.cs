using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace Turbulence.TurbLib
{
    // Collection of the barycentric weights used for interpolation or differentiation.
    public abstract class BarycentricWeights
    {
        public abstract void GetWeightsFromDB(SqlConnection conn, string tableName);

        public virtual double[] GetWeights(int cell_index)
        {
            throw new NotSupportedException();
        }

        public virtual double[] GetWeights()
        {
            throw new NotSupportedException();
        }

        public virtual double this[int cell_index, int weight_index]
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public virtual double this[int weight_index]
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public abstract int GetStencilStart(int cell_index, int interpolationOrder);

        public virtual int GetStencilEnd(int cell_index, int interpolationOrder)
        {
            throw new NotSupportedException();
        }

        public virtual int GetStencilEnd(int cell_index)
        {
            throw new NotSupportedException();
        }

        public virtual int GetOffset(int cell_index)
        {
            throw new NotSupportedException();
        }
    }

    // Collection of the barycentric weights used for interpolation.
    public class UniformBarycentricWeights : BarycentricWeights
    {
        public override void GetWeightsFromDB(SqlConnection conn, string tableName)
        {
            SqlCommand GetGridPointsCommand = new SqlCommand(String.Format("SELECT * FROM {0}", tableName), conn);
            using (SqlDataReader reader = GetGridPointsCommand.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    int num_rows = 0;
                    while (reader.Read())
                    {
                        weights = new double[reader.FieldCount];
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            weights[i] = reader.GetSqlDouble(i).Value;
                        }
                        num_rows++;
                    }
                    if (num_rows > 1)
                    {
                        throw new Exception("More than 1 row returned from DB for uniform barycentric weights, while only 1 row was expected!");
                    }
                }
                else
                {
                    reader.Close();
                    throw new Exception("No rows returned, when requesting y grid poitns from database!");
                }
            }
        }

        public override int GetStencilStart(int cell_index, int interpolationOrder)
        {
            return cell_index - interpolationOrder / 2 + 1;
        }

        public override double[] GetWeights()
        {
            return weights;
        }

        public override double this[int weight_index]
        {
            get
            {
                return weights[weight_index];
            }
        }

        double[] weights;
    }

    // Collection of the weights used for differentiation.
    public class UniformDifferentiationMatrix : UniformBarycentricWeights
    {
        public override int GetStencilStart(int cell_index, int differencingOrder)
        {
            return cell_index - differencingOrder / 2;
        }

        public override int GetStencilEnd(int cell_index, int differencingOrder)
        {
            return cell_index + differencingOrder / 2;
        }
    }

    // Collection of the weights used for differentiation.
    public class NonUniformBarycentricWeights : BarycentricWeights
    {
        public NonUniformBarycentricWeights()
        {
            grid_weights = new Dictionary<int, Weights>();
        }

        public override void GetWeightsFromDB(SqlConnection conn, string tableName)
        {
            int cell_index, offset_index, stencil_start, stencil_end;
            double[] weights;
            SqlCommand GetGridPointsCommand = new SqlCommand(String.Format("SELECT * FROM {0}", tableName), conn);
            using (SqlDataReader reader = GetGridPointsCommand.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        cell_index = reader.GetSqlInt32(0).Value;
                        offset_index = reader.GetSqlInt32(1).Value;
                        stencil_start = reader.GetSqlInt32(2).Value;
                        stencil_end = reader.GetSqlInt32(3).Value;
                        weights = new double[reader.FieldCount - 4];
                        for (int i = 0; i < reader.FieldCount - 4; i++)
                        {
                            weights[i] = reader.GetSqlDouble(i + 4).Value;
                        }
                        AddWeights(cell_index, offset_index, stencil_start, stencil_end, weights);
                    }
                }
                else
                {
                    reader.Close();
                    throw new Exception("No rows returned, when requesting y grid poitns from database!");
                }
            }
        }

        private void AddWeights(int cell_index, int offset_index, int stencil_start, int stencil_end, double[] weights)
        {
            Weights uniformWeights = new Weights(offset_index, stencil_start, stencil_end, weights);
            grid_weights.Add(cell_index, uniformWeights);
        }
        public override double[] GetWeights(int cell_index)
        {
            return grid_weights[cell_index].WeightsValues;
        }
        public override int GetStencilStart(int cell_index, int interpolationOrder)
        {
            return grid_weights[cell_index].StencilStart;
        }
        public override int GetStencilEnd(int cell_index)
        {
            return grid_weights[cell_index].StencilEnd;
        }
        public override int GetOffset(int cell_index)
        {
            return grid_weights[cell_index].OffsetIndex;
        }

        public override double this[int cell_index, int weight_index]
        {
            get
            {
                return grid_weights[cell_index].WeightsValues[weight_index];
            }
        }

        Dictionary<int, Weights> grid_weights;
    }

    // Class representing the weights for a given cell index for non-uniform grids.
    // Equivalent to a record in the database table for the barycentric weights.
    // Also used for the spline weights, where the offset_index is treated as the neighbor index.
    class Weights
    {
        public Weights(int size)
        {
            weights = new double[size];
        }
        public Weights(int offset_index, int stencil_start, int stencil_end, double[] weights)
        {
            this.weights = weights;
            this.offset_index = offset_index;
            this.stencil_start = stencil_start;
            this.stencil_end = stencil_end;
        }
        public double[] WeightsValues
        {
            get { return weights; }
        }
        public double this[int index]
        {
            set { weights[index] = value; }
            get { return weights[index]; }
        }
        public int StencilStart
        {
            set { stencil_start = value; }
            get { return stencil_start; }
        }
        public int StencilEnd
        {
            set { stencil_end = value; }
            get { return stencil_end; }
        }
        public int OffsetIndex
        {
            set { offset_index = value; }
            get { return offset_index; }
        }
        int offset_index;
        int stencil_start;
        int stencil_end;
        double[] weights;
    }
}
