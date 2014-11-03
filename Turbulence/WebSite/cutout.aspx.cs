using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class cutout : System.Web.UI.Page
{   
    //Maximum download size allowed
    //NOTE: Make sure this size matches the one in "download"
    long maxsize = 12L * 256L * 350L * 512L; //525MB

    protected void Page_Load(object sender, EventArgs e)
    {
        update();
    }
    protected void update()
    {
        //Update dt
        timestart_range.Text = "(0-1024)";
        timeend_range.Text = "(1-1025)";
        x_range.Text = "(0-1023)";
        y_range.Text = "(0-1023)";
        z_range.Text = "(0-1023)";
        xend_range.Text = "(1-1024)";
        yend_range.Text = "(1-1024)";
        zend_range.Text = "(1-1024)";
        int min_time = 0, max_time_range = 1025, min_x = 0, max_x_range = 1024,
            min_y = 0, max_y_range = 1024, min_z = 0, max_z_range = 1024;
        if (dataset.SelectedValue.Equals("isotropic1024coarse"))
        {
            dt.Text = "0.002";
            this.magnetic.Visible = false;
            this.potential.Visible = false;
            this.density.Visible = false;
            density.Checked = false;
            potential.Checked = false;
            magnetic.Checked = false;
            channel_grid_note.Visible = false;
        }
        else if (dataset.SelectedValue.Equals("mhd1024"))
        {
            dt.Text = "0.0025";
            this.magnetic.Visible = true;
            this.potential.Visible = true;
            channel_grid_note.Visible = false;
        }
        else if (dataset.SelectedValue.Equals("isotropic1024fine"))
        {
            dt.Text = "0.0002";
            timestart_range.Text = "(0-99)";
            timeend_range.Text = "(1-100)";
            this.magnetic.Visible = false;
            this.potential.Visible = false;
            this.density.Visible = false;
            density.Checked = false;
            potential.Checked = false;
            magnetic.Checked = false;
            channel_grid_note.Visible = false;
            max_time_range = 100;
        }
        else if (dataset.SelectedValue.Equals("channel"))
        {
            dt.Text = "0.0065";
            timestart_range.Text = "(0-1996)";
            timeend_range.Text = "(1-1997)";
            x_range.Text = "(0-2047)";
            y_range.Text = "(0-511)";
            z_range.Text = "(0-1535)";
            xend_range.Text = "(1-2048)";
            yend_range.Text = "(1-512)";
            zend_range.Text = "(1-1536)";
            this.magnetic.Visible = false;
            this.potential.Visible = false;
            this.density.Visible = false;
            density.Checked = false;
            potential.Checked = false;
            magnetic.Checked = false;
            min_time = 0;
            max_time_range = 1997; 
            min_x = 0; 
            max_x_range = 2048;
            min_y = 0;
            max_y_range = 512;
            min_z = 0;
            max_z_range = 1536;
            channel_grid_note.Text = "Note: Simulation was performed in a moving frame and the spatial " + 
                "locations of the data are those of the moving grid. " + 
                "For details see <a href=\"docs/README-CHANNEL.pdf\" target=\"_blank\">README-CHANNEL</a>.";
            channel_grid_note.Visible = true;
        }
        else if (dataset.SelectedValue.Equals("mixing"))
        {
            dt.Text = "0.04";
            timestart_range.Text = "(0-1011)";
            timeend_range.Text = "(1-1012)";
            this.density.Visible = true;
            this.magnetic.Visible = false;
            this.potential.Visible = false;
            channel_grid_note.Visible = false;
            potential.Checked = false;
            magnetic.Checked = false;
            min_time = 0;
            max_time_range = 1015;
        }
        else
        {
            dt.Text = "";
        }

        dlsize.Text = "";
        dllink.Text = "";

        int comps = 0;
        if (velocity.Checked) comps += 3;
        if (pressure.Checked) comps += 1;
        if (magnetic.Checked) comps += 3;
        if (potential.Checked) comps += 3;
        if (density.Checked) comps += 1;

        long xw = long.Parse(xEnd.Text),
            yw = long.Parse(yEnd.Text),
            zw = long.Parse(zEnd.Text),
            tw = long.Parse(timeend.Text);

        long xl = long.Parse(x.Text),
            yl = long.Parse(y.Text),
            zl = long.Parse(z.Text),
            tl = long.Parse(timestart.Text);

        long size = comps * 4 * (xw) * (yw) * (zw) * (tw);

        String fields = String.Format("{0}{1}{2}{3}{4}",
            velocity.Checked ? "u" : "",
            pressure.Checked ? "p" : "",
            magnetic.Checked ? "b" : "",
            potential.Checked ? "a" : "",
            density.Checked ? "d" : "");

        String authToken;

        if (String.Compare(authTokenBox.Text, "") == 0)
        {
            dlsize.Text = "<b><font color=red>Please fill-in your authorization token.</font></b>";
        }
        else if (!(velocity.Checked || pressure.Checked || magnetic.Checked || potential.Checked || density.Checked))
        {
            dlsize.Text = "<b><font color=red>Please check at lease one field</font></b>";
        }
        else if (size == 0)
        {
            dlsize.Text = "<b><font color=red>Please make sure that the width in each dimension is non-zero</font></b>";
        }
        else if (!(xl >= min_x && yl >= min_y && zl >= min_z && tl >= min_time && xw + xl <= max_x_range && yw + yl <= max_y_range && zw + zl <= max_z_range && tw + tl <= max_time_range))
        {
            dlsize.Text = "<b><font color=red>The requested region is out of bounds</font></b>";
        }
        else
        {
            authToken = authTokenBox.Text;
            dlsize.Text = String.Format("Approx download size: ");
            dlsize.Text += FormatSize(size);

            if (size > maxsize)
            {
                dllink.Text = String.Format("<b><font color=red>Maximum file size exceeded. Maximum download size is : {0}</font></b>", FormatSize(maxsize));
            }
            else
            {
                string baseUrl = Request.Url.Scheme + "://" + Request.Url.Authority + Request.ApplicationPath.TrimEnd('/') + "/";

                String dlurl = String.Format(baseUrl + "cutout/download.aspx/{0}/{1}/{2}/{3},{4}/{5},{6}/{7},{8}/{9},{10}/",
                    Server.UrlEncode(authToken), dataset.SelectedValue, fields, timestart.Text, timeend.Text, x.Text, xEnd.Text, y.Text, yEnd.Text, z.Text, zEnd.Text);

                dllink.Text = String.Format("Download link (click to begin download): " + "<a href='{0}' onclick=\"wait_message()\">{0}</a>", dlurl);
            }
        }

    }

    public String FormatSize(long size)
    {
        String Text;
        if (size >= 10L * 1024L * 1024L * 1024L) Text = (size / 1024 / 1024 / 1024).ToString() + "GB";
        else if (size >= 10L * 1024L * 1024L) Text = (size / 1024 / 1024).ToString() + "MB";
        else if (size >= 10L * 1024L) Text = (size / 1024).ToString() + "KB";
        else Text = (size).ToString() + "B";
        return Text;
    }
}