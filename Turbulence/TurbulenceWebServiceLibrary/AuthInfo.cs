using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Data.SqlClient;
using Turbulence.TurbLib;
using Turbulence.TurbLib.DataTypes;
using Turbulence.SQLInterface;


namespace TurbulenceService
{

    public class AuthInfo
    {
        bool devmode;
        string authdb;
        string authdb_server;
        /// <summary>
        /// Structure representating a row of the user table
        /// </summary>
        public class AuthToken
        {
            public string name;
            int id;
            int limit;

            public int Id { get { return id; } }
            public int Limit { get { return limit; } }

            public AuthToken(string name, int id, int limit)
            {
                this.name = name;
                this.id = id;
                this.limit = limit;
            }
        }

        public AuthInfo(string authdb, string authdb_server, bool devmode)
        {
            this.authdb = authdb;
            this.authdb_server = authdb_server;
            this.devmode = devmode;
        }

        public AuthToken VerifyToken(string tokenString, int reqsize)
        {
            AuthToken token = LoadUserFromDatabase(tokenString);
            if ((token.Limit == 0) || (token.Limit > 0) && (token.Limit >= reqsize))
            {
                return token;
            }
            else if (token.Limit < 0)
            {
                throw new Exception("Disabled identification token.  Please see http://turbulence.pha.jhu.edu/help/authtoken.aspx for more information.");
            }
            else
            {
                throw new Exception(String.Format("Request size too large ({0} > {1})", reqsize, token.Limit));
            }
        }

        protected AuthToken LoadUserFromDatabase(string tokenString)
        {

            if (devmode)
            {
                return new AuthToken("dev", -1, 0);
            }

            AuthToken token = null;
            //String cString = ConfigurationManager.ConnectionStrings[infodb].ConnectionString;
            String cString = String.Format("Server={0};Database={1};Asynchronous Processing=true;User ID={2};Password={3};Pooling=true;Max Pool Size=250;Min Pool Size=20;Connection Lifetime=7200;",
                            authdb_server, authdb, ConfigurationManager.AppSettings["turbinfo_uid"], ConfigurationManager.AppSettings["turbinfo_password"]);
            SqlConnection conn = new SqlConnection(cString);
            conn.Open();
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT uid, limit FROM users WHERE authkey = @authKey";
            cmd.Parameters.AddWithValue("@authKey", tokenString);
            SqlDataReader reader = cmd.ExecuteReader();
            if (reader.HasRows)
            {
                reader.Read();
                int uid = reader.GetSqlInt32(0).Value;
                int limit = reader.GetSqlInt32(1).Value;
                token = new AuthToken(tokenString, uid, limit);
            }
            else
            {
                throw new Exception("Invalid identification token. Please see http://turbulence.pha.jhu.edu/help/authtoken.aspx for more information.");
            }
            reader.Close();
            conn.Close();
            return token;
        }

    }
}
