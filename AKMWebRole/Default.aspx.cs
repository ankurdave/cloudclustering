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
            Guid jobID = Guid.NewGuid();
            Status.Text = string.Format("Running job {0}.", jobID);
            DownloadLog.NavigateUrl = string.Format("Log.aspx?JobID={0}", jobID);
            DownloadLog.Enabled = true;
            UpdatePanel1.Update();

            Session["jobID"] = jobID;
            Session["lastLogRefreshTime"] = DateTime.MinValue;
            Session["allLogs"] = new List<PerformanceLog>();

            Uri pointsBlobUri = null;
            if (PointsFile.HasFile)
            {
                CloudBlob pointsBlob = AzureHelper.CreateBlob(jobID.ToString(), AzureHelper.PointsBlob);
                using (BlobStream stream = pointsBlob.OpenWrite())
                {
                    PointsFile.FileContent.CopyTo(stream);
                }
                pointsBlobUri = pointsBlob.Uri;
            }
            else if (!string.IsNullOrEmpty(PointsBlob.Text))
            {
                CloudBlob pointsBlob = AzureHelper.CreateBlob(jobID.ToString(), AzureHelper.PointsBlob);
                pointsBlob.CopyFromBlob(AzureHelper.GetBlob(new Uri(PointsBlob.Text)));
                pointsBlobUri = pointsBlob.Uri;
            }

            int nInt = 0, kInt = 0, maxIterationCountInt = 0;
            int.TryParse(N.Text, out nInt);
            int.TryParse(K.Text, out kInt);
            int.TryParse(MaxIterationCount.Text, out maxIterationCountInt);

            KMeansJobData jobData = new KMeansJobData(jobID, nInt, pointsBlobUri, kInt, maxIterationCountInt, DateTime.Now)
            {
                ProgressEmail = ProgressEmail.Text
            };
            AzureHelper.EnqueueMessage(AzureHelper.ServerRequestQueue, jobData);

            WaitForResults();
        }

        private void ClearIndicators()
        {
            Visualization.Text = PointsURI.Text = CentroidsURI.Text = Workers.Text = Status.Text = StatusProgress.Text = DownloadLog.NavigateUrl = "";
            DownloadLog.Enabled = false;
        }

        private void FreezeUnfreezeUI(bool freeze = true)
        {
            Run.Enabled = N.Enabled = K.Enabled = MaxIterationCount.Enabled = PointsFile.Enabled = PointsBlob.Enabled = ProgressEmail.Enabled = !freeze;
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
            StatusProgress.Text += ".";
            if (StatusProgress.Text.Length > 3)
            {
                StatusProgress.Text = "";
            }

            UpdateUI();
        }

        private void UpdateUI()
        {
            Guid jobID = default(Guid);
            if (Session["jobID"] != null)
            {
                jobID = (Guid)Session["jobID"];
                UpdateStatus(jobID, false);
            }

            UpdateWorkers();

            UpdatePanel1.Update();

            if (Session["jobID"] != null)
            {
                AzureHelper.PollForMessage<KMeansJobResult>(AzureHelper.ServerResponseQueue,
                    ShowResults,
                    condition: message => message.JobID == jobID);
            }
        }

        private void UpdateStatus(Guid jobID, bool final)
        {
            System.Diagnostics.Trace.TraceInformation("[WebRole] ShowStatus(), JobID={0}", jobID);

            IEnumerable<PerformanceLog> logs = GetLogs(jobID, true);
            if (logs != null && logs.Count() > 0)
            {
                UpdateUIFromLogs(final, logs);
            }
        }

        private void UpdateWorkers()
        {
            // Update the list of workers
            IEnumerable<Worker> workersList = GetWorkers();
            Workers.Text = string.Empty;
            foreach (Worker worker in workersList)
            {
                Workers.Text += string.Format("<tr><td>{0}</td></tr>",
                    worker.PartitionKey
                );
            }
        }

        private void UpdateUIFromLogs(bool final, IEnumerable<PerformanceLog> logs)
        {
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

            // Update the points and centroids displays
            try
            {
                UpdatePointsCentroids(
                    AzureHelper.GetBlob(logs.First().PartitionKey, AzureHelper.PointsBlob),
                    AzureHelper.GetBlob(logs.First().PartitionKey, AzureHelper.CentroidsBlob),
                    final);
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

        private IEnumerable<Worker> GetWorkers()
        {
            return AzureHelper.WorkerStatsReporter.WorkerStats.Execute().ToList();
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

        private bool ShowResults(KMeansJobResult jobResult)
        {
            System.Diagnostics.Trace.TraceInformation("[WebRole] ShowResults(), JobID={0}", jobResult.JobID);

            StopWaitingForResults();
            Status.Text = "Done!";
            StatusProgress.Text = "";

            UpdateStatus(jobResult.JobID, true);

            UnfreezeUI();
            UpdatePanel1.Update();

            return true;
        }

        private void UpdatePointsCentroids(CloudBlob points, CloudBlob centroids, bool final)
        {
            StringBuilder visualization = new StringBuilder();
            
            using (ObjectStreamReader<ClusterPoint> pointsStream = new ObjectStreamReader<ClusterPoint>(points, ClusterPoint.FromByteArray, ClusterPoint.Size))
            {
                int pointIndex = 0;
                foreach (ClusterPoint p in pointsStream)
                {
                    visualization.AppendFormat("<div class=\"point\" style=\"top:{0}px;left:{1}px;background-color:{2}\"></div>",
                        PointUnitsToPixels(p.Y),
                        PointUnitsToPixels(p.X),
                        GuidToColor(p.CentroidID));

                    pointIndex++;
                    if (pointIndex > (final ? 10000 : 100))
                        break;
                }
            }

            using (ObjectStreamReader<Centroid> centroidsStream = new ObjectStreamReader<Centroid>(centroids, Centroid.FromByteArray, Centroid.Size))
            {
                foreach (Centroid p in centroidsStream)
                {
                    visualization.AppendFormat("<div class=\"centroid\" style=\"top:{0}px;left:{1}px;background-color:{2}\"></div>",
                        PointUnitsToPixels(p.Y),
                        PointUnitsToPixels(p.X),
                        GuidToColor(p.ID));
                }
            }

            Visualization.Text = visualization.ToString();

            PointsURI.Text = points.Uri.ToString();
            CentroidsURI.Text = centroids.Uri.ToString();
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

        protected void Refresh_Click(object sender, EventArgs e)
        {
            UpdateUI();
        }
    }
}
