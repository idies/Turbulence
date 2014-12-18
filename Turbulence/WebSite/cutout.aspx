 <%@ Page Language="C#" AutoEventWireup="true" Inherits="Website.cutout" Codebehind="cutout.aspx.cs" %>
<html xmlns="http://www.w3.org/1999/xhtml" >

<head id="Head1" runat="server"><title>Johns Hopkins Turbulence Databases (JHTDB)</title>

<link href="bootstrap-3.2.0-dist/css/bootstrap.min.css" rel="stylesheet" type="text/css" />
<script src="https://ajax.googleapis.com/ajax/libs/jquery/1.11.1/jquery.min.js"></script>
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
      
      <h2 class="titletext">JHTDB Cutout Service</h2>
     
    <form id="cutout" runat="server">
    <div style="height: 500px; width: 1050px">
        <hr />
 
   <table>
    <tr><td valign="top" class="style2">
      Authorization Token: [<a href="help/authtoken.aspx" class="note">?</a>]</td>
      <td class="style4">      
            <asp:TextBox ID="authTokenBox" runat="server" Width="256px"></asp:TextBox> 
        </td></tr>        
    <tr>
    <td valign="top" class="style2">
        Dataset: [<a href="/datasets.aspx" class="note">?</a>]</td>
    <td class="style4">
        <asp:DropDownList ID="dataset" runat="server" AutoPostBack="True"  >            
            <asp:ListItem>isotropic1024coarse</asp:ListItem>
            <asp:ListItem>isotropic1024fine</asp:ListItem>
            <asp:ListItem>mhd1024</asp:ListItem>
            <asp:ListItem>channel</asp:ListItem>
            <asp:ListItem>mixing</asp:ListItem>
            <asp:ListItem>mhddev</asp:ListItem>
        </asp:DropDownList>
        (dt: <asp:Literal ID="dt" runat="server" Visible="true"></asp:Literal>)
        </td>
        </tr>
    <tr>
        <td class="style2">Fields: [<a href="/datasets.aspx" class="note">?</a>]</td>
        <td class="style4">
            <asp:CheckBox ID="velocity" text="Velocity" runat="server" />
            <asp:CheckBox ID="pressure" text="Pressure" runat="server" />
            <asp:CheckBox ID="magnetic" text="MagneticField" visible="false" runat="server" />
            <asp:CheckBox ID="potential" text="VectorPotential" visible="false" runat="server" />
            <asp:CheckBox ID="density" text="Density" visible="false" runat="server" />
        </td>
    </tr>
    </table>

        <br />

        <table>         
        <tr>
            <td colspan="6"> Specify the cutout parameters below. Select the starting index for the cutout and the size in each dimension. Optionally, 
                a step or stride can be specified to obtain every "other" data point. If a step size is specified the data can olso optionally be filtered
                using a box filter (except in the case of the channel flow dataset). To get a filtered cutout specify the filter width for the box filter
                in units of grid points. <br /></td>
        </tr>
        <tr>
            <td ><br /></td>
        </tr>
        <tr>
            <td class="style7" valign="top" colspan="2">Starting coordinate <br /> index for cutout: [<a href="/datasets.aspx" class="note">?</a>] </td>
            <td class="style8" valign="top" colspan="2"> &nbsp;Size of cutout: [<a href="/datasets.aspx" class="note">?</a>] <br />
                <span class="style1"> &nbsp;(end index minus start index + 1)</span> </td>            
            <td class="style35" valign="top" colspan="2">
                <asp:CheckBox ID="step_checkbox" runat="server" AutoPostBack="True"
                    oncheckedchanged="step_checkbox_CheckedChanged" />
                <span ID="stepCell" runat="server" title="Optionally select a step size. If omitted every point in the range will be returned."> &nbsp;
                    <span class="style39">Step (optional) :</span>
                </span> 
            </td>
        </tr>
        <tr>
            <td class="style36">m<sub>t</sub> <asp:Literal ID="timestart_range" runat="server" Visible="true"></asp:Literal>: </td>
            <td class="style38"> <asp:TextBox ID="timestart" runat="server" Width="40px">0</asp:TextBox><br /> </td>
            <td class="style36"> &nbsp;M<sub>t</sub> <asp:Literal ID="timeend_range" runat="server" Visible="true"></asp:Literal>: </td>
            <td class="style38"> <asp:TextBox ID="timeend" runat="server" Width="40px">0</asp:TextBox><br /> </td>
            <td class="style40"> <asp:Label ID="timeStepLabel" runat="server" Visible="false"> s<sub>t</sub>: </asp:Label> </td>
            <td class="style38"> <asp:TextBox ID="timeStepSize" runat="server" Width="40px" Visible="false">1</asp:TextBox> </td>
            <td class="style9" rowspan="4">  
                <asp:Literal ID="channel_grid_note" runat="server" Visible="false"></asp:Literal> </td>
         </tr>
         <tr>
            <td class="style36"> i<sub>x</sub> <asp:Literal ID="x_range" runat="server" Visible="true"></asp:Literal>:  </td>
            <td class="style38"> <asp:TextBox ID="x" runat="server" Width="40px">0</asp:TextBox><br /> </td>
            <td class="style36"> &nbsp;N<sub>x</sub> <asp:Literal ID="xend_range" runat="server" Visible="true"></asp:Literal>:  </td>
            <td class="style38"> <asp:TextBox ID="xEnd" runat="server" Width="40px">0</asp:TextBox> <br /> </td>
            <td class="style40"> <asp:Label ID="xStepLabel" runat="server" Visible="false"> s<sub>x</sub>: </asp:Label> </td>
            <td class="style38"> <asp:TextBox  ID="xStepSize" runat="server" Width="40px" Visible="false">1</asp:TextBox> </td>
         </tr>
         <tr>
            <td class="style36"> j<sub>y</sub> <asp:Literal ID="y_range" runat="server" Visible="true"></asp:Literal>:  </td>
            <td class="style38"> <asp:TextBox ID="y" runat="server" Width="40px">0</asp:TextBox><br /> </td>
            <td class="style36"> &nbsp;N<sub>y</sub> <asp:Literal ID="yend_range" runat="server" Visible="true"></asp:Literal>:  </td>
            <td class="style38"> <asp:TextBox ID="yEnd" runat="server" Width="40px">0</asp:TextBox><br /> </td>
            <td class="style40"> <asp:Label ID="yStepLabel" runat="server" Visible="false"> s<sub>y</sub>: </asp:Label> </td>
            <td class="style38"> <asp:TextBox  ID="yStepSize" runat="server" Width="40px" Visible="false">1</asp:TextBox> </td>
         </tr>
         <tr>
            <td class="style36"> k<sub>z</sub> <asp:Literal ID="z_range" runat="server" Visible="true"></asp:Literal>:  </td>
            <td class="style38"> <asp:TextBox ID="z" runat="server" Width="40px">0</asp:TextBox> </td>
            <td class="style36"> &nbsp;N<sub>z</sub> <asp:Literal ID="zend_range" runat="server" Visible="true"></asp:Literal>:  </td>
            <td class="style38"> <asp:TextBox ID="zEnd" runat="server" Width="40px">0</asp:TextBox> </td>
            <td class="style40"> <asp:Label ID="zStepLabel" runat="server" Visible="false"> s<sub>z</sub>: </asp:Label> </td>
            <td class="style38"> <asp:TextBox  ID="zStepSize" runat="server" Width="40px" Visible="false">1</asp:TextBox> </td>
         </tr>     
        <tr>
            <td> <br /> </td>
        </tr>   
        <tr>
            <td class="style35" valign="top" colspan="5">
                <asp:CheckBox ID="filterwidth_checkbox" runat="server" AutoPostBack="True"
                    oncheckedchanged="filterwidth_checkbox_CheckedChanged" />
                <span ID="filterwidth_cell" runat="server" title="Optionally select a filter width (in units of grid points). If omitted no filtering will be performed and the strided data will be returned."> &nbsp;
                    <span class="style39">Filter width (optional) :</span>
                </span> 
                <asp:TextBox  ID="filterWidth" runat="server" Width="40px" Visible="false">1</asp:TextBox> </td>
        </tr>
        <tr>
            <td> <br /> </td>
        </tr>
        <tr>
            <td class="style36">
                <asp:Button ID="Button1" runat="server" Text="Submit" />
            </td>
        </tr>
        </table>
        <br />
        
        <asp:Literal ID="dlsize" runat="server" Visible="true"></asp:Literal>
 
        <br />
        
        <asp:Literal ID="dllink" runat="server" Visible="true"></asp:Literal>

        <br /><br />
        <code><div id="message"></div></code>

    </div>
    </form>
    
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
