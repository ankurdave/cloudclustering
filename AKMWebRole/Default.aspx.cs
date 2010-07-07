using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using AzureUtils;

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

            AzureHelper.EnqueueMessage("serverrequests", new ServerRequest {
                JobID = jobID = new Guid(),
                N = int.Parse(N.Text),
                K = int.Parse(K.Text),
                M = int.Parse(M.Text)
            });

            WaitForResults();
        }

        private void FreezeUnfreezeUI(bool freeze = true)
        {
            Run.Enabled = N.Enabled = K.Enabled = M.Enabled = !freeze;
        }

        private void FreezeUI() {
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

            AzureHelper.PollForMessage("serverresponses",
                message => ((ServerResponse)message).JobID == jobID,
                message =>
                {
                    StopWaitingForResults();
                    Status.Text = "Done! " + message.ToString();
                    UnfreezeUI();
                    return true;
                });
        }
    }
}
