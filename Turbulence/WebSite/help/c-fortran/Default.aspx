
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

<div id="title"><br />
<p >Johns Hopkins Turbulence Databases</p>

</div>
<!--#include file="../../navbar.htm" -->
<div id="leftcolumn"><div id="custom" style="margin-top: 120px">
   <p><img src="../../images/fig4-1.jpg" width="205" /></p>
      </div>
</div>
<div id="centercolumn">
      
      
    <h2 align="center"><span class="style22"><font size="+2">Using JHTDB with C &amp; Fortran</font></span></h2>

   
    <font face="Arial, Helvetica, sans-serif">
    <h3>Download</h3>
    <strong>C and Fortran Code: </strong> turblib-20150108 (<a href="/download/turblib-20150108.tar.gz">Download tar.gz here</a>) (<a href="/download/turblib-20150108.zip">Download zip here</a>)<br />
    <p>
      This downloads a directory with sample Fortran (<tt>turbf.f90, mhdf.f90, channelf.f90, mixingf.f90</tt>) and C (<tt>turbc.c, 
        mhdc.c, channelc.c, mixingc.c</tt>) code,
      and has been tested under various versions of Mac OS X, Linux, FreeBSD and Windows (under Cygwin).
      The directory also includes several gSOAP wrapper functions that need not be modified.
      Executing <em>'make</em>' will build both the Fortran and C sample code.<br />
      Please take a look at the <tt>README</tt> file platform-specific notes.
    </p>
    </font>
   
   <font face="Arial, Helvetica, sans-serif">
   <h3>Overview</h3>
   <p>
   We have written several routines which use the
   <a href="http://www.cs.fsu.edu/~engelen/soap.html">gSOAP</a> library to call <a href="http://turbulence.pha.jhu.edu/">JHTDB</a>.
   </p>
   </font>
  <font face="Arial, Helvetica, sans-serif">
  <h3>Limitations and Notes</h3>

  <ul>
    <p>
    <li>Starting with the 2010-03 release, the library will automatically exit upon any network or database failure.
    This change was made to prevent accidental use of invalid data.
    See the included <tt>README</tt> for instructions on how to override this behavior.
    </li>
