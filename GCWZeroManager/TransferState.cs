using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Timers;

namespace GCWZeroManager
{
    public class TransferState
    {
        public BackgroundWorker Worker { get; set; }
        public DoWorkEventArgs WorkerEventArgs { get; set; }
        public Timer UpdateTimer { get; set; }
        public ProgressState Progress { get; set; }
        public ScpClient Scp { get; set; }
        public SftpClient Sftp { get; set; }
        public long TotalBytes { get; set; }
        public long BytesLeft { get; set; }
        public bool IsCancelled { get; set; }

        public TransferState()
        {

        }

        public bool CancellationPending
        {
            get { return Worker.CancellationPending; }
        }

        public void Cancel()
        {
            IsCancelled = true;
            WorkerEventArgs.Result = new WorkerCompletedArgs(Scp, false, "Cancelled");
            WorkerEventArgs.Cancel = true;
            UpdateTimer.Stop();
        }

        public void StopWithError(string error)
        {
            WorkerEventArgs.Result = new WorkerCompletedArgs(Scp, false, error);
            UpdateTimer.Stop();
        }

        public void StopWithError(string error, bool connectionError)
        {
            WorkerEventArgs.Result = new WorkerCompletedArgs(Scp, false, error, connectionError);
            UpdateTimer.Stop();
        }
    }
}
