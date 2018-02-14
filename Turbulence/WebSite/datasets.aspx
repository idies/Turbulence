
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
    <br /><br /><br /><br /><br />
   <p><img src="images/iso1024-small.jpg" height="100"  /></p>
    <br />
      <p><img src="images/mhd-small.jpg" height="100"/></p><br />
      <p><img src="images/channel-small.jpg" height="100"/></p>
     <br /> 
      <p><img src="images/rstrt_0285_density.png" height="100"/></p>
    <br />
    <p><img src="images/isotropic4096.jpg" height="100" /></p>
    <br />
    <p><img src="images/rotstrat4096.jpg" height="100" /></p>
</div>
<div id="centercolumn">

        <span class="style20">Dataset
          descriptions</span></p>
     <br></br> 
        <p align="left" class="style20"><a href = "Forced_isotropic_turbulence.aspx">1. Forced isotropic turbulence:</a></p>
        <p style="margin-left:25px;">
            Direct numerical simulation (DNS) using 1,024<sup>3</sup> nodes. The full time evolution is available, over 5 large-scale turnover times.
     
      <br></br>
            <br></br>
        <p align="left" class="style20">
        <a href = "Forced_MHD_turbulence.aspx">
            2. Forced MHD turbulence:</a></p>
        <p style="margin-left:25px;">
            Direct numerical simulation (DNS) of magneto-hydrodynamic isotropic turbulence using 1,024<sup>3</sup> nodes. The full time evolution is available, over about 1 large-scale turnover time. 
      <br><br />    
            <br></br>
    
        <p align="left" class="style20">
        <a href = "Channel_Flow.aspx">
            3. Channel flow:</p>
            </a>
        <p style="margin-left:25px;">
        Direct numerical simulation (DNS) of channel flow turbulence in a domain of size 8&pi; x 2  x 3&pi; , using 2048 x 512 x 1536 nodes. The full time evolution is available, over a flow-through time across across the 8&pi channel
        <br><br /> 
            <br></br>

        <p align="left" class="style20">
        <a href = "Homogeneous_buoyancy_driven_turbulence.aspx">
            4. Homogeneous buoyancy driven turbulence:</p>
            </a>
        <p style="margin-left:25px;">
       Direct Numerical Simulation (DNS) of homogeneous buoyancy driven turbulence in a domain size 2&pi; x 2&pi;  x 2&pi;, 
            using 1,024<sup>3</sup> nodes. The full time evolution is available, covering both the buoyancy driven increase in turbulence intensity as well as the buoyancy mediated turbulence decay.
            <br></br><br></br>

<p align="left" class="style20"><a href = "Isotropic4096.aspx">5. Forced Isotropic Turbulence Dataset on 4096<sup>3</sup> Grid:</a></p>
        <p style="margin-left:25px;">
         Direct numerical simulation (DNS) using 4096<sup>3</sup> nodes. A single timestep snapshot is available.
            <br></br><br></br>
     
    <p align="left" class="style20"><a href = "Rotstrat4096.aspx">6. Rotating Stratified Turbulence Dataset on 4096<sup>3</sup> Grid:</a></p>
        <p style="margin-left:25px;">
        Direct numerical simulation (DNS) of rotating stratified turbulence using 4096<sup>3</sup> nodes. A total of 5 snapshots are available.
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
