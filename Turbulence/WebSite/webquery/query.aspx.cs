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
using System.IO;
using System.Collections.Generic;
namespace Website
{
    public partial class webquery_query : System.Web.UI.Page
    {
        // 16M max 
        private const int MAX_FILE_SIZE_BYTES = 1 * 1024 * 1024;

        edu.jhu.pha.turbulence.TurbulenceService service;

        public enum OutputType { CSV, Tab, HTML, Binary };
        
        private String previous_selected_method
        {
            get
            {
                object o = ViewState["previous_selected_method"];
                return (o == null) ? String.Empty : (string)o;
            }

            set
            {
                ViewState["previous_selected_method"] = value;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            service = new edu.jhu.pha.turbulence.TurbulenceService();
            service.Timeout = 3600000;
            spatial_ranges_update();
            dataset_update();
            spatial_flags_update();
            table_rows_update();
            error.Text = "";

            if (Session["filename"] == null)
            {
                fileinfo.Text = "[ No file currently uploaded. ]";
            }
        }


        protected void table_rows_update()
        {
            EndTimeRow.Visible = false;
            DeltaTRow.Visible = false;
            spatialRow.Visible = false;
            temporalRow.Visible = false;
            fieldRow.Visible = false;
            filterWidthRow.Visible = false;
            filterSpacingRow.Visible = false;
            vorticityEntry.Enabled = false;
            QEntry.Enabled = false;
            fieldList2.Visible = false;
            thresholdRow.Visible = false;
            QueryText.InnerText = "Query a single point";
            if (method.SelectedValue.Equals("GetPosition"))
            {
                EndTimeRow.Visible = true;
                DeltaTRow.Visible = true;
                spatialRow.Visible = true;
                if (dataset.SelectedValue.Equals("isotropic1024coarse"))
                {
                    timerange.Text = "0.0 - 2.048";
                    EndTimeRange.Text = "0.0 - 2.048";
                    DeltaTRange.Text = "DB dt = .002";
                }
                else if (dataset.SelectedValue.Equals("isotropic1024fine"))
                {
                    timerange.Text = "0.0 - 0.0198";
                    EndTimeRange.Text = "0.0 - 0.0198";
                    DeltaTRange.Text = "DB dt = .0002";
                }
                else if (dataset.SelectedValue.Equals("mhd1024"))
                {
                    timerange.Text = "0.0 - 2.56";
                    EndTimeRange.Text = "0.0 - 2.56";
                    DeltaTRange.Text = "DB dt = .0025";
                }
                else if (dataset.SelectedValue.Equals("mixing"))
                {
                    timerange.Text = "0.0 - 40.44";
                    EndTimeRange.Text = "0.0 - 40.44";
                    DeltaTRange.Text = "DB dt = .04";
                }
                else if (dataset.SelectedValue.Equals("channel"))
                {
                    timerange.Text = "0.0 - 25.9935";
                    EndTimeRange.Text = "0.0 - 25.9935";
                    DeltaTRange.Text = "DB dt = .0065";
                }
                else
                {
                    timerange.Text = "";
                    EndTimeRange.Text = "";
                    DeltaTRange.Text = "";
                }
            }
            else if (method.SelectedValue.Contains("Filter"))
            {
                fieldRow.Visible = true;
                filterWidthRow.Visible = true;
                if (method.SelectedValue.Equals("GetBoxFilterSGSsymtensor"))
                {
                    velocityEntry.Enabled = true;
                    pressureEntry.Enabled = false;
                    densityEntry.Enabled = false;
                }
                else if (method.SelectedValue.Equals("GetBoxFilterSGSscalar"))
                {
                    fieldList2.Visible = true;
                    pressureEntry.Enabled = true;
                    pressureEntry2.Enabled = true;
                    if (dataset.SelectedValue.Equals("mixing"))
                    {
                        densityEntry.Enabled = true;
                        densityEntry2.Enabled = true;
                    }
                    velocityEntry.Enabled = false;
                    magneticEntry.Enabled = false;
                    potentialEntry.Enabled = false;
                    velocityEntry2.Enabled = false;
                    magneticEntry2.Enabled = false;
                    potentialEntry2.Enabled = false;
                }
                else if (method.SelectedValue.Equals("GetBoxFilterSGSvector"))
                {
                    fieldList2.Visible = true;
                    pressureEntry.Enabled = false;
                    pressureEntry2.Enabled = true;
                    if (dataset.SelectedValue.Equals("mixing"))
                    {
                        densityEntry.Enabled = false;
                        densityEntry2.Enabled = true;
                    }
                    velocityEntry.Enabled = true;
                    velocityEntry2.Enabled = false;
                    if (dataset.SelectedValue.Equals("mhd1024"))
                    {
                        magneticEntry.Enabled = true;
                        potentialEntry.Enabled = true;
                        magneticEntry2.Enabled = false;
                        potentialEntry2.Enabled = false;
                    }
                }
                else if (method.SelectedValue.Equals("GetBoxFilterSGStensor"))
                {
                    fieldList2.Visible = true;
                    pressureEntry.Enabled = false;
                    pressureEntry2.Enabled = false;
                    densityEntry.Enabled = false;
                    densityEntry2.Enabled = false;

                    velocityEntry.Enabled = true;
                    velocityEntry2.Enabled = true;
                    if (dataset.SelectedValue.Equals("mhd1024"))
                    {
                        magneticEntry.Enabled = true;
                        potentialEntry.Enabled = true;
                        magneticEntry2.Enabled = true;
                        potentialEntry2.Enabled = true;
                    }
                }
                else
                {
                    pressureEntry.Enabled = true;
                    if (dataset.SelectedValue.Equals("mixing"))
                        densityEntry.Enabled = true;
                    if (dataset.SelectedValue.Equals("mhd1024"))
                    {
                        magneticEntry.Enabled = true;
                        potentialEntry.Enabled = true;
                    }
                }
                if (method.SelectedValue.Equals("GetBoxFilterGradient"))
                    filterSpacingRow.Visible = true;
                else
                    filterSpacingRow.Visible = false;
            }
            else if (method.SelectedValue.Equals("GetThreshold"))
            {
                xwidth_range.Visible = true;
                ywidth_range.Visible = true;
                zwidth_range.Visible = true;
                Xwidth.Visible = true;
                Ywidth.Visible = true;
                Zwidth.Visible = true;
                spatialRow.Visible = true;
                fieldRow.Visible = true;
                thresholdRow.Visible = true;
                vorticityEntry.Enabled = true;
                QEntry.Enabled = true;
                QueryText.InnerText = "Query a region";
                if (dataset.SelectedValue.Equals("isotropic1024coarse"))
                {
                    timerange.Text = "0.0 - 2.048";
                }
                else if (dataset.SelectedValue.Equals("isotropic1024fine"))
                {
                    timerange.Text = "0.0 - 0.0198";
                }
                else if (dataset.SelectedValue.Equals("mhd1024"))
                {
                    timerange.Text = "0.0 - 2.56";
                }
                else if (dataset.SelectedValue.Equals("mixing"))
                {
                    timerange.Text = "0.0 - 40.44";
                }
                else if (dataset.SelectedValue.Equals("channel"))
                {
                    timerange.Text = "0.0 - 25.9935";
                }
            }
            else
            {
                spatialRow.Visible = true;
                temporalRow.Visible = true;
            }
        }

        protected void spatial_ranges_update()
        {
            if (dataset.SelectedValue.Equals("isotropic1024coarse") ||
                dataset.SelectedValue.Equals("isotropic1024fine") ||
                dataset.SelectedValue.Equals("mhd1024") ||
                dataset.SelectedValue.Equals("mixing"))
            {
                if (method.SelectedValue.Equals("GetThreshold"))
                {
                    x_range.Text = "Starting index: <br />i_x [0, 1023]";
                    y_range.Text = "<br />i_y [0, 1023]";
                    z_range.Text = "<br />i_z [0, 1023]";
                    xwidth_range.Text = "Size: <br />N_x [1, 1024]";
                    ywidth_range.Text = "<br />N_y [1, 1024]";
                    zwidth_range.Text = "<br />N_z [1, 1024]";
                    coord_range_details.Text = "<br />";
                }
                else
                {
                    x_range.Text = "x [0, 2&pi;]";
                    y_range.Text = "y [0, 2&pi;]";
                    z_range.Text = "z [0, 2&pi;]";
                    xwidth_range.Visible = false;
                    ywidth_range.Visible = false;
                    zwidth_range.Visible = false;
                    Xwidth.Visible = false;
                    Ywidth.Visible = false;
                    Zwidth.Visible = false;
                    coord_range_details.Text = "Values outside [0,2&pi;] are treated as mod(2&pi;).";
                }
            }
            else if (dataset.SelectedValue.Equals("channel"))
            {
                if (method.SelectedValue.Equals("GetThreshold"))
                {
                    x_range.Text = "Starting index: <br />i_x [0, 2047]";
                    y_range.Text = "<br />i_y [0, 511]";
                    z_range.Text = "<br />i_z [0, 1535]";
                    xwidth_range.Text = "Size: <br />N_x [1, 2048]";
                    ywidth_range.Text = "<br />N_y [1, 512]";
                    zwidth_range.Text = "<br />N_z [1, 1536]";
                    coord_range_details.Text = "<br />";
                }
                else
                {
                    x_range.Text = "x [0, 8&pi;]";
                    y_range.Text = "y [-1, 1]";
                    z_range.Text = "z [0, 3&pi;]";
                    xwidth_range.Visible = false;
                    ywidth_range.Visible = false;
                    zwidth_range.Visible = false;
                    Xwidth.Visible = false;
                    Ywidth.Visible = false;
                    Zwidth.Visible = false;
                    coord_range_details.Text = "Values outside the range are treated as mod(8&pi;), mod(3&pi;) for x and z.<br/> The values for y must be within [-1, 1].";
                }
            }
        }

        protected void dataset_update()
        {
            this.velocityEntry.Enabled = true;
            this.velocityEntry2.Enabled = true;
            if (dataset.SelectedValue.Equals("isotropic1024coarse"))
            {
                timerange.Text = "0.0 - 2.048<br/>dt = .002";
                this.GetMagneticField.Enabled = false;
                this.GetMagneticFieldGradient.Enabled = false;
                this.GetMagneticFieldHessian.Enabled = false;
                this.GetMagneticFieldLaplacian.Enabled = false;
                this.GetVectorPotential.Enabled = false;
                this.GetVectorPotentialGradient.Enabled = false;
                this.GetVectorPotentialHessian.Enabled = false;
                this.GetVectorPotentialLaplacian.Enabled = false;
                this.magneticEntry.Enabled = false;
                this.potentialEntry.Enabled = false;
                this.magneticEntry2.Enabled = false;
                this.potentialEntry2.Enabled = false;
                this.GetForce.Enabled = true;
                this.GetPosition.Enabled = true;
                this.GetBoxFilter.Enabled = true;
                this.GetBoxFilterSGSscalar.Enabled = true;
                this.GetBoxFilterSGSvector.Enabled = true;
                this.GetBoxFilterSGSsymtensor.Enabled = true;
                this.GetBoxFilterSGStensor.Enabled = true;
                this.GetBoxFilterGradient.Enabled = true;
                this.densityEntry.Enabled = false;
                this.densityEntry2.Enabled = false;
                this.GetDensity.Enabled = false;
                this.GetDensityGradient.Enabled = false;
                this.GetDensityHessian.Enabled = false;
                if (method.SelectedValue.Contains("Magnetic") || method.SelectedValue.Contains("Vector"))
                {
                    method.SelectedValue = "GetVelocity";
                }
                if (fieldList.SelectedValue.Contains("Density"))
                {
                    fieldList.SelectedValue = "Velocity";
                }
                if (fieldList2.SelectedValue.Contains("Density"))
                {
                    fieldList2.SelectedValue = "Velocity";
                }
            }
            else if (dataset.SelectedValue.Equals("mhd1024"))
            {
                timerange.Text = "0.0 - 2.56<br/>dt = .0025";
                this.GetMagneticField.Enabled = true;
                this.GetMagneticFieldGradient.Enabled = true;
                this.GetMagneticFieldHessian.Enabled = true;
                this.GetMagneticFieldLaplacian.Enabled = true;
                this.GetVectorPotential.Enabled = true;
                this.GetVectorPotentialGradient.Enabled = true;
                this.GetVectorPotentialHessian.Enabled = true;
                this.GetVectorPotentialLaplacian.Enabled = true;
                this.magneticEntry.Enabled = true;
                this.potentialEntry.Enabled = true;
                this.magneticEntry2.Enabled = true;
                this.potentialEntry2.Enabled = true;
                this.GetForce.Enabled = true;
                this.GetPosition.Enabled = true;
                this.GetBoxFilter.Enabled = true;
                this.GetBoxFilterSGSscalar.Enabled = true;
                this.GetBoxFilterSGSvector.Enabled = true;
                this.GetBoxFilterSGSsymtensor.Enabled = true;
                this.GetBoxFilterSGStensor.Enabled = true;
                this.GetBoxFilterGradient.Enabled = true;
                this.densityEntry.Enabled = false;
                this.densityEntry2.Enabled = false;
                this.GetDensity.Enabled = false;
                this.GetDensityGradient.Enabled = false;
                this.GetDensityHessian.Enabled = false;
                if (fieldList.SelectedValue.Contains("Density"))
                {
                    fieldList.SelectedValue = "Velocity";
                }
                if (fieldList2.SelectedValue.Contains("Density"))
                {
                    fieldList2.SelectedValue = "Velocity";
                }
            }
            else if (dataset.SelectedValue.Equals("isotropic1024fine"))
            {
                timerange.Text = "0.0 - 0.0198<br/>dt = .0002";
                this.GetMagneticField.Enabled = false;
                this.GetMagneticFieldGradient.Enabled = false;
                this.GetMagneticFieldHessian.Enabled = false;
                this.GetMagneticFieldLaplacian.Enabled = false;
                this.GetVectorPotential.Enabled = false;
                this.GetVectorPotentialGradient.Enabled = false;
                this.GetVectorPotentialHessian.Enabled = false;
                this.GetVectorPotentialLaplacian.Enabled = false;
                this.magneticEntry.Enabled = false;
                this.potentialEntry.Enabled = false;
                this.magneticEntry2.Enabled = false;
                this.potentialEntry2.Enabled = false;
                this.GetForce.Enabled = true;
                this.GetPosition.Enabled = true;
                this.GetBoxFilter.Enabled = true;
                this.GetBoxFilterSGSscalar.Enabled = true;
                this.GetBoxFilterSGSvector.Enabled = true;
                this.GetBoxFilterSGSsymtensor.Enabled = true;
                this.GetBoxFilterSGStensor.Enabled = true;
                this.GetBoxFilterGradient.Enabled = true;
                this.densityEntry.Enabled = false;
                this.densityEntry2.Enabled = false;
                this.GetDensity.Enabled = false;
                this.GetDensityGradient.Enabled = false;
                this.GetDensityHessian.Enabled = false;
                if (method.SelectedValue.Contains("Magnetic") || method.SelectedValue.Contains("Vector"))
                {
                    method.SelectedValue = "GetVelocity";
                }
                if (fieldList.SelectedValue.Contains("Density"))
                {
                    fieldList.SelectedValue = "Velocity";
                }
                if (fieldList2.SelectedValue.Contains("Density"))
                {
                    fieldList2.SelectedValue = "Velocity";
                }
            }
            else if (dataset.SelectedValue.Equals("channel"))
            {
                timerange.Text = "0.0 - 25.9935<br/>dt = .0065";
                this.GetMagneticField.Enabled = false;
                this.GetMagneticFieldGradient.Enabled = false;
                this.GetMagneticFieldHessian.Enabled = false;
                this.GetMagneticFieldLaplacian.Enabled = false;
                this.GetVectorPotential.Enabled = false;
                this.GetVectorPotentialGradient.Enabled = false;
                this.GetVectorPotentialHessian.Enabled = false;
                this.GetVectorPotentialLaplacian.Enabled = false;
                this.magneticEntry.Enabled = false;
                this.potentialEntry.Enabled = false;
                this.magneticEntry2.Enabled = false;
                this.potentialEntry2.Enabled = false;
                this.GetForce.Enabled = false;
                //this.GetPosition.Enabled = false; Map to getchannelposition
                this.GetPosition.Enabled = true;
                this.GetBoxFilter.Enabled = false;
                this.GetBoxFilterSGSscalar.Enabled = false;
                this.GetBoxFilterSGSvector.Enabled = false;
                this.GetBoxFilterSGSsymtensor.Enabled = false;
                this.GetBoxFilterSGStensor.Enabled = false;
                this.GetBoxFilterGradient.Enabled = false;
                this.densityEntry.Enabled = false;
                this.densityEntry2.Enabled = false;
                this.GetDensity.Enabled = false;
                this.GetDensityGradient.Enabled = false;
                this.GetDensityHessian.Enabled = false;
                if (method.SelectedValue.Contains("Filter") ||
                    method.SelectedValue.Contains("Magnetic") || method.SelectedValue.Contains("Vector"))
                {
                    method.SelectedValue = "GetVelocity";
                }
                if (fieldList.SelectedValue.Contains("Density"))
                {
                    fieldList.SelectedValue = "Velocity";
                }
                if (fieldList2.SelectedValue.Contains("Density"))
                {
                    fieldList2.SelectedValue = "Velocity";
                }
            }
            else if (dataset.SelectedValue.Equals("mixing"))
            {
                timerange.Text = "0.0 - 40.44<br/>dt = .04";
                this.GetMagneticField.Enabled = false;
                this.GetMagneticFieldGradient.Enabled = false;
                this.GetMagneticFieldHessian.Enabled = false;
                this.GetMagneticFieldLaplacian.Enabled = false;
                this.GetVectorPotential.Enabled = false;
                this.GetVectorPotentialGradient.Enabled = false;
                this.GetVectorPotentialHessian.Enabled = false;
                this.GetVectorPotentialLaplacian.Enabled = false;
                this.magneticEntry.Enabled = false;
                this.potentialEntry.Enabled = false;
                this.magneticEntry2.Enabled = false;
                this.potentialEntry2.Enabled = false;
                this.GetForce.Enabled = true;
                this.GetPosition.Enabled = true;
                this.GetBoxFilter.Enabled = true;
                this.GetBoxFilterSGSscalar.Enabled = true;
                this.GetBoxFilterSGSvector.Enabled = true;
                this.GetBoxFilterSGSsymtensor.Enabled = true;
                this.GetBoxFilterSGStensor.Enabled = true;
                this.GetBoxFilterGradient.Enabled = true;
                this.GetBoxFilterGradient.Enabled = true;
                this.densityEntry.Enabled = true;
                this.densityEntry2.Enabled = true;
                this.GetDensity.Enabled = true;
                this.GetDensityGradient.Enabled = true;
                this.GetDensityHessian.Enabled = true;
                if (method.SelectedValue.Contains("Magnetic") || method.SelectedValue.Contains("Vector"))
                {
                    method.SelectedValue = "GetVelocity";
                }
            }
            else
            {
                timerange.Text = "";
            }
        }

        protected void spatial_flags_update()
        {
            string[] flags;
            switch (method.Text)
            {
                case "GetVelocity":
                case "GetPressure":
                case "GetMagneticField":
                case "GetVectorPotential":
                case "GetVelocityAndPressure":
                case "GetDensity":
                    flags = new string[] { "None", "Lag4", "Lag6", "Lag8", "M1Q4", "M2Q8", "M2Q14" };
                    break;
                case "GetPosition":
                    flags = new string[] { "None", "Lag4", "Lag6", "Lag8" };
                    break;
                case "GetPressureGradient":
                case "GetVelocityGradient":
                case "GetMagneticFieldGradient":
                case "GetVectorPotentialGradient":
                case "GetDensityGradient":
                    flags = new string[] { "FD4NoInt", "FD6NoInt", "FD8NoInt", "FD4Lag4", "M1Q4", "M2Q8", "M2Q14" };
                    break;
                case "GetPressureHessian":
                case "GetVelocityHessian":
                case "GetMagneticFieldHessian":
                case "GetVectorPotentialHessian":
                case "GetDensityHessian":
                    flags = new string[] { "FD4NoInt", "FD6NoInt", "FD8NoInt", "FD4Lag4", "M2Q8", "M2Q14" };
                    break;
                case "GetVelocityLaplacian":
                case "GetMagneticFieldLaplacian":
                case "GetVectorPotentialLaplacian":
                    flags = new string[] { "FD4NoInt", "FD6NoInt", "FD8NoInt", "FD4Lag4" };
                    break;
                case "GetThreshold":
                    flags = getThresholdFlags();
                    break;
                default:
                    flags = new string[] { "None" };
                    break;
            }
            setSpatialFlags(flags);
        }

        protected void setSpatialFlags(string[] flags)
        {
            string selected = spatial.Text;
            spatial.Items.Clear();
            foreach (string option in flags)
            {
                ListItem item = new ListItem(option, option);
                if (option.Equals(selected))
                    item.Selected = true;
                spatial.Items.Add(item);
            }
        }

        protected void point_Click(object sender, EventArgs e)
        {
            edu.jhu.pha.turbulence.Point3[] points = new edu.jhu.pha.turbulence.Point3[1];
            points[0] = new edu.jhu.pha.turbulence.Point3();
            points[0].x = Convert.ToSingle(x.Text);
            points[0].y = Convert.ToSingle(y.Text);
            points[0].z = Convert.ToSingle(z.Text);
            doWork(points, OutputType.HTML);
        }

        protected void reset_Click(object sender, EventArgs e)
        {
            Session["points"] = null;
            Session["filename"] = null;
            output.Text = "";
        }

        protected void upload_Click(object sender, EventArgs e)
        {
            try
            {
                edu.jhu.pha.turbulence.Point3[] points;
                List<edu.jhu.pha.turbulence.Point3> pointlist = new List<edu.jhu.pha.turbulence.Point3>();

                if (fileup1.HasFile)
                {
                    if (fileup1.PostedFile.ContentLength > MAX_FILE_SIZE_BYTES)
                    {
                        throw new Exception(String.Format("File is too large ({0} bytes), maximum file size allowed is {1} bytes.", fileup1.PostedFile.ContentLength, MAX_FILE_SIZE_BYTES));
                    }
                    StreamReader reader = new StreamReader(fileup1.PostedFile.InputStream);

                    if (inputmethod.Text.Equals("bulktxt"))
                    {

                        string delimStr = " ,|&;\t";
                        char[] delims = delimStr.ToCharArray();
                        // Read one point (for now)

                        while (reader.Peek() >= 0)
                        {
                            string line = reader.ReadLine();

                            string[] fields = line.Split(delims);
                            edu.jhu.pha.turbulence.Point3 point = new edu.jhu.pha.turbulence.Point3();
                            point.x = Convert.ToSingle(fields[0]);
                            point.y = Convert.ToSingle(fields[1]);
                            point.z = Convert.ToSingle(fields[2]);

                            pointlist.Add(point);
                        }

                        points = new edu.jhu.pha.turbulence.Point3[pointlist.Count];
                        int count = 0;
                        foreach (edu.jhu.pha.turbulence.Point3 point in pointlist)
                        {
                            points[count] = point;
                            count++;
                        }
                        Session["points"] = points;
                        Session["filename"] = fileup1.PostedFile.FileName;
                        Session["filesize"] = fileup1.PostedFile.ContentLength;


                        fileinfo.Text = String.Format("[ Current file uploaded: {0} ({1}k) ]", (string)Session["filename"], (int)Session["filesize"] / 1024);
                    }
                }
                else
                {
                    throw new Exception("No file specified!");
                }
            }
            catch (Exception exc)
            {
                output.Text = "";
                error.Text = "<hr/><h2 style=\".red\">Error!</h2><pre>" + exc.ToString() + "</pre>";
            }

        }

        protected void bulkwork_Click(object sender, EventArgs e)
        {
            try
            {
                if (fileup1.HasFile)
                {
                    upload_Click(sender, e);
                }

                edu.jhu.pha.turbulence.Point3[] points;
                if (Session["points"] != null)
                {
                    points = (edu.jhu.pha.turbulence.Point3[])Session["points"];
                    if (outputformat.Text.Equals("tabtxt"))
                    {
                        doWork(points, OutputType.Tab);
                    }
                    else if (outputformat.Text.Equals("csvtxt"))
                    {
                        doWork(points, OutputType.CSV);
                    }
                    else if (outputformat.Text.Equals("web"))
                    {
                        doWork(points, OutputType.HTML);
                    }
                    else
                    {
                        throw new Exception("Unsupported output file format.");
                    }
                }
                else
                {
                    throw new Exception("No file uploaded and/or no points stored.");
                }
            }
            catch (Exception exc)
            {
                output.Text = "";
                error.Text = "<hr/><h2 style=\".red\">Error!</h2><pre>" + exc.ToString() + "</pre>";
            }

        }

        protected void setupFileOutput(string fileName)
        {
            //string fileName = "download.csv";
            Response.Clear();
            Response.ContentType = "text/plain";
            Response.AppendHeader("Content-Disposition", "attachment;filename=" + fileName);
            //this.EnableViewState = false;


        }


        protected void doWork(edu.jhu.pha.turbulence.Point3[] points, OutputType otype)
        {
            try
            {
                bool showheader = includeHeader.Checked;
                string delim = "";
                if (otype == OutputType.Tab)
                {
                    delim = "\t";
                }
                else if (otype == OutputType.CSV)
                {
                    delim = ",";
                }

                string outputText = "";

                float timef = Convert.ToSingle(time.Text);

                edu.jhu.pha.turbulence.SpatialInterpolation spatialv;
                edu.jhu.pha.turbulence.TemporalInterpolation temporalv;
                const string authToken = "edu.jhu.pha.turbulence.web-qhtpsaoe";

                if (spatial.Text.Equals("Lag6"))
                {
                    spatialv = edu.jhu.pha.turbulence.SpatialInterpolation.Lag6;
                }
                else if (spatial.Text.Equals("Lag8"))
                {
                    spatialv = edu.jhu.pha.turbulence.SpatialInterpolation.Lag8;
                }
                else if (spatial.Text.Equals("Lag4"))
                {
                    spatialv = edu.jhu.pha.turbulence.SpatialInterpolation.Lag4;
                }
                else if (spatial.Text.Equals("FD4Lag4"))
                {
                    spatialv = edu.jhu.pha.turbulence.SpatialInterpolation.Fd4Lag4;
                }
                else if (spatial.Text.Equals("FD4NoInt"))
                {
                    spatialv = edu.jhu.pha.turbulence.SpatialInterpolation.None_Fd4;
                }
                else if (spatial.Text.Equals("FD6NoInt"))
                {
                    spatialv = edu.jhu.pha.turbulence.SpatialInterpolation.None_Fd6;
                }
                else if (spatial.Text.Equals("FD8NoInt"))
                {
                    spatialv = edu.jhu.pha.turbulence.SpatialInterpolation.None_Fd8;
                }
                else if (spatial.Text.Equals("M1Q4"))
                {
                    spatialv = edu.jhu.pha.turbulence.SpatialInterpolation.M1Q4;
                }
                else if (spatial.Text.Equals("M2Q8"))
                {
                    spatialv = edu.jhu.pha.turbulence.SpatialInterpolation.M2Q8;
                }
                else if (spatial.Text.Equals("M2Q14"))
                {
                    spatialv = edu.jhu.pha.turbulence.SpatialInterpolation.M2Q14;
                }
                else
                {
                    spatialv = edu.jhu.pha.turbulence.SpatialInterpolation.None;
                }

                if (temporal.Text.Equals("PCHIP"))
                {
                    temporalv = edu.jhu.pha.turbulence.TemporalInterpolation.PCHIP;
                }
                else
                {
                    temporalv = edu.jhu.pha.turbulence.TemporalInterpolation.None;
                }

                if (method.Text.Equals("GetVelocityAndPressure"))
                {
                    edu.jhu.pha.turbulence.Vector3P[] results;
                    results = service.GetVelocityAndPressure(authToken,
                        dataset.Text, timef, spatialv, temporalv, points);
                    if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                    {
                        outputText += String.Format("ux{0}uy{0}uz{0}p\r\n", delim);
                    }
                    foreach (edu.jhu.pha.turbulence.Vector3P result in results)
                    {
                        if (otype == OutputType.HTML)
                        {
                            outputText += String.Format("ux={0},uy={1},uz={2},p={3}<br />\n",
                                result.x, result.y, result.z, result.p);
                        }
                        else if (otype == OutputType.Tab || otype == OutputType.CSV)
                        {
                            outputText += String.Format("{1}{0}{2}{0}{3}{0}{4}\r\n",
                                delim, result.x, result.y, result.z, result.p);
                        }
                    }
                }
                else if (method.Text.Equals("GetPressure"))
                {
                    edu.jhu.pha.turbulence.Pressure[] results;
                    results = service.GetPressure(authToken,
                        dataset.Text, timef, spatialv, temporalv, points);
                    if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                    {
                        outputText += String.Format("p\r\n");
                    }
                    foreach (edu.jhu.pha.turbulence.Pressure result in results)
                    {
                        if (otype == OutputType.HTML)
                        {
                            outputText += String.Format("p={0}<br />\n", result.p);
                        }
                        else if (otype == OutputType.Tab || otype == OutputType.CSV)
                        {
                            outputText += String.Format("{0}\r\n", result.p);
                        }
                    }
                }
                else if (method.Text.Equals("GetVelocity"))
                {
                    edu.jhu.pha.turbulence.Vector3[] results;
                    results = service.GetVelocity(authToken,
                        dataset.Text, timef, spatialv, temporalv, points);
                    if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                    {
                        outputText += String.Format("ux{0}uy{0}uz\r\n", delim);
                    }
                    foreach (edu.jhu.pha.turbulence.Vector3 result in results)
                    {
                        if (otype == OutputType.HTML)
                        {
                            outputText += String.Format("ux={0},uy={1},uz={2}<br />\n",
                                result.x, result.y, result.z);
                        }
                        else if (otype == OutputType.Tab || otype == OutputType.CSV)
                        {
                            outputText += String.Format("{1}{0}{2}{0}{3}\r\n",
                                delim, result.x, result.y, result.z);
                        }
                    }
                }
                else if (method.Text.Equals("GetMagneticField"))
                {
                    edu.jhu.pha.turbulence.Vector3[] results;
                    results = service.GetMagneticField(authToken,
                        dataset.Text, timef, spatialv, temporalv, points);
                    if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                    {
                        outputText += String.Format("bx{0}by{0}bz\r\n", delim);
                    }
                    foreach (edu.jhu.pha.turbulence.Vector3 result in results)
                    {
                        if (otype == OutputType.HTML)
                        {
                            outputText += String.Format("bx={0},by={1},bz={2}<br />\n",
                                result.x, result.y, result.z);
                        }
                        else if (otype == OutputType.Tab || otype == OutputType.CSV)
                        {
                            outputText += String.Format("{1}{0}{2}{0}{3}\r\n",
                                delim, result.x, result.y, result.z);
                        }
                    }
                }
                else if (method.Text.Equals("GetVectorPotential"))
                {
                    edu.jhu.pha.turbulence.Vector3[] results;
                    results = service.GetVectorPotential(authToken,
                        dataset.Text, timef, spatialv, temporalv, points);
                    if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                    {
                        outputText += String.Format("ax{0}ay{0}az\r\n", delim);
                    }
                    foreach (edu.jhu.pha.turbulence.Vector3 result in results)
                    {
                        if (otype == OutputType.HTML)
                        {
                            outputText += String.Format("ax={0},ay={1},az={2}<br />\n",
                                result.x, result.y, result.z);
                        }
                        else if (otype == OutputType.Tab || otype == OutputType.CSV)
                        {
                            outputText += String.Format("{1}{0}{2}{0}{3}\r\n",
                                delim, result.x, result.y, result.z);
                        }
                    }
                }
                else if (method.Text.Equals("GetDensity"))
                {
                    edu.jhu.pha.turbulence.Pressure[] results;
                    results = service.GetDensity(authToken,
                        dataset.Text, timef, spatialv, temporalv, points);
                    if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                    {
                        outputText += String.Format("rho\r\n");
                    }
                    foreach (edu.jhu.pha.turbulence.Pressure result in results)
                    {
                        if (otype == OutputType.HTML)
                        {
                            outputText += String.Format("&rho;={0}<br />\n", result.p);
                        }
                        else if (otype == OutputType.Tab || otype == OutputType.CSV)
                        {
                            outputText += String.Format("{0}\r\n", result.p);
                        }
                    }
                }
                else if (method.Text.Equals("GetForce"))
                {
                    edu.jhu.pha.turbulence.Vector3[] results;
                    results = service.GetForce(authToken,
                        dataset.Text, timef, spatialv, temporalv, points);
                    if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                    {
                        outputText += String.Format("fx{0}fy{0}fz\r\n", delim);
                    }
                    foreach (edu.jhu.pha.turbulence.Vector3 result in results)
                    {
                        if (otype == OutputType.HTML)
                        {
                            outputText += String.Format("fx={0},fy={1},fz={2}<br />\n",
                                result.x, result.y, result.z);
                        }
                        else if (otype == OutputType.Tab || otype == OutputType.CSV)
                        {
                            outputText += String.Format("{1}{0}{2}{0}{3}\r\n",
                                delim, result.x, result.y, result.z);
                        }
                    }
                }
                else if (method.Text.Equals("NullOp"))
                {
                    edu.jhu.pha.turbulence.Vector3[] results;
                    results = service.NullOp(authToken, points);
                    if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                    {
                        outputText += String.Format("x{0}y{0}z\r\n", delim);
                    }
                    foreach (edu.jhu.pha.turbulence.Vector3 result in results)
                    {
                        if (otype == OutputType.HTML)
                        {
                            outputText += String.Format("x={0},y={1},z={2}<br />\n",
                                result.x, result.y, result.z);
                        }
                        else if (otype == OutputType.Tab || otype == OutputType.CSV)
                        {
                            outputText += String.Format("{1}{0}{2}{0}{3}\r\n",
                                delim, result.x, result.y, result.z);
                        }
                    }
                }
                else if (method.Text.Equals("GetPressureHessian"))
                {
                    edu.jhu.pha.turbulence.PressureHessian[] results;
                    results = service.GetPressureHessian(authToken,
                        dataset.Text, timef, spatialv, temporalv, points);
                    if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                    {
                        outputText += String.Format("d2pdxdx{0}d2pdxdy{0}d2pdxdz{0}d2pdydy{0}d2pdydz{0}d2pdzdz\r\n", delim);
                    }
                    foreach (edu.jhu.pha.turbulence.PressureHessian result in results)
                    {
                        if (otype == OutputType.HTML)
                        {
                            outputText += String.Format("<p>d2pdxdx={0},d2pdxdy={1},<br/>d2pdxdz={2},d2pdydy={3},<br/>d2pdydz={4},d2pdzdz={5}</p>\n",
                                result.d2pdxdx, result.d2pdxdy, result.d2pdxdz, result.d2pdydy, result.d2pdydz, result.d2pdzdz);
                        }
                        else if (otype == OutputType.Tab || otype == OutputType.CSV)
                        {
                            outputText += String.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}\r\n",
                               delim,
                               result.d2pdxdx, result.d2pdxdy, result.d2pdxdz, result.d2pdydy, result.d2pdydz, result.d2pdzdz);
                        }
                    }
                }
                else if (method.Text.Equals("GetDensityHessian"))
                {
                    edu.jhu.pha.turbulence.PressureHessian[] results;
                    results = service.GetDensityHessian(authToken,
                        dataset.Text, timef, spatialv, temporalv, points);
                    if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                    {
                        outputText += String.Format("d2rhodxdx{0}d2rhodxdy{0}d2rhodxdz{0}d2rhodydy{0}d2rhodydz{0}d2rhodzdz\r\n", delim);
                    }
                    foreach (edu.jhu.pha.turbulence.PressureHessian result in results)
                    {
                        if (otype == OutputType.HTML)
                        {
                            outputText += String.Format("<p>d2&rho;dxdx={0},d2&rho;dxdy={1},<br/>d2&rho;dxdz={2},d2&rho;dydy={3},<br/>d2&rho;dydz={4},d2&rho;dzdz={5}</p>\n",
                                result.d2pdxdx, result.d2pdxdy, result.d2pdxdz, result.d2pdydy, result.d2pdydz, result.d2pdzdz);
                        }
                        else if (otype == OutputType.Tab || otype == OutputType.CSV)
                        {
                            outputText += String.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}\r\n",
                               delim,
                               result.d2pdxdx, result.d2pdxdy, result.d2pdxdz, result.d2pdydy, result.d2pdydz, result.d2pdzdz);
                        }
                    }
                }
                else if (method.Text.Equals("GetVelocityGradient"))
                {
                    edu.jhu.pha.turbulence.VelocityGradient[] results;
                    results = service.GetVelocityGradient(authToken,
                        dataset.Text, timef, spatialv, temporalv, points);
                    if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                    {
                        outputText += String.Format("duxdx{0}duxdy{0}duxdz{0}duydx{0}duydy{0}duydz{0}duzdx{0}duzdy{0}duzdz\r\n", delim);
                    }

                    foreach (edu.jhu.pha.turbulence.VelocityGradient result in results)
                    {
                        if (otype == OutputType.HTML)
                        {
                            outputText += String.Format("<p>duxdx={0},duxdy={1},duxdz={2},<br/>duydx={3},duydy={4},duydz={5},<br/>duzdx={6},duzdy={7},duzdz={8}</p>\n",
                                result.duxdx, result.duxdy, result.duxdz,
                                result.duydx, result.duydy, result.duydz,
                                result.duzdx, result.duzdy, result.duzdz);
                        }
                        else if (otype == OutputType.Tab || otype == OutputType.CSV)
                        {
                            outputText += String.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}\r\n",
                               delim,
                               result.duxdx, result.duxdy, result.duxdz,
                               result.duydx, result.duydy, result.duydz,
                               result.duzdx, result.duzdy, result.duzdz);
                        }

                    }
                }
                else if (method.Text.Equals("GetMagneticFieldGradient"))
                {
                    edu.jhu.pha.turbulence.VelocityGradient[] results;
                    results = service.GetMagneticFieldGradient(authToken,
                        dataset.Text, timef, spatialv, temporalv, points);
                    if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                    {
                        outputText += String.Format("dbxdx{0}dbxdy{0}dbxdz{0}dbydx{0}dbydy{0}dbydz{0}dbzdx{0}dbzdy{0}dbzdz\r\n", delim);
                    }

                    foreach (edu.jhu.pha.turbulence.VelocityGradient result in results)
                    {
                        if (otype == OutputType.HTML)
                        {
                            outputText += String.Format("<p>dbxdx={0},dbxdy={1},dbxdz={2},<br/>dbydx={3},dbydy={4},dbydz={5},<br/>dbzdx={6},dbzdy={7},dbzdz={8}</p>\n",
                                result.duxdx, result.duxdy, result.duxdz,
                                result.duydx, result.duydy, result.duydz,
                                result.duzdx, result.duzdy, result.duzdz);
                        }
                        else if (otype == OutputType.Tab || otype == OutputType.CSV)
                        {
                            outputText += String.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}\r\n",
                               delim,
                               result.duxdx, result.duxdy, result.duxdz,
                               result.duydx, result.duydy, result.duydz,
                               result.duzdx, result.duzdy, result.duzdz);
                        }

                    }
                }
                else if (method.Text.Equals("GetVectorPotentialGradient"))
                {
                    edu.jhu.pha.turbulence.VelocityGradient[] results;
                    results = service.GetVectorPotentialGradient(authToken,
                        dataset.Text, timef, spatialv, temporalv, points);
                    if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                    {
                        outputText += String.Format("daxdx{0}daxdy{0}daxdz{0}daydx{0}daydy{0}daydz{0}dazdx{0}dazdy{0}dazdz\r\n", delim);
                    }

                    foreach (edu.jhu.pha.turbulence.VelocityGradient result in results)
                    {
                        if (otype == OutputType.HTML)
                        {
                            outputText += String.Format("<p>daxdx={0},daxdy={1},daxdz={2},<br/>daydx={3},daydy={4},daydz={5},<br/>dazdx={6},dazdy={7},dazdz={8}</p>\n",
                                result.duxdx, result.duxdy, result.duxdz,
                                result.duydx, result.duydy, result.duydz,
                                result.duzdx, result.duzdy, result.duzdz);
                        }
                        else if (otype == OutputType.Tab || otype == OutputType.CSV)
                        {
                            outputText += String.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}\r\n",
                               delim,
                               result.duxdx, result.duxdy, result.duxdz,
                               result.duydx, result.duydy, result.duydz,
                               result.duzdx, result.duzdy, result.duzdz);
                        }

                    }
                }
                else if (method.Text.Equals("GetPressureGradient"))
                {
                    edu.jhu.pha.turbulence.Vector3[] results;
                    results = service.GetPressureGradient(authToken,
                        dataset.Text, timef, spatialv, temporalv, points);
                    if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                    {
                        outputText += String.Format("dpdx{0}dpdy{0}dpdz\r\n", delim);
                    }

                    foreach (edu.jhu.pha.turbulence.Vector3 result in results)
                    {
                        if (otype == OutputType.HTML)
                        {
                            outputText += String.Format("dpdx={0},dpdy={1},dpdz={2}<br/>\n",
                                result.x, result.y, result.z);
                        }
                        else if (otype == OutputType.Tab || otype == OutputType.CSV)
                        {
                            outputText += String.Format("{1}{0}{2}{0}{3}\r\n",
                                delim, result.x, result.y, result.z);
                        }
                    }
                }
                else if (method.Text.Equals("GetDensityGradient"))
                {
                    edu.jhu.pha.turbulence.Vector3[] results;
                    results = service.GetDensityGradient(authToken,
                        dataset.Text, timef, spatialv, temporalv, points);
                    if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                    {
                        outputText += String.Format("drhodx{0}drhody{0}drhodz\r\n", delim);
                    }

                    foreach (edu.jhu.pha.turbulence.Vector3 result in results)
                    {
                        if (otype == OutputType.HTML)
                        {
                            outputText += String.Format("d&rho;dx={0},d&rho;dy={1},d&rho;dz={2}<br/>\n",
                                result.x, result.y, result.z);
                        }
                        else if (otype == OutputType.Tab || otype == OutputType.CSV)
                        {
                            outputText += String.Format("{1}{0}{2}{0}{3}\r\n",
                                delim, result.x, result.y, result.z);
                        }
                    }
                }
                else if (method.Text.Equals("GetVelocityHessian"))
                {
                    edu.jhu.pha.turbulence.VelocityHessian[] results;
                    results = service.GetVelocityHessian(authToken, dataset.Text, timef, spatialv, temporalv, points);
                    if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                    {
                        outputText += String.Format("d2uxdxdx{0}d2uxdxdy{0}d2uxdxdz{0}d2uxdydy{0}d2uxdydz{0}d2uxdzdz{0}d2uydxdx{0}d2uydxdy{0}d2uydxdz{0}d2uydydy{0}d2uydydz{0}d2uydzdz{0}d2uzdxdx{0}d2uzdxdy{0}d2uzdxdz{0}d2uzdydy{0}d2uzdydz{0}d2uzdzdz\r\n", delim);
                    }
                    foreach (edu.jhu.pha.turbulence.VelocityHessian result in results)
                    {
                        if (otype == OutputType.HTML)
                        {
                            outputText += String.Format("<p>d2uxdxdx={0},d2uxdxdy={1},d2uxdxdz={2},<br/>d2uxdydy={3},d2uxdydz={4},d2uxdzdz={5},<br/>d2uydxdx={6},d2uydxdy={7},d2uydxdz={8},<br/>d2uydydy={9},d2uydydz={10},d2uydzdz={11},<br/>d2uzdxdx={12},d2uzdxdy={13},d2uzdxdz={14},<br/>d2uzdydy={15},d2uzdydz={16},d2uzdzdz={17}</p>",
                                result.d2uxdxdx, result.d2uxdxdy, result.d2uxdxdz,
                                result.d2uxdydy, result.d2uxdydz, result.d2uxdzdz,
                                result.d2uydxdx, result.d2uydxdy, result.d2uydxdz,
                                result.d2uydydy, result.d2uydydz, result.d2uydzdz,
                                result.d2uzdxdx, result.d2uzdxdy, result.d2uzdxdz,
                                result.d2uzdydy, result.d2uzdydz, result.d2uzdzdz);
                        }
                        else if (otype == OutputType.Tab || otype == OutputType.CSV)
                        {
                            outputText += String.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}{0}{11}{0}{12}{0}{13}{0}{14}{0}{15}{0}{16}{0}{17}{0}{18}\r\n",
                               delim,
                               result.d2uxdxdx, result.d2uxdxdy, result.d2uxdxdz,
                               result.d2uxdydy, result.d2uxdydz, result.d2uxdzdz,
                               result.d2uydxdx, result.d2uydxdy, result.d2uydxdz,
                               result.d2uydydy, result.d2uydydz, result.d2uydzdz,
                               result.d2uzdxdx, result.d2uzdxdy, result.d2uzdxdz,
                               result.d2uzdydy, result.d2uzdydz, result.d2uzdzdz);
                        }
                    }
                }
                else if (method.Text.Equals("GetMagneticFieldHessian"))
                {
                    edu.jhu.pha.turbulence.VelocityHessian[] results;
                    results = service.GetMagneticHessian(authToken, dataset.Text, timef, spatialv, temporalv, points);
                    if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                    {
                        outputText += String.Format("d2bxdxdx{0}d2bxdxdy{0}d2bxdxdz{0}d2bxdydy{0}d2bxdydz{0}d2bxdzdz{0}d2bydxdx{0}d2bydxdy{0}d2bydxdz{0}d2bydydy{0}d2bydydz{0}d2bydzdz{0}d2bzdxdx{0}d2bzdxdy{0}d2bzdxdz{0}d2bzdydy{0}d2bzdydz{0}d2bzdzdz\r\n", delim);
                    }
                    foreach (edu.jhu.pha.turbulence.VelocityHessian result in results)
                    {
                        if (otype == OutputType.HTML)
                        {
                            outputText += String.Format("<p>d2bxdxdx={0},d2bxdxdy={1},d2bxdxdz={2},<br/>d2bxdydy={3},d2bxdydz={4},d2bxdzdz={5},<br/>d2bydxdx={6},d2bydxdy={7},d2bydxdz={8},<br/>d2bydydy={9},d2bydydz={10},d2bydzdz={11},<br/>d2bzdxdx={12},d2bzdxdy={13},d2bzdxdz={14},<br/>d2bzdydy={15},d2bzdydz={16},d2bzdzdz={17}</p>",
                                result.d2uxdxdx, result.d2uxdxdy, result.d2uxdxdz,
                                result.d2uxdydy, result.d2uxdydz, result.d2uxdzdz,
                                result.d2uydxdx, result.d2uydxdy, result.d2uydxdz,
                                result.d2uydydy, result.d2uydydz, result.d2uydzdz,
                                result.d2uzdxdx, result.d2uzdxdy, result.d2uzdxdz,
                                result.d2uzdydy, result.d2uzdydz, result.d2uzdzdz);
                        }
                        else if (otype == OutputType.Tab || otype == OutputType.CSV)
                        {
                            outputText += String.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}{0}{11}{0}{12}{0}{13}{0}{14}{0}{15}{0}{16}{0}{17}{0}{18}\r\n",
                               delim,
                               result.d2uxdxdx, result.d2uxdxdy, result.d2uxdxdz,
                               result.d2uxdydy, result.d2uxdydz, result.d2uxdzdz,
                               result.d2uydxdx, result.d2uydxdy, result.d2uydxdz,
                               result.d2uydydy, result.d2uydydz, result.d2uydzdz,
                               result.d2uzdxdx, result.d2uzdxdy, result.d2uzdxdz,
                               result.d2uzdydy, result.d2uzdydz, result.d2uzdzdz);
                        }
                    }
                }
                else if (method.Text.Equals("GetVectorPotentialHessian"))
                {
                    edu.jhu.pha.turbulence.VelocityHessian[] results;
                    results = service.GetVectorPotentialHessian(authToken, dataset.Text, timef, spatialv, temporalv, points);
                    if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                    {
                        outputText += String.Format("d2axdxdx{0}d2axdxdy{0}d2axdxdz{0}d2axdydy{0}d2axdydz{0}d2axdzdz{0}d2aydxdx{0}d2aydxdy{0}d2aydxdz{0}d2aydydy{0}d2aydydz{0}d2aydzdz{0}d2azdxdx{0}d2azdxdy{0}d2azdxdz{0}d2azdydy{0}d2azdydz{0}d2azdzdz\r\n", delim);
                    }
                    foreach (edu.jhu.pha.turbulence.VelocityHessian result in results)
                    {
                        if (otype == OutputType.HTML)
                        {
                            outputText += String.Format("<p>d2axdxdx={0},d2axdxdy={1},d2axdxdz={2},<br/>d2axdydy={3},d2axdydz={4},d2axdzdz={5},<br/>d2aydxdx={6},d2aydxdy={7},d2aydxdz={8},<br/>d2aydydy={9},d2aydydz={10},d2aydzdz={11},<br/>d2azdxdx={12},d2azdxdy={13},d2azdxdz={14},<br/>d2azdydy={15},d2azdydz={16},d2azdzdz={17}</p>",
                                result.d2uxdxdx, result.d2uxdxdy, result.d2uxdxdz,
                                result.d2uxdydy, result.d2uxdydz, result.d2uxdzdz,
                                result.d2uydxdx, result.d2uydxdy, result.d2uydxdz,
                                result.d2uydydy, result.d2uydydz, result.d2uydzdz,
                                result.d2uzdxdx, result.d2uzdxdy, result.d2uzdxdz,
                                result.d2uzdydy, result.d2uzdydz, result.d2uzdzdz);
                        }
                        else if (otype == OutputType.Tab || otype == OutputType.CSV)
                        {
                            outputText += String.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}{0}{11}{0}{12}{0}{13}{0}{14}{0}{15}{0}{16}{0}{17}{0}{18}\r\n",
                               delim,
                               result.d2uxdxdx, result.d2uxdxdy, result.d2uxdxdz,
                               result.d2uxdydy, result.d2uxdydz, result.d2uxdzdz,
                               result.d2uydxdx, result.d2uydxdy, result.d2uydxdz,
                               result.d2uydydy, result.d2uydydz, result.d2uydzdz,
                               result.d2uzdxdx, result.d2uzdxdy, result.d2uzdxdz,
                               result.d2uzdydy, result.d2uzdydz, result.d2uzdzdz);
                        }
                    }
                }
                else if (method.Text.Equals("GetVelocityLaplacian"))
                {
                    edu.jhu.pha.turbulence.Vector3[] results;
                    results = service.GetVelocityLaplacian(authToken, dataset.Text, timef, spatialv, temporalv, points);
                    if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                    {
                        outputText += String.Format("grad2ux{0}grad2uy{0}grad2uz\r\n", delim);
                    }
                    foreach (edu.jhu.pha.turbulence.Vector3 result in results)
                    {
                        if (otype == OutputType.HTML)
                        {
                            outputText += String.Format("grad2ux={0},grad2uy={1},grad2uz={2}<br/>",
                                result.x, result.y, result.z);
                        }
                        else if (otype == OutputType.Tab || otype == OutputType.CSV)
                        {
                            outputText += String.Format("{1}{0}{2}{0}{3}\r\n",
                                delim, result.x, result.y, result.z);
                        }
                    }
                }
                else if (method.Text.Equals("GetMagneticFieldLaplacian"))
                {
                    edu.jhu.pha.turbulence.Vector3[] results;
                    results = service.GetMagneticFieldLaplacian(authToken, dataset.Text, timef, spatialv, temporalv, points);
                    if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                    {
                        outputText += String.Format("grad2bx{0}grad2by{0}grad2bz\r\n", delim);
                    }
                    foreach (edu.jhu.pha.turbulence.Vector3 result in results)
                    {
                        if (otype == OutputType.HTML)
                        {
                            outputText += String.Format("grad2bx={0},grad2by={1},grad2bz={2}<br/>",
                                result.x, result.y, result.z);
                        }
                        else if (otype == OutputType.Tab || otype == OutputType.CSV)
                        {
                            outputText += String.Format("{1}{0}{2}{0}{3}\r\n",
                                delim, result.x, result.y, result.z);
                        }
                    }
                }
                else if (method.Text.Equals("GetVectorPotentialLaplacian"))
                {
                    edu.jhu.pha.turbulence.Vector3[] results;
                    results = service.GetVectorPotentialLaplacian(authToken, dataset.Text, timef, spatialv, temporalv, points);
                    if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                    {
                        outputText += String.Format("grad2ax{0}grad2ay{0}grad2az\r\n", delim);
                    }
                    foreach (edu.jhu.pha.turbulence.Vector3 result in results)
                    {
                        if (otype == OutputType.HTML)
                        {
                            outputText += String.Format("grad2ax={0},grad2ay={1},grad2az={2}<br/>",
                                result.x, result.y, result.z);
                        }
                        else if (otype == OutputType.Tab || otype == OutputType.CSV)
                        {
                            outputText += String.Format("{1}{0}{2}{0}{3}\r\n",
                                delim, result.x, result.y, result.z);
                        }
                    }
                }
                else if (method.Text.Equals("GetPosition"))
                {
                    edu.jhu.pha.turbulence.Point3[] results;
                    float EndTimef = Convert.ToSingle(EndTime.Text);
                    float dtf = Convert.ToSingle(dt.Text);
                    if (dataset.SelectedValue.Equals("channel"))
                    {
                        results = service.GetChannelPosition(authToken,
                            dataset.Text, timef, EndTimef, dtf, spatialv, points);
                    }
                    else
                    {
                        results = service.GetPosition(authToken,
                            dataset.Text, timef, EndTimef, dtf, spatialv, points);
                    }
                        
                    if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                    {
                        outputText += String.Format("x{0}y{0}z\r\n", delim);
                    }
                    foreach (edu.jhu.pha.turbulence.Point3 result in results)
                    {
                        if (otype == OutputType.HTML)
                        {
                            outputText += String.Format("x={0},y={1},z={2}<br />\n",
                                result.x, result.y, result.z);
                        }
                        else if (otype == OutputType.Tab || otype == OutputType.CSV)
                        {
                            outputText += String.Format("{1}{0}{2}{0}{3}\r\n",
                                delim, result.x, result.y, result.z);
                        }
                    }
                }
                else if (method.Text.Equals("GetBoxFilter"))
                {
                    string field = fieldList.Text.ToLower();
                    float fw = Convert.ToSingle(filterWidth.Text);
                    if (fieldList.Text.Equals("Pressure"))
                    {
                        edu.jhu.pha.turbulence.Vector3[] results;

                        results = service.GetBoxFilter(authToken,
                            dataset.Text, field, timef, fw, points);
                        if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                        {
                            outputText += String.Format("p\r\n", delim);
                        }
                        foreach (edu.jhu.pha.turbulence.Vector3 result in results)
                        {
                            if (otype == OutputType.HTML)
                            {
                                outputText += String.Format("p={0}<br />\n",
                                    result.x);
                            }
                            else if (otype == OutputType.Tab || otype == OutputType.CSV)
                            {
                                outputText += String.Format("{1}\r\n",
                                    delim, result.x);
                            }
                        }
                    }
                    else if (fieldList.Text.Equals("Density"))
                    {
                        edu.jhu.pha.turbulence.Vector3[] results;

                        results = service.GetBoxFilter(authToken,
                            dataset.Text, field, timef, fw, points);
                        if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                        {
                            outputText += String.Format("rho\r\n", delim);
                        }
                        foreach (edu.jhu.pha.turbulence.Vector3 result in results)
                        {
                            if (otype == OutputType.HTML)
                            {
                                outputText += String.Format("&rho;={0}<br />\n",
                                    result.x);
                            }
                            else if (otype == OutputType.Tab || otype == OutputType.CSV)
                            {
                                outputText += String.Format("{1}\r\n",
                                    delim, result.x);
                            }
                        }
                    }
                    else
                    {
                        edu.jhu.pha.turbulence.Vector3[] results;

                        results = service.GetBoxFilter(authToken,
                            dataset.Text, field, timef, fw, points);
                        if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                        {
                            outputText += String.Format("ux{0}uy{0}uz\r\n", delim);
                        }
                        foreach (edu.jhu.pha.turbulence.Vector3 result in results)
                        {
                            if (otype == OutputType.HTML)
                            {
                                if (fieldList.Text.Equals("Velocity"))
                                {
                                    outputText += String.Format("ux={0},uy={1},uz={2}<br />\n",
                                        result.x, result.y, result.z);
                                }
                                if (fieldList.Text.Equals("Magnetic Field"))
                                {
                                    outputText += String.Format("bx={0},by={1},bz={2}<br />\n",
                                        result.x, result.y, result.z);
                                }
                                if (fieldList.Text.Equals("Vector Potential"))
                                {
                                    outputText += String.Format("ax={0},ay={1},az={2}<br />\n",
                                        result.x, result.y, result.z);
                                }
                            }
                            else if (otype == OutputType.Tab || otype == OutputType.CSV)
                            {
                                outputText += String.Format("{1}{0}{2}{0}{3}\r\n",
                                    delim, result.x, result.y, result.z);
                            }
                        }
                    }
                }
                else if (method.Text.Equals("GetBoxFilterSGSscalar"))
                {
                    float[] results;
                    string field1 = TurbulenceService.DataInfo.GetCharFieldName(fieldList.Text.ToLower());
                    string field2 = TurbulenceService.DataInfo.GetCharFieldName(fieldList2.Text.ToLower());
                    string field = string.Concat(field1, field2);
                    float fw = Convert.ToSingle(filterWidth.Text);

                    results = service.GetBoxFilterSGSscalar(authToken,
                        dataset.Text, field, timef, fw, points);
                    if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                    {
                        outputText += String.Format("{1}{0}\r\n", delim, field);
                    }
                    foreach (float result in results)
                    {
                        if (otype == OutputType.HTML)
                        {
                            outputText += String.Format("{1}={0}<br />\n", result, field);
                        }
                        else if (otype == OutputType.Tab || otype == OutputType.CSV)
                        {
                            outputText += String.Format("{1}\r\n", result);
                        }
                    }
                }
                else if (method.Text.Equals("GetBoxFilterSGSvector"))
                {
                    edu.jhu.pha.turbulence.Vector3[] results;
                    string field1 = TurbulenceService.DataInfo.GetCharFieldName(fieldList.Text.ToLower());
                    string field2 = TurbulenceService.DataInfo.GetCharFieldName(fieldList2.Text.ToLower());
                    string field = string.Concat(field1, field2);
                    float fw = Convert.ToSingle(filterWidth.Text);

                    results = service.GetBoxFilterSGSvector(authToken,
                        dataset.Text, field, timef, fw, points);
                    if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                    {
                        outputText += String.Format("{1}x{2}{0}{1}y{2}{0}{1}z{2}{0}\r\n", delim, field1, field2);
                    }
                    foreach (edu.jhu.pha.turbulence.Vector3 result in results)
                    {
                        if (otype == OutputType.HTML)
                        {
                            outputText += String.Format("{3}x{4}={0},{3}y{4}={1},{3}z{4}={2}<br />\n",
                                result.x, result.y, result.z, field1, field2);
                        }
                        else if (otype == OutputType.Tab || otype == OutputType.CSV)
                        {
                            outputText += String.Format("{1}{0}{2}{0}{3}\r\n",
                                delim, result.x, result.y, result.z);
                        }
                    }
                }
                else if (method.Text.Equals("GetBoxFilterSGSsymtensor"))
                {
                    edu.jhu.pha.turbulence.SGSTensor[] results;
                    string field = fieldList.Text.ToLower();
                    float fw = Convert.ToSingle(filterWidth.Text);

                    results = service.GetBoxFilterSGS(authToken,
                        dataset.Text, field, timef, fw, points);
                    if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                    {
                        outputText += String.Format("{1}x{1}x{0}{1}x{1}y{0}{1}x{1}z{0}{1}y{1}y{0}{1}y{1}z{0}{1}z{1}z\r\n", delim, TurbulenceService.DataInfo.GetCharFieldName(field));
                    }
                    foreach (edu.jhu.pha.turbulence.SGSTensor result in results)
                    {
                        if (otype == OutputType.HTML)
                        {
                            if (fieldList.Text.Equals("Velocity"))
                            {
                                outputText += String.Format("uxux={0},uxuy={1},uxuz={2},uyuy={3},uyuz={4},uzuz={5}<br />\n",
                                    result.xx, result.xy, result.xz, result.yy, result.yz, result.zz);
                            }
                            if (fieldList.Text.Equals("Magnetic Field"))
                            {
                                outputText += String.Format("bxbx={0},bxby={1},bxbz={2},byby={3},bybz={4},bzbz={5}<br />\n",
                                    result.xx, result.xy, result.xz, result.yy, result.yz, result.zz);
                            }
                            if (fieldList.Text.Equals("Vector Potential"))
                            {
                                outputText += String.Format("axax={0},axay={1},axaz={2},ayay={3},ayaz={4},azaz={5}<br />\n",
                                    result.xx, result.xy, result.xz, result.yy, result.yz, result.zz);
                            }
                        }
                        else if (otype == OutputType.Tab || otype == OutputType.CSV)
                        {
                            outputText += String.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}\r\n",
                                delim, result.xx, result.xy, result.xz, result.yy, result.yz, result.zz);
                        }
                    }
                }
                else if (method.Text.Equals("GetBoxFilterSGStensor"))
                {
                    edu.jhu.pha.turbulence.VelocityGradient[] results;
                    string field1 = TurbulenceService.DataInfo.GetCharFieldName(fieldList.Text.ToLower());
                    string field2 = TurbulenceService.DataInfo.GetCharFieldName(fieldList2.Text.ToLower());
                    string field = string.Concat(field1, field2);
                    float fw = Convert.ToSingle(filterWidth.Text);

                    results = service.GetBoxFilterSGStensor(authToken,
                        dataset.Text, field, timef, fw, points);
                    if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                    {
                        outputText += String.Format("{1}x{2}x{0}{1}x{2}y{0}{1}x{2}z{0}{1}y{2}x{0}{1}y{2}y{0}{1}y{2}z{0}{1}z{2}x{0}{1}z{2}y{0}{1}z{2}z{0}\r\n", delim, field1, field2);
                    }
                    foreach (edu.jhu.pha.turbulence.VelocityGradient result in results)
                    {
                        if (otype == OutputType.HTML)
                        {
                            outputText += String.Format("{9}x{10}x={0},{9}x{10}y={1},{9}x{10}z={2},{9}y{10}x={3},{9}y{10}y={4},{9}y{10}z={5},{9}z{10}x={6},{9}z{10}y={7},{9}z{10}z={8}<br />\n",
                                result.duxdx, result.duxdy, result.duxdz, result.duydx, result.duydy, result.duydz, result.duzdx, result.duzdy, result.duzdz, field1, field2);
                        }
                        else if (otype == OutputType.Tab || otype == OutputType.CSV)
                        {
                            outputText += String.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}\r\n",
                                delim, result.duxdx, result.duxdy, result.duxdz, result.duydx, result.duydy, result.duydz, result.duzdx, result.duzdy, result.duzdz);
                        }
                    }
                }
                else if (method.Text.Equals("GetBoxFilterGradient"))
                {
                    string field = fieldList.Text.ToLower();
                    float fw = Convert.ToSingle(filterWidth.Text);
                    float spacing = Convert.ToSingle(filterSpacing.Text);
                    if (fieldList.Text.Equals("Pressure"))
                    {
                        edu.jhu.pha.turbulence.VelocityGradient[] results;

                        results = service.GetBoxFilterGradient(authToken,
                            dataset.Text, field, timef, fw, spacing, points);
                        if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                        {
                            outputText += String.Format("dpdx{0}dpdy{0}dpdz\r\n", delim);
                        }
                        foreach (edu.jhu.pha.turbulence.VelocityGradient result in results)
                        {
                            if (otype == OutputType.HTML)
                            {
                                outputText += String.Format("dpdx={0},dpdy={1},dpdz={2}<br/>\n",
                                    result.duxdx, result.duxdy, result.duxdz);
                            }
                            else if (otype == OutputType.Tab || otype == OutputType.CSV)
                            {
                                outputText += String.Format("{1}{0}{2}{0}{3}\r\n",
                                    delim, result.duxdx, result.duxdy, result.duxdz);
                            }
                        }
                    }
                    else if (fieldList.Text.Equals("Density"))
                    {
                        edu.jhu.pha.turbulence.VelocityGradient[] results;

                        results = service.GetBoxFilterGradient(authToken,
                            dataset.Text, field, timef, fw, spacing, points);
                        if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                        {
                            outputText += String.Format("drhodx{0}drhody{0}drhodz\r\n", delim);
                        }
                        foreach (edu.jhu.pha.turbulence.VelocityGradient result in results)
                        {
                            if (otype == OutputType.HTML)
                            {
                                outputText += String.Format("d&rho;dx={0},d&rho;dy={1},d&rho;dz={2}<br/>\n",
                                    result.duxdx, result.duxdy, result.duxdz);
                            }
                            else if (otype == OutputType.Tab || otype == OutputType.CSV)
                            {
                                outputText += String.Format("{1}{0}{2}{0}{3}\r\n",
                                    delim, result.duxdx, result.duxdy, result.duxdz);
                            }
                        }
                    }
                    else
                    {
                        edu.jhu.pha.turbulence.VelocityGradient[] results;
                        results = service.GetBoxFilterGradient(authToken,
                            dataset.Text, field, timef, fw, spacing, points);
                        if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                        {
                            if (fieldList.Text.Equals("Velocity"))
                            {
                                outputText += String.Format("duxdx{0}duxdy{0}duxdz{0}duydx{0}duydy{0}duydz{0}duzdx{0}duzdy{0}duzdz\r\n", delim);
                            }
                            if (fieldList.Text.Equals("Magnetic Field"))
                            {
                                outputText += String.Format("dbxdx{0}dbxdy{0}dbxdz{0}dbydx{0}dbydy{0}dbydz{0}dbzdx{0}dbzdy{0}dbzdz\r\n", delim);
                            }
                            if (fieldList.Text.Equals("Vector Potential"))
                            {
                                outputText += String.Format("daxdx{0}daxdy{0}daxdz{0}daydx{0}daydy{0}daydz{0}dazdx{0}dazdy{0}dazdz\r\n", delim);
                            }
                        }

                        foreach (edu.jhu.pha.turbulence.VelocityGradient result in results)
                        {
                            if (otype == OutputType.HTML)
                            {
                                if (fieldList.Text.Equals("Velocity"))
                                {
                                    outputText += String.Format("<p>duxdx={0},duxdy={1},duxdz={2},<br/>duydx={3},duydy={4},duydz={5},<br/>duzdx={6},duzdy={7},duzdz={8}</p>\n",
                                        result.duxdx, result.duxdy, result.duxdz,
                                        result.duydx, result.duydy, result.duydz,
                                        result.duzdx, result.duzdy, result.duzdz);
                                }
                                if (fieldList.Text.Equals("Magnetic Field"))
                                {
                                    outputText += String.Format("<p>dbxdx={0},dbxdy={1},dbxdz={2},<br/>dbydx={3},dbydy={4},dbydz={5},<br/>dbzdx={6},dbzdy={7},dbzdz={8}</p>\n",
                                        result.duxdx, result.duxdy, result.duxdz,
                                        result.duydx, result.duydy, result.duydz,
                                        result.duzdx, result.duzdy, result.duzdz);
                                }
                                if (fieldList.Text.Equals("Vector Potential"))
                                {
                                    outputText += String.Format("<p>daxdx={0},daxdy={1},daxdz={2},<br/>daydx={3},daydy={4},daydz={5},<br/>dazdx={6},dazdy={7},dazdz={8}</p>\n",
                                        result.duxdx, result.duxdy, result.duxdz,
                                        result.duydx, result.duydy, result.duydz,
                                        result.duzdx, result.duzdy, result.duzdz);
                                }
                            }
                            else if (otype == OutputType.Tab || otype == OutputType.CSV)
                            {
                                outputText += String.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}\r\n",
                                   delim,
                                   result.duxdx, result.duxdy, result.duxdz,
                                   result.duydx, result.duydy, result.duydz,
                                   result.duzdx, result.duzdy, result.duzdz);
                            }

                        }
                    }
                }
                else if (method.Text.Equals("GetThreshold"))
                {
                    edu.jhu.pha.turbulence.ThresholdInfo[] results;
                    string field = fieldList.Text.ToLower(); 
                    // Remove " Magnitude" from the field name if it exists.
                    if (field.Contains("magnitude"))
                    {
                        field = field.Substring(0, field.IndexOf(" magnitude"));
                    }
                    float thresholdValue = Convert.ToSingle(threshold.Text);
                    int X_int = Convert.ToInt32(x.Text);
                    int Y_int = Convert.ToInt32(y.Text);
                    int Z_int = Convert.ToInt32(z.Text);
                    int x_width_int = Convert.ToInt32(Xwidth.Text);
                    int y_width_int = Convert.ToInt32(Ywidth.Text);
                    int z_width_int = Convert.ToInt32(Zwidth.Text);
                    results = service.GetThreshold(authToken, dataset.Text, field, timef, thresholdValue, spatialv,
                        X_int, Y_int, Z_int, x_width_int, y_width_int, z_width_int);
                    if (results.Length == 0)
                    {
                        outputText += "There are no points with values above the specified threshold.";
                    }
                    else
                    {
                        if (showheader && (otype == OutputType.Tab || otype == OutputType.CSV))
                        {
                            outputText += String.Format("i_x{0}i_y{0}i_z{0}value\r\n", delim);
                        }
                        foreach (edu.jhu.pha.turbulence.ThresholdInfo result in results)
                        {
                            if (otype == OutputType.HTML)
                            {
                                outputText += String.Format("i_x={0},i_y={1},i_z={2},value={3}<br />\n",
                                    result.x, result.y, result.z, result.value);
                            }
                            else if (otype == OutputType.Tab || otype == OutputType.CSV)
                            {
                                outputText += String.Format("{1}{0}{2}{0}{3}{0}{4}\r\n",
                                    delim, result.x, result.y, result.z, result.value);
                            }
                        }
                    }
                }
                else
                {
                    throw new Exception("Unknown method!");
                }
                output.Text = "";
                if (otype == OutputType.HTML)
                {
                    output.Text = outputText;
                }
                else if (otype == OutputType.Tab)
                {
                    setupFileOutput("output.txt");
                    Response.Write(outputText);
                    Response.End();
                    Response.Close();
                }
                else if (otype == OutputType.CSV)
                {
                    setupFileOutput("output.csv");
                    Response.Write(outputText);
                    Response.End();
                    Response.Close();
                }
                else
                {
                    throw new Exception("Unsupported output type: " + otype.ToString());
                }
            }
            catch (Exception e)
            {
                output.Text = "";
                error.Text = "<hr/><h2 style=\".red\">Error!</h2><pre>" + e.ToString() + "</pre>";
            }
        }

        protected void dataset_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (dataset.SelectedValue.Equals("isotropic1024coarse"))
            {
                if (!method.SelectedValue.Equals("GetThreshold"))
                {
                    y.Text = "3.14";
                }
                time.Text = "1.0";
                EndTime.Text = "1.004";
                dt.Text = "0.001";
            }
            else if (dataset.SelectedValue.Equals("mhd1024"))
            {
                if (!method.SelectedValue.Equals("GetThreshold"))
                {
                    y.Text = "3.14";
                }
                time.Text = "1.0";
                EndTime.Text = "1.004";
                dt.Text = "0.001";
            }
            else if (dataset.SelectedValue.Equals("isotropic1024fine"))
            {
                if (!method.SelectedValue.Equals("GetThreshold"))
                {
                    y.Text = "3.14";
                }
                time.Text = "0.0";
                EndTime.Text = "0.004";
                dt.Text = "0.001";
            }
            else if (dataset.SelectedValue.Equals("channel"))
            {
                if (!method.SelectedValue.Equals("GetThreshold"))
                {
                    y.Text = "0.0";
                }
            }
            else if (dataset.SelectedValue.Equals("mixing"))
            {
                if (!method.SelectedValue.Equals("GetThreshold"))
                {
                    y.Text = "3.14";
                }
                time.Text = "5.0";
                EndTime.Text = "5.08";
                dt.Text = "0.02";
            }
        }

        protected void method_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (dataset.SelectedValue.Equals("isotropic1024coarse") ||
                dataset.SelectedValue.Equals("isotropic1024fine") ||
                dataset.SelectedValue.Equals("mhd1024") ||
                dataset.SelectedValue.Equals("mixing"))
            {
                if (method.SelectedValue.Equals("GetThreshold"))
                {
                    x.Text = "0";
                    y.Text = "0";
                    z.Text = "0";
                    velocityEntry.Text = "Velocity Magnitude";
                    magneticEntry.Text = "Magnetic Field Magnitude";
                    potentialEntry.Text = "Vector Potential Magnitude";
                }
                else if (previous_selected_method.Equals("GetThreshold"))
                {
                    x.Text = "3.14";
                    y.Text = "3.14";
                    z.Text = "3.14";
                    velocityEntry.Text = "Velocity";
                    magneticEntry.Text = "Magnetic Field";
                    potentialEntry.Text = "Vector Potential";
                }
            }
            else if (dataset.SelectedValue.Equals("channel"))
            {
                if (method.SelectedValue.Equals("GetThreshold"))
                {
                    x.Text = "0";
                    y.Text = "0";
                    z.Text = "0";
                    velocityEntry.Text = "Velocity Magnitude";
                    magneticEntry.Text = "Magnetic Field Magnitude";
                    potentialEntry.Text = "Vector Potential Magnitude";
                }
                else if (previous_selected_method.Equals("GetThreshold"))
                {
                    x.Text = "3.14";
                    y.Text = "0.0";
                    z.Text = "3.14";
                    velocityEntry.Text = "Velocity";
                    magneticEntry.Text = "Magnetic Field";
                    potentialEntry.Text = "Vector Potential";
                }
            }
            previous_selected_method = method.SelectedValue;
        }

        protected void fieldList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (method.SelectedValue.Equals("GetThreshold"))
            {
                setSpatialFlags(getThresholdFlags());
            }
        }

        protected string[] getThresholdFlags()
        {
            string[] flags;
            if (fieldList.Text.ToLower().Contains("vorticity") || fieldList.Text.Equals("Q"))
            {
                flags = new string[] { "FD4NoInt", "FD6NoInt", "FD8NoInt" };
            }
            else
            {
                flags = new string[] { "None" };
            }
            return flags;
        }
    }

}