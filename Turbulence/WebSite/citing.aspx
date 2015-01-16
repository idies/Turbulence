
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
       
</div>
<div id="centercolumn">
      
  <p align="left" ><strong><br />
        Citing the database
      in your work: </strong></p>
      <p c>The JH Turbulence Database (JHTDB) is developed as an open
        resource by the Johns Hopkins University, under the sponsorship of the
        National Science Foundation. Continued support for the database depends
        on demonstrable evidence of the database's value to the scientific community.
        We kindly request that you cite the database in your
        publications and presentations. The following citations are suggested:<br />
      </p>
      <p >For journal articles, proceedings, etc..,
        we suggest:</p>
      <blockquote>
        <p>Y. Li, E. Perlman, M. Wan,
            Y. Yang, R. Burns, C. Meneveau, R. Burns, S. Chen, A. Szalay &amp; G.
            Eyink. &quot;A public turbulence database cluster and applications
            to study Lagrangian evolution of velocity increments in turbulence&quot;.
             Journal of Turbulence <strong>9</strong>, No. 31 (2008). </p>
        <p>E. Perlman, R. Burns,
                      Y. Li, and C. Meneveau. &quot;Data Exploration of Turbulence Simulations
                      using a Database Cluster&quot;. Supercomputing SC07, ACM, IEEE,
                      2007.<br />
                      </p>
      </blockquote>      
      <p >For presentations, posters, etc.., we suggest:</p>
      <blockquote>
        <p >  Data obtained from the JHTDB at http://turbulence.pha.jhu.edu</p>
      </blockquote>  
      <p >For articles that use the channel flow data, we suggest also including:</p>
      <blockquote>
        <p>J. Graham, M. Lee, N. Malaya, R.D. Moser, G. Eyink, C. Meneveau, K. Kanov, R. Burns &amp; A. Szalay, &quot;Turbulent channel flow data set&quot;
(2013), available at <a href="http://turbulence.pha.jhu.edu/docs/README-CHANNEL.pdf">http://turbulence.pha.jhu.edu/docs/README-CHANNEL.pdf</a> </p>
      </blockquote>  
      <p >For articles that use the mhd flow data, we suggest also including:</p>
      <blockquote>
        <p>H. Aluie, G. Eyink, E. Vishniac, S. Chen K. Kanov, R. Burns, C. Meneveau, A. Szalay, &quot;Forced MHD turbulence data set&quot; (2013), available at 
        <a href="http://turbulence.pha.jhu.edu/docs/README-MHD.pdf">http://turbulence.pha.jhu.edu/docs/README-MHD.pdf</a> </p>
      </blockquote>   
      <p >For articles that use the mixing flow data, we suggest also including:</p>
      <blockquote>
        <p>D. Livescu, C. Canada, K. Kanov, R. Burns & IDIES staff, J. Pulido, &quot;Homogeneous Buoyancy driven turbulence data set&quot; (2014), 
        available at <a href="http://turbulence.pha.jhu.edu/docs/README-HBDT.pdf">http://turbulence.pha.jhu.edu/docs/README-HBDT.pdf</a> </p>
      </blockquote> 
      <p >For articles that use the Lagrangian tracking GetPosition function, we suggest also including:</p>
      <blockquote>
        <p>H. Yu, K. Kanov, E. Perlman, J. Graham, E. Frederix, R. Burns, A. Szalay, G. Eyink, and C. Meneveau. 
        &quot;Studying Lagrangian dynamics of turbulence using on-demand fluid particle tracking in a public turbulence database&quot;. Journal of Turbulence <strong>13</strong>, No. 12 (2012). </p>
      </blockquote>
    
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
