
<%@ Page Language="C#" AutoEventWireup="true" Inherits="_Default" Codebehind="Default.aspx.cs" %>
<%@ OutputCache Duration="60" Location="Any" VaryByParam="none" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head><title>Johns Hopkins Turbulence Databases (JHTDB)</title>

<link href="bootstrap-3.2.0-dist/css/bootstrap.min.css" rel="stylesheet" type="text/css" />
<script type="text/javascript" src="Scripts/jquery.min.js"></script>
<script  type="text/javascript" src="bootstrap-3.2.0-dist/js/bootstrap.min.js"></script>
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
<!--navbar -->
<!--#include file="navbar.htm" -->
<div id="leftcolumn">
   <div id="custom" style="margin-top: 120px">
   <p><img src="images/fig4-1.jpg" width="205" /></p>
   </div>
</div>
<div id="centercolumn">
<p class="style34"><font size="2" face="Arial, Helvetica, sans-serif">
<asp:Literal ID="testingserver" runat="server"></asp:Literal>
<asp:Literal ID="status" runat="server"></asp:Literal>
</font></p>
      <p align="center"><span class="style29"><strong><font face="Arial, Helvetica, sans-serif">Welcome
                to the Johns Hopkins Turbulence Database (JHTDB) site</font></strong></span><span class="style27"><font face="Arial, Helvetica, sans-serif"></font></span></p>
      
      <p class="style34"><font size="2" face="Arial, Helvetica, sans-serif">
      This website is a portal to an Open Numerical Turbulence 
      Laboratory that enables access to multi-Terabyte turbulence databases. 
      The data reside on several nodes and disks on our database 
      cluster computers and are stored in small 3D subcubes. 
      Positions are indexed using a Z-curve for efficient access.</font></p>
      <p class="style34"><font size="2" face="Arial, Helvetica, sans-serif">Access
          to the data is facilitated by a Web services interface that permits
          numerical experiments to be run across the Internet. We offer C, Fortran and
          Matlab interfaces layered above <a href="instructionswebserv.aspx">Web
            services</a> so that scientists
            can use familiar programming tools on their client
            platforms.

       Calls to fetch subsets of the data can be made directly from within a program
            being executed on the client's platform. <a href="webquery/query.aspx">Manual
            queries</a> for  data at
            individual points and times via web-browser are also supported. Evaluation
            of velocity and pressure at arbitrary points and time is supported
            using interpolations executed on the database nodes. Spatial differentiation
            using various order approximations (up to 8th order) and filtering are also supported
             (for details,
            see <a href="analysisdoc.aspx">documentation page</a>). Particle tracking can be performed both forward
            and backward in time using a second order accurate Runge-Kutta integration scheme. Subsets of the data can
            be downloaded in hdf5 file format using the <a href="cutout.aspx"> data cutout service</a>. </font></p>
      <p class="style34"><font size="2" face="Arial, Helvetica, sans-serif">
      To date the Web-services-accessible databases contain a  space-time history of a direct numerical simulation (DNS) of isotropic turbulent flow, in incompressible fluid in 3D, 
      a DNS of the incompressible magneto-hydrodynamic (MHD) equations, a DNS of forced, fully developed turbulent channel flow, 
      and a DNS of homogeneous buoyancy driven turbulence. 
      The datasets comprise over 20 Terabytes for the isotropic turbulence data, 56 Terabytes for the MHD data, 130 Terabytes for the channel flow data 
      and 27 Terabytes for the homogeneous buoyancy driven turbulence data. 
      Basic characteristics of the data sets can be found in the <a href="datasets.aspx">datasets description page</a>.
      Technical details about the database techniques used for this project are described in the <a href="publications.aspx">publications</a>.
      </font></p>
      <p align="left"><font size="2" face="Arial, Helvetica, sans-serif">The
          JHTDB project is funded by the US <a href="http://www.nsf.gov/">National
          Science Foundation</a> <a href="http://www.nsf.gov/"><img src="images/nsf.jpg" width="40"  border="0" /></a>.</font></p>
      <p align="center"><font size="2" color="red">Questions and comments?
            <a href="mailto:turbulence@lists.johnshopkins.edu">turbulence@lists.johnshopkins.edu</a></font></p>
      <p align="center"><span class="style13">
      <asp:Literal ID="tracked" runat="server"></asp:Literal>
  points queried</span></p>
    
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
