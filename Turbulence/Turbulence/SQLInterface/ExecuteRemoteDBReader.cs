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
    public static void ExecuteRemoteDBReader(
        string serverName,
        string filePath,
        int BlobByteSize,
        int atomDim,
        string zindexQuery)
    {
        SqlDataRecord record = new SqlDataRecord(new SqlMetaData[] { new SqlMetaData("zindex", SqlDbType.BigInt),
                                                                    new SqlMetaData("data", SqlDbType.VarBinary, -1)});
        SqlContext.Pipe.SendResultsStart(record);

        //TurbulenceBlob blob = new TurbulenceBlob(table);
        byte[] rawdata = new byte[BlobByteSize];

        string pathSource = filePath;
        FileStream filedb = new FileStream(pathSource, FileMode.Open, System.IO.FileAccess.Read);
        //string[] tester = { "In filedb..."};
        //System.IO.File.WriteAllLines(@"e:\filedb\debug.txt", tester);

        //while (reader.Read())
        int left_bracket_pos = zindexQuery.IndexOf('[');
        int right_bracket_pos = zindexQuery.IndexOf(']');
        string box = zindexQuery.Substring(left_bracket_pos + 1, right_bracket_pos - left_bracket_pos - 1);
        long zindexToRead = 0;
        //for (int i = 0; i < zindexToRead.Length; i++)
        foreach (var s in box.Split(','))
        {
            long num;
            if (long.TryParse(s, out num))
            {
                zindexToRead = num;
                //Reset blob to line up with beginning of file by taking the modulo of the 512 cube zindex  This could be done by the databasemap maybe.
                //One possibility is to take the thisblob-zmin. 
                //thisBlob is the spatial blob.  fileBlob is the corresponding blob in relation to the file. 
                //long fileBlob = thisBlob - startz; /*We need to align the first blob with the start of the file */
                long fileBlob = zindexToRead % 134217728;
                long z = fileBlob / (atomDim * atomDim * atomDim);
                long offset = z * BlobByteSize;
                filedb.Seek(offset, SeekOrigin.Begin);
                //Test
                //string[] lines= { "Offset chosen = ", offset.ToString(), z.ToString(), table.BlobByteSize.ToString(), thisBlob.ToString(),pathSource, table.atomDim.ToString()};
                //System.IO.File.WriteAllLines(@"e:\filedb\debug.txt", lines);

                int bytes = filedb.Read(rawdata, 0, BlobByteSize);

                record.SetInt64(0, zindexToRead);
                record.SetBytes(1, 0, rawdata, 0, BlobByteSize);
                SqlContext.Pipe.SendResultsRow(record);
            }
        }

        filedb.Close();
        SqlContext.Pipe.SendResultsEnd();
        rawdata = null;

    }

};
