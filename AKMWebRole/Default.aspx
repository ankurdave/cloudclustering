<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="Default.aspx.cs" Inherits="AKMWebRole._Default" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
    <asp:Timer ID="UpdateTimer" runat="server" Enabled="False" Interval="2000" OnTick="UpdateTimer_Tick">
    </asp:Timer>
    <asp:ScriptManager ID="ScriptManager1" runat="server">
    </asp:ScriptManager>
    <asp:UpdatePanel ID="InputFields" runat="server">
        <ContentTemplate>
            <div class="visualblock">
                <h2>
                    Job Input</h2>
                <table>
                    <tr>
                        <td>
                            Points (n):
                        </td>
                        <td>
                            <asp:TextBox ID="N" runat="server"></asp:TextBox>
                        </td>
                    </tr>
                    <tr>
                        <td class="continuation">
                            or Points File:
                        </td>
                        <td>
                            <asp:FileUpload ID="PointsFile" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td class="continuation">
                            or Points Blob URI:
                        </td>
                        <td>
                            <asp:TextBox ID="PointsBlob" runat="server"></asp:TextBox>
                        </td>
                    </tr>
                    <tr>
                        <td>
                            Clusters (k):
                        </td>
                        <td>
                            <asp:TextBox ID="K" runat="server"></asp:TextBox>
                        </td>
                    </tr>
                    <tr>
                        <td>
                            Max iterations:
                        </td>
                        <td>
                            <asp:TextBox ID="MaxIterationCount" runat="server">0</asp:TextBox>&nbsp;(0 is unlimited)
                        </td>
                    </tr>
                </table>
                <p>
                    Send progress updates to email address:
                    <asp:TextBox ID="ProgressEmail" runat="server"></asp:TextBox>
                </p>
                <p>
                    <asp:Button ID="Run" runat="server" OnClick="Run_Click" Text="Run K-Means" /></p>
                <p>
                    <asp:Button ID="ClearBlobs" runat="server" OnClick="ClearBlobs_Click" Text="Clear Blobs" /></p>
                <p>
                    <asp:Button ID="Refresh" runat="server" Text="Refresh" OnClick="Refresh_Click" /></p>
                <p>
                    <strong>Status:</strong>
                    <asp:Label ID="Status" runat="server" Text="Click Run K-Means to see results."></asp:Label><asp:Label
                        ID="StatusProgress" runat="server" Text=""></asp:Label></p>
                <p>
                    <asp:HyperLink ID="DownloadLog" Text="Download Log" runat="server" Enabled="False"
                        Target="_blank"></asp:HyperLink></p>
                <p>
                    <strong>Points:</strong>
                    <asp:Label ID="PointsURI" runat="server" Text=""></asp:Label></p>
                <p>
                    <strong>Centroids:</strong>
                    <asp:Label ID="CentroidsURI" runat="server" Text=""></asp:Label></p>
            </div>
        </ContentTemplate>
        <Triggers>
            <asp:PostBackTrigger ControlID="Run" />
        </Triggers>
    </asp:UpdatePanel>
    <asp:UpdatePanel ID="Performance" runat="server">
        <ContentTemplate>
            <div class="visualblock long">
                <h2>
                    Performance</h2>
                <table>
                    <tr>
                        <th>
                            Method
                        </th>
                        <th>
                            Min time (s)
                        </th>
                        <th>
                            Average time (s)
                        </th>
                        <th>
                            Max time (s)
                        </th>
                        <th>
                            Count
                        </th>
                    </tr>
                    <asp:Literal ID="StatsSummary" runat="server"></asp:Literal></table>
                <table>
                    <tr>
                        <th>
                            Iteration
                        </th>
                        <th>
                            Method
                        </th>
                        <th>
                            Time taken (s)
                        </th>
                    </tr>
                    <asp:Literal ID="Stats" runat="server"></asp:Literal></table>
            </div>
            <div class="visualblock">
                <h2>
                    Visualization</h2>
                <div class="visualization">
                    <asp:Literal ID="Visualization" runat="server"></asp:Literal></div>
            </div>
        </ContentTemplate>
    </asp:UpdatePanel>
    <asp:UpdatePanel ID="WorkerList" runat="server">
        <ContentTemplate>
            <div class="visualblock long">
                <h2>
                    Workers</h2>
                <table>
                    <tr>
                        <th>
                            Worker ID
                        </th>
                    </tr>
                    <asp:Literal ID="Workers" runat="server"></asp:Literal></table>
            </div>
        </ContentTemplate>
    </asp:UpdatePanel>
</asp:Content>
