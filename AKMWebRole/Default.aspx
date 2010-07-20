<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="Default.aspx.cs" Inherits="AKMWebRole._Default" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
    <p>
        <asp:ScriptManager ID="ScriptManager1" runat="server">
        </asp:ScriptManager>
    </p>
    <asp:UpdatePanel ID="UpdatePanel1" runat="server">
        <ContentTemplate>
            <p>
                Points (n):
                <asp:TextBox ID="N" runat="server"></asp:TextBox>
                <br />
                Clusters (k):
                <asp:TextBox ID="K" runat="server"></asp:TextBox>
                <br />
                Machines (m):
                <asp:TextBox ID="M" runat="server"></asp:TextBox>
                <br />
                Max iterations:
                <asp:TextBox ID="MaxIterationCount" runat="server"></asp:TextBox>
            </p>
            
            <p><asp:Button ID="Run" runat="server" onclick="Run_Click" Text="Run K-Means" /></p>

            <div style="float:right">
                <p><strong>Status:</strong> <asp:Label ID="Status" runat="server" Text="Click Run K-Means to see results."></asp:Label></p>
                <table><tr><th>Method</th><th>Min time (s)</th><th>Average time (s)</th><th>Max time (s)</th><th>Count</th></tr><asp:Literal ID="StatsSummary" runat="server"></asp:Literal></table>
                <table><tr><th>Iteration</th><th>Method</th><th>Time taken (s)</th></tr><asp:Literal ID="Stats" runat="server"></asp:Literal></table>
            </div>

            <div class="visualization"><asp:Literal ID="Visualization" runat="server"></asp:Literal></div>
            
            <p><strong>Points:</strong></p>
            <table><asp:Label ID="Points" runat="server" Text=""></asp:Label></table>

            <p><strong>Centroids:</strong></p>
            <table><asp:Label ID="Centroids" runat="server" Text=""></asp:Label></table>

            <asp:Timer ID="UpdateTimer" runat="server" Enabled="False" Interval="1000" 
                ontick="UpdateTimer_Tick">
            </asp:Timer>
        </ContentTemplate>
    </asp:UpdatePanel>
    <p>
        &nbsp;</p>
</asp:Content>
