using System;
using System.Data;
using System.Data.SqlTypes;
using System.IO;
using Microsoft.SqlServer.Server;

namespace Turbulence.TurbLib
{
    public class Functions
    {
        [SqlFunction]
        public static SqlString WriteToFile(SqlBytes binary, SqlString path, SqlBoolean append)
        {
            try
            {
                if (!binary.IsNull && !path.IsNull && !append.IsNull)
                {
                    var dir = Path.GetDirectoryName(path.Value);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    using (var fs = new FileStream(path.Value, append ? FileMode.Append : FileMode.OpenOrCreate))
                    {
                        byte[] byteArr = binary.Value;
                        for (int i = 0; i < byteArr.Length; i++)
                        {
                            fs.WriteByte(byteArr[i]);
                        };
                    }
                    return "SUCCESS";
                }
                else
                    return "NULL INPUT";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}