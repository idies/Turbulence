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
						<img src="images/transition_bl.png" width="205" />
					</p>
					<div style="text-align: center">Transitional boundary layer</div>
				</div>
				<div id="centercolumn">
					<p>
						<span class="style20">Dataset
                        description
						</span>
					</p>
					<p align="left" class="style20">Transitional boundary layer:</p>
					<p style="margin-left: 25px;">
						Simulation data provenance: Dr. Jin Lee and Prof. Tamer Zaki of JHU
                        (see <a href="docs/README-transition_bl.pdf" target="_blank">README-transition_bl</a> for more details).
					</p>
					<ul>
						<li>Direct numerical simulation (DNS) of a transitional boundary layer over a plate with an elliptical leading edge.</li>
						<li>Navier-Stokes was discretized on a curvilinear grid and solved using a finite volume DNS code.</li>
						<li>A fractional-step algorithm was adopted, and the spatial discretization was a staggered volume-flux formulation.</li>
						<li>The viscous terms were integrated in time implicitly using the Crank-Nicolson and the advections terms were treated explicitly using the Adams-Bashforth.</li>
						<li>Pressure was treated using implicit Euler in the &delta;p-form. The pressure equation was Fourier transformed in the span, and the resulting Helmholtz equation was solved for every spanwise wavenumber using two-dimensional multi-grid.</li>
						<li>After the simulation has reached a statistical stationary state, 4701 frames of data, which includes the 3 components of the velocity vector and the pressure, are generated and written in files that can be accessed directly by the database (FileDB system).</li>
						<li>Since the grid is staggered, data at the wall are not stored in the database. However, JHTDB provides values in the region between the wall and the first grid point, y&isin;[0, 0.0036], using 4th-order Lagrange polynomial inter- and extrapolation.</li>
						<li>The y-locations of the grid points in the vertical direction can be downloaded from this <a href="docs/transition_bl/y.txt" target="_blank">text file</a>.</li>
						<li>The time-averaged statistics can be downloaded from this <a href="docs/transition_bl/Transition_BL_Time_Averaged_Profiles.h5" target="_blank">HDF5 file</a>. Brief notes are <a href="docs/transition_bl/Notes_for_Transition_BL_Time_Averaged_Profiles.txt" target="_blank">here</a>.</li>
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
                    changes to the site without notice.
				</em>
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
