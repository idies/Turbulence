﻿using System;
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
        string turbinfoserver,
        string outputdb,
        short datasetID,
        string field,
        int blobDim,
        int timestep,
        int filter_width,
        int x_stride,
        int y_stride,
        int z_stride,
        int components,
        string QueryBox)
    {
        byte[] result = null;
        float[] cutout = null;
        int atomSize = 8; /*Is this number correct?  Re-used from importdata*/
        try
        {
            SqlDataRecord record = new SqlDataRecord(new SqlMetaData("data", SqlDbType.VarBinary, -1));
            SqlConnection contextConn;
            contextConn = new SqlConnection("context connection=true");
            //contextConn.Open();
            /* Connection to output database */
            SqlConnection standardConn;

            string cString;
            cString = String.Format("Data Source={0};Initial Catalog={1};User ID='turbquery';Password='aa2465ways2k';Pooling=false;", serverName, outputdb);
            standardConn = new SqlConnection(cString);

            TurbServerInfo serverinfo = TurbServerInfo.GetTurbServerInfo(codedb, turbinfodb, turbinfoserver);
            TurbDataTable table = TurbDataTable.GetTableInfo(serverName, dbname, field, blobDim, serverinfo);
            string DBtableName = String.Format("{0}.dbo.{1}", dbname, table.TableName);

            //Worker worker = Worker.GetWorker(table, (int)Worker.Workers.GetMHDBoxFilterSV, 0, 0.0f, contextConn);

            int[] coordinates = new int[6];
            ParseQueryBox(QueryBox, coordinates);
            int[] resolution = { coordinates[3] / x_stride, coordinates[4] / y_stride, coordinates[5] / z_stride };
            GetMHDBoxFilter worker = new GetMHDBoxFilter(table, filter_width);
            worker.GetData(datasetID, serverinfo, timestep, coordinates, table.dbtype);
            cutout = worker.GetResult(coordinates, x_stride, y_stride, z_stride);

            // Populate the record
            int cutout_byte_length = Buffer.ByteLength(cutout);
            result = new byte[cutout_byte_length];


            Buffer.BlockCopy(cutout, 0, result, 0, cutout_byte_length);
            record.SetBytes(0, 0, result, 0, cutout.Length);
            /*Loop through to create a record for each chunk of data */

            // Send the record to the client.
            //SqlContext.Pipe.Send(record);
            //Send the record to the database
            /*We aren't going to use partitions, because it is all going into the same table for now.*/
            int partitions = 1;
            int p = 0;
            long inc = new Morton3D(0, 0, atomSize).Key;
            long[] firstBoxes = new long[partitions];
            long[] lastBoxes = new long[partitions];
            firstBoxes[p] = new Morton3D(coordinates[0], coordinates[1], coordinates[2]);
            /*Adjust query box based on filter */
            lastBoxes[p] = new Morton3D(coordinates[3] / x_stride - atomSize, coordinates[4] / y_stride - atomSize, coordinates[5] / z_stride - atomSize);

            CutoutDataReader.ExportDataFormat df = new CutoutDataReader.ExportDataFormat(
                    firstBoxes[p],
                    lastBoxes[p],
                    inc, atomSize);
            /*Not sure we need timeoff, which is a timestep offset.  I'm only planning on doing 1 timestep at a time. */
            CutoutDataReader cutoutDataReader = (CutoutDataReader)CutoutDataReader.GetReader(timestep, 0, resolution, components, df, result);

            standardConn.Open();
            SqlBulkCopy bulkCopy = new SqlBulkCopy(standardConn, SqlBulkCopyOptions.Default, null);
            bulkCopy.DestinationTableName = "vel";
            bulkCopy.BulkCopyTimeout = 0;
            bulkCopy.BatchSize = 1000;
            bulkCopy.WriteToServer(cutoutDataReader);
            standardConn.Close();
            //contextConn.Close();
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
