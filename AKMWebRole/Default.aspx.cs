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
        private Guid jobID;

        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void Run_Click(object sender, EventArgs e)
        {
            FreezeUI();
            Status.Text = "Running...";

            jobID = Guid.NewGuid();
            AzureHelper.EnqueueMessage(AzureHelper.ServerRequestQueue, new KMeansJobData(jobID, int.Parse(N.Text), int.Parse(K.Text), int.Parse(M.Text), int.Parse(MaxIterationCount.Text)));

            WaitForResults();
        }

        private void FreezeUnfreezeUI(bool freeze = true)
        {
            Run.Enabled = N.Enabled = K.Enabled = M.Enabled = !freeze;
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

            System.Diagnostics.Trace.TraceInformation("[WebRole] UpdateTimer_Tick(), JobID={0}", jobID);

            AzureHelper.PollForMessage(AzureHelper.ServerResponseQueue,
                message => true /*((KMeansJobResult)message).JobID == jobID*/,
                ShowResults);
        }

        private bool ShowResults(AzureMessage message)
        {
            KMeansJobResult jobResult = message as KMeansJobResult;

            System.Diagnostics.Trace.TraceInformation("[WebRole] ShowResults(), JobID={0}", jobResult.JobID);

            StopWaitingForResults();
            Status.Text = "Done!";

            CloudBlob points = AzureHelper.GetBlob(jobResult.Points);
            using (BlobStream pointsStream = points.OpenRead())
            {
                StringBuilder pointsString = new StringBuilder();
                byte[] bytes = new byte[ClusterPoint.Size];
                while (pointsStream.Position + ClusterPoint.Size <= pointsStream.Length)
                {
                    pointsStream.Read(bytes, 0, bytes.Length);
                    ClusterPoint p = ClusterPoint.FromByteArray(bytes);
                    pointsString.AppendFormat("({0},{1},{2}), ", p.X, p.Y, p.CentroidID);
                }
                Points.Text = pointsString.ToString();
            }

            CloudBlob centroids = AzureHelper.GetBlob(jobResult.Centroids);
            using (BlobStream centroidsStream = centroids.OpenRead())
            {
                StringBuilder centroidsString = new StringBuilder();
                byte[] bytes = new byte[Centroid.Size];
                while (centroidsStream.Position + Centroid.Size <= centroidsStream.Length)
                {
                    centroidsStream.Read(bytes, 0, bytes.Length);
                    Centroid p = Centroid.FromByteArray(bytes);
                    centroidsString.AppendFormat("({0},{1},{2}), ", p.ID, p.X, p.Y);
                }
                Centroids.Text = centroidsString.ToString();
            }

            UnfreezeUI();
            return true;
        }
    }
}
