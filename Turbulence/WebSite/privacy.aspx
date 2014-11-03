
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >

<head><title>Johns Hopkins Turbulence Databases (JHTDB)</title>

<link href="bootstrap-3.2.0-dist/css/bootstrap.min.css" rel="stylesheet" type="text/css" />
<script src="https://ajax.googleapis.com/ajax/libs/jquery/1.11.1/jquery.min.js"></script>
<script src="bootstrap-3.2.0-dist/js/bootstrap.min.js"></script>
<link href="turbulence.css" rel="stylesheet" type="text/css" />
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
<div id="title">
<p >Johns Hopkins Turbulence Databases</p>
<p >turbulence.pha.jhu.edu</p>
</div>
<!--#include file="navbar.htm" -->
<div id="leftcolumn">
   <p><img src="images/fig4-1.jpg" width="205" height="365" /></p>
      <p><img src="images/fig2.jpg" width="205" height="163" /></p>
</div>
<div id="centercolumn">
  <h2>Our Privacy Policy</h2>

<p>
For web requests, we log the clientIP, web page and performance information.
For web service requests, we log the client IP, user (<a href="help/authtoken.aspx">auth token</a>),
method, a condensed histogram* of the request and performance information.
These logs are used for management, for infrastructure experiments and for education in analyzing web site traffic.
Aggregate statistics may be reported in the future.
<br />
<i>*Histrogram information is stored for us to analyze access patterns of the databases
and to experiment with load-balancing algorithms. This histogram is currently 4096 bits representing
which regions of space are accessed in the execution of a request.</i>
</p>
<p align="center"><font size="2" color="red">Questions? turbulence&#64;pha&#46;jhu&#46;edu</font></p>

    
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
