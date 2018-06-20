<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head>
	<title>Johns Hopkins Turbulence Databases (JHTDB)</title>
	<link href="bootstrap-3.2.0-dist/css/bootstrap.min.css" rel="stylesheet" type="text/css" />
	<script src="Scripts/jquery.min.js"></script>
	<script src="bootstrap-3.2.0-dist/js/bootstrap.min.js"></script>
	<link href="turbulence.css" rel="stylesheet" type="text/css" />
</head>
<body>
	<div id="pagewrapper">

		<div class="content">
			<div id="main">
				<div class="transparency"></div>
				<div id="title">
					<br />
					<p>Johns Hopkins Turbulence Databases</p>
				</div>
				<!--#include file="navbar.htm" -->
				<div id="leftcolumn">
					<br />
					<br />
					<br />
					<br />
					<p>
						<img src="images/channel5200.png" width="205" />
					</p>
					<div style="text-align: center">Channel Flow at Re<sub>&tau;</sub>=5200</div>
					<br />

				</div>
				<div id="centercolumn">

					<p>
						<span class="style20">Dataset description</span>
					</p>

					<p align="left" class="style20">Channel Flow at Re<sub>&tau;</sub>=5200:</p>
					<p style="margin-left: 25px;">
						Simulation data provenance: UT Texas using the UT Texas DNS code 
            (see <a href="docs/README-CHANNEL5200.pdf" target="_blank">README-CHANNEL5200</a> for more details).
					</p>
					<ul>
						<li>Direct numerical simulation (DNS) of channel flow in a domain of size 8&pi; &times; 2 &times; 3&pi;, using 10240 &times; 1536 &times; 7680 nodes.</li>
						<li>Incompressible Navier-Stokes equations are solved using the pseudo-spectral (Fourier-Galerkin) method in wall-parallel (x, z) planes, and the 7th-order B-spline collocation method in the wall-normal (y) direction.</li>
						<li>Simulation is run and equilibrated using prescribed bulk velocity=1.</li>
						<li>After the simulation has reached a statistical stationary state, 
							11 frames of data with 3 velocity components and pressure are stored in the database. 
							The frames are apart from each other for 0.7 flow-through time.</li>
						<li>The friction velocity is u<sub>&tau;</sub> = 0.0414872.</li>
						<li>The viscosity is &nu; = 8 &times; 10<sup>-6</sup>.</li>
						<li>The friction velocity Reynolds number is Re<sub>&tau;</sub> = 5185.897.</li>
						<li>The y-locations of the grid points in the vertical direction can be downloaded from this <a href="docs/channel5200/channel5200-y.txt" target="_blank">text file</a>; the corresponding B-spline knot locations can be obtained from this <a href="docs/channel5200/channel5200-y-knots.txt" target="_blank">text file</a>.</li>
						<!--<li>More details about the DNS are provided in the accompanying <a href="docs/README-CHANNEL.pdf">README-CHANNEL</a> document.</li>-->
						<%--<li>A table with the time history of friction velocity Reynolds number can be downloaded from this <a href="docs/channel/re-tau.txt" target="_blank">text file</a>.</li>--%>
						<li>Mean profiles of mean velocity, Reynolds stresses, vorticity variance,
							mean pressure, pressure variance, pressure-velocity covariance, 
							terms in Reynolds stress transport equation and 1D energy spectra, 
							can be found <a href="http://turbulence.ices.utexas.edu/channel2015/content/Data_2015_5200.html" target="_blank">here</a>.</li>
<%--						<li>Files with tables of the streamwise (k<sub>x</sub>) spectra of u, v, w, p at various heights can be downloaded for the following y+ values: 
       
							<a href="docs/channel/spectra-kx-yplus-10.11.txt" target="_blank">10.11</a>, <a href="docs/channel/spectra-kx-yplus-29.89.txt" target="_blank">29.89</a>, <a href="docs/channel/spectra-kx-yplus-99.75.txt" target="_blank">99.75</a>, <a href="docs/channel/spectra-kx-yplus-371.6.txt" target="_blank">371.6</a>, and <a href="docs/channel/spectra-kx-yplus-999.7.txt" target="_blank">999.7</a>.</li>
						<li>Files with tables of the spanwise (k<sub>z</sub>) spectra of u, v, w, p at various heights can be downloaded for the following y+ values: 
       
							<a href="docs/channel/spectra-kz-yplus-10.11.txt" target="_blank">10.11</a>, <a href="docs/channel/spectra-kz-yplus-29.89.txt" target="_blank">29.89</a>, <a href="docs/channel/spectra-kz-yplus-99.75.txt" target="_blank">99.75</a>, <a href="docs/channel/spectra-kz-yplus-371.6.txt" target="_blank">371.6</a>, and <a href="docs/channel/spectra-kz-yplus-999.7.txt" target="_blank">999.7</a>.</li>--%>
						<li>GetPosition and Filtering functions are not implemented for this dataset.</li>
					</ul>


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
