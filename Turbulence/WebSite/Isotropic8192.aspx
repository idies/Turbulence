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
						<img src="images/isotropic8192.png" width="205" />
					</p>
					<div style="text-align: center">Forced Isotropic Turbulence Dataset on 8192<sup>3</sup> Grid:</div>
					<br />

				</div>
				<div id="centercolumn">

					<p>
						<span class="style20">Dataset description</span>
					</p>

					<p align="left" class="style20">Forced Isotropic Turbulence Dataset on 8192<sup>3</sup> Grid:</p>
					<p style="margin-left: 25px;">
						Simulation data provenance: Georgia Tech DNS code
                        (see <a href="docs/README-isotropic8192.pdf" target="_blank">README-isotropic8192</a> for more details).
					</p>
					<ul>
						<li>Direct numerical simulation (DNS) using 8192<sup>3</sup> nodes.</li>
						<li>Navier-Stokes is solved using pseudo-spectral method. </li>
						<li>Time integration uses second-order Runge-Kutta. </li>
						<li>The simulation is de-aliased using phase-shifting and truncation. </li>
						<li>Energy is injected by keeping the energy density in the lowest wavenumber modes prescribed following the approach of Donzis & Yeung.</li>
						<li>After the simulation has reached a statistical stationary state, a frame of data, which includes the 3 components of the velocity vector and the pressure, are generated and written in files that can be accessed directly by the database (FileDB system).  </li>
						<li>Domain: 2π &times; 2π &times; 2π</li>
						<li>Grid: 8192<sup>3</sup></li>
                        <li>Number of snapshots available: 6</li>
                        <li>Taylor-scale Reynolds number Re<sub>λ</sub> ~ 1200-1300 for snapshots 0-4, and Re<sub>λ</sub> ~ 610 for snapshot 5</li>
						<li>Viscosity, dissipation, RMS velocity, and Kolmogorov scale: see <a href="docs/README-isotropic8192.pdf" target="_blank">README-isotropic8192</a></li>
						<li>GetPosition is not implemented for this dataset.</li>
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
