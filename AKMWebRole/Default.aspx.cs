using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using AzureUtils;
using Microsoft.WindowsAzure.StorageClient;
using System.Text;

namespace AKMWebRole
{
    public partial class _Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void Run_Click(object sender, EventArgs e)
        {
            FreezeUI();
            ClearIndicators();
            Status.Text = "Running...";

            Guid jobID = Guid.NewGuid();
            Session.Add("jobID", jobID);
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

            AzureHelper.PollForMessage(AzureHelper.StatusQueue,
                message => ((KMeansJobStatus)message).JobID == jobID,
                ShowStatus);
            AzureHelper.PollForMessage(AzureHelper.ServerResponseQueue,
                message => ((KMeansJobResult)message).JobID == jobID,
                ShowResults);
        }

        private bool ShowStatus(AzureMessage message)
        {
            KMeansJobStatus jobStatus = message as KMeansJobStatus;

            System.Diagnostics.Trace.TraceInformation("[WebRole] ShowStatus(), JobID={0}", jobStatus.JobID);

            DateTime now = DateTime.Now;

            Stats.Text += string.Format("<tr><td>{0}</td><td>{1}</td><td>{2}</td></tr>",
                jobStatus.IterationNumber,
                (now - jobStatus.IterationStartTime).TotalSeconds,
                (now - jobStatus.JobStartTime).TotalSeconds);

            UpdatePointsCentroids(jobStatus.Points, jobStatus.Centroids);

            return true;
        }

        private bool ShowResults(AzureMessage message)
        {
            KMeansJobResult jobResult = message as KMeansJobResult;

            System.Diagnostics.Trace.TraceInformation("[WebRole] ShowResults(), JobID={0}", jobResult.JobID);

            StopWaitingForResults();
            Status.Text = "Done!";

            UnfreezeUI();
            return true;
        }

        private void UpdatePointsCentroids(Uri pointsUri, Uri centroidsUri)
        {
            StringBuilder visualization = new StringBuilder();
            StringBuilder centroidsString = new StringBuilder();
            StringBuilder pointsString = new StringBuilder();

            CloudBlob pointsBlob = AzureHelper.GetBlob(pointsUri);
            using (BlobStream pointsStream = pointsBlob.OpenRead())
            {
                byte[] bytes = new byte[ClusterPoint.Size];
                while (pointsStream.Position + ClusterPoint.Size <= pointsStream.Length)
                {
                    pointsStream.Read(bytes, 0, bytes.Length);
                    ClusterPoint p = ClusterPoint.FromByteArray(bytes);

                    pointsString.AppendFormat("<tr><td>{0}</td><td>{1}</td><td>{2}</td></tr>", p.X, p.Y, p.CentroidID);
                    visualization.AppendFormat("<div class=\"point\" style=\"top:{0}px;left:{1}px;background-color:{2}\"></div>",
                        PointUnitsToPixels(p.Y),
                        PointUnitsToPixels(p.X),
                        GuidToColor(p.CentroidID));
                }
            }

            CloudBlob centroidsBlob = AzureHelper.GetBlob(centroidsUri);
            using (BlobStream centroidsStream = centroidsBlob.OpenRead())
            {
                byte[] bytes = new byte[Centroid.Size];
                while (centroidsStream.Position + Centroid.Size <= centroidsStream.Length)
                {
                    centroidsStream.Read(bytes, 0, bytes.Length);
                    Centroid p = Centroid.FromByteArray(bytes);

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

        private string PointUnitsToPixels(float pointUnits)
        {
            int pixels = (int)((pointUnits + 50) * 5); // scale from (-50,50) to (0,500)
            return pixels.ToString();
        }
    }
}
