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
using System.Timers;
using System.Runtime.InteropServices;

namespace GCWZeroManager
{
    /// <summary>
    /// Interaction logic for TransferProgressWindow.xaml
    /// </summary>
    public partial class TransferProgressWindow : Window
    {
        struct WorkerThreadArgsUpload
        {
            public List<string> existingFiles;
            public string remotePath;
        }

        struct WorkerThreadArgsDownload
        {
            public string localPath;
        }

        BackgroundWorker workerThread;
        List<FileUploadDownloadNode> uploadFiles;
        long totalBytes;
        ScpClient scp = null;
        bool okToUpdate = false;

        string errorMessage = "Unknown error";
        public string ErrorMessage
        {
            get { return errorMessage; }
        }

        bool connectionError = false;
        public bool IsConnectionError
        {
            get { return connectionError; }
        }

        public TransferProgressWindow()
        {
            InitializeComponent();
        }

        public bool TryUploadFiles(List<OPKFile> files, string remotePath)
        {
            List<FileUploadDownloadNode> fileNodes = new List<FileUploadDownloadNode>();

            foreach (OPKFile opk in files)
            {
                FileUploadDownloadNode fileUploadNode = new FileUploadDownloadNode();
                fileUploadNode.Filename = opk.Filename;
                fileUploadNode.Path = opk.LocalPath;
                fileUploadNode.Size = opk.Size;

                fileNodes.Add(fileUploadNode);
            }

            return TryUploadFiles(fileNodes, remotePath);
        }

