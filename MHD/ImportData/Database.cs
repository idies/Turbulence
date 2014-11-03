using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Collections;

namespace ImportData
{
    /// <summary>
    /// Wrapper for writing/reading data to/from the database nodes.
    /// </summary>
    public class Database
    {
        SqlConnection sqlcon;
        string connectionString;

        public MHDDataReader dataReader;

        public Database(string connectionString, int recordSize, int dataToWrite)
        {
            this.connectionString = connectionString;
            this.dataReader = (MHDDataReader)MHDDataReader.GetReader(recordSize);
            if (dataToWrite > 0)
                this.dataReader.data = new byte[dataToWrite];
        }

        public  void Open()
        {
            sqlcon = new SqlConnection(connectionString);
            sqlcon.Open();
        }

        public void Close()
        {
            sqlcon.Close();
        }

        /// <summary>
        /// Execute a query.
        /// </summary>
        /// <param name="tableName"></param>
        public void ExecuteQuery(string tableName, string select, byte[] rawdata, int data_size)
        {
            SqlCommand command = new SqlCommand(select, sqlcon);
            using (SqlDataReader reader = command.ExecuteReader())
            {
                long bytesRead = 0;
                int bufferIndex = 0;
                while (reader.Read())
                {
                    bufferIndex = (int)bytesRead;
                    bytesRead += reader.GetBytes(0, 0, rawdata, bufferIndex, data_size - bufferIndex);
                }
                reader.Close();
            }
        }

