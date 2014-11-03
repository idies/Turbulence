using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Turbulence.TurbLib;
using Turbulence.SQLInterface;
using System.Collections;
using System.Collections.Generic;

public partial class StoredProcedures
{
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void GetPDF(
        short datasetID,
        string serverName,
        string dbname,
        string codedb,
        string cachedb,
        string turbinfodb,
        string tableName,
        int workerType,
        int blobDim,
        int timestep,
        int spatialInterp,  // TurbulenceOptions.SpatialInterpolation
        float arg,          // Extra argument (not used by all workers)
        int binSize,
        int numberOfBins,
        string QueryBox)
    {
        SqlDataRecord record;

        int[] bins = new int[numberOfBins];

        int[] coordinates = new int[6];
        ParseQueryBox(QueryBox, coordinates);

        GetPDFUsingCutout(datasetID, serverName, dbname, codedb, turbinfodb, tableName, workerType, blobDim, timestep, spatialInterp, arg, binSize,
            coordinates,
            bins);

        record = new SqlDataRecord(
            new SqlMetaData[] {
            new SqlMetaData("bin", SqlDbType.Int),
            new SqlMetaData("count", SqlDbType.Int) });
        SqlContext.Pipe.SendResultsStart(record);
        for(int i = 0; i < bins.Length; i++)
        {
            record.SetInt32(0, i);
            record.SetInt32(1, bins[i]);
            SqlContext.Pipe.SendResultsRow(record);
        }
        SqlContext.Pipe.SendResultsEnd();
        return;
    }

    private static void GetPDFUsingCutout(
        short datasetID,
        string serverName,
        string dbname,
        string codedb,
        string turbinfodb,
        string tableName,
        int workerType,
        int blobDim,
        int timestep,
        int spatialInterp,
        float arg,
        int binSize,
        int[] coordinates,
        int[] bins)
    {
        float[] cutout = null;
        try
        {
            SqlConnection contextConn;
            contextConn = new SqlConnection("context connection=true");
            contextConn.Open();

            TurbDataTable table = TurbDataTable.GetTableInfo(serverName, dbname, tableName, blobDim, contextConn);
            string DBtableName = String.Format("{0}.dbo.{1}", dbname, table.TableName);

            //Worker worker = Worker.GetWorker(table, workerType, spatialInterp, arg, contextConn);
            Turbulence.SQLInterface.workers.GetCurl worker = 
                new Turbulence.SQLInterface.workers.GetCurl(table, (TurbulenceOptions.SpatialInterpolation)spatialInterp);
            contextConn.Close();

            int[] cutout_coordinates;
            cutout_coordinates = worker.GetCutoutCoordinates(coordinates);
            int x_width, y_width, z_width;
            x_width = cutout_coordinates[3] - cutout_coordinates[0];
            y_width = cutout_coordinates[4] - cutout_coordinates[1];
            z_width = cutout_coordinates[5] - cutout_coordinates[2];
            //cutout = new byte[table.Components * sizeof(float) * x_width * y_width * z_width];
            ulong cutout_size = (ulong)table.Components * (ulong)x_width * (ulong)y_width * (ulong)z_width;
            if (cutout_size > int.MaxValue / sizeof(float))
            {
                throw new Exception("Cutout size too big! Consider using more threads!");
            }
            else
            {
                cutout = new float[cutout_size];
            }

            BigArray<float> big_cutout = null;
            GetCutoutForWorker(worker, table, datasetID, turbinfodb, timestep, coordinates, cutout_coordinates, false, ref big_cutout, ref cutout);

            worker.GetPDFUsingCutout(cutout, cutout_coordinates, coordinates, bins, binSize);

            cutout = null;
        }
        catch (Exception ex)
        {
            if (cutout != null)
            {
                cutout = null;
            }
            throw ex;
        }
    }
};
