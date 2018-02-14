
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head><title>Johns Hopkins Turbulence Databases (JHTDB)</title>

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
<div id="leftcolumn"><div id="custom" style="margin-top: 120px">
   <p><img src="images/fig4-1.jpg" width="205"  /></p></div>
      
</div>
<div id="centercolumn">
      
 <h3><font face="Arial, Helvetica, sans-serif"> 
      Database
          Functions  </font></h3><strong>For detailed formulae used in the functions, <a href="docs/Database-functions.pdf" target="_blank">DOWNLOAD DOCUMENT</a> </strong>
        <table style="width: 100%;" class="newStyle2">
            <tr>
                <td class="newStyle2">
                    &nbsp;
                </td>
                <td align="center" class="newStyle2">Isotropic</td>
                <td align="center" class="newStyle2">MHD</td>
                <td align="center" class="newStyle2">Channel</td>
                <td align="center" class="newStyle2">Mixing</td>
                <td align="center" class="newStyle2">Isotropic4096</td>
                <td align="center" class="newStyle2">Rotstrat4096</td>
            </tr>
            <tr><td class="newStyle2">GetVelocity</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            </tr>
            <tr><td class="newStyle2">GetMagneticField</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
             <td align="center" class="newStyle2">&nbsp;</td>
            </tr>
            <tr><td class="newStyle2">GetVectorPotential</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            </tr>
            <tr><td class="newStyle2">GetPressure</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            </tr>

             <tr><td class="newStyle2">GetTemperature</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            </tr>

            <tr><td class="newStyle2">GetVelocityAndPressure</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            </tr>

             <tr><td class="newStyle2">GetVelocityAndTemperature</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            </tr>
            <tr><td class="newStyle2">GetForce</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            </tr>
            <tr><td class="newStyle2">GetVelocityGradient*</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
             <td align="center" class="newStyle2">&#10003;</td>
            </tr>
            <tr><td class="newStyle2">GetMagneticFieldGradient*</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            </tr>
            <tr><td class="newStyle2">GetVectorPotentialGradient*</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
                <td align="center" class="newStyle2">&nbsp;</td>
            </tr>
            <tr><td class="newStyle2">GetPressureGradient</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            </tr>
            <tr><td class="newStyle2">GetTemperatureGradient</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            </tr>
            <tr><td class="newStyle2">GetPressureHessian</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            </tr>
             <tr><td class="newStyle2">GetTemperatureHessian</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            </tr>
            <tr><td class="newStyle2">GetVelocityLaplacian</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
                <td align="center" class="newStyle2">&#10003;</td>
            </tr>
            <tr><td class="newStyle2">GetMagneticFieldLaplacian</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            </tr>
            <tr><td class="newStyle2">GetVectorPotentialLaplacian</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
                <td align="center" class="newStyle2">&nbsp;</td>
            </tr>
            <tr><td class="newStyle2">GetVelocityHessian</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            </tr>
            <tr><td class="newStyle2">GetMagneticFieldHessian</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
                <td align="center" class="newStyle2">&nbsp;</td>
            </tr>
            <tr><td class="newStyle2">GetVectorPotentialHessian</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
                <td align="center" class="newStyle2">&nbsp;</td>
            </tr>
            <tr><td class="newStyle2">GetPosition</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
                <td align="center" class="newStyle2">&nbsp;</td>
            </tr>
            <tr><td class="newStyle2">GetBoxFilter</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
                <td align="center" class="newStyle2">&#10003;</td>
            </tr>
            <tr><td class="newStyle2">GetBoxFilterSGSscalar</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
                <td align="center" class="newStyle2">&#10003;</td>
            </tr>
            <tr><td class="newStyle2">GetBoxFilterSGSvector</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
                <td align="center" class="newStyle2">&#10003;</td>
            </tr>
            <tr><td class="newStyle2">GetBoxFilterSGSsymtensor</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
                <td align="center" class="newStyle2">&#10003;</td>
            </tr>
            <tr><td class="newStyle2">GetBoxFilterSGStensor</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
                <td align="center" class="newStyle2">&nbsp;</td>
            </tr>
            <tr><td class="newStyle2">GetBoxFilterGradient</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
                <td align="center" class="newStyle2">&#10003;</td>
            </tr>
            <tr><td class="newStyle2">GetDensity</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
                <td align="center" class="newStyle2">&nbsp;</td>
            </tr>
            <tr><td class="newStyle2">GetDensityGradient</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
                <td align="center" class="newStyle2">&nbsp;</td>
            </tr>
            <tr><td class="newStyle2">GetDensityHessian</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            <td align="center" class="newStyle2">&nbsp;</td>
            </tr>
            <tr><td class="newStyle2">GetThreshold</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            </tr>
            <tr><td class="newStyle2">GetInvariant</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
            <td align="center" class="newStyle2">&#10003;</td>
                <td align="center" class="newStyle2">&#10003;</td>
            </tr>
        </table>
    <p><font face="Arial, Helvetica, sans-serif">
    
    <br />
    <br />
        <span class="style28"><em>*Note: The divergence-free condition in the simulation is enforced based on the spectral representation of the derivatives. 
    The JHTDB analysis tools for gradients are based on finite differencing of various orders. 
    Therefore, when evaluating the divergence using these spatially more localized derivative operators, a non-negligible error in the divergence is obtained, as expected.</em></span>
    </font></p>
    <h3><font face="Arial, Helvetica, sans-serif">Database Spatial
        Differentiation Options:</font></h3>
      <p><strong><em>Options for GetVelocityGradient, GetMagneticFieldGradient, GetVectorPotentialGradient, GetPressureGradient and GetDensityGradient</em></strong></p>
	    
      <p><em>FD4: 4th-order centered finite differencing (can be spatially interpolated)<br />
      </em><em>FD6: 6th-order centered finite differencing (without spatial interpolation)<br />
      </em><em>FD8: 8th-order centered finite differencing (without spatial interpolation) <br />
      </em><em>M1Q4: Splines with smoothness 1 (3rd order) over 4 data points. <br />
      </em><em>M2Q8: Splines with smoothness 2 (5th order) over 8 data points. <br />
      </em><em>M2Q14: Splines with smoothness 2 (5th order) over 14 data points. </em></p>
       
      <p><strong><em>Options for GetPressureHessian, GetVelocityLaplacian, GetMagneticFieldLaplacian, GetVectorPotentialLaplacian,
            GetVelocityHessian, GetMagneticFieldHessian and GetVectorPotentialHessian</em></strong> </p>
	      
      <p><em>FD4: 4th-order centered finite differencing (can be spatially interpolated)<br />
      </em><em>FD6: 6th-order centered finite differencing (without spatial interpolation)<br />
      </em><em>FD8: 8th-order centered finite differencing (without spatial interpolation) <br />
      </em><em>M2Q8: Splines with smoothness 2 (5th order) over 8 data points. <br />
      </em><em>M2Q14: Splines with smoothness 2 (5th order) over 14 data points.</em></p>
       
    <h3><font face="Arial, Helvetica, sans-serif">Database Spatial Interpolation
        Options:</font></h3>
    <p><strong><em>Interpolation options for GetVelocity, GetMagneticField, 
        GetVectorPotential, GetPressure, GetVelocityAndPressure, GetDensity and GetPosition</em></strong></p>
    
      <p><em>NoSInt: No Space interpolation (value at the datapoint closest to each coordinate value)<br />
        </em><em>Lag4: 4th-order Lagrange Polynomial interpolation along each spatial direction|<br />
        </em><em>Lag6: 6th-order Lagrange Polynomial interpolation along each spatial direction<br />
        </em><em>Lag8: 8th-order Lagrange Polynomial interpolation along each spatial direction <br />
        </em><em>M1Q4: Splines with smoothness 1 (3rd order) over 4 data points. <br />
        </em><em>M2Q8: Splines with smoothness 2 (5th order) over 8 data points. <br />
        </em><em>M2Q14: Splines with smoothness 2 (5th order) over 14 data points.</em></p>
          
    <p><strong><em>Interpolation options for GetVelocityGradient, 
        GetMagneticFieldGradient, GetVectorPotentialGradient, GetPressureGradient,
        GetDensityGradeint,
        GetPressureHessian, GetVelocityLaplacian, GetMagneticFieldLaplacian, 
        GetVectorPotentialLaplacian, GetVelocityHessian, GetMagneticFieldHessian, 
        GetVectorPotentialHessian and GetDensityHessian</em></strong></p>
    
      <p><em>FD4NoInt: No interpolation (value of the 4th order finite-difference
        evaluations at the datapoint closest to each coordinate value is returned)<br />
        FD6NoInt: No interpolation (value of the 6th order finite-difference
        evaluations at the datapoint closest to each coordinate value is returned)<br />
        FD8NoInt: No interpolation (value of the 8th order finite-difference
        evaluations at the datapoint closest to each coordinate value is returned) </em></p>
      <p><em>FD4Lag4: 4th-order Lagrange Polynomial interpolation in each direction,
          of the 4th-order finite difference values on the grid.<br />
        </em><em>M2Q8: Splines with smoothness 2 (5th order) over 8 data points. <br />
        </em><em>M2Q14: Splines with smoothness 2 (5th order) over 14 data points.</em></p>

        
    <h3><font face="Arial, Helvetica, sans-serif">Database Time Interpolation
      Options:</font></h3>
    <p><strong><em>For all variables and derivatives listed above, the two
      options are:</em></strong></p>
    
      <p><em>NoTInt: No interpolation (the
            value at the closest stored time will be returned).</em></p>
      <p><em>PCHIP: Piecewise Cubic Hermite Interpolation
          Polynomial method is used, in which
            the value from the two nearest
            time points is interpolated at time t using Cubic Hermite Interpolation
          Polynomial, with centered finite difference evaluation of the end-point
            time derivatives (i.e. a total of four temporal points are used).</em></p>
       
    <h3><font face="Arial, Helvetica, sans-serif">Database Spatial Filtering Options:</font></h3>
    <p><strong><em>Options for GetBoxFilter, GetBoxFilterSGSscalar, GetBoxFilterSGSvector, GetBoxFilterSGSsymtensor, 
        GetBoxFilterSGStensor and GetBoxFilterGradient:</em></strong></p>
    
      <p><em>Field: The field or combination of fields, which should be filtered. <br />For the GetBoxFilter, GetBoxFilterGradient and
            GetBoxFilterSGSsymtensor the valid options are --
            "velocity" or "u", "magnetic" or "b", "potential" or "a", "pressure" or "p", density or "d". 
            <br />For the GetBoxFilterSGSscalar, GetBoxFilterSGSvector, GetBoxFilterSGStensor two fields should be specified 
            using single characters, such as "pd", "up" or "ub". 
            <br />GetBoxFilterSGSscalar operates on two scalar fields, e.g. "pp" or "pd".
            <br />GetBoxFilterSGSvector operates on a vector and a scalar field, e.g. "up" or "bp".
            <br />GetBoxFilterSGStensor operates on two vector fields, e.g. "ub" or "ba".</em></p>
      <p><em>Filter width (odd multiple of the grid resolution): Size of the box filter to be applied along each spatial direction. 
                NOTE: This value will be rounded to the nearest odd multiple of the grid resolution.</em></p>
     
    <p><strong><em>Additional option for GetBoxFilterGradient:</em></strong></p>
    
      <p><em>Spacing (multiple of the grid resolution): The finite differencing spacing that should be used.</em></p>
      
    <h3><font face="Arial, Helvetica, sans-serif">Database Threshold Options:</font></h3>
    <p><strong><em>Options for GetThreshold:</em></strong></p>
    
      <p><em>Field (one of vorticity, Q, velocity, magnetic, potential, pressure, density): The field, which should be thresholded.</em></p>
      <p><em>X, Y, Z, Xwidth, Ywidth, Zwidth: The spatial region to be examined. X, Y and Z specify the grid node to be used as the
                bottom left corner of a box, and Xwidth, Ywidth and Zwidth specify the box's dimensions. 
                NOTE: These options are similar to those used for the GetRawData functions and the cutout service.</em></p>
      <p><em>Returns: All grid locations from the specified region, at which the norm of the given field is above the given threshold, and  
                the norm of the field at each of these locations.</em></p>
     
    <h3>&nbsp; </h3>
    
    
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
