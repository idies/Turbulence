<%@ Page Language="C#" AutoEventWireup="true" CodeFile="monitor.aspx.cs" Inherits="status_monitor" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Turbulence Database Monitor</title>
    <style type="text/css">
        .errorbox 
        {
        	border: 2px dashed red;
            background-color:#fff0f0;
        }
    </style>
</head>
<body>

<h1>Database Status</h1>
<p><i>Updated <%=DateTime.Now.ToString() %>.</i></p>
<asp:datagrid ID="dbstatusgrid" runat="server" CellPadding="4" 
    ForeColor="#333333" GridLines="None" >
    <FooterStyle BackColor="#1C5E55" Font-Bold="True" ForeColor="White" />
    <EditItemStyle BackColor="#7C6F57" />
    <SelectedItemStyle BackColor="#C5BBAF" Font-Bold="True" ForeColor="#333333" />
    <PagerStyle BackColor="#666666" ForeColor="White" HorizontalAlign="Center" />
    <AlternatingItemStyle BackColor="White" />
    <ItemStyle BackColor="#E3EAEB" />
    <HeaderStyle BackColor="#1C5E55" Font-Bold="True" ForeColor="White" />
</asp:datagrid>

<h1>Web Service Test</h1>
<asp:datagrid ID="wsstatusgrid" runat="server" CellPadding="4" 
    ForeColor="#333333" GridLines="None" >
    <FooterStyle BackColor="#1C5E55" Font-Bold="True" ForeColor="White" />
    <EditItemStyle BackColor="#7C6F57" />
    <SelectedItemStyle BackColor="#C5BBAF" Font-Bold="True" ForeColor="#333333" />
    <PagerStyle BackColor="#666666" ForeColor="White" HorizontalAlign="Center" />
    <AlternatingItemStyle BackColor="White" />
    <ItemStyle BackColor="#E3EAEB" />
    <HeaderStyle BackColor="#1C5E55" Font-Bold="True" ForeColor="White" />
</asp:datagrid>

<h1>Cutout Service Test</h1>
<asp:datagrid ID="cutoutstatusgrid" runat="server" CellPadding="4" 
    ForeColor="#333333" GridLines="None" >
    <FooterStyle BackColor="#1C5E55" Font-Bold="True" ForeColor="White" />
    <EditItemStyle BackColor="#7C6F57" />
    <SelectedItemStyle BackColor="#C5BBAF" Font-Bold="True" ForeColor="#333333" />
    <PagerStyle BackColor="#666666" ForeColor="White" HorizontalAlign="Center" />
    <AlternatingItemStyle BackColor="White" />
    <ItemStyle BackColor="#E3EAEB" />
    <HeaderStyle BackColor="#1C5E55" Font-Bold="True" ForeColor="White" />
</asp:datagrid>

<asp:Literal ID="errorheader" runat="server" Visible="false"><hr /><h1>Error Details</h1></asp:Literal>
<asp:Literal ID="errortext" runat="server"></asp:Literal>
<br />

</body>
</html>
