
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >

<head><title>Johns Hopkins Turbulence Databases (JHTDB)</title>

<link href="../../bootstrap-3.2.0-dist/css/bootstrap.min.css" rel="stylesheet" type="text/css" />
<script src="https://ajax.googleapis.com/ajax/libs/jquery/1.11.1/jquery.min.js"></script>
<script src="../../bootstrap-3.2.0-dist/js/bootstrap.min.js"></script>
<link href="../../turbulence.css" rel="stylesheet" type="text/css" />
<script type="text/javascript">
        function wait_message() {
            document.getElementById("message").innerHTML = "<b>Please wait, your request is being processed. Depending on the size of your request, this may take a moment</b>";
        }
    
    </script>
</head>
<body>
<div id="pagewrapper">

<div class="content">
<div id="main">
<div class="transparency"></div>
<div id="title"><br />
<p >Johns Hopkins Turbulence Databases</p>

</div>
<!--#include file="../../navbar.htm" -->
<div id="leftcolumn">
   <p><img src="../../images/fig4-1.jpg" width="205" /></p>

</div>
<div id="centercolumn">
    <h2 align="center"><span class="style22"><font size="+2">Using JHTDB with .NET</font></span></h2>
    <br />

    <font face="Arial, Helvetica, sans-serif">
    <h3>Overview</h3>
    <p>
    Microsoft <a href="http://msdn.microsoft.com/vstudio/">Visual Studio</a> 2005 and later will automatically generate interfaces to web services.</p>
  
    <p>To add Web Reference, right click on a project in the Solution Explorer, choose "Add Web Reference...",
    and specify the URL <code>http://turbulence.pha.jhu.edu/service/turbulence.asmx</code>.  
    </font></p>
  
    <font face="Arial, Helvetica, sans-serif">
    <h3>Example</h3>
    </font>
    <p class="code">
    <strong>C#</strong><br />
    <code>
    Random random = new Random();<br />
    edu.jhu.pha.turbulence.TurbulenceService service = new edu.jhu.pha.turbulence.TurbulenceService();<br />
    edu.jhu.pha.turbulence.Point3[] points = new edu.jhu.pha.turbulence.Point3[10];<br />
    edu.jhu.pha.turbulence.Vector3[] output;<br />
    for (int i = 0; i < points.Length; i++) {<br />
    &nbsp;&nbsp;points[i] = new edu.jhu.pha.turbulence.Point3();<br />
    &nbsp;&nbsp;points[i].x = (float)(random.NextDouble() * 2.0 * 3.14);<br />
    &nbsp;&nbsp;points[i].y = (float)(random.NextDouble() * 2.0 * 3.14);<br />
    &nbsp;&nbsp;points[i].z = (float)(random.NextDouble() * 2.0 * 3.14);<br />
    }<br />
    output = service.GetVelocity("jhu.edu.pha.turbulence.testing-200711", "isotropic1024fine", 0.0024f,
    turbulence.SpatialInterpolation.Lag6, turbulence.TemporalInterpolation.None, points);<br />
    for (int r = 0; r < results.Length; r++) {<br />
    &nbsp;&nbsp;Console.WriteLine("X={0} Y={0} Z={0}", output[r].x, output[r].y, output[r].z);<br />
    }<br />
    </code>
    </p>  
    
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
