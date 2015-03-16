
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
<div id="leftcolumn"><div id="custom" style="margin-top: 120px">
   <p><img src="../../images/fig4-1.jpg" width="205"  /></p></div>
    
</div>
<div id="centercolumn">
     
    <h2 align="center"><span class="style22"><font size="+2">Using JHTDB with Python</font></span></h2>
  
  <h3> Installation</h3> 
ubuntu 14.04
 
Bare-bone installation: <p class="code">
 sudo apt-get install build-essential gfortran<br />
sudo apt-get install python-setuptools<br />
sudo apt-get install python-dev<br />
sudo easy_install numpy<br />
sudo python setup.py install<br />
</p>
Note that doing this should, in principle, also install sympy on your system, since it's used by pyJHTDB.
 
Happy fun installation:
 <p class="code">
 sudo apt-get install build-essential gfortran<br />
sudo apt-get install python-setuptools<br />
sudo apt-get install python-dev<br />
sudo apt-get install libpng-dev libfreetype6-dev<br />
sudo apt-get install libhdf5-dev<br />
sudo easy_install numpy<br />
sudo easy_install h5py<br />
sudo easy_install matplotlib<br />
sudo python setup.py install<br />
 </p>
<br />
More information and source code can be found on github at <a href="https://github.com/idies/pyJHTDB">https://github.com/idies/pyJHTDB</a>
    
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
