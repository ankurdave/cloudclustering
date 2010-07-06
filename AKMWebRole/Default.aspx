<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="Default.aspx.cs" Inherits="AKMWebRole._Default" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
    <p>
        Points (n):
        <asp:TextBox ID="N" runat="server"></asp:TextBox>
        <br />
        Clusters (k):
        <asp:TextBox ID="K" runat="server"></asp:TextBox>
        <br />
        Machines (m):
        <asp:TextBox ID="M" runat="server"></asp:TextBox>
    </p>
    <p>
        <asp:Button ID="Run" runat="server" Text="Run K-Means" onclick="Run_Click" />
    </p>
</asp:Content>
