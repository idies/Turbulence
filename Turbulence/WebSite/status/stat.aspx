<%@ Page Language="C#" AutoEventWireup="true" Inherits="Website.status_monitor3" CodeBehind="stat.aspx.cs" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
	<title>JHTDB usage statistics</title>
	<style type="text/css">
		.errorbox {
			border: 2px dashed red;
			background-color: #fff0f0;
		}
	</style>
</head>
<body>

	<form id="form1" runat="server">

		<h1>JHTDB usage statistics</h1>
		<tr runat="server" id="startdate">
			<td>Start date (yyyy-mm-dd): </td>
			<td>
				<asp:TextBox ID="startdateobx" runat="server">  </asp:TextBox>
			</td>
		</tr>

		<tr runat="server" id="enddate">
			<td>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; End date (yyyy-mm-dd): </td>
			<td>
				<asp:TextBox ID="enddatebox" runat="server">  </asp:TextBox>
			</td>
		</tr>

		<td valign="top">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; By </td>
		<td>
			<asp:DropDownList ID="queryunit" runat="server">
				<asp:ListItem>day</asp:ListItem>
				<asp:ListItem>week</asp:ListItem>
				<asp:ListItem>month</asp:ListItem>
			</asp:DropDownList></td>

		<td valign="top">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; Easy reading: </td>
		<td>
			<asp:DropDownList ID="easyreading" runat="server">
				<asp:ListItem>False</asp:ListItem>
				<asp:ListItem>True</asp:ListItem>
			</asp:DropDownList></td>

		<td valign="top">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; Points or Requests: </td>
		<td>
			<asp:DropDownList ID="requestspoint" runat="server">
				<asp:ListItem>Points</asp:ListItem>
				<asp:ListItem>Requests</asp:ListItem>
			</asp:DropDownList></td>

		<td valign="top">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; Country or City: </td>
		<td>
			<asp:DropDownList ID="countrycity" runat="server">
				<asp:ListItem>Country</asp:ListItem>
				<asp:ListItem>City</asp:ListItem>
			</asp:DropDownList></td>

		<td valign="top">&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; Production or test website: </td>
		<td>
			<asp:DropDownList ID="prod_test" runat="server">
				<asp:ListItem>Production</asp:ListItem>
				<asp:ListItem>Test</asp:ListItem>
			</asp:DropDownList></td>

		<tr>
			<td colspan="3" align="center"></td>
		</tr>

		<p>
			<asp:Button ID="Go" runat="server" Text="Query" OnClick="point_Click" />
		</p>

		<%--<asp:Literal ID="errorheader" runat="server" Visible="false"><hr /><h1>Error Details</h1></asp:Literal>--%>
		<asp:Literal ID="errortext" runat="server"></asp:Literal>

		<p><i>Updated <%=DateTime.Now.ToString() %>.</i></p>
		<asp:DataGrid ID="dbstatusgrid" runat="server" CellPadding="4"
			ForeColor="#333333" GridLines="None">
			<FooterStyle BackColor="#1C5E55" Font-Bold="True" ForeColor="White" />
			<EditItemStyle BackColor="#7C6F57" />
			<SelectedItemStyle BackColor="#C5BBAF" Font-Bold="True" ForeColor="#333333" />
			<PagerStyle BackColor="#666666" ForeColor="White" HorizontalAlign="Center" />
			<AlternatingItemStyle BackColor="White" />
			<ItemStyle BackColor="#E3EAEB" />
			<HeaderStyle BackColor="#1C5E55" Font-Bold="True" ForeColor="White" />
		</asp:DataGrid>

		<h1>Completed points/requests by locations</h1>
		<asp:DataGrid ID="wsstatusgrid" runat="server" CellPadding="4"
			ForeColor="#333333" GridLines="None">
			<FooterStyle BackColor="#1C5E55" Font-Bold="True" ForeColor="White" />
			<EditItemStyle BackColor="#7C6F57" />
			<SelectedItemStyle BackColor="#C5BBAF" Font-Bold="True" ForeColor="#333333" />
			<PagerStyle BackColor="#666666" ForeColor="White" HorizontalAlign="Center" />
			<AlternatingItemStyle BackColor="White" />
			<ItemStyle BackColor="#E3EAEB" />
			<HeaderStyle BackColor="#1C5E55" Font-Bold="True" ForeColor="White" />
		</asp:DataGrid>

		<br />

	</form>

</body>
</html>
