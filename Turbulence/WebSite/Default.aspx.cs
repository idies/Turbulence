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
        updateStatusMessage();
        updateParticleTrackCount();
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

    private void updateStatusMessage()
    {
        status.Text = "";
        try
        {
            String cString = ConfigurationManager.ConnectionStrings["turbinfo"].ConnectionString;
            SqlConnection conn = new SqlConnection(cString);
            conn.Open();
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT message FROM messages WHERE id = 'status'";
            string result = (string)cmd.ExecuteScalar();
            status.Text = result;
            //Cache.Insert("particles-tracked", count, null, DateTime.Now.AddSeconds(15.0), TimeSpan.Zero);
            conn.Close();
        }
        catch (Exception e)
        {
            status.Text = "<p>" + e.ToString() + "</p>";
        }
    }

    private void updateParticleTrackCount() {
        // No need to cache if we do page-level caching
        //if (Cache.Get("particles-tracked") == null)
        //{
        try
        {
            String cString = ConfigurationManager.ConnectionStrings["turbinfo"].ConnectionString;
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