        /// <summary>
        /// Get the execution plan of a query.
        /// </summary>
        /// <param name="tableName"></param>
        public void GetExecutionPlan(string tableName, string select, byte[] rawdata, int data_size)
        {
            SqlCommand command = new SqlCommand();
            command.Connection = sqlcon;
            command.CommandText = "SET SHOWPLAN_ALL ON";
            command.CommandType = CommandType.Text;
            command.ExecuteNonQuery();
            command.CommandText = select;
            command.CommandType = CommandType.Text;
            command.CommandTimeout = 0;
            using (SqlDataReader reader = command.ExecuteReader())
            {
                long bytesRead = 0;
                int bufferIndex = 0;
                Console.WriteLine();
                Console.WriteLine("Execution plan:");
                while (reader.Read())
                {
                    //bufferIndex = (int)bytesRead;
                    //bytesRead += reader.GetBytes(0, 0, rawdata, bufferIndex, data_size - bufferIndex);
                    Console.WriteLine("StmtText: " + reader[0].ToString());
                    Console.WriteLine("StmtId: " + reader[1].ToString());
                    Console.WriteLine("NodeId: " + reader[2].ToString());
                    Console.WriteLine("Parent: " + reader[3].ToString());
                    Console.WriteLine("Physicalop: " + reader[4].ToString());
                    Console.WriteLine("Logicalop: " + reader[5].ToString());
                    Console.WriteLine("Argument: " + reader[6].ToString());
                    Console.WriteLine("DefinedValues: " + reader[7].ToString());
                    Console.WriteLine("EstimateRows: " + reader[8].ToString());
                    Console.WriteLine("Estimateio: " + reader[9].ToString());
                    Console.WriteLine("Estimatecpu: " + reader[10].ToString());
                    Console.WriteLine("AvgRowSize: " + reader[11].ToString());
                    Console.WriteLine("TotalSubtreeCost: " + reader[12].ToString());
                    Console.WriteLine("OutputList: " + reader[13].ToString());
                    Console.WriteLine("Warnings: " + reader[14].ToString());
                    Console.WriteLine("Type: " + reader[15].ToString());
                    Console.WriteLine("Parallel: " + reader[16].ToString());
                    Console.WriteLine("EstimateExecutions: " + reader[17].ToString());
                }
                reader.Close();
            }
            command.CommandText = "SET SHOWPLAN_ALL OFF";
            command.CommandType = CommandType.Text;
            command.ExecuteNonQuery();
        }
        public void DropAndCreateTempTable(string tableName)
        {
            DropTable(tableName);
            SqlCommand command = new SqlCommand(String.Format("CREATE TABLE {0} (zindex int)", tableName), sqlcon);
            try
            {
                command.ExecuteNonQuery();
                //Console.WriteLine(String.Format("Table {0} created successfully", tableName));
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        public void InsertIntoTable(string tableName, ArrayList points)
        {
            int recordCount = 0;

            DataTable dataTable = new DataTable(tableName);
            dataTable.Columns.Add("zindex", typeof(long));

            for (int i = 0; i < points.Count; i++)
            {
                DataRow row;
                // populate the values
                // using your custom logic
                row = dataTable.NewRow();

                row[0] = points[i];

                // add it to the base for final addition to the DB
                dataTable.Rows.Add(row);
                recordCount++;
            }

            // make sure to enable triggers
            // more on triggers in next post
            SqlBulkCopy bulkCopy = 
                new SqlBulkCopy
                (
                sqlcon, 
                SqlBulkCopyOptions.TableLock | 
                SqlBulkCopyOptions.FireTriggers | 
                SqlBulkCopyOptions.UseInternalTransaction,
                null
                );

            // set the destination table name
            bulkCopy.DestinationTableName = tableName;

            try
            {
                // write the data in the "dataTable"
                bulkCopy.WriteToServer(dataTable);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            // reset
            dataTable.Clear();
            //Console.WriteLine("{0} values inserted successfully", recordCount);

        }
        public long InsertIntoTable(string tableName, byte[] records, int record_size)
        {
            int recordCount = 0;

            DataTable dataTable = new DataTable(tableName);
            //TODO: This should not be hardcoded:
            dataTable.Columns.Add("timestep", typeof(int));
            dataTable.Columns.Add("zindex", typeof(long));
            dataTable.Columns.Add("data", typeof(byte[]));

            int sourceIndex = 0;
            int cube_size = 0;
            for (int i = 0; i < records.GetLength(0)/record_size; i++)
            {
                DataRow row;
                // populate the values
                // using your custom logic
                row = dataTable.NewRow();

                row[0] = System.BitConverter.ToInt32(records, sourceIndex);
                sourceIndex += sizeof(int);
                row[1] = System.BitConverter.ToInt64(records, sourceIndex);
                sourceIndex += sizeof(long);
                if (record_size > 8000)
                {
                    cube_size = (int)System.BitConverter.ToInt64(records, sourceIndex);
                    sourceIndex += sizeof(long);
                }
                else
                {
                    cube_size = System.BitConverter.ToInt16(records, sourceIndex);
                    sourceIndex += sizeof(short);
                }
                //Array.Copy(records, sourceIndex, (Array)row[2], 0, cube_size);
                byte[] data = new byte[cube_size];
                Array.Copy(records, sourceIndex, data, 0, cube_size);
                row[2] = data;
                sourceIndex += cube_size;

                // add it to the base for final addition to the DB
                dataTable.Rows.Add(row);
                recordCount++;
            }

            SqlBulkCopy bulkCopy = new SqlBulkCopy(sqlcon);

            // set the destination table name
            bulkCopy.DestinationTableName = tableName;
            bulkCopy.BatchSize = 4096;

            try
            {
                // write the data in the "dataTable"
                bulkCopy.WriteToServer(dataTable);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return 0;
            }
            // reset
            dataTable.Clear();
            //Console.WriteLine("{0} values inserted successfully", recordCount);
            return records.GetLength(0);

        }
        public long InsertIntoTable_wDataReader(string tableName)
        {
            //it is important to reset the DataReader
            //in order to be able to insert the data and start from the beginning
            dataReader.Reset();

            SqlBulkCopy bulkCopy = new SqlBulkCopy(sqlcon);

            // set the destination table name
            bulkCopy.DestinationTableName = tableName;
            //bulkCopy.BatchSize = 1024;

            try
            {
                // write the data in the "dataTable"
                bulkCopy.WriteToServer(dataReader);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return 0;
            }
            return dataReader.data.GetLength(0);

        }
        public void DropTable(string tableName)
        {
            SqlCommand command = new SqlCommand(String.Format("IF  EXISTS " +
                "(SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{0}') AND type in (N'U'))" +
                "DROP TABLE {0}", tableName), sqlcon);
            try
            {
                command.ExecuteNonQuery();
                //Console.WriteLine(String.Format("Table {0} dropped successfully", tableName));
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
