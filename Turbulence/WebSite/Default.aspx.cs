using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Web.Caching;
using System.Data.SqlClient;
using Turbulence.TurbLib;
using Turbulence.TurbLib.DataTypes;
using Turbulence.SQLInterface;


public partial class _Default : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        const string infodb_string = TurbulenceService.TurbulenceService.infodb_string;
        const string logdb_string = TurbulenceService.TurbulenceService.logdb_string;

        updateStatusMessage(infodb_string);
        updateParticleTrackCount(logdb_string);
        string hostname = (string)Request.Headers["Host"];
        if (hostname.Equals("test.turbulence.pha.jhu.edu") || hostname.Equals("dev.turbulence.pha.jhu.edu") || hostname.Equals("mhddev.turbulence.pha.jhu.edu"))
        {
            testingserver.Text = "<h1 color=\"red\">" + hostname + "</h1><h2>Testing Server -- May Be Unstable</h2>";
        }
        else
        {
            testingserver.Text = "";
        }
    }

    private void updateStatusMessage(string infodb_string)
    {
        status.Text = "";
        try
        {
            bool conn_mess = false;
            string cString = ConfigurationManager.ConnectionStrings[infodb_string].ConnectionString;
            using (var conn = new SqlConnection(cString))
            {
                try
                {
                    conn.Open();
                    SqlCommand cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT message FROM messages WHERE id = 'status'";
                    string result = (string)cmd.ExecuteScalar();
                    status.Text = result;
                    //Cache.Insert("particles-tracked", count, null, DateTime.Now.AddSeconds(15.0), TimeSpan.Zero);
                    conn.Close();
                    conn_mess = true;
                }
                catch { }
            }
            if (!conn_mess)
            {
                cString = ConfigurationManager.ConnectionStrings["turbinfo_backup_conn"].ConnectionString;
                using (var conn = new SqlConnection(cString))
                {
                    try
                    {
                        conn.Open();
                        SqlCommand cmd = conn.CreateCommand();
                        cmd.CommandText = "SELECT message FROM messages WHERE id = 'status'";
                        string result = (string)cmd.ExecuteScalar();
                        status.Text = result;
                        //Cache.Insert("particles-tracked", count, null, DateTime.Now.AddSeconds(15.0), TimeSpan.Zero);
                        conn.Close();
                        conn_mess = true;
                    }
                    catch { }
                }
            }
        }
        catch (Exception e)
        {
            status.Text = "<p>" + e.ToString() + "</p>";
        }
    }

    private void updateParticleTrackCount(string logdb_string)
{
    // No need to cache if we do page-level caching
    //if (Cache.Get("particles-tracked") == null)
    //{
    try
    {
        String cString = ConfigurationManager.ConnectionStrings[logdb_string].ConnectionString;
        SqlConnection conn = new SqlConnection(cString);
        conn.Open();
        SqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT SUM(records) FROM particlecount";
        long count = (Int64)cmd.ExecuteScalar();
        tracked.Text = count.ToString("0,0");
        //Cache.Insert("particles-tracked", count, null, DateTime.Now.AddSeconds(15.0), TimeSpan.Zero);
        conn.Close();
    }
    catch (Exception e)
    {
        tracked.Text = e.ToString();
    }
    //}
    //tracked.Text = Cache.Get("particles-tracked").ToString();
}
}
