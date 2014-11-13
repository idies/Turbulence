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
    public static void GetThreshold(
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
        double threshold,
        string QueryBox)
    {
        //TimeSpan cacheTime = new TimeSpan(0), IOTime = new TimeSpan(0), computeTime = new TimeSpan(0), resultSendingTime = new TimeSpan(0);
        //DateTime startTime, endTime, initialTimeStamp;

        //initialTimeStamp = startTime = DateTime.Now;

        HashSet<SQLUtility.PartialResult> points_above_threshold = new HashSet<SQLUtility.PartialResult>();
        SqlDataRecord record;

        int[] coordinates = new int[6];
        ParseQueryBox(QueryBox, coordinates);

        SqlConnection standardConn;
        string connString;
        if (serverName.Contains("_"))
            connString = String.Format("Data Source={0};Initial Catalog={1};Trusted_Connection=True;Pooling=false;Connect Timeout = 600;", serverName.Remove(serverName.IndexOf("_")), cachedb);
        else
            connString = String.Format("Data Source={0};Initial Catalog={1};Trusted_Connection=True;Pooling=false;Connect Timeout = 600;", serverName, cachedb);
        standardConn = new SqlConnection(connString);
        standardConn.Open();

        SqlCommand set_xact_abort_on = standardConn.CreateCommand();
        set_xact_abort_on.CommandText = "SET XACT_ABORT ON";
        set_xact_abort_on.ExecuteNonQuery();

        SqlTransaction sqlTxn = standardConn.BeginTransaction(IsolationLevel.Snapshot);
        SqlDataReader reader = null;
        bool update_cache_data = false;
        bool delete_existing = false;
        int cache_info_ordinal = -1;
        long start_index = -1;
        long end_index = -1;
        double stored_threshold = -1.0;
        try
        {
            SqlCommand cmd = standardConn.CreateCommand();
            cmd.Transaction = sqlTxn;
            cmd.CommandText = String.Format("select ordinal, datasetID, serverName, dbName, timestep, worker, spatialOption, start_index, end_index, threshold, date_used, rows " +
                "from {0}..cache_info where DatasetID = @datasetID " +
                "and serverName = @serverName " +
                "and dbName = @dbName " +
                "and timestep  = @timestep " +
                "and worker = @worker " +
                "and spatialOption = @spatialOption", cachedb);
            cmd.CommandTimeout = 600;
            cmd.Parameters.AddWithValue("@datasetID", datasetID);
            cmd.Parameters.AddWithValue("@serverName", serverName);
            cmd.Parameters.AddWithValue("@dbName", dbname);
            cmd.Parameters.AddWithValue("@timestep", timestep);
            cmd.Parameters.AddWithValue("@worker", workerType);
            cmd.Parameters.AddWithValue("@spatialOption", spatialInterp);
            reader = cmd.ExecuteReader();
            int rows = -1;
            if (!reader.HasRows)
            {
                // There is no data for the requested dataset-timestep-function combination.
                update_cache_data = true;
                reader.Close();
                reader.Dispose();
                sqlTxn.Commit();
                sqlTxn.Dispose();
            }
            else
            {
                reader.Read();
                cache_info_ordinal = reader.GetInt32(0);
                start_index = reader.GetInt64(7);
                end_index = reader.GetInt64(8);
                stored_threshold = reader.GetDouble(9);
                rows = reader.GetInt32(11);
                reader.Close();
                reader.Dispose();

                if (stored_threshold <= threshold && CheckCoordinates(start_index, end_index, coordinates))
                {
                    // We can return data from the cache
                    SQLUtility.PartialResult point;

                    cmd.CommandText = String.Format("select zindex, data " +
                        "from {0}..cache_data where cache_info_ordinal = @ordinal ", cachedb);
                    cmd.Transaction = sqlTxn;
                    cmd.Parameters.Clear();
                    cmd.CommandTimeout = 600;
                    cmd.Parameters.AddWithValue("@ordinal", cache_info_ordinal);
                    reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        long zindex = reader.GetInt64(0);
                        double norm = reader.GetDouble(1);
                        if (norm > threshold && PointInRange(zindex, coordinates))
                        {
                            point = new SQLUtility.PartialResult(zindex);
                            point.norm = norm;
                            points_above_threshold.Add(point);
                        }
                    }
                    reader.Close();
                    reader.Dispose();
                    sqlTxn.Commit();
                    sqlTxn.Dispose();
                }
                else
                {
                    // The data stored in the cache are for a higher treshold.
                    // Recompute for the threshold specified and update the cache.
                    update_cache_data = true;
                    delete_existing = true;
                    sqlTxn.Commit();
                    sqlTxn.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            try
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
                sqlTxn.Rollback();
                sqlTxn.Dispose();
            }
            catch (Exception ex2)
            {
                throw new Exception(String.Format("Error in GetThreshold, could not roll back the transaction: {0}! INNER EXCEPTION: {1}", ex2.Message, ex.ToString()));
            }
            standardConn.Close();
            standardConn.Dispose();
            throw new Exception(String.Format("Error in GetThreshold! Server: {0} INNER EXCEPTION: {1}", serverName, ex.ToString()));
        }

        //endTime = DateTime.Now;
        //cacheTime += endTime - startTime;
        //startTime = endTime;

        if (update_cache_data)
        {
            GetThresholdUsingCutout(datasetID, serverName, dbname, codedb, turbinfodb, tableName, workerType, blobDim, timestep, spatialInterp, arg, threshold,
                coordinates,
                out points_above_threshold);
        }

        record = new SqlDataRecord(
            new SqlMetaData[] {
                new SqlMetaData("zindex", SqlDbType.BigInt),
                new SqlMetaData("value", SqlDbType.Float) });
        SqlContext.Pipe.SendResultsStart(record);
        foreach (SQLUtility.PartialResult result in points_above_threshold)
        {
            record.SetInt64(0, result.zindex);
            record.SetSqlDouble(1, result.norm);
            SqlContext.Pipe.SendResultsRow(record);
        }
        SqlContext.Pipe.SendResultsEnd();

        //endTime = DateTime.Now;
        //resultSendingTime = endTime - startTime;
        //record = new SqlDataRecord(GetSqlMetaData());
        //SqlContext.Pipe.SendResultsStart(record);
        //record.SetInt32(0, scan_count);
        //record.SetInt32(1, logical_reads);
        //record.SetInt32(2, physical_reads);
        //record.SetInt32(3, read_ahead_reads);
        //record.SetDouble(4, cacheTime.TotalSeconds);
        //record.SetDouble(5, IOTime.TotalSeconds);
        //record.SetDouble(6, -1);
        //record.SetDouble(7, computeTime.TotalSeconds);
        //record.SetDouble(8, resultSendingTime.TotalSeconds);
        //record.SetDouble(9, (endTime - initialTimeStamp).TotalSeconds);
        //SqlContext.Pipe.SendResultsRow(record);
        //SqlContext.Pipe.SendResultsEnd();

        // Finally, update the cache.
        // Start a new transaction to update the cache_info and cache_data tables.
        sqlTxn = standardConn.BeginTransaction(IsolationLevel.Snapshot);
        try
        {
            start_index = new Morton3D(coordinates[2], coordinates[1], coordinates[0]);
            end_index = new Morton3D(coordinates[5], coordinates[4], coordinates[3]);
            UpdateCacheInfo(serverName, dbname, cachedb, datasetID, ref cache_info_ordinal, timestep, workerType, spatialInterp,
                start_index, end_index, threshold,
                points_above_threshold.Count, delete_existing, update_cache_data, standardConn, sqlTxn);

            if (update_cache_data)
            {
                DataTable cache_data = createCacheDataTable();
                foreach (SQLUtility.PartialResult result in points_above_threshold)
                {
                    DataRow newRow = cache_data.NewRow();
                    newRow["cache_info_ordinal"] = cache_info_ordinal;
                    newRow["zindex"] = result.zindex;
                    newRow["data"] = result.norm;
                    cache_data.Rows.Add(newRow);
                }
                cache_data.EndLoadData();
                UpdateCacheData(cachedb, cache_data, standardConn, sqlTxn);
            }

            sqlTxn.Commit();
            sqlTxn.Dispose();
        }
        catch (Exception ex)
        {
            try
            {
                sqlTxn.Rollback();
                sqlTxn.Dispose();
            }
            catch (Exception ex2)
            {
                throw new Exception(String.Format("Error in GetThreshold, could not roll back the transaction: {0}! INNER EXCEPTION: {1}", ex2.Message, ex.ToString()));
            }
            standardConn.Close();
            standardConn.Dispose();
            throw new Exception(String.Format("Error in GetThreshold! Server: {0} INNER EXCEPTION: {1}", serverName, ex.ToString()));
        }
        standardConn.Close();
        standardConn.Dispose();
    }

    private static void UpdateCacheInfo(string serverName, string dbname, string cachedb, short datasetID, ref int cache_info_ordinal, int timestep, int workerType, int spatialInterp,
        long start_index, long end_index, double threshold, int num_points_above_threshold,
        bool delete_existing, bool update_cache_data,
        SqlConnection standardConn, SqlTransaction sqlTxn)
    {
        SqlCommand cmd = standardConn.CreateCommand();

        if (delete_existing)
        {
            //NOTE: We do not have to delete from the cache_data table, because of the cascade on the foreigh key constraint
            cmd.CommandText = String.Format("delete from " +
                "{0}..cache_info where DatasetID = @datasetID " +
                "and serverName = @serverName " +
                "and dbName = @dbName " +
                "and timestep = @timestep " +
                "and worker = @worker " +
                "and spatialOption = @spatialOption", cachedb);
            cmd.Transaction = sqlTxn;
            cmd.CommandTimeout = 600;
            cmd.Parameters.AddWithValue("@datasetID", datasetID);
            cmd.Parameters.AddWithValue("@serverName", serverName);
            cmd.Parameters.AddWithValue("@dbName", dbname);
            cmd.Parameters.AddWithValue("@timestep", timestep);
            cmd.Parameters.AddWithValue("@worker", workerType);
            cmd.Parameters.AddWithValue("@spatialOption", spatialInterp);
            cmd.ExecuteNonQuery();
        }

        if (update_cache_data)
        {
            cmd.CommandText = String.Format("INSERT INTO {0}.[dbo].[cache_info] VALUES (@datasetID, @serverName, @dbName, @timestep, @workerType, @spatialOption, " +
                "@start_index, @end_index, @threshold, @date_used, @rows) " +
                "SELECT CONVERT(int, SCOPE_IDENTITY())", cachedb);
            cmd.Transaction = sqlTxn;
            cmd.Parameters.Clear();
            cmd.CommandTimeout = 600;
            cmd.Parameters.AddWithValue("@datasetID", datasetID);
            cmd.Parameters.AddWithValue("@serverName", serverName);
            cmd.Parameters.AddWithValue("@dbName", dbname);
            cmd.Parameters.AddWithValue("@timestep", timestep);
            cmd.Parameters.AddWithValue("@workerType", workerType);
            cmd.Parameters.AddWithValue("@spatialOption", spatialInterp);
            cmd.Parameters.AddWithValue("@start_index", start_index);
            cmd.Parameters.AddWithValue("@end_index", end_index);
            cmd.Parameters.AddWithValue("@threshold", threshold);
            cmd.Parameters.AddWithValue("@date_used", DateTime.Now);
            cmd.Parameters.AddWithValue("@rows", num_points_above_threshold);
            object ordinal = cmd.ExecuteScalar();
            cache_info_ordinal = System.Convert.ToInt32(ordinal);
        }
        else
        {
            cmd.CommandText = String.Format("UPDATE {0}.[dbo].[cache_info] SET date_used = @date_used WHERE ordinal = @ordinal", cachedb);
            cmd.Transaction = sqlTxn;
            cmd.Parameters.Clear();
            cmd.CommandTimeout = 600;
            cmd.Parameters.AddWithValue("@date_used", DateTime.Now);
            cmd.Parameters.AddWithValue("@ordinal", cache_info_ordinal);
            cmd.ExecuteNonQuery();
        }
    }

    private static void UpdateCacheData(string cachedb, DataTable cache_data, SqlConnection standardConn, SqlTransaction sqlTxn)
    {
        // First, check if there is enough space in the cache and if not free up space
        // by removing the least recently used data.

        // Determine the maximum size of the database (in KB).
        SqlCommand cmd = standardConn.CreateCommand();
        cmd.Transaction = sqlTxn;
        cmd.CommandText = String.Format("SELECT sum(max_size) " +
            "FROM sys.master_files " +
            "WHERE DB_NAME(database_id) = '{0}' and type = 0", cachedb);
        cmd.CommandTimeout = 600;
        object result = cmd.ExecuteScalar();
        int max_size = System.Convert.ToInt32(result);

        // Estimate the size of a row in the cache_data table.
        // The procedure returns the data size in KB.
        cmd.CommandText = String.Format("USE {0} EXEC sp_spaceused 'cache_data'", cachedb);
        cmd.Transaction = sqlTxn;
        cmd.CommandTimeout = 600;
        SqlDataReader reader = cmd.ExecuteReader();
        float row_size = 0;
        int reserved = 0;
        int new_rows = cache_data.Rows.Count;

        try
        {
            while (reader.Read())
            {
                int rows = System.Int32.Parse(reader.GetString(1));
                string reserved_string = reader.GetString(2);
                int space_pos = reserved_string.IndexOf(" ");
                int.TryParse(reserved_string.Substring(0, space_pos), out reserved);
                string data_size_string = reader.GetString(3);
                space_pos = data_size_string.IndexOf(" ");
                int data_size;
                int.TryParse(data_size_string.Substring(0, space_pos), out data_size);
                if (rows > 0)
                {
                    row_size = (float)data_size / rows;
                }
            }
        }
        catch (Exception ex)
        {
            reader.Close();
            reader.Dispose();
            throw new Exception(String.Format("Error estimating the cached data size! INNER EXCEPTION: {0}", ex.ToString()));
        }
        reader.Close();
        reader.Dispose();

        // Check if the currently reservered space + the new rows exceeds the maximum size.
        // If it does we go through and remove the least recently used data until we have 
        // freed up enough space for the new rows to be added.
        if (reserved + new_rows * row_size > max_size && max_size > 0)
        {
            float total_rows_to_delete = (reserved - (max_size - new_rows * row_size)) / row_size;
            int rows_to_delete = 0;

            cmd.CommandText = String.Format("SELECT ordinal, rows FROM {0}..cache_info ORDER BY date_used", cachedb);
            cmd.Transaction = sqlTxn;
            cmd.CommandTimeout = 600;
            reader = cmd.ExecuteReader();
            List<int> records_to_delete = new List<int>();
            try
            {
                while (reader.Read())
                {
                    records_to_delete.Add(reader.GetInt32(0));
                    rows_to_delete += reader.GetInt32(1);

                    if (rows_to_delete >= total_rows_to_delete)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                reader.Close();
                reader.Dispose();
                throw new Exception(String.Format("Error creating space in the cache! INNER EXCEPTION: {0}", ex.ToString()));
            }
            reader.Close();
            reader.Dispose();

            string queryString = String.Format("DELETE FROM {0}..cache_info WHERE ordinal IN (", cachedb);
            bool first_ordinal = true;
            foreach (long ordinal in records_to_delete)
            {
                if (!first_ordinal)
                {
                    queryString += ", ";
                }
                queryString += ordinal.ToString();
                first_ordinal = false;
            }
            queryString += ")";
            cmd.CommandText = queryString;
            cmd.Transaction = sqlTxn;
            cmd.CommandTimeout = 600;
            cmd.ExecuteNonQuery();
        }

        // Finally, we load the data into the cache.
        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(standardConn, SqlBulkCopyOptions.Default, sqlTxn))
        {
            bulkCopy.DestinationTableName = String.Format("{0}.[dbo].[cache_data]", cachedb);
            bulkCopy.WriteToServer(cache_data);
        }
    }

    /// <summary>
    /// Checks whether the given coordiantes are within the given [start_index, end_index] region
    /// </summary>
    /// <param name="start_index"></param> zindex of the bottom let corner of the region
    /// <param name="end_index"></param> zidnex of the top right corner of the region
    /// <param name="coordiantes"></param> coordiantes in the form [x_s, y_s, z_s, x_e, y_e, z_e],
    /// where (x_s, y_s, z_s) is the bottom left and (x_e, y_e, z_e) is the top right
    /// <returns></returns>
    private static bool CheckCoordinates(long start_index, long end_index, int[] coordiantes)
    {
        Morton3D start = new Morton3D(start_index);
        Morton3D end = new Morton3D(end_index);
        if (coordiantes[0] >= start.X && coordiantes[0] < end.X &&
            coordiantes[1] >= start.Y && coordiantes[1] < end.Y &&
            coordiantes[2] >= start.Z && coordiantes[2] < end.Z &&
            coordiantes[3] > start.X && coordiantes[3] <= end.X &&
            coordiantes[4] > start.Y && coordiantes[4] <= end.Y &&
            coordiantes[5] > start.Z && coordiantes[5] <= end.Z)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the given point (zindex) is within the specified boundaries (coordiantes).
    /// The coordiantes are assumed to be given as bottom left corner and top right corner,
    /// where the top right corner is the last possition + 1 in each dimension. The point's
    /// coordinates should therefore be strictly less than the top right corner.
    /// </summary>
    /// <param name="zindex"></param>
    /// <param name="coordiantes"></param>
    /// <returns></returns>
    public static bool PointInRange(long zindex, int[] coordiantes)
    {
        Morton3D point = new Morton3D(zindex);
        if (coordiantes[0] <= point.X && point.X < coordiantes[3] &&
            coordiantes[1] <= point.Y && point.Y < coordiantes[4] &&
            coordiantes[2] <= point.Z && point.Z < coordiantes[5])
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private static DataTable createCacheDataTable()
    {
        DataTable dt = new DataTable("CacheData");

        DataColumn cache_info_ordinal = new DataColumn("cache_info_ordinal");
        cache_info_ordinal.DataType = typeof(int);
        dt.Columns.Add(cache_info_ordinal);

        DataColumn zindex = new DataColumn("zindex");
        zindex.DataType = typeof(long);
        dt.Columns.Add(zindex);

        DataColumn data = new DataColumn("data");
        data.DataType = typeof(double);
        dt.Columns.Add(data);

        dt.BeginLoadData();

        return dt;
    }

    private static void GetThresholdUsingCutout(
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
        double threshold,
        int[] coordinates,
        out HashSet<SQLUtility.PartialResult> points_above_threshold)
    {
        try
        {
            SqlConnection contextConn;
            contextConn = new SqlConnection("context connection=true");
            contextConn.Open();

            TurbDataTable table = TurbDataTable.GetTableInfo(serverName, dbname, tableName, blobDim, contextConn);
            string DBtableName = String.Format("{0}.dbo.{1}", dbname, table.TableName);

            Worker worker = Worker.GetWorker(table, workerType, spatialInterp, arg, contextConn);
            contextConn.Close();

            worker.GetData(datasetID, turbinfodb, timestep, coordinates);
            
            //endTime = DateTime.Now;
            //IOTime = endTime - startTime;
            //startTime = endTime;

            points_above_threshold = worker.GetThresholdUsingCutout(coordinates, threshold);

            //endTime = DateTime.Now;
            //computeTime = endTime - startTime;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

};
