using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Data;

namespace IsoImportData
{
    /// <summary>
    /// Wrapper for loading data into the database nodes.
    /// </summary>
    class IsoDatabase
    {
        SqlConnection sqlcon;
        string connectionString;
        private static DateTime start;

        public IsoDatabase(string connectionString)
        {
            this.connectionString = connectionString;
            Open();
        }

        public void Open()
        {
            sqlcon = new SqlConnection(connectionString);
            sqlcon.Open();
        }

        public void Close()
        {
            sqlcon.Close();
        }

        /// <summary>
        /// Turns TRACE flag 610 ON
        /// </summary>
        public void EnableMinimalLoggin()
        {
            SqlCommand cmd = new SqlCommand("DBCC TRACEON(610);", sqlcon);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Turns TRACE flag 1211 ON
        /// </summary>
        public void DisableLockEscalation()
        {
            SqlCommand cmd = new SqlCommand("DBCC TRACEON(1211);", sqlcon);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Get the partition limits for the given slice and partition number
        /// </summary>
        public void GetPartLimits(int sliceNum, int partitionNum, ref long minLim, ref long maxLim)
        {
            SqlCommand cmd = new SqlCommand(
                String.Format("select minLim, maxLim from turblib.dbo.PartLimits08 where sliceNum = {0} and partitionNum = {1}", sliceNum, partitionNum),
                sqlcon);
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                reader.Read();
                minLim = reader.GetInt64(0);
                maxLim = reader.GetInt64(1);
            }
        }

        /// <summary>
        /// Intiate bulk copy with database server.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="reader"></param>
        public void BulkCopy(string tableName, IDataReader reader)
        {
            // We don't want or need very good transaction protection...
            //using (SqlTransaction sqlTxn = sqlcon.BeginTransaction(IsolationLevel.ReadUncommitted))
            //{

            // Do we need the table lock?  Will try using txns instead....            

            using (SqlBulkCopy bulkCopy =
                         new SqlBulkCopy(sqlcon.ConnectionString, SqlBulkCopyOptions.Default))
            {
                bulkCopy.DestinationTableName = tableName;
                bulkCopy.BulkCopyTimeout = 0;
                bulkCopy.BatchSize = 1000;
                //bulkCopy.NotifyAfter = 100000;
                //bulkCopy.SqlRowsCopied += new SqlRowsCopiedEventHandler(BulkCopyStatus);

                try
                {
                    // Write from the source to the destination.
                    Console.Write("Writing to server");
                    start = DateTime.Now;
                    bulkCopy.WriteToServer(reader);
                    //sqlTxn.Commit();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);

                    Console.WriteLine("Failed: {0}", reader);
                     
                    //sqlTxn.Rollback();
                }
                finally
                {
                    // Close the DataReader The SqlBulkCopy
                    // object is automatically closed at the end
                    // of the using block.
                    reader.Close();
                }
            }
            //}
        }

        private static void BulkCopyStatus(
            object sender, SqlRowsCopiedEventArgs e)
        {
            TimeSpan elapsed = DateTime.Now - start;
            double MBs = e.RowsCopied * 1024.0 / 1024.0 / 1024.0 / elapsed.TotalSeconds;
            Console.WriteLine("Copied {0} so far... Rate = {1} MB/s", e.RowsCopied, MBs);
        }

    }
}
