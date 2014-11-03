
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
<div id="title">
<p >Johns Hopkins Turbulence Databases</p>
<p >turbulence.pha.jhu.edu</p>
</div>
<!--#include file="navbar.htm" -->
<div id="leftcolumn">
   <p><img src="images/isotropic.jpg" width="205"  /></p>
   <div style="text-align: center">Forced Isotropic Turbulence</div><br />
      <p><img src="images/mhd.jpg" width="205"/></p>
      <div style="text-align: center">Forced MHD Turbulence</div><br />
      <p><img src="images/channel.jpg" width="205"/></p>
      <div style="text-align: center">Channel Flow</div><br />
      <p><img src="images/turb3d.png" width="205"/></p>
      <div style="text-align: center">HB Driven Turbulence</div>
</div>
<div id="centercolumn">
<blockquote>
        <span class="style20"><font size="+2">Dataset
          descriptions</font></span></p>
        <p align="left" class="style20">1. Forced isotropic turbulence:</p>
        <p style="margin-left:25px;">
            <font face="Arial, Helvetica, sans-serif">Simulation data provenance: JHU DNS code 
            (see <a href="docs/README-isotropic.pdf" target="_blank">README-isotropic</a> for more details).</font></p>
      <ul>
        <li><font face="Arial, Helvetica, sans-serif">Direct numerical simulation (DNS) using 1,024<sup>3</sup> nodes.</font></li>

        <li><font face="Arial, Helvetica, sans-serif">Navier-Stokes is solved using pseudo-spectral
          method.</font></li>
        <li><font face="Arial, Helvetica, sans-serif"> Energy is injected by keeping constant the total
        energy in shells such that |k| is less or equal to 2. </font></li>
        <li><font face="Arial, Helvetica, sans-serif"> After the simulation has reached a statistical
          stationary state, 1,024 frames of data with 3 velocity components and
          pressure are stored in the database. Extra time frames at the beginning
          and at the end have been added to be used for temporal-interpolations. </font></li>
        <li><font face="Arial, Helvetica, sans-serif">The Taylor-scale Reynolds
            number fluctuates around R<sub>&lambda;</sub>~
        433</~
        433.</font></li>
        <li><font face="Arial, Helvetica, sans-serif">There is one dataset (&quot;coarse&quot;)
            with 1024 timesteps available, for time t between 0 and 2.048 (the
            frames are stored at every 10 time-steps of the DNS). Intermediate
            times can be queried using temporal-interpolation. </font></li>
        <li><font face="Arial, Helvetica, sans-serif">There is another dataset (&quot;fine&quot;) that 
            stores every single time-step of the DNS, for testing purposes. Times available 
            are for t between 0.0002 and 0.0198). </font></li>
        <li><font face="Arial, Helvetica, sans-serif">A table with the time history
            of the total kinetic energy and Taylor-scale Reynolds number as function
            of time can be downloaded from this <a href="ener_Re_time.txt" target="_blank">text
            file</a>. </font></li>
      </ul>      
        <p align="left" class="style20">
            2. Forced MHD turbulence:</p>
        <p style="margin-left:25px;">
            <font face="Arial, Helvetica, sans-serif">Simulation data provenance: JHU DNS code 
            (see <a href="docs/README-MHD.pdf" target="_blank">README-MHD</a> for more details).</font></p>
      <ul>
        <li><font face="Arial, Helvetica, sans-serif">Direct numerical simulation (DNS) using 1,024<sup>3</sup> nodes.</font></li>

        <li><font face="Arial, Helvetica, sans-serif">Incompressible MHD equations are solved using pseudo-spectral
          method.</font></li>
        <li><font face="Arial, Helvetica, sans-serif"> Energy is injected by 
            using a Taylor-Green flow stirring force. </font></li>
        <li><font face="Arial, Helvetica, sans-serif"> After the simulation has reached a statistical
          stationary state, 1,024 frames of data with 3 velocity components,
          pressure, 3 magnetic field and magnetic vector potential components are stored in the database. </font></li>
        <li><font face="Arial, Helvetica, sans-serif">The Taylor-scale Reynolds
            number fluctuates around R<sub>&lambda;</sub>~
            186.</font></li>
        <li><font face="Arial, Helvetica, sans-serif">1024 timesteps are available, 
            for time t between 0 and 2.56 (the
            frames are stored at every 10 time-steps of the DNS). Intermediate
            times can be queried using temporal-interpolation. </font></li>
        <li><font face="Arial, Helvetica, sans-serif">A table with the 
            spectra
            of the velocity, magnetic field, Elsasser variables, cross-helicity and magnetic 
            helicity can be downloaded from this </font><font face="Arial, Helvetica, sans-serif" style="text-decoration: underline"> <a href="Spectra-MHD.txt" target="_blank">text
            file</a></font><font face="Arial, Helvetica, sans-serif">. </font></li>
        <li><font face="Arial, Helvetica, sans-serif">A table with the time histories of energy and dissipation, both kinetic and magnetic, as 
            well as of magnetic and cross helicity,  can be downloaded from this </font><font face="Arial, Helvetica, sans-serif" style="text-decoration: underline"> <a href="TimeSeries.txt" target="_blank">text
            file</a></font><font face="Arial, Helvetica, sans-serif">. </font></li>
      </ul>      
        <p align="left" class="style20">
            3. Channel flow:</p>
        <p style="margin-left:25px;">
            <font face="Arial, Helvetica, sans-serif">Simulation data provenance: Collaboration of UT Texas and JHU, using the UT Texas DNS code 
            (see <a href="docs/README-CHANNEL.pdf" target="_blank">README-CHANNEL</a> for more details).</font></p>
      <ul>
        <li><font face="Arial, Helvetica, sans-serif">Direct numerical simulation (DNS) of channel flow in a domain of size 8&pi; x 2  x 3&pi; , using 2048 x 512 x 1536 nodes.</font></li>
        <li><font face="Arial, Helvetica, sans-serif">Incompressible Navier-Stokes equations 
            are solved using the pseudo-spectral (Fourier-Galerkin) method in wall-parallel 
            (x, z) planes, and the 7th-order B-spline collocation method in the wall-normal 
            (y) direction.</font></li>
        <li><font face="Arial, Helvetica, sans-serif">Simulation is run and equilibrated using prescribed bulk velocity=1, then switched to imposed pressure gradient 
        (dP/dx = 0.0025) and further equilibrated.</font></li>
        <li><font face="Arial, Helvetica, sans-serif">After the simulation has reached a (nearly) statistical stationary state, 2,000 frames of data with 
        3 velocity components and pressure are stored in the database. The frames are stored at every 5 time-steps of the DNS. 
        This corresponds to about 1/2 of a channel flow-through time. Intermediate times can be queried using temporal-interpolation.</font></li>
        <li><font face="Arial, Helvetica, sans-serif">The friction velocity is u<sub>&tau;</sub> = 0.0499.</font></li>
        <li><font face="Arial, Helvetica, sans-serif">The viscosity is &nu; = 5 x 10<sup>-5</sup>.</font></li>
        <li><font face="Arial, Helvetica, sans-serif">The friction velocity Reynolds number is Re<sub>&tau;</sub> ~ 1000.</font></li>
        <li><font face="Arial, Helvetica, sans-serif">The y-locations of the grid points in the vertical direction can be downloaded from this <a href="docs/channel/y.txt" target="_blank">text file</a>; the corresponding B-spline knot locations can be obtained from this <a href="docs/channel/y-knots.txt" target="_blank">text file</a>.</font></li>
        <!--<li><font face="Arial, Helvetica, sans-serif">More details about the DNS are provided in the accompanying <a href="docs/README-CHANNEL.pdf">README-CHANNEL</a> document.</font></li>-->
        <li><font face="Arial, Helvetica, sans-serif">A table with the time history of friction velocity Reynolds number can be downloaded from this <a href="docs/channel/re-tau.txt" target="_blank">text file</a>.</font></li>
        <li><font face="Arial, Helvetica, sans-serif">A table with the vertical profiles of mean velocity, Reynolds shear stresses, viscous stress, normal stress, 
        mean pressure, pressure variance and pressure-velocity covariance in viscous units, can be downloaded from this <a href="docs/channel/profiles.txt" target="_blank">text file</a>.</font></li>
        <li><font face="Arial, Helvetica, sans-serif">Files with tables of the streamwise (k<sub>x</sub>) spectra of u, v, w, p at various heights can be downloaded for the following y+ values: 
        <a href="docs/channel/spectra-kx-yplus-10.11.txt" target="_blank">10.11</a>, <a href="docs/channel/spectra-kx-yplus-29.89.txt" target="_blank">29.89</a>, <a href="docs/channel/spectra-kx-yplus-99.75.txt" target="_blank">99.75</a>, <a href="docs/channel/spectra-kx-yplus-371.6.txt" target="_blank">371.6</a>, and <a href="docs/channel/spectra-kx-yplus-999.7.txt" target="_blank">999.7</a>.</font></li>
        <li><font face="Arial, Helvetica, sans-serif">Files with tables of the spanwise (k<sub>z</sub>) spectra of u, v, w, p at various heights can be downloaded for the following y+ values: 
        <a href="docs/channel/spectra-kz-yplus-10.11.txt" target="_blank">10.11</a>, <a href="docs/channel/spectra-kz-yplus-29.89.txt" target="_blank">29.89</a>, <a href="docs/channel/spectra-kz-yplus-99.75.txt" target="_blank">99.75</a>, <a href="docs/channel/spectra-kz-yplus-371.6.txt" target="_blank">371.6</a>, and <a href="docs/channel/spectra-kz-yplus-999.7.txt" target="_blank">999.7</a>.</font></li>
        <li><font face="Arial, Helvetica, sans-serif"> GetPosition and Filtering functions not yet implemented for the channel flow dataset.</font></li>
      </ul>
        <p align="left" class="style20">
            4. Homogeneous buoyancy driven turbulence:</p>
        <p style="margin-left:25px;">
            <font face="Arial, Helvetica, sans-serif">Simulation data provenance: Los Alamos National Laboratory using the LANL DNS code
            (see <a href="docs/README-HBDT.pdf" target="_blank">README-HBDT</a> for more details).</font></p>
      <ul>
        <li><font face="Arial, Helvetica, sans-serif">Direct Numerical Simulation (DNS) of homogeneous buoyancy driven turbulence in a domain size 2&pi; x 2&pi;  x 2&pi;, 
            using 1,024<sup>3</sup> nodes.</font></li>
        <li><font face="Arial, Helvetica, sans-serif">The incompressible two-fluid Navier-Stokes equations are solved using a pseudo-spectral method.
            These equations represent the large speed of sound limit for the fully compressible 
            Navier-Stokes equations with two fluids having different molar masses and obeying the ideal-gas
            equation of state.</font></li>
        <li><font face="Arial, Helvetica, sans-serif">The domain is triply periodic and the homogeneity of the fluctuating 
            quantities is ensured by imposing mean zero velocity and constant mean pressure
            gradient. These conditions are similar to those encountered in the interior
            of the Rayleigh-Taylor mixing layer.</font></li>
        <li><font face="Arial, Helvetica, sans-serif">The two fluids are initialized as random blobs, with a characteristic 
            size of about 1/5 of the domain. The flow starts from rest, in the presence of 
            a constant gravitational acceleration, and the fluids start moving in opposite
            direction due to differential buoyancy forces. Turbulence fluctuations are 
            generated and the turbulent kinetic energy increases; however, as the fluids
            become molecularly mixed, the buoyancy forces decrease and at some point the 
            turbulence starts decaying.</font></li>
        <li><font face="Arial, Helvetica, sans-serif">Due to the change in specific volume during mixing, the divergence of 
            velocity is not zero, but related to the density field. This leads to a 
            variable coefficient Poisson equation for pressure, which is decomposed in two 
            parts, for the gradient and curl components of &nabla;<i>p</i>/&rho;. These are 
            solved using direct solvers to ensure mass conservation and baroclinic 
            generation of vorticity to machine precision.</font></li>
        <li><font face="Arial, Helvetica, sans-serif">The 1015 time frames stored in the database cover both the buoyancy driven increase in
            turbulence intensity as well as the buoyancy mediated turbulence decay. Each
            time frame contains the density, 3 velocity components, and pressure at the grid 
            points. The frames are stored at a constant time interval of 0.04, which 
            represents between 20 to 50 DNS time steps. </font></li>            
        <li><font face="Arial, Helvetica, sans-serif">Schmidt number: 1.0</font></li>
        <li><font face="Arial, Helvetica, sans-serif">Froude number: 1.0</font></li>
        <li><font face="Arial, Helvetica, sans-serif">Atwood number: 0.05</font></li>
        <li><font face="Arial, Helvetica, sans-serif">Maximum turbulent Reynolds number: Re<sub>&tau;</sub> ~ 17,765.</font></li>
        <li><font face="Arial, Helvetica, sans-serif">Minimum turbulent Reynolds number during decay phase: Re<sub>&tau;</sub> ~ 1,595.</font></li>
        <li><font face="Arial, Helvetica, sans-serif">A file with the time history of the Favre turbulent kinetic energy, </br>
            k<sup>&tilde;</sup> = &lt;&rho;u<sub>i</sub><sup>''</sup>u<sub>i</sub><sup>''</sup>&gt; &frasl; 2&lt;&rho;&gt;, Reynolds stresses,
            R<sub>ii</sub> = &lt;&rho;u<sub>i</sub><sup>''</sup>u<sub>i</sub><sup>''</sup>&gt; (no summation over i), vertical mass flux,
            a<sub>v</sub> = &lt;&rho; u<sub>1</sub><sup>'</sup>&gt; &frasl; &lt;&rho;&gt;, turbulent Reynolds number, 
            Re<sub>t</sub> = k<sup>&tilde;2</sup> &frasl; &nu;&epsilon;, eddy turnover time,
            &tau; = k<sup>&tilde;</sup> &frasl; &epsilon;, kinetic energy dissipation, &epsilon;,
            density variance and density-specific volume correlation can be found <a href="docs/hbdt/data1.txt" target="_blank">here</a>.</font></li>
        <li><font face="Arial, Helvetica, sans-serif">Files with tables of the power spectra of density, 3 velocity components,
            and mass flux can be downloaded at the following times: <a href="docs/hbdt/spectra1.txt" target="_blank">6.56</a>, <a href="docs/hbdt/spectra2.txt" target="_blank">11.4</a>, 
            <a href="docs/hbdt/spectra3.txt" target="_blank">15.0</a>, <a href="docs/hbdt/spectra4.txt" target="_blank">20.0</a>,
            <a href="docs/hbdt/spectra5.txt" target="_blank">30.0</a>, and <a href="docs/hbdt/spectra6.txt" target="_blank">40.0</a>.</font></li>
        <li><font face="Arial, Helvetica, sans-serif">Files with tables of the density PDF can be downloaded for the following
            times: <a href="docs/hbdt/pdf1.txt" target="_blank">0.0</a>, <a href="docs/hbdt/pdf2.txt" target="_blank">6.56</a>, <a href="docs/hbdt/pdf3.txt" target="_blank">11.4</a>, 
            <a href="docs/hbdt/pdf4.txt" target="_blank">15.0</a>, <a href="docs/hbdt/pdf5.txt" target="_blank">20.0</a>,
            <a href="docs/hbdt/pdf6.txt" target="_blank">30.0</a>, and <a href="docs/hbdt/pdf7.txt" target="_blank">40.0</a>.</font></li>
      </ul>      
      
    
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
