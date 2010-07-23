using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using AzureUtils;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;
using System.Data.Services.Client;
using System.IO;

namespace AKMWebRole
{
    public partial class _Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }

        private void DeleteAllBlobs()
        {
            foreach (CloudBlobContainer container in AzureHelper.StorageAccount.CreateCloudBlobClient().ListContainers())
            {
                System.Diagnostics.Trace.Write("Deleting container " + container.Name + "... ");
                container.BeginDelete((ar) => { }, null);
                System.Diagnostics.Trace.WriteLine("done.");
            }
        }

        protected void ClearBlobs_Click(object sender, EventArgs e)
        {
            DeleteAllBlobs();
        }

        protected void Run_Click(object sender, EventArgs e)
        {
            FreezeUI();
            ClearIndicators();
            Status.Text = "Running...";

            Guid jobID = Guid.NewGuid();
            Session["jobID"] = jobID;
            Session["lastLogRefreshTime"] = DateTime.MinValue;
            Session["allLogs"] = new List<PerformanceLog>();
            AzureHelper.EnqueueMessage(AzureHelper.ServerRequestQueue, new KMeansJobData(jobID, int.Parse(N.Text), int.Parse(K.Text), int.Parse(M.Text), int.Parse(MaxIterationCount.Text), DateTime.Now));

            WaitForResults();
        }

        private void ClearIndicators()
        {
            Visualization.Text = "";
            Points.Text = "";
            Centroids.Text = "";
            Status.Text = "";
            Stats.Text = "";
        }

        private void FreezeUnfreezeUI(bool freeze = true)
        {
            Run.Enabled = N.Enabled = K.Enabled = M.Enabled = MaxIterationCount.Enabled = !freeze;
        }

        private void FreezeUI()
        {
            FreezeUnfreezeUI(true);
        }

        private void UnfreezeUI()
        {
            FreezeUnfreezeUI(false);
        }

        private void WaitStopWaitingForResults(bool wait = true)
        {
            UpdateTimer.Enabled = wait;
        }
        private void WaitForResults()
        {
            WaitStopWaitingForResults(true);
        }
        private void StopWaitingForResults()
        {
            WaitStopWaitingForResults(false);
        }

        protected void UpdateTimer_Tick(object sender, EventArgs e)
        {
            Status.Text += ".";

            Guid jobID = (Guid)Session["jobID"];
            
            System.Diagnostics.Trace.TraceInformation("[WebRole] UpdateTimer_Tick(), JobID={0}", jobID);

            UpdateStatus(jobID, false);

            AzureHelper.PollForMessage(AzureHelper.ServerResponseQueue,
                message => ((KMeansJobResult)message).JobID == jobID,
                ShowResults);
        }

        private void UpdateStatus(Guid jobID, bool final)
        {
            System.Diagnostics.Trace.TraceInformation("[WebRole] ShowStatus(), JobID={0}", jobID);

            IEnumerable<PerformanceLog> logs;
            logs = GetLogs(jobID, true);
            if (logs == null || logs.Count() == 0)
                return;

            // Show all logs
            Stats.Text = string.Empty;
            foreach (PerformanceLog log in logs)
            {
                Stats.Text += string.Format("<tr><td>{0}</td><td>{1}</td><td>{2}</td></tr>",
                    log.IterationCount,
                    log.MethodName,
                    (log.EndTime - log.StartTime).TotalSeconds);
            }

            // Show the group stats
            var logsByMethod = logs.GroupBy(log => log.MethodName);
            StatsSummary.Text = string.Empty;
            foreach (IGrouping<string, PerformanceLog> logGroup in logsByMethod)
            {
                StatsSummary.Text += string.Format("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td></tr>",
                    logGroup.Key,
                    logGroup.Min(log => (log.EndTime - log.StartTime).TotalSeconds),
                    logGroup.Average(log => (log.EndTime - log.StartTime).TotalSeconds),
                    logGroup.Max(log => (log.EndTime - log.StartTime).TotalSeconds),
                    logGroup.Count());
            }

            try
            {
                UpdatePointsCentroids(
                    AzureHelper.GetBlob(logs.First().PartitionKey, AzureHelper.PointsBlob),
                    AzureHelper.GetBlob(logs.First().PartitionKey, AzureHelper.CentroidsBlob),
                    false);
            }
            catch (StorageClientException e)
            {
                Trace.Write("Information", "Updating points and centroids failed. Will try again later.", e);
            }
            catch (IOException e)
            {
                Trace.Write("Information", "Updating points and centroids failed. Will try again later.", e);
            }
        }

        private IEnumerable<PerformanceLog> GetLogs(Guid jobID, bool final)
        {
            IEnumerable<PerformanceLog> logs;
            if (!final)
            {
                // Get the logs that were added since the last refresh time
                DateTime lastLogRefreshTime = (DateTime)Session["lastLogRefreshTime"];
                var newLogs = (AzureHelper.PerformanceLogger.PerformanceLogs.Where(log => log.PartitionKey == jobID.ToString()
                    && log.Timestamp > lastLogRefreshTime // Causes DataServiceQueryException, for some reason!
                    ) as DataServiceQuery<PerformanceLog>).Execute().ToList();
                Session["lastLogRefreshTime"] = DateTime.UtcNow;
                if (newLogs.Count() == 0)
                    return null;

                // Add these logs to allLogs
                List<PerformanceLog> allLogs = (List<PerformanceLog>)Session["allLogs"];

                allLogs.AddRange(newLogs);
                allLogs.Sort((x, y) => x.StartTime.CompareTo(y.StartTime));

                Session["allLogs"] = logs = allLogs;
            }
            else
            {
                // Get all logs in one query
                logs = (AzureHelper.PerformanceLogger.PerformanceLogs.Where(log => log.PartitionKey == jobID.ToString()) as DataServiceQuery<PerformanceLog>).Execute().ToList().OrderBy(log => log.StartTime);
            }
            return logs;
        }

        private bool ShowResults(AzureMessage message)
        {
            KMeansJobResult jobResult = message as KMeansJobResult;

            System.Diagnostics.Trace.TraceInformation("[WebRole] ShowResults(), JobID={0}", jobResult.JobID);

            StopWaitingForResults();
            Status.Text = "Done!";

            UpdateStatus(jobResult.JobID, true);
            
            UpdatePointsCentroids(AzureHelper.GetBlob(jobResult.Points), AzureHelper.GetBlob(jobResult.Centroids), true);

            UnfreezeUI();
            return true;
        }

        private void UpdatePointsCentroids(CloudBlob points, CloudBlob centroids, bool final)
        {
            StringBuilder visualization = new StringBuilder();
            StringBuilder centroidsString = new StringBuilder();
            StringBuilder pointsString = new StringBuilder();

            using (PointStream<ClusterPoint> pointsStream = new PointStream<ClusterPoint>(points, ClusterPoint.FromByteArray, ClusterPoint.Size))
            {
                int pointIndex = 0;
                foreach (ClusterPoint p in pointsStream)
                {
                    pointsString.AppendFormat("<tr><td>{0}</td><td>{1}</td><td>{2}</td></tr>", p.X, p.Y, p.CentroidID);
                    visualization.AppendFormat("<div class=\"point\" style=\"top:{0}px;left:{1}px;background-color:{2}\"></div>",
                        PointUnitsToPixels(p.Y),
                        PointUnitsToPixels(p.X),
                        GuidToColor(p.CentroidID));

                    pointIndex++;
                    if (pointIndex > (final ? 10000 : 100))
                        break;
                }
            }

            using (PointStream<Centroid> centroidsStream = new PointStream<Centroid>(centroids, Centroid.FromByteArray, Centroid.Size))
            {
                foreach (Centroid p in centroidsStream)
                {
                    centroidsString.AppendFormat("<tr><td>{0}</td><td>{1}</td><td>{2}</td></tr>", p.ID, p.X, p.Y);
                    visualization.AppendFormat("<div class=\"centroid\" style=\"top:{0}px;left:{1}px;background-color:{2}\"></div>",
                        PointUnitsToPixels(p.Y),
                        PointUnitsToPixels(p.X),
                        GuidToColor(p.ID));
                }
            }

            Points.Text = pointsString.ToString();
            Centroids.Text = centroidsString.ToString();
            Visualization.Text = visualization.ToString();
        }

        private string GuidToColor(Guid guid)
        {
            // Just take the first 6 hex digits of the Guid
            return "#" + guid.ToString("N").Substring(0, 6);
        }

        private string PointUnitsToPixels(double pointUnits)
        {
            int pixels = (int)((pointUnits + 50) * 5); // scale from (-50,50) to (0,500)
            return pixels.ToString();
        }
    }
}