<!--    <li>Requests are limited to roughly 250,000 points.</li>-->
    </p>
  </ul>
  </font>
  <font face="Arial, Helvetica, sans-serif">
  <h3>Fortran Specific Notes</h3>
  <p>
  Character arrays (such as <code>authkey</code> and <code>dataset</code>) need to be passed
  as C-style strings. This requires the addition of a <code>NULL</code> character at the end of the string,
  for example:
  </p>
  </font>
  
  <p class="code">
  <code>character*100 :: dataset = 'isotropic1024fine' // CHAR(0)</code>
  </p>
  <font face="Arial, Helvetica, sans-serif">
  <h3>Interpolation Flags</h3>

  <p class="note">
  <strong>Note:</strong> Detailed descriptions of the underlying functions can be found in the
  <a href="/analysisdoc.aspx">analysis tools documentation</a>.
  </p>
  </font>

  <p class="code">
  <strong>Fortran</strong><br />
  <code>
  ! ---- Temporal Interpolation Options ----<br />
  integer, parameter :: NoTInt = 0 ! No temporal interpolation<br />
  integer, parameter :: PCHIPInt = 1 ! Piecewise cubic Hermit interpolation in time<br />
  <br />
  ! ---- Spatial Interpolation Flags for 
      Get[Field] functions ----<br />
  integer, parameter :: NoSInt = 0 ! No spatial interpolation<br />
  integer, parameter :: Lag4 = 4 ! 4th order Lagrangian interpolation in space<br />
  integer, parameter :: Lag6 = 6 ! 6th order Lagrangian interpolation in space<br />
  integer, parameter :: Lag8 = 8 ! 8th order Lagrangian interpolation in space<br />
  <br />
  ! ---- Spatial Differentiation &amp; Interpolation Flags for Get[Field]Gradient, 
      Get[Field]Laplacian and Get[Field]Hessian ----<br />
  integer, parameter :: FD4NoInt = 40 ! 4th order finite differential scheme for grid values, no spatial interpolation<br />
  integer, parameter :: FD6NoInt = 60 ! 6th order finite differential scheme for grid values, no spatial interpolation<br />
  integer, parameter :: FD8NoInt = 80 ! 8th order finite differential scheme for grid values, no spatial interpolation<br />
  integer, parameter :: FD4Lag4 = 44 ! 4th order finite differential scheme for grid values, 4th order Lagrangian interpolation in space<br />
  </code><br /><br />
  Interpolation flags from Fortran are passed in as integer values, but we include these parameters
  at the top of turbf.f90 and mhdf.f90 for reference.  
  </p>

  <p class="code">
  <strong>C</strong><br />
  <code>
  enum SpatialInterpolation {<br />
  &nbsp;&nbsp;/* Spatial Interpolation Flags for Get[Field] */<br />
  &nbsp;&nbsp;NoSInt = 0, /* No spatial interpolatio */<br />
  &nbsp;&nbsp;Lag4 = 4,   /* 4th order Lagrangian interpolation in space */<br />
  &nbsp;&nbsp;Lag6 = 6,   /* 4th order Lagrangian interpolation in space */<br />
  &nbsp;&nbsp;Lag8 = 8,   /* 4th order Lagrangian interpolation in space */<br />
  <br />
  &nbsp;&nbsp;/* Spatial Differentiation &amp; Interpolation Flags for Get[Field]Gradient, 
      Get[Field]Laplacian, Get[Field]Hessian */<br />
  &nbsp;&nbsp;FD4NoInt = 40, /* 4th order finite differential scheme for grid values, no spatial interpolation */<br />
  &nbsp;&nbsp;FD6NoInt = 60, /* 6th order finite differential scheme for grid values, no spatial interpolation */<br />
  &nbsp;&nbsp;FD8NoInt = 80, /* 8th order finite differential scheme for grid values, no spatial interpolation */<br />
  &nbsp;&nbsp;FD4Lag4 = 44,  /*  4th order finite differential scheme for grid values, 4th order Lagrangian interpolation in space */<br />
  };<br />
  <br />
  enum TemporalInterpolation {<br />
  &nbsp;&nbsp;NoTInt = 0,   /* No temporal interpolation */<br />
  &nbsp;&nbsp;PCHIPInt = 1, /* Piecewise cubic Hermit interpolation in time *<br />
  };<br />
  </code>
  </p>

  <font face="Arial, Helvetica, sans-serif">
  <h3>Function Descriptions</h3>

  <h4>soapinit &amp; soapdestroy</h4>
  </font>
  <p class="code">
  <strong>Fortran</strong><br />
  <code>CALL soapinit()<br />
  ...<br />
  CALL soapdestroy()</code><br />
  </p>
  
  <p class="code">
  <strong>C</strong><br />
  <code>void soapinit()<br />
  ...<br />
  void soapdestroy()</code>
  </p>
  <p>
  <code>soapinit()</code> must be called before any WebService call can be run.
  <code>soapdestroy()</code> may be called at the end to release these resources.
  </p>
  <font face="Arial, Helvetica, sans-serif">
  <h4>GetVelocity</h4>
  </font>
  <p class="code">
  <strong>Fortran</strong><br />
  <code>CALL getvelocity(character*N <a href="/help/authtoken.aspx">authkey</a>,
  character*N <a href="/datasets.aspx">dataset</a>, real time,
  spatial interpolation option, temporal interpolation option,
  integer count, real(3,count) input, real(3,count) output)</code><br />
  <br />
  <em>Input</em><br />
  <code>
  do i = 1, count, 1<br />
  &nbsp;&nbsp;input(1, i) = 0.1*i <font class="red">! x</font><br />
  &nbsp;&nbsp;input(2, i) = 0.3*i <font class="red">! y</font><br />
  &nbsp;&nbsp;input(3, i) = 0.2*i <font class="red">! z</font><br />
  end do<br />
  </code>
  <em>Output</em><br />
  <code>
  do i = 1, count, 1<br />
  &nbsp;&nbsp;WRITE (*,*) output(1, i) <font class="red">! Vx</font><br />
  &nbsp;&nbsp;WRITE (*,*) output(2, i) <font class="red">! Vy</font><br />
  &nbsp;&nbsp;WRITE (*,*) output(3, i) <font class="red">! Vz</font><br />
  end do
  </code>
  </p>
  <p class="code">
  <strong>C</strong><br />
  <code>int getVelocity (char *<a href="/help/authtoken.aspx">authkey</a>,
      char *<a href="/datasets.aspx">dataset</a>, float time,
      enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
      int count, float input[count][3], float output[count][3])
  </code><br />
  <br />
  <em>Input</em><br />
  <code>
  for (int i = 0, i < count; i++) {<br />
  &nbsp;&nbsp;input[i][0] = 0.1*i <font class="red">! x</font><br />
  &nbsp;&nbsp;input[i][1] = 0.3*i <font class="red">! y</font><br />
  &nbsp;&nbsp;input[i][2] = 0.2*i <font class="red">! z</font><br />
  }
  </code><br />
  <em>Output</em><br />
  <code>
  for (int i = 0, i &lt; count; i++)<br />
  &nbsp;&nbsp;printf("Vx=%f Vy=%f Vz=%f\n", output[i][0], output[i][1], output[i][2]);<br />
  </code>
  </p>
  <font face="Arial, Helvetica, sans-serif">
  <h4>GetForce</h4>
  </font>
  <p class="code">
  <strong>Fortran</strong><br />
  <code>CALL getforce(character*N <a href="/help/authtoken.aspx">authkey</a>,
  character*N <a href="/datasets.aspx">dataset</a>, real time,
  spatial interpolation option, temporal interpolation option,
  integer count, real(3,count) input, real(3,count) output)</code><br />
  <br />
  <em>Input</em><br />
  <code>
  do i = 1, count, 1<br />
  &nbsp;&nbsp;input(1, i) = 0.1*i <font class="red">! x</font><br />
  &nbsp;&nbsp;input(2, i) = 0.3*i <font class="red">! y</font><br />
  &nbsp;&nbsp;input(3, i) = 0.2*i <font class="red">! z</font><br />
  end do<br />
  </code>
  <em>Output</em><br />
  <code>
  do i = 1, count, 1<br />
  &nbsp;&nbsp;WRITE (*,*) output(1, i) <font class="red">! fx</font><br />
  &nbsp;&nbsp;WRITE (*,*) output(2, i) <font class="red">! fy</font><br />
  &nbsp;&nbsp;WRITE (*,*) output(3, i) <font class="red">! fz</font><br />
  end do
  </code>
  </p>
  <p class="code">
  <strong>C</strong><br />
  <code>int getForce (char *<a href="/help/authtoken.aspx">authkey</a>,
        char *<a href="/datasets.aspx">dataset</a>, float time,
        enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
        int count, float input[count][3], float output[count][3])
  </code><br />
  <br />
  <em>Input</em><br />
  <code>
  for (int i = 0, i < count; i++) {<br />
  &nbsp;&nbsp;input[i][0] = 0.1*i <font class="red">! x</font><br />
  &nbsp;&nbsp;input[i][1] = 0.3*i <font class="red">! y</font><br />
  &nbsp;&nbsp;input[i][2] = 0.2*i <font class="red">! z</font><br />
  }
  </code><br />
  <em>Output</em><br />
  <code>
  for (int i = 0, i &lt; count; i++)<br />
  &nbsp;&nbsp;printf("fx=%f fy=%f fz=%f\n", output[i][0], output[i][1], output[i][2]);<br />
  </code>
  </p>
  <font face="Arial, Helvetica, sans-serif">
  <h4>GetVelocityAndPressure</h4>
  </font>
  <p class="code">
  <strong>Fortran</strong><br />
  <code>CALL getvelocityandpressure(character*N <a href="/help/authtoken.aspx">authkey</a>, character*N <a href="/datasets.aspx">dataset</a>, real time,
    spatial interpolation option, temporal interpolation option,
    integer count, real(3,count) input, real(4,count) output)</code><br />
  <br />
  <em>Example</em><br />
  <code>
  do i = 1, count, 1<br />
  &nbsp;&nbsp;WRITE (*,*) output(1, i) <font class="red">! Vx</font><br />
  &nbsp;&nbsp;WRITE (*,*) output(2, i) <font class="red">! Vy</font><br />
  &nbsp;&nbsp;WRITE (*,*) output(3, i) <font class="red">! Vz</font><br />
  &nbsp;&nbsp;WRITE (*,*) output(4, i) <font class="red">! Pressure</font><br />
  end do
  </code>
  </p>
  <p class="code">
  <strong>C</strong><br />
  <code>int getVelocityAndPressure (char *<a href="/help/authtoken.aspx">authkey</a>,
        char *<a href="/datasets.aspx">dataset</a>, float time,
        enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
        int count, float input[count][3], float output[count][4])
  </code><br /><br />
  <em>Example</em><br />
  <code>
  for (int i = 0, i &lt; count; i++)<br />
  &nbsp;&nbsp;printf("Vx=%f Vy=%f Vz=%f P=%f\n", output[i][0], output[i][1], output[i][2], output[i][3]);<br />
  </code>
  </p>
  <font face="Arial, Helvetica, sans-serif">
  <p>
  This function is the same as <code>GetVelocity</code> except for the extra return value for pressure.
  </p>

  <h4>GetVelocityGradient</h4>
  </font>
  
  <p class="code">
  <strong>Fortran</strong><br />
  <code>CALL getvelocityandpressure(character*N <a href="/help/authtoken.aspx">authkey</a>,
    character*N <a href="/datasets.aspx">dataset</a>, real time,
    spatial interpolation option, temporal interpolation option,
    integer count, real(3,count) input, real(9,count) output)</code><br />
  <br />
  <em>Example</em><br />
  <code>
  write(*, *) 'Velocity gradient at 10 particle locations'<br />
  CALL getvelocitygradient(authkey, dataset,  time, FD4Lag4, NoTInt, 10, points, dataout9)<br />
  do i = 1, 10, 1 <br />
  &nbsp;&nbsp;write(*,*) i, ': (duxdx=', dataout9(1,i), ', duxdy=', dataout9(2,i), &amp; <br />
  &nbsp;&nbsp;&nbsp;&nbsp;', duxdz=', dataout9(3,i), ', duydx=', dataout9(4,i),  &amp;<br />
  &nbsp;&nbsp;&nbsp;&nbsp;', duydy=', dataout9(5,i), ', duydz=', dataout9(6,i),  &amp;<br />
  &nbsp;&nbsp;&nbsp;&nbsp;', duzdx=', dataout9(7,i), ', duzdy=', dataout9(8,i),  &amp;<br />
  &nbsp;&nbsp;&nbsp;&nbsp;', duzdz=', dataout9(9,i), ')'<br />
  end do<br />
  </code>
  
  <p class="code">
  <strong>C</strong><br />
  <code>int getVelocityGradient (char *<a href="/help/authtoken.aspx">authkey</a>,
        char *<a href="/datasets.aspx">dataset</a>, float time,
        enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
        int count, float input[count][3], float output[count][9])
  </code><br /><br />
  <em>Example</em><br />
  <code>
  getVelocityGradient (authtoken, dataset, time, FD4Lag4, temporalInterp, 10, points, result9);<br />
  for (p = 0; p &lt; 10; p++) {<br />
  &nbsp;&nbsp;printf("%d: duxdx=%f, duxdy=%f, duxdz=%f, duydx=%f, duydy=%f, duydz=%f, duzdx=%f, duzdy=%f, duzdz=%f\n", p,
  result9[p][0], result9[p][1], result9[p][2],
  result9[p][3], result9[p][4], result9[p][5],
  result9[p][6], result9[p][7], result9[p][8]);<br />
  }<br />
  </code>
  </p>
  <font face="Arial, Helvetica, sans-serif">  
  <h4>GetVelocityHessian</h4>
  </font>
  <p class="code">
  <strong>Fortran</strong><br />
  <code>CALL getvelocityhessian(character*N <a href="/help/authtoken.aspx">authkey</a>,
    character*N <a href="/datasets.aspx">dataset</a>, real time,
    spatial interpolation option, temporal interpolation option,
    integer count, real(3,count) input, real(18,count) output)</code><br />
  <br />
  <em>Example</em><br />
  <code>
  write(*, *) 'Velocity Hessian at 10 particle locations'<br />
  CALL getvelocityhessian(authkey, dataset,  time, FD4Lag4, NoTInt, 10, points, dataout18)<br />
  do i = 1, 10, 1<br />
  &nbsp;&nbsp;write(*,*) i, ': (d2uxdxdx=', dataout18(1,i), &amp;<br />
  &nbsp;&nbsp;&nbsp;&nbsp;', d2uxdxdy=', dataout18(2,i), &amp;<br />
  &nbsp;&nbsp;&nbsp;&nbsp;', d2uxdxdz=', dataout18(3,i), &amp;<br />
  &nbsp;&nbsp;&nbsp;&nbsp;', d2uxdydy=', dataout18(4,i), &amp;<br />
  &nbsp;&nbsp;&nbsp;&nbsp;', d2uxdydz=', dataout18(5,i), &amp;<br />
  &nbsp;&nbsp;&nbsp;&nbsp;', d2uxdzdz=', dataout18(6,i), &amp;<br />
  &nbsp;&nbsp;&nbsp;&nbsp;', d2uydxdx=', dataout18(7,i), &amp;<br />
  &nbsp;&nbsp;&nbsp;&nbsp;', d2uydxdy=', dataout18(8,i), &amp;<br />
  &nbsp;&nbsp;&nbsp;&nbsp;', d2uydxdz=', dataout18(9,i), &amp;<br />
  &nbsp;&nbsp;&nbsp;&nbsp;', d2uydydy=', dataout18(10,i), &amp;<br />
  &nbsp;&nbsp;&nbsp;&nbsp;', d2uydydz=', dataout18(11,i), &amp;<br />
  &nbsp;&nbsp;&nbsp;&nbsp;', d2uydzdz=', dataout18(12,i), &amp;<br />
  &nbsp;&nbsp;&nbsp;&nbsp;', d2uzdxdx=', dataout18(13,i), &amp;<br />
  &nbsp;&nbsp;&nbsp;&nbsp;', d2uzdxdy=', dataout18(14,i), &amp;<br />
  &nbsp;&nbsp;&nbsp;&nbsp;', d2uzdxdz=', dataout18(15,i), &amp;<br />
  &nbsp;&nbsp;&nbsp;&nbsp;', d2uzdydy=', dataout18(16,i), &amp;<br />
  &nbsp;&nbsp;&nbsp;&nbsp;', d2uzdydz=', dataout18(18,i), &amp;<br />
  &nbsp;&nbsp;&nbsp;&nbsp;', d2uzdzdz=', dataout18(18,i), ')'<br />
  end do<br />
  </code>
  </p>
  <p class="code">
  <strong>C</strong><br />
  <code>int getVelocityHessian(char *<a href="/help/authtoken.aspx">authkey</a>,
        char *<a href="/datasets.aspx">dataset</a>, float time,
        enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
        int count, float input[count][3], float output[count][18])</code><br />
  <br />
  <em>Example</em><br />
  <code>
  getVelocityHessian (authtoken, dataset, time, FD4Lag4, temporalInterp, 10, points, result18);<br />
  for (p = 0; p &lt; 10; p++) {<br />
  &nbsp;&nbsp;printf("%d: d2uxdxdx=%f, d2uxdxdy=%f, d2uxdxdz=%f, d2uxdydy=%f, d2uxdydz=%f, d2uxdzdz=%f, d2uydxdx=%f, d2uydxdy=%f, d2uydxdz=%f, d2uydydy=%f, d2uydydz=%f, d2uydzdz=%f, d2uzdxdx=%f, d2uzdxdy=%f, d2uzdxdz=%f, d2uzdydy=%f, d2uzdydz=%f, d2uzdzdz=%f\n",
          p,
          result18[p][0], result18[p][1], result18[p][2],
          result18[p][3], result18[p][4], result18[p][5],
          result18[p][6], result18[p][7], result18[p][8],
          result18[p][9], result18[p][10], result18[p][11],
          result18[p][12], result18[p][13], result18[p][14],
          result18[p][15], result18[p][16], result18[p][17]);<br />
  }<br />
  </code>
  </p>

  <font face="Arial, Helvetica, sans-serif">
  <h4>GetVelocityLaplacian</h4>
  </font>
  <p class="code">
  <strong>Fortran</strong><br />
  <code>CALL getvelocitylaplacian(character*N <a href="/help/authtoken.aspx">authkey</a>,
    character*N <a href="/datasets.aspx">dataset</a>, real time,
    spatial interpolation option, temporal interpolation option,
    integer count, real(3,count) input, real(3,count) output)</code><br />
  <br />
  <em>Example</em><br />
  <code>
  write(*, *) 'Velocity Laplacian at 10 particle locations'<br />
  CALL getvelocitylaplacian(authkey, dataset,  time, FD4Lag4, NoTInt, 10, points, dataout3)<br />
  do i = 1, 10, 1<br />
  &nbsp;&nbsp;write(*,*) i, ': (grad2ux=', dataout3(1,i), ', grad2uy=', dataout3(2,i), ', grad2uz=', dataout3(3,i), ')'<br />
  end do<br />
  </code>
  </p>
    
  <p class="code">
  <strong>C</strong><br />
  <code>int getVelocityHessian(char *<a href="/help/authtoken.aspx">authkey</a>,
        char *<a href="/datasets.aspx">dataset</a>, float time,
        enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
        int count, float input[count][3], float output[count][18])</code><br />
  <br />
  <em>Example</em><br />
  <code>
  getVelocityLaplacian (authtoken, dataset, time, FD4Lag4, temporalInterp, 10, points, result3);
  for (p = 0; p &lt; 10; p++) {<br />
  &nbsp;&nbsp;printf("%d: grad2ux=%f, grad2uy=%f, grad2uz=%f\n",
            p, result3[p][0],  result3[p][1],  result3[p][2]);<br />
  }<br />
  </code>
  </p>

  <font face="Arial, Helvetica, sans-serif">
  <h4>GetPressureGradient</h4>
  </font>
  <p class="code">
  <strong>Fortran</strong><br />
  <code>CALL getpressuregradient(character*N <a href="/help/authtoken.aspx">authkey</a>,
    character*N <a href="/datasets.aspx">dataset</a>, real time,
    spatial interpolation option, temporal interpolation option,
    integer count, real(3,count) input, real(3,count) output)</code><br />
  <br />
  <em>Example</em><br />
  <code>
  write(*, *) 'Pressure gradient at 10 particle locations'<br />
  CALL getpressuregradient(authkey, dataset,  time, FD4Lag4, NoTInt, 10, points, dataout3) <br />
  do i = 1, 10, 1<br />
  &nbsp;&nbsp;write(*,*) i, ': (dpdx=', dataout3(1,i), ', dpdy=', dataout3(2,i), ', dpdz=', dataout3(3,i), ')'<br />
  end do<br />
  </code>
  </p>
  
  <p class="code">
  <strong>C</strong><br />
  <code>int getPressureGradient(char *<a href="/help/authtoken.aspx">authkey</a>,
        char *<a href="/datasets.aspx">dataset</a>, float time,
        enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
        int count, float input[count][3], float output[count][3])
  </code><br /><br />
  <em>Example</em><br />
  <code>
  getPressureGradient (authtoken, dataset, time, FD4Lag4, temporalInterp, 10, points, result3);<br />
  for (p = 0; p &lt; 10; p++) {<br />
  &nbsp;&nbsp;printf("%d: dpdx=%f,dpdy=%f,dpdz=%f\n", p, result3[p][0], result3[p][1], result3[p][2]);<br />
  }<br />
  </code>
  </p>

  <font face="Arial, Helvetica, sans-serif">
  <h4>GetPressureHessian</h4>
  </font>
  <p class="code">
  <strong>Fortran</strong><br />
  <code>CALL getpressurehessian(character*N <a href="/help/authtoken.aspx">authkey</a>,
    character*N <a href="/datasets.aspx">dataset</a>, real time,
    spatial interpolation option, temporal interpolation option,
    integer count, real(3,count) input, real(6,count) output)</code><br />
  <br />
  <em>Example</em><br />
  
  write(*, *) 'Velocity hessian at 10 particle locations'<br />
  CALL getpressurehessian(authkey, dataset,  time, FD4Lag4, NoTInt, 10, points, dataout6)<br />
  do i = 1, 10, 1<br />
  &nbsp;&nbsp;write(*,*) i, ': (d2pdxdx=', dataout6(1,i), ', d2pdxdy=', dataout6(2,i), &amp;<br />
  &nbsp;&nbsp;&nbsp;&nbsp;', d2pdxdz=', dataout6(3,i), ', d2pdydy=', dataout6(4,i),  &amp;<br />
  &nbsp;&nbsp;&nbsp;&nbsp;', d2pdydz=', dataout6(5,i), ', d2pdzdz', dataout6(6,i), ')'<br />
  end do<br />
  </p>
  
  <p class="code">
  <strong>C</strong><br />
  <code>int getPressureHessian(char *<a href="/help/authtoken.aspx">authkey</a>,
        char *<a href="/datasets.aspx">dataset</a>, float time,
        enum SpatialInterpolation spatial, enum TemporalInterpolation temporal,
        int count, float input[count][3], float output[count][6])
  </code><br /><br />
  <em>Example</em><br />
  <code>
  getPressureHessian(authtoken, dataset, time, FD4Lag4, temporalInterp, 10, points, result6);<br />
  for (p = 0; p &lt; 10; p++) {
  &nbsp;&nbsp;printf("%d: d2pdxdx=%f,d2pdxdy=%f,d2pdxdz=%f, d2pdydy=%f, d2pdydz=%f, d2pdzdz=%f\n", p,
  result6[p][0],  result6[p][1],  result6[p][2], result6[p][3],  result6[p][4],  result6[p][5]);<br />
  }
  </code>
  </p>

  <font face="Arial, Helvetica, sans-serif">
  <h4>And similarly for GetPosition, GetMagneticField, GetVectorPotential, GetMagneticFieldGradient, etc.</h4>
  </font>
    
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
