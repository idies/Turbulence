using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Data;

namespace ImportData
{
    /// <summary>
    /// Wrapper for loading data into the database nodes.
    /// </summary>
    class Database
    {
        SqlConnection sqlcon;
        string connectionString;

        public Database(string connectionString)
        {
            this.connectionString = connectionString;
            Open();
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
        /// Intiate bulk copy with database server.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="reader"></param>
        public void BulkCopy(string tableName, IDataReader reader)
        {

            // We don't want or need very good transaction protection...
            using (SqlTransaction sqlTxn = sqlcon.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                
                // Do we need the table lock?  Will try using txns instead....

                using (SqlBulkCopy bulkCopy =
                             new SqlBulkCopy(sqlcon, SqlBulkCopyOptions.Default, sqlTxn))
                {
                    bulkCopy.DestinationTableName = tableName;
                    bulkCopy.BulkCopyTimeout = 0;
                    bulkCopy.NotifyAfter = 2;
                    bulkCopy.BatchSize = 32;
                    bulkCopy.SqlRowsCopied += new SqlRowsCopiedEventHandler(BulkCopyStatus);

                    try
                    {
                        // Write from the source to the destination.
                        bulkCopy.WriteToServer(reader);
                        sqlTxn.Commit();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        sqlTxn.Rollback();
                    }
                    finally
                    {
                        // Close the DataReader The SqlBulkCopy
                        // object is automatically closed at the end
                        // of the using block.
                        reader.Close();
                    }
                }
            }
        }
        private static void BulkCopyStatus(
            object sender, SqlRowsCopiedEventArgs e)
        {
            Console.WriteLine("Copied {0} so far...", e.RowsCopied);
        }

    }
}
