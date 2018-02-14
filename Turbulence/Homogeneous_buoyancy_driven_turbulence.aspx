
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head><title>Johns Hopkins Turbulence Databases (JHTDB)</title>

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
<div id="title"><br />
<p >Johns Hopkins Turbulence Databases</p>
</div>
<!--#include file="navbar.htm" -->
<div id="leftcolumn"> 
<br> <br /><br> <br /><br> <br /><br> <br /><br> <br /><br> <br /><br> <br />
<p><img src="images/rstrt_0285_density.png" width="205"/></p>
      <div style="text-align: center">HB Driven Turbulence</div>
</div>
<div id="centercolumn">

        <span class="style20">Dataset
          description</span></p>
       <p align="left" class="style20">
            Homogeneous buoyancy driven turbulence:</p>
        <p style="margin-left:25px;">
            Simulation data provenance: Los Alamos National Laboratory using the LANL DNS code
            (see <a href="docs/README-HBDT.pdf" target="_blank">README-HBDT</a> for more details).</p>
      <ul>
        <li>Direct Numerical Simulation (DNS) of homogeneous buoyancy driven turbulence in a domain size 2&pi; x 2&pi;  x 2&pi;, 
            using 1,024<sup>3</sup> nodes.</li>
        <li>The incompressible two-fluid Navier-Stokes equations are solved using a pseudo-spectral method.
            These equations represent the large speed of sound limit for the fully compressible 
            Navier-Stokes equations with two fluids having different molar masses and obeying the ideal-gas
            equation of state.</li>
        <li>The domain is triply periodic and the homogeneity of the fluctuating 
            quantities is ensured by imposing mean zero velocity and constant mean pressure
            gradient. These conditions are similar to those encountered in the interior
            of the Rayleigh-Taylor mixing layer.</li>
        <li>The two fluids are initialized as random blobs, with a characteristic 
            size of about 1/5 of the domain. The flow starts from rest, in the presence of 
            a constant gravitational acceleration, and the fluids start moving in opposite
            direction due to differential buoyancy forces. Turbulence fluctuations are 
            generated and the turbulent kinetic energy increases; however, as the fluids
            become molecularly mixed, the buoyancy forces decrease and at some point the 
            turbulence starts decaying.</li>
        <li>Due to the change in specific volume during mixing, the divergence of 
            velocity is not zero, but related to the density field. This leads to a 
            variable coefficient Poisson equation for pressure, which is decomposed in two 
            parts, for the gradient and curl components of &nabla;<i>p</i>/&rho;. These are 
            solved using direct solvers to ensure mass conservation and baroclinic 
            generation of vorticity to machine precision.</li>
        <li>The 1015 time frames stored in the database cover both the buoyancy driven increase in
            turbulence intensity as well as the buoyancy mediated turbulence decay. Each
            time frame contains the density, 3 velocity components, and pressure at the grid 
            points. The frames are stored at a constant time interval of 0.04, which 
            represents between 20 to 50 DNS time steps. </li>            
        <li>Schmidt number: 1.0</li>
        <li>Froude number: 1.0</li>
        <li>Atwood number: 0.05</li>
        <li>Maximum turbulent Reynolds number: Re<sub>&tau;</sub> ~ 17,765.</li>
        <li>Minimum turbulent Reynolds number during decay phase: Re<sub>&tau;</sub> ~ 1,595.</li>
        <li>A file with the time history of the Favre turbulent kinetic energy, </br>
            k<sup>&tilde;</sup> = &lt;&rho;u<sub>i</sub><sup>''</sup>u<sub>i</sub><sup>''</sup>&gt; &frasl; 2&lt;&rho;&gt;, Reynolds stresses,
            R<sub>ii</sub> = &lt;&rho;u<sub>i</sub><sup>''</sup>u<sub>i</sub><sup>''</sup>&gt; (no summation over i), vertical mass flux,
            a<sub>v</sub> = &lt;&rho; u<sub>1</sub><sup>'</sup>&gt; &frasl; &lt;&rho;&gt;, turbulent Reynolds number, 
            Re<sub>t</sub> = k<sup>&tilde;2</sup> &frasl; &nu;&epsilon;, eddy turnover time,
            &tau; = k<sup>&tilde;</sup> &frasl; &epsilon;, kinetic energy dissipation, &epsilon;,
            density variance and density-specific volume correlation can be found <a href="docs/hbdt/data1.txt" target="_blank">here</a>.(Note: 
      Until July 22, 2015, the time-history file that was posted on this site included the total kinetic energy instead of the Favre
      turbulent kinetic energy.  The file posted since July 22, 2015 lists the Favre turbulent kinetic energy)</li>
        <li>Files with tables of the power spectra of density, 3 velocity components,
            and mass flux can be downloaded at the following times: <a href="docs/hbdt/spectra1.txt" target="_blank">6.56</a>, <a href="docs/hbdt/spectra2.txt" target="_blank">11.4</a>, 
            <a href="docs/hbdt/spectra3.txt" target="_blank">15.0</a>, <a href="docs/hbdt/spectra4.txt" target="_blank">20.0</a>,
            <a href="docs/hbdt/spectra5.txt" target="_blank">30.0</a>, and <a href="docs/hbdt/spectra6.txt" target="_blank">40.0</a>.</li>
        <li>Files with tables of the density PDF can be downloaded for the following
            times: <a href="docs/hbdt/pdf1.txt" target="_blank">0.0</a>, <a href="docs/hbdt/pdf2.txt" target="_blank">6.56</a>, <a href="docs/hbdt/pdf3.txt" target="_blank">11.4</a>, 
            <a href="docs/hbdt/pdf4.txt" target="_blank">15.0</a>, <a href="docs/hbdt/pdf5.txt" target="_blank">20.0</a>,
            <a href="docs/hbdt/pdf6.txt" target="_blank">30.0</a>, and <a href="docs/hbdt/pdf7.txt" target="_blank">40.0</a>.</li>
      </ul> 
    
</div>
<div id="rightcolumn"> 
      
</div>  
</div><!-- Main -->   

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
      <font face="Arial, Helvetica, sans-serif" color="#000033" size="-2">
      Last update: <%=System.IO.File.GetLastWriteTime(Server.MapPath(Request.Url.AbsolutePath)).ToString()%></p>
      </font>
</div>
</div><!--close content.  Used for transparency -->
</div><!-- wrapper -->


</body>
</html>
