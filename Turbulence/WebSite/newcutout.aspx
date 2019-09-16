
<html xmlns="http://www.w3.org/1999/xhtml" >

<head id="Head1" runat="server"><title>Johns Hopkins Turbulence Databases (JHTDB)</title>

<link href="bootstrap-3.2.0-dist/css/bootstrap.min.css" rel="stylesheet" type="text/css" />
<script src="Scripts/jquery.min.js"></script>
<script src="bootstrap-3.2.0-dist/js/bootstrap.min.js"></script>
<link href="turbulence.css" rel="stylesheet" type="text/css" />
<script type="text/javascript">
    function wait_message() {
        document.getElementById("message").innerHTML = "<b>Please wait, your request is being processed. Depending on the size of your request, this may take a moment</b>";
    }
    
    </script>
    <style type="text/css">
        .style7
        {
            width: 200px;
        }
        .style8
        {
            width: 330px;
        }
        .style9
        {
            width: 300px;
        }
        .style35
        {
            width: 180px;
        }
        .style36
        {
            width: 80px;
        }
        .style38
        {
            width: 40px;
        }
        .style39
        {
            text-decoration: underline;
        }
        .style40
        {
            width: 10px;
        }
    </style>
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

<div id="centercolumn-wide">
	<br />
	The new cutout service is built on SciServer, a cloud-based data-driven cluster, of The Institute for Data Intensive Engineering and Science (IDIES) at Johns Hopkins University. Users get the advantages of more reliable and faster data access since the SciServer is located close to our turbulence data servers. The new cutout service generates the output only in HDF5 format and a XDMF file, the latter of which can be opened in Paraview to visualize the data. Users need to create a SciServer account before using the new service. Click below to access.
	</b>
	<br /> 
	  <h2 class="titletext"><a href="http://turbulence.idies.jhu.edu/cutout/" target="_blank">JHTDB new Cutout Service via SciServer</a></h2>
      
      <%--<h2 class="titletext">JHTDB HDF5 and VTK Cutout Service</h2>--%>
      
     <%-- <iframe src='http://dsp033.pha.jhu.edu/jhtdb/' width='900' height='500' style="width: 100%; height: 100%"   ></iframe>--%>

     
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
