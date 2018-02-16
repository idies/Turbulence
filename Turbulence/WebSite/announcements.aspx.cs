
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


public partial class announcements : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
<<<<<<< HEAD
    protected global::System.Web.UI.WebControls.Literal announcement;
    protected void Page_Load(object sender, EventArgs e)
        {
            updateAnnouncements();
        }
=======
        const string infodb_string = TurbulenceService.TurbulenceService.infodb_string;
        updateAnnouncements(infodb_string);
    }
>>>>>>> FileDBmerg2

    private void updateAnnouncements(string infodb_string)
    {
        announcement.Text = "";
        try
        {
            String cString = ConfigurationManager.ConnectionStrings[infodb_string].ConnectionString;
            SqlConnection conn = new SqlConnection(cString);
            conn.Open();
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT '<li><b>' + CAST(created_at AS VARCHAR(11)) +'</b><br />' + announcement + '</li>' as arow FROM announcements order by created_at desc";
            //string result = (string)cmd.ExecuteScalar();
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                announcement.Text = announcement.Text + reader["arow"];
            }


            conn.Close();
        }
        catch (Exception e)
        {
            announcement.Text = ConfigurationManager.ConnectionStrings[infodb_string].ConnectionString + "<p>" + e.ToString() + "</p>";
        }
    }
}
