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
						<img src="images/channel.jpg" width="205" />
					</p>
					<div style="text-align: center">Channel Flow</div>
					<br />

				</div>
				<div id="centercolumn">

					<p>
						<span class="style20">Dataset description</span>
					</p>

					<p align="left" class="style20">Channel flow:</p>
					<p style="margin-left: 25px;">
						Simulation data provenance: Collaboration of UT Austin and JHU, using the UT Austin DNS code 
            (see <a href="docs/README-CHANNEL.pdf" target="_blank">README-CHANNEL</a> for more details).
					</p>
					<ul>
						<li>Direct numerical simulation (DNS) of channel flow in a domain of size 8&pi; x 2  x 3&pi; , using 2048 x 512 x 1536 nodes.</li>
						<li>Incompressible Navier-Stokes equations are solved using the pseudo-spectral (Fourier-Galerkin) method in wall-parallel (x, z) planes, and the 7th-order B-spline collocation method in the wall-normal (y) direction.</li>
						<li>Simulation is run and equilibrated using prescribed bulk velocity=1, then switched to imposed pressure gradient 
							(dP/dx = 0.0025) and further equilibrated.</li>
						<li>After the simulation has reached a (nearly) statistical stationary state, 4,000 frames of data with 
							3 velocity components and pressure are stored in the database. The frames are stored at every 5 time-steps of the DNS. 
							This corresponds to about one channel flow-through time. Intermediate times can be queried using temporal-interpolation.</li>
						<li>The friction velocity is u<sub>&tau;</sub> = 0.0499.</li>
						<li>The viscosity is &nu; = 5 x 10<sup>-5</sup>.</li>
						<li>The friction velocity Reynolds number is Re<sub>&tau;</sub> ~ 1000.</li>
						<li>The y-locations of the grid points in the vertical direction can be downloaded from this <a href="docs/channel/y.txt" target="_blank">text file</a>; the corresponding B-spline knot locations can be obtained from this <a href="docs/channel/y-knots.txt" target="_blank">text file</a>.</li>
						<!--<li>More details about the DNS are provided in the accompanying <a href="docs/README-CHANNEL.pdf">README-CHANNEL</a> document.</li>-->
						<li>A table with the time history of friction velocity Reynolds number can be downloaded from this <a href="docs/channel/re-tau.txt" target="_blank">text file</a>.</li>
						<li>A table with the vertical profiles of mean velocity, Reynolds shear stresses, viscous stress, normal stress, 
							mean pressure, pressure variance and pressure-velocity covariance in viscous units, can be downloaded from this <a href="docs/channel/profiles.txt" target="_blank">text file</a>.</li>
						<li>Files with tables of the streamwise (k<sub>x</sub>) spectra of u, v, w, p at various heights can be downloaded for the following y+ values: 
       
							<a href="docs/channel/spectra-kx-yplus-10.11.txt" target="_blank">10.11</a>, <a href="docs/channel/spectra-kx-yplus-29.89.txt" target="_blank">29.89</a>, <a href="docs/channel/spectra-kx-yplus-99.75.txt" target="_blank">99.75</a>, <a href="docs/channel/spectra-kx-yplus-371.6.txt" target="_blank">371.6</a>, and <a href="docs/channel/spectra-kx-yplus-999.7.txt" target="_blank">999.7</a>.</li>
						<li>Files with tables of the spanwise (k<sub>z</sub>) spectra of u, v, w, p at various heights can be downloaded for the following y+ values: 
       
							<a href="docs/channel/spectra-kz-yplus-10.11.txt" target="_blank">10.11</a>, <a href="docs/channel/spectra-kz-yplus-29.89.txt" target="_blank">29.89</a>, <a href="docs/channel/spectra-kz-yplus-99.75.txt" target="_blank">99.75</a>, <a href="docs/channel/spectra-kz-yplus-371.6.txt" target="_blank">371.6</a>, and <a href="docs/channel/spectra-kz-yplus-999.7.txt" target="_blank">999.7</a>.</li>
						<%--<li>GetPosition and Filtering functions not yet implemented for the channel flow dataset.</li>--%>
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
