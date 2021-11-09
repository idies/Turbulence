
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

<div id="centercolumnwide">
     

<h2 class="titletext">Vortices within vortices: hierarchical nature of vortex tubes in turbulence</h2>

    <iframe width="560"  height="315"  src="//www.youtube.com/embed/hMMsPGihUi0" frameborder="0" allowfullscreen></iframe>

    <p class="videodescription">In this work we investigate the classical idea that smaller structures in turbulent flows, while engaged in their own 
        internal dynamics, are advected by the larger structures. They are not advected undistorted, however. We see instead 
        that the small scale structures are sheared and twisted by the larger scales. This illuminates the basic mechanisms of 
        the turbulent cascade, here in the Re-lambda = 430 isotropic turbulence dataset from DNS using 1024x1024x1024 grid-points. 
        For more details, see : Kai B�rger et al. (2012), <a href="https://arxiv.org/abs/1210.3325" target="_blank">arXiv:1210.3325</a>.</p>
    
</div>
<div id="rightcolumn"> 
      
</div>  
</div><!-- Main -->   
<br /><br />
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
      Last update: <%=System.IO.File.GetLastWriteTime(Server.MapPath(Request.Url.AbsolutePath)).ToString()%></font></p>
</div>
</div><!--close content.  Used for transparency -->
</div><!-- wrapper -->


</body>
</html>
