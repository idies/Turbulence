
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
<br> <br /><br> <br />
   <p><img src="images/mhd.jpg" width="205"/></p><div style="text-align: center">Forced MHD Turbulence</div><br />

</div>
<div id="centercolumn">

        <span class="style20">Dataset
          description</span></p>
      <p align="left" class="style20">
            Forced MHD turbulence:</p>
        <p style="margin-left:25px;">
            Simulation data provenance: JHU DNS code 
            (see <a href="docs/README-MHD.pdf" target="_blank">README-MHD</a> for more details).</p>
      <ul>
        <li>Direct numerical simulation (DNS) using 1,024<sup>3</sup> nodes.</li>

        <li>Incompressible MHD equations are solved using pseudo-spectral
          method.</li>
        <li> Energy is injected by 
            using a Taylor-Green flow stirring force. </li>
        <li> After the simulation has reached a statistical
          stationary state, 1,024 frames of data with 3 velocity components,
          pressure, 3 magnetic field and magnetic vector potential components are stored in the database. </li>
        <li>The Taylor-scale Reynolds
            number fluctuates around R<sub>&lambda;</sub>~
            186.</li>
        <li>1024 timesteps are available, 
            for time t between 0 and 2.56 (the
            frames are stored at every 10 time-steps of the DNS). Intermediate
            times can be queried using temporal-interpolation. </li>
        <li>A table with the 
            spectra
            of the velocity, magnetic field, Elsasser variables, cross-helicity and magnetic 
            helicity can be downloaded from this  <a href="Spectra-MHD.txt" target="_blank">text
            file</a>. </li>
        <li>A table with the time histories of energy and dissipation, both kinetic and magnetic, as 
            well as of magnetic and cross helicity,  can be downloaded from this <a href="TimeSeries.txt" target="_blank">text
            file</a>. </li>  
      
    
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
