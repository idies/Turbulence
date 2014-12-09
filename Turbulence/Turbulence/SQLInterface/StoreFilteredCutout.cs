using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Turbulence.TurbLib;
using Turbulence.SQLInterface;
using Turbulence.SQLInterface.workers;
using System.Collections.Generic;

    public partial class StoredProcedures
    {
        [Microsoft.SqlServer.Server.SqlProcedure]
        public static void StoreFilteredCutout(
            string serverName,
            string dbname,
            string codedb,
            string turbinfodb,
            short datasetID,
            string field,
            int blobDim,
            int timestep,
            int filter_width,
            int x_stride,
            int y_stride,
            int z_stride,
            string QueryBox)
        {
            byte[] result = null;
            float[] cutout = null;
            try
            {
                SqlDataRecord record = new SqlDataRecord(new SqlMetaData("data", SqlDbType.VarBinary, -1));
                SqlConnection contextConn;
                contextConn = new SqlConnection("context connection=true");
                contextConn.Open();
                /* Connection to output database */
                SqlConnection standardConn;
                string cString;
                cString = String.Format("Data Source={0};Initial Catalog={1};Trusted_Connection=True;Pooling=false;", serverName, codedb);
                standardConn = new SqlConnection(cString);
                TurbDataTable table = TurbDataTable.GetTableInfo(serverName, dbname, field, blobDim, contextConn);
                string DBtableName = String.Format("{0}.dbo.{1}", dbname, table.TableName);

                //Worker worker = Worker.GetWorker(table, (int)Worker.Workers.GetMHDBoxFilterSV, 0, 0.0f, contextConn);
                GetMHDBoxFilterSV worker = new GetMHDBoxFilterSV(table, filter_width);
                contextConn.Close();

                int[] coordinates = new int[6];
                ParseQueryBox(QueryBox, coordinates);

                worker.GetData(datasetID, turbinfodb, timestep, coordinates);

                cutout = worker.GetResult(coordinates, x_stride, y_stride, z_stride);

                // Populate the record
                int cutout_byte_length = Buffer.ByteLength(cutout);
                result = new byte[cutout_byte_length];
                Buffer.BlockCopy(cutout, 0, result, 0, cutout_byte_length);
                record.SetBytes(0, 0, result, 0, cutout.Length);
                // Send the record to the client.
                //SqlContext.Pipe.Send(record);
                //Send the record to the database

                SqlBulkCopy bulkCopy = new SqlBulkCopy(standardConn, SqlBulkCopyOptions.Default, null);
                bulkCopy.DestinationTableName = "vel";
                bulkCopy.BulkCopyTimeout = 0;
                bulkCopy.BatchSize = 1000;
                /* bulkCopy.WriteToServer(cutout); */

                result = null;
                cutout = null;
            }
            catch (Exception ex)
            {
                if (result != null)
                {
                    result = null;
                }
                if (cutout != null)
                {
                    cutout = null;
                }
                throw new Exception(String.Format("Error generating filtered cutout.  [Inner Exception: {0}])",
                    ex.ToString()));
            }
        }

};
