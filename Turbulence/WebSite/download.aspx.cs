using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using System.Runtime.InteropServices;

public partial class download : System.Web.UI.Page
{
    protected unsafe void Page_Load(object sender, EventArgs e)
    {
        //DateTime start = DateTime.Now;

        //The URL the user entered up to the .aspx
        String myPath = Request.FilePath.ToString();

        //The URL the user entered including everything after the .aspx
        String fullPath = Request.Url.AbsolutePath.ToString();

        if (myPath.Equals(fullPath))
        {
            Response.Redirect("cutout.aspx");
            Response.End();
        }

        //Treat everything after the URL of the page as a parameter
        String args_ = fullPath.Substring(fullPath.IndexOf(myPath) + myPath.Length);

        //Test validity of parameters given
        if (!System.Text.RegularExpressions.Regex.IsMatch(args_,
            "^/[a-zA-Z0-9_]+([-.][a-zA-Z0-9_]+)*/(isotropic1024fine|isotropic1024coarse|mhd1024|channel)/[upba]{1,4}/\\d+,\\d+/\\d+,\\d+/\\d+,\\d+/\\d+,\\d+/?$"))
        {
            Response.StatusCode = 400;
            Response.Write("Error: Bad request. URL should be in the format of /authToken/dataset/fields/time,timesteps/xlow,xwidth/ylow,ywidth/zlow,zwidth");
            Response.End();
        }
        else
        {
            // Redirect to turbulence.pha.jhu.edu/cutout/download.aspx/...
            String new_path = fullPath;
            new_path = System.Text.RegularExpressions.Regex.Replace(new_path, "download.aspx", "cutout/download.aspx");
            Response.Redirect(new_path);
            Response.End();
        }
    }
}