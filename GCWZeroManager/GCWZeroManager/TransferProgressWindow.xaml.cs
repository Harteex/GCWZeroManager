using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Renci.SshNet;
using System.ComponentModel;
using Renci.SshNet.Sftp;
using System.IO;
using Renci.SshNet.Common;
using System.Threading;
using System.Net.Sockets;

namespace GCWZeroManager
{
    /// <summary>
    /// Interaction logic for TransferProgressWindow.xaml
    /// </summary>
    public partial class TransferProgressWindow : Window
    {
        BackgroundWorker workerThread;
        List<OPKFile> files;
        long totalBytes;
        ScpClient scp = null;

        public TransferProgressWindow()
        {
            InitializeComponent();
        }

        public void UploadFiles(List<OPKFile> files, ConnectionInfo cInfo)
        {
            this.files = files;

            totalBytes = 0;
            foreach (OPKFile f in files)
            {
                totalBytes += f.Bytes;
            }

            labelTotalBytesData.Content = HelperTools.GetFormattedSize(totalBytes);
            labelTotalFilesData.Content = files.Count;
            labelFilesRemainingData.Content = files.Count;

            // Start thread (backgroundworker) which does the transfer and updates the progress
            workerThread = new BackgroundWorker();
            workerThread.DoWork += new DoWorkEventHandler(workerThread_DoWork);
            workerThread.RunWorkerCompleted += new RunWorkerCompletedEventHandler(workerThread_RunWorkerCompleted);
            workerThread.ProgressChanged += OnProgressChanged;
            workerThread.WorkerReportsProgress = true;
            workerThread.WorkerSupportsCancellation = true;
            workerThread.RunWorkerAsync(cInfo);
        }

        private void workerThread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                if (this.IsVisible)
                    DialogResult = null;
                return;
            }

            WorkerCompletedArgs wArgs = (WorkerCompletedArgs)e.Result;

            if (wArgs.Scp != null && wArgs.Scp.IsConnected)
                wArgs.Scp.Disconnect();

