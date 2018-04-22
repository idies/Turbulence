<%@ Page Language="C#" AutoEventWireup="true" Inherits="Website.webquery_query" CodeBehind="query.aspx.cs" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head>
	<title>Johns Hopkins Turbulence Databases (JHTDB)</title>

	<link href="../bootstrap-3.2.0-dist/css/bootstrap.min.css" rel="stylesheet" type="text/css" />
	<script src="https://ajax.googleapis.com/ajax/libs/jquery/1.11.1/jquery.min.js"></script>
	<script src="../bootstrap-3.2.0-dist/js/bootstrap.min.js"></script>
	<link href="../turbulence.css" rel="stylesheet" type="text/css" />
</head>
<body>
	<div id="pagewrapper">

		<div class="content">
			<div id="main">
				<div class="transparency"></div>
				<div id="title">
					<br />
					<p>Johns Hopkins Turbulence Databases</p>

					<span class="style33">
						<asp:Literal ID="testingserver" runat="server"></asp:Literal>
						<asp:Literal ID="status" runat="server"></asp:Literal><br />
					</span>
				</div>
				<!--navbar -->
				<!--#include file="../navbar.htm" -->

				<div id="centercolumn-wide">
					<h3 align="center">Query the JHTDB</h3>

					<form id="query" runat="server">
						<div>
							<hr />

							<table width="100%">
								<tr>
									<td valign="top" style="min-width: 250px;">

										<table>
											<tr>
												<td valign="top">Dataset: [<a href="/datasets.aspx" class="note">?</a>]</td>
												<td>
													<asp:DropDownList ID="dataset" runat="server" AutoPostBack="True"
														OnSelectedIndexChanged="dataset_SelectedIndexChanged">
														<asp:ListItem>channel</asp:ListItem>
														<asp:ListItem>isotropic1024coarse</asp:ListItem>
														<asp:ListItem>isotropic1024fine</asp:ListItem>
														<asp:ListItem>isotropic4096</asp:ListItem>
														<asp:ListItem>mhd1024</asp:ListItem>
														<asp:ListItem>mixing</asp:ListItem>
														<asp:ListItem>rotstrat4096</asp:ListItem>
														<asp:ListItem>transition_bl</asp:ListItem>
													</asp:DropDownList></td>
												<td></td>
											</tr>
											<tr runat="server" id="TimeRow">
												<td>Time: [<a href="/datasets.aspx" class="note">?</a>]</td>
												<td>
													<asp:TextBox ID="time" runat="server">1.0</asp:TextBox>
												</td>
												<td>
													<asp:Literal ID="timerange" runat="server" Visible="true"></asp:Literal><br />
												</td>
											</tr>
											<tr runat="server" id="SnapshotRow">
												<td>Snapshot Number: [<a href="/datasets.aspx" class="note">?</a>]</td>
												<td>
													<asp:TextBox ID="SnapshotNumber" runat="server">0</asp:TextBox>
												</td>
												<td>
													<asp:Literal ID="SnapshotRange" runat="server"></asp:Literal>
												</td>
											</tr>
											<tr runat="server" id="EndTimeRow">
												<td>EndTime: [<a href="/datasets.aspx" class="note">?</a>]</td>
												<td>
													<asp:TextBox ID="EndTime" runat="server">1.04</asp:TextBox>
												</td>
												<td>
													<asp:Literal ID="EndTimeRange" runat="server"></asp:Literal>
												</td>
											</tr>
											<tr runat="server" id="DeltaTRow">
												<td>DeltaT: [<a href="/datasets.aspx" class="note">?</a>]</td>
												<td>
													<asp:TextBox ID="dt" runat="server">0.01</asp:TextBox>
												</td>
												<td>
													<asp:Literal ID="DeltaTRange" runat="server"></asp:Literal>
												</td>
											</tr>
											<tr>
												<td>Function: [<a href="/analysisdoc.aspx" class="note">?</a>]</td>
												<td>
													<asp:DropDownList ID="method" runat="server" AutoPostBack="True"
														OnSelectedIndexChanged="method_SelectedIndexChanged">
														<asp:ListItem>GetVelocity</asp:ListItem>
														<asp:ListItem ID="GetPressure">GetPressure</asp:ListItem>
														<asp:ListItem ID="GetTemperature">GetTemperature</asp:ListItem>
														<asp:ListItem ID="GetMagneticField" runat="server">GetMagneticField</asp:ListItem>
														<asp:ListItem ID="GetVectorPotential" runat="server">GetVectorPotential</asp:ListItem>
														<asp:ListItem ID="GetDensity" runat="server">GetDensity</asp:ListItem>
														<asp:ListItem ID="GetVelocityAndPressure">GetVelocityAndPressure</asp:ListItem>
														<asp:ListItem ID="GetVelocityAndTemperature">GetVelocityAndTemperature</asp:ListItem>
														<asp:ListItem>GetVelocityGradient</asp:ListItem>
														<asp:ListItem ID="GetPressureGradient">GetPressureGradient</asp:ListItem>
														<asp:ListItem ID="GetTemperatureGradient">GetTemperatureGradient</asp:ListItem>
														<asp:ListItem ID="GetMagneticFieldGradient" runat="server">GetMagneticFieldGradient</asp:ListItem>
														<asp:ListItem ID="GetVectorPotentialGradient" runat="server">GetVectorPotentialGradient</asp:ListItem>
														<asp:ListItem ID="GetDensityGradient" runat="server">GetDensityGradient</asp:ListItem>
														<asp:ListItem>GetVelocityHessian</asp:ListItem>
														<asp:ListItem ID="GetPressureHessian">GetPressureHessian</asp:ListItem>
														<asp:ListItem ID="GetTemperatureHessian">GetTemperatureHessian</asp:ListItem>
														<asp:ListItem ID="GetMagneticFieldHessian" runat="server">GetMagneticFieldHessian</asp:ListItem>
														<asp:ListItem ID="GetVectorPotentialHessian" runat="server">GetVectorPotentialHessian</asp:ListItem>
														<asp:ListItem ID="GetDensityHessian" runat="server">GetDensityHessian</asp:ListItem>
														<asp:ListItem>GetVelocityLaplacian</asp:ListItem>
														<asp:ListItem ID="GetMagneticFieldLaplacian" runat="server">GetMagneticFieldLaplacian</asp:ListItem>
														<asp:ListItem ID="GetVectorPotentialLaplacian" runat="server">GetVectorPotentialLaplacian</asp:ListItem>
														<asp:ListItem ID="GetForce" runat="server">GetForce</asp:ListItem>
														<asp:ListItem ID="GetPosition" runat="server">GetPosition</asp:ListItem>
														<asp:ListItem ID="GetBoxFilter" runat="server">GetBoxFilter</asp:ListItem>
														<asp:ListItem ID="GetBoxFilterSGSscalar" runat="server">GetBoxFilterSGSscalar</asp:ListItem>
														<asp:ListItem ID="GetBoxFilterSGSvector" runat="server">GetBoxFilterSGSvector</asp:ListItem>
														<asp:ListItem ID="GetBoxFilterSGSsymtensor" runat="server">GetBoxFilterSGSsymtensor</asp:ListItem>
														<asp:ListItem ID="GetBoxFilterSGStensor" runat="server">GetBoxFilterSGStensor</asp:ListItem>
														<asp:ListItem ID="GetBoxFilterGradient" runat="server">GetBoxFilterGradient</asp:ListItem>
														<asp:ListItem ID="GetThreshold" runat="server">GetThreshold</asp:ListItem>
														<asp:ListItem>GetInvariant</asp:ListItem>
														<asp:ListItem>NullOp</asp:ListItem>
													</asp:DropDownList>
												</td>
												<td></td>
											</tr>

											<tr runat="server" id="spatialRow">
												<td>Spatial Interpolation<br />
													and Differentiation: 
                    [<a href="/analysisdoc.aspx" class="note">?</a>]
												</td>
												<td>
													<asp:DropDownList ID="spatial" runat="server">
														<asp:ListItem>None</asp:ListItem>
														<asp:ListItem>Lag4</asp:ListItem>
														<asp:ListItem>Lag6</asp:ListItem>
														<asp:ListItem>Lag8</asp:ListItem>
														<asp:ListItem>FD4NoInt</asp:ListItem>
														<asp:ListItem>FD8NoInt</asp:ListItem>
														<asp:ListItem>FD6NoInt</asp:ListItem>
														<asp:ListItem>FD4Lag4</asp:ListItem>
														<asp:ListItem>M1Q4</asp:ListItem>
														<asp:ListItem>M2Q8</asp:ListItem>
														<asp:ListItem>M2Q14</asp:ListItem>
													</asp:DropDownList></td>
												<td></td>
											</tr>
											<tr runat="server" id="temporalRow">
												<td>Temporal Interpolation: [<a href="/analysisdoc.aspx" class="note">?</a>]</td>
												<td>
													<asp:DropDownList ID="temporal" runat="server" AutoPostBack="True"
														OnSelectedIndexChanged="method_SelectedIndexChanged">
														<asp:ListItem>None</asp:ListItem>
														<asp:ListItem>PCHIP</asp:ListItem>
													</asp:DropDownList></td>
												<td></td>
											</tr>
											<tr runat="server" id="fieldRow">
												<td>Field(s): [<a href="/analysisdoc.aspx" class="note">?</a>]</td>
												<td>
													<asp:DropDownList ID="fieldList" runat="server" AutoPostBack="True"
														OnSelectedIndexChanged="fieldList_SelectedIndexChanged">
														<asp:ListItem ID="velocityEntry" runat="server">Velocity</asp:ListItem>
														<asp:ListItem ID="pressureEntry" runat="server">Pressure</asp:ListItem>
														<asp:ListItem ID="temperatureEntry" runat="server">Temperature</asp:ListItem>
														<asp:ListItem ID="magneticEntry" runat="server">Magnetic Field</asp:ListItem>
														<asp:ListItem ID="potentialEntry" runat="server">Vector Potential</asp:ListItem>
														<asp:ListItem ID="densityEntry" runat="server">Density</asp:ListItem>
														<asp:ListItem ID="vorticityEntry" runat="server">Vorticity Magnitude</asp:ListItem>
														<asp:ListItem ID="QEntry" runat="server">Q</asp:ListItem>
													</asp:DropDownList></td>
												<td>
													<asp:DropDownList ID="fieldList2" runat="server">
														<asp:ListItem ID="velocityEntry2" runat="server">Velocity</asp:ListItem>
														<asp:ListItem ID="pressureEntry2" runat="server">Pressure</asp:ListItem>
														<asp:ListItem ID="temperatureEntry2" runat="server">Temperature</asp:ListItem>
														<asp:ListItem ID="magneticEntry2" runat="server">Magnetic Field</asp:ListItem>
														<asp:ListItem ID="potentialEntry2" runat="server">Vector Potential</asp:ListItem>
														<asp:ListItem ID="densityEntry2" runat="server">Density</asp:ListItem>
													</asp:DropDownList>
												</td>
											</tr>
											<tr runat="server" id="filterWidthRow">
												<td>Filter Width: [<a href="/analysisdoc.aspx" class="note">?</a>]</td>
												<td>
													<asp:TextBox ID="filterWidth" runat="server">0.042951</asp:TextBox>
												</td>
												<td>Odd multiple of dx (dx = 2*PI/N<sub>x</sub>)
												</td>
											</tr>
											<tr runat="server" id="filterSpacingRow">
												<td>Finite Differencing Spacing: [<a href="/analysisdoc.aspx" class="note">?</a>]</td>
												<td>
													<asp:TextBox ID="filterSpacing" runat="server">0.024544</asp:TextBox>
												</td>
												<td>Multiple of dx
												</td>
											</tr>
											<tr runat="server" id="thresholdRow">
												<td>Threshold: [<a href="/analysisdoc.aspx" class="note">?</a>]</td>
												<td>
													<asp:TextBox ID="threshold" runat="server">0.5</asp:TextBox>
												</td>
											</tr>
										</table>
										<br />

										<hr />

										<h3 runat="server" id="QueryText">Query a single point</h3>
										<br />
										<table>
											<tr>
												<th>
													<asp:Literal ID="x_range" runat="server" Visible="true"></asp:Literal></th>
												<th>
													<asp:Literal ID="y_range" runat="server" Visible="true"></asp:Literal></th>
												<th>
													<asp:Literal ID="z_range" runat="server" Visible="true"></asp:Literal></th>
											</tr>
											<tr>
												<td>
													<asp:TextBox ID="x" runat="server">3.14</asp:TextBox></td>
												<td>
													<asp:TextBox ID="y" runat="server">0</asp:TextBox></td>
												<td>
													<asp:TextBox ID="z" runat="server">3.14</asp:TextBox></td>
											</tr>
											<tr>
												<td>
													<br />
												</td>
											</tr>
											<tr>
												<th>
													<asp:Literal ID="xwidth_range" runat="server" Visible="true"></asp:Literal></th>
												<th>
													<asp:Literal ID="ywidth_range" runat="server" Visible="true"></asp:Literal></th>
												<th>
													<asp:Literal ID="zwidth_range" runat="server" Visible="true"></asp:Literal></th>
											</tr>
											<tr>
												<td>
													<asp:TextBox ID="Xwidth" runat="server">16</asp:TextBox></td>
												<td>
													<asp:TextBox ID="Ywidth" runat="server">16</asp:TextBox></td>
												<td>
													<asp:TextBox ID="Zwidth" runat="server">16</asp:TextBox></td>
											</tr>

											<tr>
												<td colspan="3" align="center"><em>
													<asp:Literal ID="coord_range_details" runat="server" Visible="true"></asp:Literal></em></td>
											</tr>
											<tr>
												<td colspan="3" align="center">
													<asp:Button ID="Go" runat="server" Text="Query" OnClick="point_Click" /></td>
											</tr>
										</table>
										<br />

										<hr />
										<h3>Bulk upload</h3>
										<p>
											Input format: [<a href="sampledata.txt" class="note">sample</a>]
        <asp:RadioButtonList ID="inputmethod" runat="server">
			<asp:ListItem Value="bulktxt" Text="Text file (CSV or tab delimited) - 1 point (3 floats) per line" Enabled="true" Selected="True" />
		</asp:RadioButtonList>

											<asp:FileUpload ID="fileup1" runat="server" />
											<asp:Literal ID="fileinfo" runat="server"></asp:Literal><br />
											<asp:Button ID="Button1" runat="server" Text="Upload File" OnClick="upload_Click" />
											or 
        <asp:Button ID="Button2" runat="server" Text="Clear File" OnClick="reset_Click" />
										</p>

										<p>
											Output format:
        <asp:RadioButtonList ID="outputformat" runat="server">
			<asp:ListItem Value="web" Text="Web - Output box on this page" Enabled="true" Selected="False" />
			<asp:ListItem Value="tabtxt" Text="Text file - Tab delimited" Selected="True" Enabled="True" />
			<asp:ListItem Value="csvtxt" Text="Text file - CSV" Selected="False" Enabled="True" />
		</asp:RadioButtonList>
											<asp:CheckBox ID="includeHeader" runat="server" Checked="True" Text="Include header in text output" />
										</p>

										<asp:Button ID="bulkwork" runat="server" Text="Perform Query" OnClick="bulkwork_Click" />
									</td>
									<td valign="top" bgcolor="#f0f0f0" style="min-width: 250px;">
										<h3 align="center">Output</h3>
										<asp:Literal ID="error" runat="server"></asp:Literal><br />
										<p style="margin-left: 25px; margin-right: 25px;">
											<asp:Literal ID="output" runat="server"></asp:Literal>
										</p>
									</td>
								</tr>
							</table>

						</div>
					</form>

				</div>
				<div id="rightcolumn">
				</div>
			</div>
			<!-- Main -->

			<div id="bottom">
				Disclaimer: <em>While many efforts have
            been made to ensure that these data are accurate and reliable within
            the limits of the current state of the art, neither JHU nor any other
            party involved in creating, producing or delivering the website shall
            be liable for any damages arising out of users' access to, or use
            of, the website or web services. Users use the website and web services
            at their own risk. JHU does not warrant that the functional aspects
            of the website will be uninterrupted or error free, and may make
            changes to the site without notice. </em>
				<p align="center" class="style22 style32">
					<font face="Arial, Helvetica, sans-serif" color="#000033" size="-2">Last update: <%=System.IO.File.GetLastWriteTime(Server.MapPath(Request.Url.AbsolutePath)).ToString()%></font>
				</p>
			</div>
		</div>
		<!--close content.  Used for transparency -->
	</div>
	<!-- wrapper -->


</body>
</html>
