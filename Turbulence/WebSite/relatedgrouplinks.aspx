
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head><title>Johns Hopkins Turbulence Databases (JHTDB)</title>

<link href="bootstrap-3.2.0-dist/css/bootstrap.min.css" rel="stylesheet" type="text/css" />
<script src="https://ajax.googleapis.com/ajax/libs/jquery/1.11.1/jquery.min.js"></script>
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
   <p><img src="images/fig4-1.jpg" width="205" height="365" /></p>
      <p><img src="images/fig2.jpg" width="205" height="163" /></p>
</div>
<div id="centercolumn">
      
  <p><span class="style21"><strong><font size="+2"><br />
      Related
          group websites :<br />
    </font></strong></span></p>
      <ul>
        <li class="style22">
          <p><a href="http://idies.jhu.edu/" target="_blank">The Institute for Data Intensive Engineering and Science</a></p>
        </li>
        <li class="style22">
          <p><a href="http://www.me.jhu.edu/meneveau" target="_blank">Turbulence Research Group</a> (Meneveau)</p>
        </li>
        <li class="style22">
          <p><a href="http://hssl.cs.jhu.edu/" target="_blank">Hopkins Storage Systems Lab (HSSL)</a> (Burns)</p>
        </li>     
        <li class="style22">
          <p><a href="http://www.sdss.jhu.edu/~szalay/servers.html" target="_blank">Collaborative Research on Large Databases</a> (Szalay) </p>
        </li>
        <li class="style22">
          <p><a href="http://www.sdss.org/" target="_blank">Sloan Digital Sky Survey</a></p>
        </li>
        <li class="style22">
          <p><a href="http://www.jhu.edu/~ceafm/" target="_blank">Center for Environmental &amp; Applied Fluid Mechanics</a></p>
        </li>
        <li class="style22">
          <p><a href="http://ctr.stanford.edu" target="_blank">Center for Turbulence Research at Stanford</a></p>
        </li> 
    </ul>      <div align="center"></div>    
    
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
