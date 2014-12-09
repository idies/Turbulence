
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >

<head><title>Johns Hopkins Turbulence Databases (JHTDB)</title>

<link href="../bootstrap-3.2.0-dist/css/bootstrap.min.css" rel="stylesheet" type="text/css" />
<script src="https://ajax.googleapis.com/ajax/libs/jquery/1.11.1/jquery.min.js"></script>
<script src="../bootstrap-3.2.0-dist/js/bootstrap.min.js"></script>
<link href="../turbulence.css" rel="stylesheet" type="text/css" />
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
<!--#include file="../navbar.htm" -->
<div id="leftcolumn">
   <br /> 
</div>
<div id="centercolumn">
  
<h2>Group/Individual Identifiers for the Johns Hopkins Turbulence Databases</h2>

<h3>Purpose</h3>

We would like to gather statistics on how individuals or groups are using our service.
To this end, each web service request requires you to provide an "authorization token".

The <a href="/webquery/query.aspx">form-based interface</a> does not require any form of identification to use.

We have a generic identifier that may be used if you are simply testing out
the interfaces.  The number and size of requests may be limited.

The current testing identifier is: <br />
<code>edu.jhu.pha.turbulence.testing-201311</code>

<h3>Get a Group/Individual Identifier</h3>

<p>Please send an e-mail to turbulence&#64;pha&#46;jhu&#46;edu with a short description on your intended use of the database.</p>

    
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
