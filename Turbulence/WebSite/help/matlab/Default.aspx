
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
     
    <h2 align="center"><span class="style22"><font size="+2">Using JHTDB with Matlab</font></span></h2>
    <br />
    
    <font face="Arial, Helvetica, sans-serif">
    <h3>Download</h3>  
    <strong>Matlab Code: </strong> turbmat-20141017 (<a href="/download/turbmat-20150108.tar.gz">tar.gz</a>) (<a href="/download/turbmat-20150108.zip">zip</a>) 
    <br />
    <p>This downloads a directory which constains the Matlab interface. Included are sample
    Matlab M-files (<tt>turbm.m, mhd.m, channelm.m, mixingm.m</tt>) that illustrate the basic functionality of the 
    interface. These files may also be adapted to the end-user's needs. The directory also 
    includes several gSOAP wrapper functions that need not be modified. The interface has
    been tested under newer installations of Matlab on various versions of Mac OS X, Linux, 
    and Windows. <br/><br/>
         
    Please see the <tt>README</tt> file for more information.
    </p>
    </font>
    
    
    <font face="Arial, Helvetica, sans-serif">
    <h3>Overview</h3>
    <p>
    We have written several routines which use Matlab web service
    functions to call <a href="http://turbulence.pha.jhu.edu/">JHTDB</a>. 
    All communication with JHTDB is provided through the TurbulenceService Matlab
    class which uses the Matlab intrinsic web service functions to
    create SOAP messages, query the Turbulence Database, and parse the
    results. For each database function a wrapper has been created to
    perform the data translation and retrieval.
    </p>

    <p>
    The Matlab interface now includes the Matlab-Fast-SOAP package which
    provides optimized web service functions for creating, sending, and
    parsing SOAP messages. The Matlab-Fast-SOAP package has been found
    to provide a 100x speedup over the intrinsic Matlab SOAP functions
    used in the original implementation of the interface. Clients are
    now able to easily and quickly retrieve large datasets which
    previously would have taken Matlab much longer to process the
    request and parse the results.
    </p>
    </font>
      

    <font face="Arial, Helvetica, sans-serif">
    <h3>Limitations and Known Issues</h3>
     <ul>
       <p>
       <!--<li>Current requests are limited to roughly 250,000 points.</li><br />-->
       <li>Error handling is performed by the Matlab SOAP communication calls. If a <br />
       SOAP error occurs during execution of the interface functions, all SOAP <br />
       error information will be display to the Matlab terminal and the execution <br />
       will be terminated. We do not currently provide a method for explicit error <br />
       handling/catching.</li><br />
       <li>When retrieving large amounts of data, the heap memory of Matlab's Java <br /> 
       Virtual Machine may overflow. In this event it is required to increase the <br />
       Java heap memory in Matlab. For additional information please see: <br /><br />
      <a href="http://www.mathworks.com/support/solutions/en/data/1-18I2C">How to increase Matlab JVM heap space</a></li>    
    </p></ul>
    </font>
 
  <font face="Arial, Helvetica, sans-serif">
  <h3>Interpolation Flags</h3>
  <p class="note">
  <strong>Note:</strong> Detailed descriptions of the underlying functions can be found in the
  <a href="/analysisdoc.aspx">analysis tools documentation</a>.
  </p>
  </font>

  <p class="code">
  <code>
  <font color="green">% ---- Temporal Interpolation Options ----</font><br />
  NoTInt = <font color="purple">'None'</font>; <font color="green">% No temporal interpolation</font><br />
  PCHIPInt = <font color="purple">'PCHIP'</font>; <font color="green">% Piecewise cubic Hermit interpolation in time</font><br /><br />
  
  <font color="green">% ---- Spatial Interpolation Flags for GetVelocity &amp; GetVelocityAndPressure ----</font><br />
  NoSInt = <font color="purple">'None'</font>; <font color="green">% No spatial interpolation</font><br />
  Lag4 = <font color="purple">'Lag4'</font>; <font color="green">% 4th order Lagrangian interpolation in space</font><br />
  Lag6 = <font color="purple">'Lag6'</font>; <font color="green">% 6th order Lagrangian interpolation in space</font><br />
  Lag8 = <font color="purple">'Lag8'</font>; <font color="green">% 8th order Lagrangian interpolation in space</font><br /><br />
  
  <font color="green">% ---- Spatial Differentiation &amp; Interpolation Flags for GetVelocityGradient &amp; GetPressureGradient ----</font><br />
  FD4NoInt = <font color="purple">'None_Fd4'</font>; <font color="green">% 4th order finite differential scheme for grid values, no spatial interpolation</font><br />
  FD6NoInt = <font color="purple">'None_Fd6'</font>; <font color="green">% 6th order finite differential scheme for grid values, no spatial interpolation</font><br />
  FD8NoInt = <font color="purple">'None_Fd8'</font>; <font color="green">% 8th order finite differential scheme for grid values, no spatial interpolation</font><br />
  FD4Lag4  = <font color="purple">'Fd4Lag4'</font>; <font color="green">% 4th order finite differential scheme for grid values, 4th order Lagrangian interpolation in space</font><br />
  </code>
  </p>

  <font face="Arial, Helvetica, sans-serif">
  <h3>Function Descriptions</h3>
  </font>
 
  <font face="Arial, Helvetica, sans-serif">
  <h4>GetVelocity</h4>
  </font>
  <p class="code">
  <code>real(3,count) output = getVelocity(char <a href="/help/authtoken.aspx">authkey</a>,
    char <a href="/datasets.aspx">dataset</a>, real time, <br />
    &nbsp;&nbsp;spatial interpolation option, temporal interpolation option, integer count, <br />
    &nbsp;&nbsp;real(3,count) input);</code><br />
  <br />
  <em>Example</em><br /><br />
  <code>
  for i = 1:10 <br />
  &nbsp;&nbsp;points(1, i) = 0.1*i; <font color="green">% x</font><br />
  &nbsp;&nbsp;points(2, i) = 0.3*i; <font color="green">% y</font><br />
  &nbsp;&nbsp;points(3, i) = 0.2*i; <font color="green">% z</font><br />
  end<br /><br />
 
  fprintf(<font color="purple">'\nRequesting velocity at 10 points...\n'</font>); <br />
  result3 = getVelocity (authkey, dataset, time, Lag6, NoTInt, 10, points); <br />
  for i = 1:10<br />
  &nbsp;&nbsp;fprintf(1,<font color="purple">'Vx =  %f\n'</font>, result3(1,i)); <br />
  &nbsp;&nbsp;fprintf(1,<font color="purple">'Vy =  %f\n'</font>, result3(2,i)); <br />
  &nbsp;&nbsp;fprintf(1,<font color="purple">'Vz =  %f\n'</font>, result3(3,i)); <br />
  end
  </code>
  </p>
  
  <font face="Arial, Helvetica, sans-serif">
  <h4>GetVelocityAndPressure</h4>
  </font>
  <p class="code">
  <code>real(4,count) output = getvelocityandpressure(char <a href="/help/authtoken.aspx">authkey</a>, 
    char <a href="/datasets.aspx">dataset</a>, real time, <br />
    &nbsp;&nbsp;spatial interpolation option, temporal interpolation option, integer count, <br />
    &nbsp;&nbsp;real(3,count) input);</code><br /><br />
  <em>Example</em><br /><br />
  <code>
    fprintf(<font color="purple">'Requesting velocity and pressure at 10 points...\n'</font>); <br />
    result4 = getVelocityAndPressure(authkey, dataset, time, Lag6, NoTInt, 10, points); <br />
    for i = 1:10<br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">'Vx =  %f\n'</font>, result4(1,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">'Vy =  %f\n'</font>, result4(2,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">'Vz =  %f\n'</font>, result4(3,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">'Pressure =  %f\n'</font>, result4(4,i)); <br />
    end
  </code>
  </p>
  
  <font face="Arial, Helvetica, sans-serif">
  <h4>GetVelocityGradient</h4>
  </font>
  
  <p class="code">
  <code>real(9,count) output = getVelocityAndPressure(char <a href="/help/authtoken.aspx">authkey</a>,
    char <a href="/datasets.aspx">dataset</a>, real time, <br />
    &nbsp;&nbsp;spatial interpolation option, temporal interpolation option, integer count, <br />
    &nbsp;&nbsp;real(3,count) input);</code><br /><br />
  
  <em>Example</em><br /><br />
  <code>
    fprintf(1,<font color="purple">'Velocity gradient at 10 particle locations...\n'</font>);<br />
    result9 = getVelocityGradient(authkey, dataset,  time, FD4Lag4, NoTInt, 10, points);<br />
    for i = 1:10 <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">'%i : duxdx=%f'</font>, i, result9(1,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', duxdy=%f'</font>, result9(2,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', duxdz=%f'</font>, result9(3,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', duydx=%f'</font>, result9(4,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', duydy=%f'</font>, result9(5,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', duydz=%f'</font>, result9(6,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', duzdx=%f'</font>, result9(7,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', duzdy=%f'</font>, result9(8,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', duzdz=%f'</font>, result9(9,i)); <br />
    end<br />
  </code>
  </p>
  
  <font face="Arial, Helvetica, sans-serif">
  <h4>GetVelocityHessian</h4>
  </font>
  <p class="code">
  <code>real(18,count) output = getVelocityHessian(char <a href="/help/authtoken.aspx">authkey</a>,
    char <a href="/datasets.aspx">dataset</a>, real time, <br />
    &nbsp;&nbsp;spatial interpolation option, temporal interpolation option, <br />
    &nbsp;&nbsp;integer count, real(3,count) input)</code><br /><br />
  
  <em>Example</em><br /><br />
  <code>
    fprintf(1,<font color="purple">'Velocity Hessian at 10 particle locations...\n'</font>);<br />
    result18 = getVelocityHessian(authkey, dataset,  time, FD4Lag4, NoTInt, 10, points);<br />
    for i = 1:10<br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">'%i : d2uxdxdx=%f'</font>, i, result18(1,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', d2uxdxdy=%f'</font>, result18(2,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', d2uxdxdz=%f'</font>, result18(3,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', d2uxdydy=%f'</font>, result18(4,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', d2uxdydz=%f'</font>, result18(5,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', d2uxdzdz=%f'</font>, result18(6,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', d2uydxdx=%f'</font>, result18(7,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', d2uydxdy=%f'</font>, result18(8,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', d2uydxdz=%f'</font>, result18(9,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', d2uydydy=%f'</font>, result18(10,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', d2uydydz=%f'</font>, result18(11,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', d2uydzdz=%f'</font>, result18(12,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', d2uzdxdx=%f'</font>, result18(13,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', d2uzdxdy=%f'</font>, result18(14,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', d2uzdxdz=%f'</font>, result18(15,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', d2uzdydy=%f'</font>, result18(16,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', d2uzdydz=%f'</font>, result18(18,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', d2uzdzdz=%f\n'</font>, result18(18,i)); <br />
    end<br />
  </code>
  </p>
  
  <font face="Arial, Helvetica, sans-serif">
  <h4>GetVelocityLaplacian</h4>
  </font>
  <p class="code">
  <code>real(3,count) output = getVelocityLaplacian(char <a href="/help/authtoken.aspx">authkey</a>,
    char <a href="/datasets.aspx">dataset</a>, real time, <br />
    &nbsp;&nbsp;spatial interpolation option, temporal interpolation option, integer count, <br />
    &nbsp;&nbsp;real(3,count) input);</code><br /><br />
  
  <em>Example</em><br /><br />
  <code>
    fprintf(1,<font color="purple">'Velocity Laplacian at 10 particle locations...\n'</font>);<br />
    result3 = getVelocityLaplacian(authkey, dataset,  time, FD4Lag4, NoTInt, 10, points);<br />
    for i = 1:10<br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">'%i: (grad2ux=%f, grad2uy=%f, grad2uz=%f\n'</font>, ... <br />
    &nbsp;&nbsp;&nbsp;&nbsp;i, result3(1,i), result3(2,i), result3(3,i));<br />
    end<br />
  </code>
  </p>
    

  <font face="Arial, Helvetica, sans-serif">
  <h4>GetPressureGradient</h4>
  </font>
  
  <p class="code">
  <code>real(3,count) output = getPressureGradient(char <a href="/help/authtoken.aspx">authkey</a>,
    char <a href="/datasets.aspx">dataset</a>, real time, <br />
    &nbsp;&nbsp;spatial interpolation option, temporal interpolation option, integer count, <br />
    &nbsp;&nbsp;real(3,count) input);</code><br /><br />

  <em>Example</em><br /><br />
  <code>
    fprintf(1,<font color="purple">'Pressure gradient at 10 particle locations...\n'</font>);<br />
    result3 = getPressureGradient(authkey, dataset,  time, FD4Lag4, NoTInt, 10, points); <br />
    for i = 1:10<br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">'%i: dpdx=%f, dpdy=%f, dpdz=%f\n'</font>, ... <br />
    &nbsp;&nbsp;&nbsp;&nbsp;i, result3(1,i), result3(2,i), result3(3,i));<br />
    end<br />
  </code>
  </p> 
  
  <font face="Arial, Helvetica, sans-serif">
  <h4>GetPressureHessian</h4>
  </font>

  <p class="code">
  <code>real(6,count) output = getPressureHessian(char <a href="/help/authtoken.aspx">authkey</a>,
    char <a href="/datasets.aspx">dataset</a>, real time, <br />
    &nbsp;&nbsp;spatial interpolation option, temporal interpolation option, integer count, <br />
    &nbsp;&nbsp;real(3,count) input);</code><br /><br />
  
  <em>Example</em><br /><br />
  <code>
    fprintf(1,<font color="purple">'Velocity hessian at 10 particle locations...\n'</font>);<br />
    result6 = getPressureHessian(authkey, dataset,  time, FD4Lag4, NoTInt, 10, points);<br />
    for i = 1:10<br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">'%i: d2pdxdx=%f'</font>, i, result6(1,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', d2pdxdy=%f'</font>, result6(2,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', d2pdxdz=%f'</font>, result6(3,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', d2pdydy=%f'</font>, result6(4,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', d2pdydz=%f'</font>, result6(5,i)); <br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">', d2pdzdz=%f\n'</font>, result6(6,i)); <br />
    end<br/>
  </code>  
  </p>    

  <font face="Arial, Helvetica, sans-serif">
  <h4>GetForce</h4>
  </font>

  <p class="code">
  <code>real(3,count) output = getForce(char <a href="/help/authtoken.aspx">authkey</a>,
    char <a href="/datasets.aspx">dataset</a>, real time, <br />
    &nbsp;&nbsp;spatial interpolation option, temporal interpolation option, integer count, <br />
    &nbsp;&nbsp;real(3,count) input);</code><br /><br />
  
  <em>Example</em><br /><br />
  <code>
    fprintf(1,<font color="purple">'Requesting forcing at 10 points...\n'</font>);<br />
    result3 = getForce(authkey, dataset,  time, Lag6, NoTInt, 10, points);<br />
    for i = 1:10<br />
    &nbsp;&nbsp;fprintf(1,<font color="purple">'%i: %f, %f, %f\n'</font>, i, result3(1,i),  result3(2,i),  result3(3,i)); <br />
    end<br />
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
