<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="Default.aspx.cs" Inherits="AKMWebRole._Default" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
    <p>
        <asp:ScriptManager ID="ScriptManager1" runat="server">
        </asp:ScriptManager>
    </p>
    <asp:UpdatePanel ID="UpdatePanel1" runat="server" UpdateMode="Conditional">
        <ContentTemplate>
            <p>
                Points (n):
                <asp:TextBox ID="N" runat="server"></asp:TextBox>
                &nbsp;or Points File:
                <asp:FileUpload ID="PointsFile" runat="server" />
                &nbsp;or Points Blob URI:
                <asp:TextBox ID="PointsBlob" runat="server"></asp:TextBox>
                <br />
                Clusters (k):
                <asp:TextBox ID="K" runat="server"></asp:TextBox>
                <br />
                Machines (m):
                <asp:TextBox ID="M" runat="server"></asp:TextBox>
                <br />
                Max iterations:
                <asp:TextBox ID="MaxIterationCount" runat="server">0</asp:TextBox>
                &nbsp;(0 is unlimited)</p>
            
            <p><asp:Button ID="Run" runat="server" onclick="Run_Click" Text="Run K-Means" /></p>
            <p><asp:Button ID="ClearBlobs" runat="server" onclick="ClearBlobs_Click" Text="Clear Blobs" /></p>

            <div style="float:right">
                <p><strong>Status:</strong> <asp:Label ID="Status" runat="server" Text="Click Run K-Means to see results."></asp:Label><asp:Label ID="StatusProgress" runat="server" Text=""></asp:Label></p>
                <p><asp:HyperLink ID="DownloadLog" Text="Download Log" runat="server" 
                        Enabled="False" Target="_blank"></asp:HyperLink></p>
                <table><tr><th>Method</th><th>Min time (s)</th><th>Average time (s)</th><th>Max time (s)</th><th>Count</th></tr><asp:Literal ID="StatsSummary" runat="server"></asp:Literal></table>
                <table><tr><th>Iteration</th><th>Method</th><th>Time taken (s)</th></tr><asp:Literal ID="Stats" runat="server"></asp:Literal></table>
            </div>

            <div class="visualization"><asp:Literal ID="Visualization" runat="server"></asp:Literal></div>
            
            <p><strong>Points:</strong> <asp:Label ID="PointsURI" runat="server" Text=""></asp:Label></p>
            <table><asp:Label ID="Points" runat="server" Text=""></asp:Label></table>

            <p><strong>Centroids:</strong> <asp:Label ID="CentroidsURI" runat="server" Text=""></asp:Label></p>
            <table><asp:Label ID="Centroids" runat="server" Text=""></asp:Label></table>

            <asp:Timer ID="UpdateTimer" runat="server" Enabled="False" Interval="1000" 
                ontick="UpdateTimer_Tick">
            </asp:Timer>
        </ContentTemplate>
        <Triggers>
            <asp:PostBackTrigger ControlID="Run" />
        </Triggers>
    </asp:UpdatePanel>
    <p>
        &nbsp;</p>
</asp:Content>
