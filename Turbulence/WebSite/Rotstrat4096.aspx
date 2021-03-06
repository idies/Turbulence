﻿<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

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
						<img src="images/rotstrat4096.jpg" width="205" />
					</p>
					<div style="text-align: center">Rotating Stratified Turbulence Dataset on 4096<sup>3</sup> Grid:</div>
					<br />

				</div>
				<div id="centercolumn">

					<p>
						<span class="style20">Dataset description</span>
					</p>

					<p align="left" class="style20">Rotating Stratified Turbulence Dataset on 4096<sup>3</sup> Grid:</p>
					<p style="margin-left: 25px;">
						Simulation data provenance: GHOST code
                        (see <a href="docs/README-rotstrat4096.pdf" target="_blank">README-rotstrat4096</a> for more details).
					</p>
					<ul>
						<li>Direct numerical simulation (DNS) of rotating and stratified turbulence using 4096<sup>3</sup> nodes, on a periodic grid using a pseudo-spectral parallel code, GHOST.</li>
						<li>Navier-Stokes/Boussinesq equations are solved using a pseudo-spectral method. </li>
						<li>Solid body rotation force acting as the only external forcing mechanism.</li>
						<li>Time integration uses fourth-order Runge-Kutta. </li>
						<li>After the simulation has reached a statistical stationary state, 5 frames of data, which includes the 3 components of the velocity vector and the temperature fluctuations, are generated and written in files that can be accessed directly by the database (FileDB system).</li>
						<li>Domain: 2π &times; 2π &times; 2π</li>
						<li>Grid: 4096<sup>3</sup></li>
						<li>Number of snapshots available: 5</li>
						<li>Viscosity ν = 4 &times; 10<sup>-5</sup></li>
						<li>Prandlt number Pr = ν/κ = 1</li>
						<li>Brunt-Väisälä frequency N = 13.2</li>
						<li>Inertial wave frequency f = 2.67</li>
						<li>RMS velocity U<sub>0</sub> = 0.83</li>
						<li>Scale of energy spectrum peak L<sub>0</sub> = 2π/k<sub>0</sub> = 2.5</li>
						<li>Integral length scale L<sub>int</sub> = 2π ∫ EV(k)dk / ∫ kEV(k)dk = 2.6</li>
						<li>Froude number Fr = U<sub>0</sub>/L<sub>0</sub>N = 0.0242</li>
						<li>Rossby number Ro = U<sub>0</sub>/L<sub>0</sub>f = 0.12</li>
						<li>Reynolds number Re = U<sub>0</sub>L<sub>0</sub>/ν = 5.4 x 104</li>
						<li>Kinetic Energy Dissipation ε<sub>V</sub> = 0.0123</li>
						<li>Potential Energy Dissipation ε<sub>P</sub> = 0.0077</li>
						<li>Kolmogorov scale η = 1.51515 &times; 10-3</li>
						<li>k<sub>max</sub>η = 3.1</li>
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
