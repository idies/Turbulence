
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
<div id="custom" style="margin-top: 120px">
   <p><img src="images/fig4-1.jpg" width="205"  /></p>
   </div>
      
</div>
<div id="centercolumn">
 <h2 align="center"><span class="style22"><font size="+2">Using the JHU Turbulence Database Matlab Analysis Tools</font></span></h2>
    <br />
    
    <font face="Arial, Helvetica, sans-serif">
    <h3>Download</h3>  
    <strong>Matlab Code: </strong> Turbmat-Tools-0.3.2
    (<a href="/download/Turbmat-Tools-0.3.2.tar.gz">Download tar.gz here</a>) (<a href="/download/Turbmat-Tools-0.3.2.zip">Download zip here</a>)<br />
    <p>

      This downloads a directory which contains a set of Matlab analysis
      tools. Included are several ready to use Matlab scripts that
      provide functionality for plotting/animating velocity and
      vorticity fields, computing and plotting longitudinal energy
      spectra, and computing PDF's of velocity, pressure, and velocity
      increments. Each script will prompt the user for any required
      input information using GUI boxes so no modifications are required
      by the user.<br /><br />

      Note that a copy of <a href="help/matlab/">Turbmat</a> is required
      for Turbmat-Tools to run. The latest copy of Turbmat can be
      downloaded from <a href="help/matlab/">here</a>

      <br /><br />
      Please see the <tt>README</tt> file for more information.

    </p>
    </font>
    
    <font face="Arial, Helvetica, sans-serif">
    <h3>Overview</h3>
    <p>
      Turbmat-Tools is a Matlab package with six ready-to-use scripts
      that make use of the <a href="help/matlab/">Turbmat</a> package to
      fetch, process and visualize data from
      the <a href="http://turbulence.pha.jhu.edu/">JHU Turbulence
      Database Cluster</a>. The <a href="help/matlab/">Turbmat</a>
      package provides a wrapper around Matlab web service functions for
      calling the JHU Turbulence Database Cluster. Turbmat-Tools
      provides example scripts for performing requests on the
      database. These scripts do not require any modification. All input
      data is obtained from the user by GUI input boxes.
    </p>
    <p>
      All six scripts make use of the TurbTools class. This class,
      purposefully developed for Turbmat-Tools, contains a large set of
      useful functions to request, parse and visualize data from the
      database.
    </p>
    <p>
      To increase the performance of this package, there has been implemented local
      caching functionality. This is accomplished by creating an extra layer between
      the Matlab code and the database, called TurbCache. The TurbCache class stores
      requests in a uniquely named cache file, and tries to retrieve a request
      straight from this cache file if possible. This then avoids the necesity of a
      direct expensive request on the database.
    </p>
    </font>

    <font face="Arial, Helvetica, sans-serif">
    <h3>Provided Scripts</h3>
     <ul>
       <p>
	 <li>
	   <b><tt>turbm_pdf.m</tt></b> : By fetching a large cube of data, this script calculates the
	   probability density functions (PDF) of quantities such as pressure,
	   velocity components and velocity gradient components. The velocity
	   gradient components are grouped in transverse and longitudinal sets, and
	   logarithmically shown.
	 </li>
	 <br />
	 <li>
	   <b><tt>turbm_pdfVelocityIncrements.m</tt></b> : This scripts fetches a provided number of
	   blocks, consisting of 32x128x512 physical grid points, queried by 32x32x32
	   points. Within this block, we can calculate velocity increments ranging
	   from 1 physical grid point to 256 grid points. This is the equivalent of
	   around 2 to 550 Kolmogorov length scales. We can accumulate important
	   turbulence statistics from the velocity increments, such as skewness and
	   kurtosis. This script presents these statistics in a few graphs.
	   <br />
	   </br>
	   NOTE: The <a href="http://www.mathworks.com/help/toolbox/stats/">Matlab Statistics Toolbox</a> is required for this script to work.
	 </li>
	 <br />
	 <li>
	   <b><tt>turbm_spectra1D.m</tt></b> : This script generates a provided number of randomly
	   positioned and oriented lines, crossing the complete simulation domain
	   present in the database. For all grid points that are on these lines, the
	   inline velocity component is queried. For every single line, we now have a
	   signal that can be transformed to the frequency domain, using Matlab's
	   standard FFT function. The discrete Fourier transform of the line signals
	   can be used to compute the energy spectrum. This energy spectrum is scaled
	   and displayed by this script.
	 </li>
	 <br />
	 <li>
	   <b><tt>turbm_vorticity3D.m</tt></b> : This script allows a user to fetch an arbitrary volume of
	   points in a certain time step. From these evenly spaced points, we can
	   calculate certain scalar quantities, such as Q-criterion, Lambda-2 or
	   Vorticity magnitude, using the velocity gradient tensor components. When
	   drawing iso-surfaces for the named quantities, the resulting structures
	   will be representative for local vortex structures. This scripts will show
	   these iso-surfaces, by using an interpolated 3D scalar field.
	 </li>
	 <br />
	 <li>
	   <b><tt>turbm_velocity2D.m</tt></b> : We can extract 2D slices of points with arbitrary dimension,
	   orientation and position. On these 2D surfaces, we can show all three
	   velocity components by a 2D vector map and a colormap. This script allows
	   a user to do so, by asking numerous input values. In addition, a user can
	   look at absolute velocity components or to relative velocity components,
	   with respect to their average values.
	 </li>
	 <br />
	 <li>
	   <b><tt>turbm_velocity2Dzoom.m</tt></b> : This script has much resemblance with the 2D Velocity
	   script. It applies the same code, using four steps to zoom in from 1024^2
	   points to 16^2 points. This allows a user to recognize both large scale
	   structures in the 1024^2 surface, as well as small scale structures in the
	   16^2 one. 
	 </li> 
    </p></ul>
    </font>     
      
    
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
      Last update: <%=System.IO.File.GetLastWriteTime(Server.MapPath(Request.Url.AbsolutePath)).ToString()%></font></p>
</div>
</div><!--close content.  Used for transparency -->
</div><!-- wrapper -->


</body>
</html>
