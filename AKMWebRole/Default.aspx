<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="Default.aspx.cs" Inherits="AKMWebRole._Default" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
    <asp:Timer ID="UpdateTimer" runat="server" Enabled="False" Interval="2000" OnTick="UpdateTimer_Tick">
    </asp:Timer>
    <asp:ScriptManager ID="ScriptManager1" runat="server">
    </asp:ScriptManager>
    <table>
        <tr>
            <td valign="top">
                <div class="visualblock">
                    <h2>
                        Job Input</h2>
                    <table>
                        <tr>
                            <td>
                                Points (n):
                            </td>
                            <td>
                                <asp:UpdatePanel ID="UpdatePanel_N" runat="server">
                                    <ContentTemplate>
                                        <asp:TextBox ID="N" runat="server"></asp:TextBox>
                                    </ContentTemplate>
                                </asp:UpdatePanel>
                            </td>
                        </tr>
                        <tr>
                            <td class="continuation">
                                or Points File:
                            </td>
                            <td>
                                <asp:UpdatePanel ID="UpdatePanel_PointsFile" runat="server">
                                    <ContentTemplate>
                                        <asp:FileUpload ID="PointsFile" runat="server" />
                                    </ContentTemplate>
                                </asp:UpdatePanel>
                            </td>
                        </tr>
                        <tr>
                            <td class="continuation">
                                or Points Blob URI:
                            </td>
                            <td>
                                <asp:UpdatePanel ID="UpdatePanel_PointsBlob" runat="server">
                                    <ContentTemplate>
                                        <asp:TextBox ID="PointsBlob" runat="server"></asp:TextBox>
                                    </ContentTemplate>
                                </asp:UpdatePanel>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                Clusters (k):
                            </td>
                            <td>
                                <asp:UpdatePanel ID="UpdatePanel_K" runat="server">
                                    <ContentTemplate>
                                        <asp:TextBox ID="K" runat="server"></asp:TextBox>
                                    </ContentTemplate>
                                </asp:UpdatePanel>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                Max iterations:
                            </td>
                            <td>
                                <asp:UpdatePanel ID="UpdatePanel_MaxIterationCount" runat="server">
                                    <ContentTemplate>
                                        <asp:TextBox ID="MaxIterationCount" runat="server">0</asp:TextBox>&nbsp;(0 is unlimited)
                                    </ContentTemplate>
                                </asp:UpdatePanel>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                Notification email:
                            </td>
                            <td>
                                <asp:UpdatePanel ID="UpdatePanel_ProgressEmail" runat="server">
                                    <ContentTemplate>
                                        <asp:TextBox ID="ProgressEmail" runat="server"></asp:TextBox>
                                    </ContentTemplate>
                                </asp:UpdatePanel>
                            </td>
                        </tr>
                    </table>
                    <asp:UpdatePanel ID="UpdatePanel_Buttons" runat="server">
                        <ContentTemplate>
                            <p>
                                <asp:Button ID="Run" runat="server" OnClick="Run_Click" Text="Run K-Means" />
                                <br />
                                <asp:Button ID="ClearBlobs" runat="server" OnClick="ClearBlobs_Click" Text="Clear Blobs" />
                                <br />
                                <asp:Button ID="Refresh" runat="server" Text="Refresh" OnClick="Refresh_Click" />
                            </p>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                    <h3>
                        Status</h3>
                    <asp:UpdatePanel ID="UpdatePanel_Status" runat="server">
                        <ContentTemplate>
                            <p>
                                <asp:Label ID="Status" runat="server" Text="Click Run K-Means to see results."></asp:Label><asp:Label
                                    ID="StatusProgress" runat="server" Text=""></asp:Label>
                            </p>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                    <asp:UpdatePanel ID="UpdatePanel_DownloadLog" runat="server">
                        <ContentTemplate>
                            <p>
                                <asp:HyperLink ID="DownloadLog" Text="Download Log" runat="server" Enabled="False"
                                    Target="_blank"></asp:HyperLink>
                            </p>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                    <h3>
                        Points Blob</h3>
                    <asp:UpdatePanel ID="UpdatePanel_PointsURI" runat="server">
                        <ContentTemplate>
                            <p>
                                <asp:Label ID="PointsURI" runat="server" Text=""></asp:Label>
                            </p>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                    <h3>
                        Centroids Blob</h3>
                    <asp:UpdatePanel ID="UpdatePanel_CentroidsURI" runat="server">
                        <ContentTemplate>
                            <p>
                                <asp:Label ID="CentroidsURI" runat="server" Text=""></asp:Label>
                            </p>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </div>
            </td>
            <td valign="top">
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
                        <asp:UpdatePanel ID="UpdatePanel_StatsSummary" runat="server">
                            <ContentTemplate>
                                <asp:Literal ID="StatsSummary" runat="server"></asp:Literal>
                            </ContentTemplate>
                        </asp:UpdatePanel>
                    </table>
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
                        <asp:UpdatePanel ID="UpdatePanel_Stats" runat="server">
                            <ContentTemplate>
                                <asp:Literal ID="Stats" runat="server"></asp:Literal>
                            </ContentTemplate>
                        </asp:UpdatePanel>
                    </table>
                </div>
            </td>
            <td valign="top">
                <div class="visualblock long">
                    <h2>
                        Workers</h2>
                    <table>
                        <tr>
                            <th>
                                Worker ID
                            </th>
                        </tr>
                        <asp:UpdatePanel ID="UpdatePanel_Workers" runat="server">
                            <ContentTemplate>
                                <asp:Literal ID="Workers" runat="server"></asp:Literal>
                            </ContentTemplate>
                        </asp:UpdatePanel>
                    </table>
                </div>
                <div class="visualblock">
                    <h2>
                        Visualization</h2>
                    <div class="visualization">
                        <asp:UpdatePanel ID="UpdatePanel_Visualization" runat="server">
                            <ContentTemplate>
                                <asp:Literal ID="Visualization" runat="server"></asp:Literal>
                            </ContentTemplate>
                        </asp:UpdatePanel>
                    </div>
                </div>
            </td>
        </tr>
    </table>
</asp:Content>
