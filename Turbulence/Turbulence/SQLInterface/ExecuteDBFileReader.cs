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
    public static void ExecuteDBFileReader(
        string serverName,
        string dbname,
        string filePath,
        int BlobByteSize,
        int atomDim,
        string zindexQuery,
        int zlistCount,
        int dbtype)
    {
        SqlDataRecord record = new SqlDataRecord(new SqlMetaData[] { new SqlMetaData("zindex", SqlDbType.BigInt),
                                                                    new SqlMetaData("data", SqlDbType.VarBinary, -1)});
        SqlContext.Pipe.SendResultsStart(record);

        //TurbulenceBlob blob = new TurbulenceBlob(table);
        byte[] rawdata = new byte[BlobByteSize];

        string pathSource = filePath;

        bool read_entire_file = false;
        int left_bracket_pos = zindexQuery.IndexOf('[');
        int right_bracket_pos = zindexQuery.IndexOf(']');
        string box = zindexQuery.Substring(left_bracket_pos + 1, right_bracket_pos - left_bracket_pos - 1);

        byte[] z_rawdata = new byte[BlobByteSize]; ; //Initilize for small cutout, reassign below if we are reading in the entire file.
        FileStream filedb = null;
        try
        {
            
            filedb = new FileStream(pathSource, FileMode.Open, System.IO.FileAccess.Read); /* This is in case we don't read in the entire file */

            List<SQLUtility.zlistTable> zlist = new List<SQLUtility.zlistTable>();
            if (dbtype == 2)
            {
                using (SqlConnection contextConn = new SqlConnection("context connection=true"))
                {
                    contextConn.Open();
                    zlist = SQLUtility.fileDB2zlistTable(dbname, contextConn);
                    contextConn.Close();
                }
            }

            if (zlistCount > 4096 && filedb.Length < (2147483648 - 0))
            {
                z_rawdata = File.ReadAllBytes(pathSource); /*Read it all in at once*/
                read_entire_file = true;
            }

            //while (reader.Read())
            long thisBlob = 0;
            //for (int i = 0; i < zindexToRead.Length; i++)
            foreach (var s in box.Split(','))
            {
                long num;
                if (long.TryParse(s, out num))
                {
                    thisBlob = num;
                    //Reset blob to line up with beginning of file by taking the modulo of the 512 cube zindex  This could be done by the databasemap maybe.
                    //One possibility is to take the thisblob-zmin. 
                    //thisBlob is the spatial blob.  fileBlob is the corresponding blob in relation to the file. 
                    //long fileBlob = thisBlob - startz; /*We need to align the first blob with the start of the file */
                    long offset = 0;
                    if (dbtype == 1)
                    {
                        long fileBlob = thisBlob % 134217728;
                        long z = fileBlob / (atomDim * atomDim * atomDim);
                        offset = z * BlobByteSize;
                    }
                    else if (dbtype == 2)
                    {
                        //offset = SQLUtility.fileDB2offset(dbname, table, thisBlob, standardConn);
                        //start = DateTime.Now;
                        SQLUtility.zlistTable zresult = zlist.Find(x => (x.startZ <= thisBlob && thisBlob <= x.endZ));
                        offset = (thisBlob - zresult.startZ) / (atomDim * atomDim * atomDim);
                        offset = (zresult.blobBefore + offset) * BlobByteSize;
                        //file.WriteLine(string.Format("startZ {0}, endZ {1}, blobBefore {2}, Offset {3}", result.startZ, result.endZ, result.blobBefore, offset));
                        //file.WriteLine(string.Format("Find thisBlob: {0}", DateTime.Now - start));
                    }

                    if (read_entire_file)
                    {
                        //stopWatch.Start();
                        Array.Copy(z_rawdata, offset, rawdata, 0, BlobByteSize);
                        //stopWatch.Stop();
                    }
                    else
                    {
                        //stopWatch1.Start();
                        filedb.Seek(offset, SeekOrigin.Begin);
                        int bytes = filedb.Read(rawdata, 0, BlobByteSize);
                        //stopWatch1.Stop();
                    }

                    record.SetInt64(0, thisBlob);
                    record.SetBytes(1, 0, rawdata, 0, BlobByteSize);
                    SqlContext.Pipe.SendResultsRow(record);
                }
            }

            filedb.Close();
            SqlContext.Pipe.SendResultsEnd();
            rawdata = null;
        }
        catch (FileNotFoundException e)
        {
            long thisBlob = 0;
            //for (int i = 0; i < zindexToRead.Length; i++)
            foreach (var s in box.Split(','))
            {
                long num;
                if (long.TryParse(s, out num))
                {
                    thisBlob = num;
                    record.SetInt64(0, thisBlob);
                    record.SetBytes(1, 0, rawdata, 0, BlobByteSize);
                    SqlContext.Pipe.SendResultsRow(record);
                }
            }
            SqlContext.Pipe.SendResultsEnd();
        }
    }

};