        public bool TryUploadFiles(List<FileUploadDownloadNode> files, string remotePath)
        {
            if (files.Count == 0)
            {
                MessageBox.Show("No files to upload", "No files", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            this.uploadFiles = files;

            totalBytes = 0;
            foreach (FileUploadDownloadNode f in files)
            {
                totalBytes += f.Size.Bytes;
            }

            labelTotalBytesData.Content = HelperTools.GetFormattedSize(totalBytes);
            labelTotalFilesData.Content = files.Count;
            labelFilesRemainingData.Content = files.Count;

            if (!ConnectionManager.Instance.IsConnected)
            {
                if (!ConnectionManager.Instance.Connect())
                {
                    MessageBox.Show("Connection failed!", "Connection failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            SftpClient sftp = ConnectionManager.Instance.GetActiveSftpConnection();

            List<string> fileList = new List<string>();
            try
            {
                foreach (SftpFile file in sftp.ListDirectory(remotePath))
                {
                    if (file.IsRegularFile)
                        fileList.Add(file.Name);
                }
            }
            catch (SshConnectionException)
            {
                ConnectionManager.Instance.Disconnect(false);
                MessageBox.Show("Connection lost!", "Connection lost", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            WorkerThreadArgsUpload args = new WorkerThreadArgsUpload();
            args.existingFiles = fileList;
            args.remotePath = remotePath;

            // Start thread (backgroundworker) which does the transfer and updates the progress
            workerThread = new BackgroundWorker();
            workerThread.DoWork += new DoWorkEventHandler(workerThread_DoWork);
            workerThread.RunWorkerCompleted += new RunWorkerCompletedEventHandler(workerThread_RunWorkerCompleted);
            workerThread.ProgressChanged += OnProgressChanged;
            workerThread.WorkerReportsProgress = true;
            workerThread.WorkerSupportsCancellation = true;
            workerThread.RunWorkerAsync(args);

            return true;
        }

        public void DownloadFiles(List<FileUploadDownloadNode> files, string localPath)
        {
            if (files.Count == 0)
            {
                MessageBox.Show("No files to download", "No files", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.uploadFiles = files;

            totalBytes = 0;
            foreach (FileUploadDownloadNode f in files)
            {
                totalBytes += f.Size.Bytes;
            }

            labelTotalBytesData.Content = HelperTools.GetFormattedSize(totalBytes);
            labelTotalFilesData.Content = files.Count;
            labelFilesRemainingData.Content = files.Count;

            if (!ConnectionManager.Instance.IsConnected)
            {
                if (!ConnectionManager.Instance.Connect())
                {
                    MessageBox.Show("Connection failed!", "Connection failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            WorkerThreadArgsDownload args = new WorkerThreadArgsDownload();
            args.localPath = localPath;

            // Start thread (backgroundworker) which does the transfer and updates the progress
            workerThread = new BackgroundWorker();
            workerThread.DoWork += new DoWorkEventHandler(workerThread_DoWork);
            workerThread.RunWorkerCompleted += new RunWorkerCompletedEventHandler(workerThread_RunWorkerCompleted);
            workerThread.ProgressChanged += OnProgressChanged;
            workerThread.WorkerReportsProgress = true;
            workerThread.WorkerSupportsCancellation = true;
            workerThread.RunWorkerAsync(args);
        }

        private void OnUpdateTimer(object source, ElapsedEventArgs e)
        {
            okToUpdate = true;
        }

        private void workerThread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                connectionError = false;

                if (this.IsVisible)
                    DialogResult = null;
                return;
            }

            WorkerCompletedArgs wArgs = (WorkerCompletedArgs)e.Result;

            if (wArgs.Scp != null && wArgs.Scp.IsConnected)
                wArgs.Scp.Disconnect();

            errorMessage = wArgs.ErrorMsg;
            connectionError = wArgs.ConnectionError;

            if (this.IsVisible)
                DialogResult = wArgs.Result;
        }

        private void workerThread_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            ProgressState progress = new ProgressState();
            progress.CurFileProgressPercent = 0;
            progress.FilesRemaining = uploadFiles.Count;
            progress.Status = "Transferring...";

            long bytesLeft = totalBytes;

            workerThread.ReportProgress(0, progress);

            scp = ConnectionManager.Instance.ConnectWithActiveConnectionSCP();

            if (scp == null || !scp.IsConnected)
            {
                MessageBox.Show("Connection failed!", "Connection failed (2)", MessageBoxButton.OK, MessageBoxImage.Error);

                e.Result = new WorkerCompletedArgs(scp, false, "Connection Failed!");
                return;
            }

            System.Timers.Timer updateTimer = new System.Timers.Timer();
            updateTimer.Elapsed += OnUpdateTimer;
            updateTimer.Interval = 20;
            updateTimer.Enabled = true;

            if (e.Argument is WorkerThreadArgsUpload)
            {
                WorkerThreadArgsUpload args = (WorkerThreadArgsUpload)e.Argument;
                List<string> fileList = args.existingFiles;
                string remotePath = args.remotePath;

                foreach (FileUploadDownloadNode fileUploadNode in uploadFiles)
                {
                    if (worker.CancellationPending)
                    {
                        e.Result = new WorkerCompletedArgs(scp, false, "Cancelled");
                        e.Cancel = true;
                        updateTimer.Stop();
                        return;
                    }

                    if (fileList.Contains(fileUploadNode.Filename))
                    {
                        MessageBoxResult result = MessageBox.Show("File " + fileUploadNode.Filename + " already exists, do you want to overwrite?", "File exists", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.No)
                        {
                            progress.FilesRemaining--;
                            bytesLeft -= fileUploadNode.Size.Bytes;
                            continue;
                        }
                    }

                    if (worker.CancellationPending)
                    {
                        e.Result = new WorkerCompletedArgs(scp, false, "Cancelled");
                        e.Cancel = true;
                        updateTimer.Stop();
                        return;
                    }

                    try
                    {
                        int totalPercent;

                        scp.Uploading += new EventHandler<ScpUploadEventArgs>(delegate(object _sender, ScpUploadEventArgs _e)
                            {
                                if (okToUpdate)
                                {
                                    progress.CurFileProgressPercent = _e.Size == 0 ? 100 : (int)((_e.Uploaded * 100) / _e.Size);
                                    totalPercent = 100 - (totalBytes == 0 ? 0 : (int)(((bytesLeft - _e.Uploaded) * 100) / totalBytes));
                                    workerThread.ReportProgress(totalPercent, progress);

                                    okToUpdate = false;
                                }
                            });

                        bool errorHandled = false;
                        scp.ErrorOccurred += new EventHandler<Renci.SshNet.Common.ExceptionEventArgs>(delegate(object _sender, Renci.SshNet.Common.ExceptionEventArgs _e)
                            {
                                if (errorHandled)
                                    return;

                                errorHandled = true;
                                // Try to clean up
                                try
                                {
                                    ConnectionManager.Instance.DeleteFile(System.IO.Path.Combine(remotePath, fileUploadNode.Filename));
                                }
                                catch (Exception)
                                { }
                            });

                        scp.Upload(new FileInfo(fileUploadNode.Path), remotePath);

                        progress.FilesRemaining--;
                        bytesLeft -= fileUploadNode.Size.Bytes;
                        totalPercent = 100 - (totalBytes == 0 ? 0 : (int)((bytesLeft * 100) / totalBytes));
                        workerThread.ReportProgress(totalPercent, progress);
                    }
                    catch (ScpException se)
                    {
                        e.Result = new WorkerCompletedArgs(scp, false, se.Message, false);
                        updateTimer.Stop();
                        return;
                    }
                    catch (SocketException se)
                    {
                        // Was this due to a cancellation?
                        if (worker.CancellationPending)
                        {
                            e.Result = new WorkerCompletedArgs(scp, false, "Cancelled");
                            e.Cancel = true;
                            updateTimer.Stop();
                            return;
                        }
                        else
                        {
                            e.Result = new WorkerCompletedArgs(scp, false, "Error");
                            updateTimer.Stop();
                            return;
                        }
                    }
                    catch (SshOperationTimeoutException se)
                    {
                        e.Result = new WorkerCompletedArgs(scp, false, se.Message);
                        updateTimer.Stop();
                        return;
                    }
                    catch (SshConnectionException se)
                    {
                        e.Result = new WorkerCompletedArgs(scp, false, "Error");
                        updateTimer.Stop();
                        return;
                    }
                }
            }
            else if (e.Argument is WorkerThreadArgsDownload)
            {
                WorkerThreadArgsDownload args = (WorkerThreadArgsDownload)e.Argument;
                string localPath = args.localPath;

                foreach (FileUploadDownloadNode fileUploadNode in uploadFiles)
                {
                    if (worker.CancellationPending)
                    {
                        e.Result = new WorkerCompletedArgs(scp, false, "Cancelled");
                        e.Cancel = true;
                        updateTimer.Stop();
                        return;
                    }

                    if (File.Exists(System.IO.Path.Combine(localPath, fileUploadNode.Filename)))
                    {
                        MessageBoxResult result = MessageBox.Show("File " + fileUploadNode.Filename + " already exists, do you want to overwrite?", "File exists", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.No)
                        {
                            progress.FilesRemaining--;
                            bytesLeft -= fileUploadNode.Size.Bytes;
                            continue;
                        }
                    }

                    if (worker.CancellationPending)
                    {
                        e.Result = new WorkerCompletedArgs(scp, false, "Cancelled");
                        e.Cancel = true;
                        updateTimer.Stop();
                        return;
                    }

                    try
                    {
                        int totalPercent;

                        scp.Downloading += new EventHandler<ScpDownloadEventArgs>(delegate(object _sender, ScpDownloadEventArgs _e)
                        {
                            if (okToUpdate)
                            {
                                progress.CurFileProgressPercent = _e.Size == 0 ? 100 : (int)((_e.Downloaded * 100) / _e.Size);
                                totalPercent = 100 - (totalBytes == 0 ? 0 : (int)(((bytesLeft - _e.Downloaded) * 100) / totalBytes));
                                workerThread.ReportProgress(totalPercent, progress);

                                okToUpdate = false;
                            }
                        });

                        bool errorHandled = false;
                        scp.ErrorOccurred += new EventHandler<Renci.SshNet.Common.ExceptionEventArgs>(delegate(object _sender, Renci.SshNet.Common.ExceptionEventArgs _e)
                        {
                            if (errorHandled)
                                return;

                            errorHandled = true;
                            // Try to clean up
                            try
                            {
                                if (File.Exists(System.IO.Path.Combine(localPath, fileUploadNode.Filename)))
                                    File.Delete(System.IO.Path.Combine(localPath, fileUploadNode.Filename));
                            }
                            catch (Exception)
                            { }
                        });

                        scp.Download(fileUploadNode.Path, new FileInfo(System.IO.Path.Combine(localPath, fileUploadNode.Filename)));

                        progress.FilesRemaining--;
                        bytesLeft -= fileUploadNode.Size.Bytes;
                        totalPercent = 100 - (totalBytes == 0 ? 0 : (int)((bytesLeft * 100) / totalBytes));
                        workerThread.ReportProgress(totalPercent, progress);
                    }
                    catch (ScpException se)
                    {
                        e.Result = new WorkerCompletedArgs(scp, false, se.Message, false);
                        updateTimer.Stop();
                        return;
                    }
                    catch (SocketException se)
                    {
                        // Was this due to a cancellation?
                        if (worker.CancellationPending)
                        {
                            e.Result = new WorkerCompletedArgs(scp, false, "Cancelled");
                            e.Cancel = true;
                            updateTimer.Stop();
                            return;
                        }
                        else
                        {
                            e.Result = new WorkerCompletedArgs(scp, false, "Error");
                            updateTimer.Stop();
                            return;
                        }
                    }
                    catch (SshOperationTimeoutException se)
                    {
                        e.Result = new WorkerCompletedArgs(scp, false, se.Message);
                        updateTimer.Stop();
                        return;
                    }
                    catch (SshConnectionException se)
                    {
                        e.Result = new WorkerCompletedArgs(scp, false, "Error");
                        updateTimer.Stop();
                        return;
                    }
                }
            }

            progress.Status = "Complete";
            workerThread.ReportProgress(100, progress);
            e.Result = new WorkerCompletedArgs(scp, true, "");
            updateTimer.Stop();
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
            if (workerThread != null && workerThread.IsBusy)
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

        #region Hide window icon and close button
        // From http://stackoverflow.com/a/1559522/2038264
        // By Nir http://stackoverflow.com/users/3509/nir
        // cc by-sa 3.0 https://creativecommons.org/licenses/by-sa/3.0/

        [DllImport("user32.dll")]
        static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        private const int GWL_STYLE = -16;

        private const uint WS_SYSMENU = 0x80000;

        protected override void OnSourceInitialized(EventArgs e)
        {
            IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE,
                GetWindowLong(hwnd, GWL_STYLE) & (0xFFFFFFFF ^ WS_SYSMENU));

            base.OnSourceInitialized(e);
        }

        #endregion
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
        private bool connectionError;

        public WorkerCompletedArgs(ScpClient scp, bool result, string errorMsg)
        {
            this.scp = scp;
            this.result = result;
            this.errorMsg = errorMsg;
            this.connectionError = !result;
        }

        public WorkerCompletedArgs(ScpClient scp, bool result, string errorMsg, bool connectionError)
        {
            this.scp = scp;
            this.result = result;
            this.errorMsg = errorMsg;
            this.connectionError = connectionError;
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

        public bool ConnectionError
        {
            get { return connectionError; }
            set { connectionError = value; }
        }
    }
}
