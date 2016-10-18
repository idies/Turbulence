<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="announcements.aspx.cs" Inherits="announcements" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
<title>Johns Hopkins Turbulence Databases (JHTDB)</title>

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
   <p><img src="images/isotropic.jpg" width="205"  /></p>
   <div style="text-align: center">Forced Isotropic Turbulence</div> 
     
      <p><img src="images/mhd.jpg" width="205"/></p><div style="text-align: center">Forced MHD Turbulence</div><br />
    <br />
      <p><img src="images/channel.jpg" width="205"/></p>
      <div style="text-align: center">Channel Flow</div><br />
      <br /> 
      <p><img src="images/rstrt_0285_density.png" width="205"/></p>
      <div style="text-align: center">HB Driven Turbulence</div>
</div>
<div id="centercolumn">

        <span class="style20">Past Announcements</span></p>
        <ul><asp:Literal ID="announcement" runat="server"></asp:Literal></ul>    
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