            if (this.IsVisible)
                DialogResult = wArgs.Result;
        }

        private void workerThread_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            ConnectionInfo cInfo = (ConnectionInfo)e.Argument;
            SftpClient sftp = ConnectionManager.Instance.ConnectSFTP(cInfo);

            if (sftp == null || !sftp.IsConnected)
            {
                MessageBox.Show("Connection failed!", "Connection failed", MessageBoxButton.OK, MessageBoxImage.Error);

                e.Result = new WorkerCompletedArgs(null, false, "Connection Failed!");
                return;
            }

            ProgressState progress = new ProgressState();
            progress.CurFileProgressPercent = 0;
            progress.FilesRemaining = files.Count;
            progress.Status = "Transferring...";

            long bytesLeft = totalBytes;

            List<string> fileList = new List<string>();
            foreach (SftpFile file in sftp.ListDirectory(ConnectionManager.Instance.OPKDir))
            {
                if (file.IsRegularFile)
                    fileList.Add(file.Name);
            }

            workerThread.ReportProgress(0, progress);

            sftp.Disconnect();

            scp = ConnectionManager.Instance.ConnectSCP(cInfo);

            if (scp == null || !scp.IsConnected)
            {
                MessageBox.Show("Connection failed!", "Connection failed (2)", MessageBoxButton.OK, MessageBoxImage.Error);

                e.Result = new WorkerCompletedArgs(scp, false, "Connection Failed!");
                return;
            }

            foreach (OPKFile opk in files)
            {
                if (worker.CancellationPending)
                {
                    e.Result = new WorkerCompletedArgs(scp, false, "Cancelled");
                    e.Cancel = true;
                    return;
                }

                if (fileList.Contains(opk.Filename))
                {
                    MessageBoxResult result = MessageBox.Show("File " + opk.Filename + " already exists, do you want to overwrite?", "File exists", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.No)
                    {
                        progress.FilesRemaining--;
                        bytesLeft -= opk.Bytes;
                        continue;
                    }
                }

                if (worker.CancellationPending)
                {
                    e.Result = new WorkerCompletedArgs(scp, false, "Cancelled");
                    e.Cancel = true;
                    return;
                }

                try
                {
                    int totalPercent;

                    scp.Uploading += new EventHandler<ScpUploadEventArgs>(delegate(object _sender, ScpUploadEventArgs _e)
                        {
                            //
                            progress.CurFileProgressPercent = (int)((_e.Uploaded * 100) / _e.Size);
                            totalPercent = 100 - (int)(((bytesLeft - _e.Uploaded) * 100) / totalBytes);
                            workerThread.ReportProgress(totalPercent, progress);
                        });

                    bool errorHandled = false;
                    scp.ErrorOccurred += new EventHandler<Renci.SshNet.Common.ExceptionEventArgs>(delegate(object _sender, Renci.SshNet.Common.ExceptionEventArgs _e)
                        {
                            if (errorHandled) // FIXME maybe do locking, and later on waiting for this operation to finish... also check that deletefiles doesn't throw any exception (if it doesn't exist)
                                return;

                            errorHandled = true;
                            // Try to clean up
                            List<OPKFile> tempList = new List<OPKFile>();
                            tempList.Add(opk);
                            ConnectionManager.Instance.DeleteFiles(tempList);
                        });

                    scp.Upload(new FileInfo(opk.Path), ConnectionManager.Instance.OPKDir);

                    progress.FilesRemaining--;
                    bytesLeft -= opk.Bytes;
                    totalPercent = 100 - (int)((bytesLeft * 100) / totalBytes);
                    workerThread.ReportProgress(totalPercent, progress);
                }
                catch (ScpException se)
                {
                    MessageBox.Show("Error: " + se.Message);
                    e.Result = new WorkerCompletedArgs(scp, false, "Error: " + se.Message);
                    return;
                }
                catch (SocketException se)
                {
                    // Was this due to a cancellation?
                    if (worker.CancellationPending)
                    {
                        e.Result = new WorkerCompletedArgs(scp, false, "Cancelled");
                        e.Cancel = true;
                        return;
                    }
                    else
                    {
                        e.Result = new WorkerCompletedArgs(scp, false, "Error");
                        return;
                    }
                }
                catch (SshConnectionException se)
                {
                    e.Result = new WorkerCompletedArgs(scp, false, "Error");
                    return;
                }
            }

            progress.Status = "Complete";
            workerThread.ReportProgress(100, progress);
            e.Result = new WorkerCompletedArgs(scp, true, "");
        }

        private void OnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressState progress = (ProgressState)e.UserState;
            
            progressBarTotal.Value = e.ProgressPercentage;
            progressBarFile.Value = progress.CurFileProgressPercent;
            labelStatusData.Content = progress.Status;
            labelFilesRemainingData.Content = "" + progress.FilesRemaining;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (workerThread.IsBusy)
            {
                workerThread.CancelAsync();
                if (scp != null && scp.IsConnected)
                {
                    scp.Disconnect();
                }
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // TODO prevent closing until cancelled...
        }
    }

    public class ProgressState
    {
        private int curFileProgressPercent;
        private string status;
        private int filesRemaining;

        public ProgressState()
        {
        }

        public ProgressState(int curFileProgressPercent, string status, int filesRemaining)
        {
            this.curFileProgressPercent = curFileProgressPercent;
            this.status = status;
            this.filesRemaining = filesRemaining;
        }

        public int CurFileProgressPercent
        {
            get { return curFileProgressPercent; }
            set { curFileProgressPercent = value; }
        }

        public string Status
        {
            get { return status; }
            set { status = value; }
        }

        public int FilesRemaining
        {
            get { return filesRemaining; }
            set { filesRemaining = value; }
        }
    }

    public class WorkerCompletedArgs
    {
        private ScpClient scp;
        private bool result;
        private string errorMsg;

        public WorkerCompletedArgs(ScpClient scp, bool result, string errorMsg)
        {
            this.scp = scp;
            this.result = result;
            this.errorMsg = errorMsg;
        }

        public ScpClient Scp
        {
            get { return scp; }
            set { scp = value; }
        }

        public bool Result
        {
            get { return result; }
            set { result = value; }
        }

        public string ErrorMsg
        {
            get { return errorMsg; }
            set { errorMsg = value; }
        }
    }
}
