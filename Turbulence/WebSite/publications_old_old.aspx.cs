
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

public partial class publications : System.Web.UI.Page
{
    protected global::System.Web.UI.WebControls.Literal publication;
    protected global::System.Web.UI.WebControls.Literal samplepublication;

    protected void Page_Load(object sender, EventArgs e)
    {
        showPublications();
        showSamplePublications();
    }

    private void showPublications()
    {
        publication.Text = "";
        try
        {
            String cString = ConfigurationManager.ConnectionStrings["turbinfo"].ConnectionString;
            SqlConnection conn = new SqlConnection(cString);
            conn.Open();
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT '<li>' + [authors] + ' <a href=\"' + link + '\" target=\"_blank\">' + title + '</a>' + ' ' + journalpub + '</li>' as arow  FROM[publications] as arow  where pubtype = 1  order by pubdate desc";
            //string result = (string)cmd.ExecuteScalar();
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                publication.Text = publication.Text + reader["arow"];
            }
            conn.Close();
        }
        catch (Exception e)
        {
            publication.Text = ConfigurationManager.ConnectionStrings["turbinfo"].ConnectionString + "<p>" + e.ToString() + "</p>";
        }
    }
    private void showSamplePublications()
    {
        samplepublication.Text = "";
        try
        {
            String cString = ConfigurationManager.ConnectionStrings["turbinfo"].ConnectionString;
            SqlConnection conn = new SqlConnection(cString);
            conn.Open();
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT '<li>' + [authors] + ' <a href=\"' + link + '\" target=\"_blank\">' + title + '</a>' + ' ' + journalpub + '</li>' as arow  FROM[publications]  where pubtype = 2  order by pubdate desc";
            //string result = (string)cmd.ExecuteScalar();
            SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                samplepublication.Text = samplepublication.Text + reader["arow"];
            }
            conn.Close();
        }
        catch (Exception e)
        {
            publication.Text = ConfigurationManager.ConnectionStrings["turbinfo"].ConnectionString + "<p>" + e.ToString() + "</p>";
        }
    }
}
