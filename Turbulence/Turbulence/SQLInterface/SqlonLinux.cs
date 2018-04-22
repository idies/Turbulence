using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Turbulence.TurbLib;
using Turbulence.SciLib;
using Turbulence.SQLInterface;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
/* Added for FileDB*/
using System.IO;
/* NOTE: This is the experimental filedb version of ExecuteMHDWorker! */

public partial class StoredProcedures
{
    //static Regex io_regex = new Regex(@"Scan count ([0-9]+), logical reads ([0-9]+), physical reads ([0-9]+), read-ahead reads ([0-9]+)", RegexOptions.Compiled);
    //static int scan_count = 0;
    //static int logical_reads = 0;
    //static int physical_reads = 0;
    // static int read_ahead_reads = 0;

    /// <summary>
    /// A single interface to multiple database functions.
    /// 
    /// This is currently a mess and should be cleaned up, but
    /// at this point we do have the majority of the unique logic
    /// for each of the calculation functions removed.
    /// </summary>
    [Microsoft.SqlServer.Server.SqlProcedure]
    public static void SqlonLinux(
        string serverName,
        string dbname,
        string codedb,
        string turbinfodb,
        string turbinfoserver,
        string dataset,
        int workerType,
        int blobDim,
        float time,
        int spatialInterp,  // TurbulenceOptions.SpatialInterpolation
        int temporalInterp, // TurbulenceOptions.TemporalInterpolation
        float arg,          // Extra argument (not used by all workers)
        int inputSize,
        string tempTable,
        long startz,
        long endz)
    {

        TurbServerInfo serverinfo = TurbServerInfo.GetTurbServerInfo(codedb, turbinfodb, turbinfoserver);
        /*SqlConnection standardConn;
        SqlConnection contextConn;

        string connString;
        if (serverName.Contains("_"))
            connString = String.Format("Data Source={0};Initial Catalog={1};User ID='turbquery';Password='aa2465ways2k';Pooling=false;", serverName.Remove(serverName.IndexOf("_")), serverinfo.codeDB);
        else
            connString = String.Format("Data Source={0};Initial Catalog={1};User ID='turbquery';Password='aa2465ways2k';Pooling=false;", serverName, serverinfo.codeDB);
        standardConn = new SqlConnection(connString);

        contextConn = new SqlConnection("context connection=true");*/

        //TurbDataTable table = TurbDataTable.GetTableInfo(serverName, dbname, dataset, blobDim, serverinfo);

        SqlConnection contextConn = new SqlConnection("context connection=true");
        contextConn.Open();
        contextConn.Close();

        String cString = String.Format("Data Source={0};Initial Catalog={1};User ID='turbquery';Password='aa2465ways2k';Pooling=false;", serverinfo.infoDB_server, serverinfo.infoDB);
        SqlConnection turbinfoConn = new SqlConnection(cString);
        turbinfoConn.Open();
        turbinfoConn.Close();

        /*String cString = String.Format("Data Source={0};Initial Catalog={1};User ID='turbquery';Password='aa2465ways2k';Pooling=false;", serverinfo.infoDB_server, serverinfo.infoDB);
        SqlConnection turbinfoConn = new SqlConnection(cString);
        turbinfoConn.Open();
        turbinfoConn.Close();*/


        //SqlCommand command = turbinfoConn.CreateCommand();
        //command.CommandText = String.Format("SELECT MIN(minLim), MAX(maxLim) FROM {0}.dbo.databasemap " +
        //    "WHERE productiondatabasename = '{1}'", serverinfo.infoDB, dbname);
    }
}
