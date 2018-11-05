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
using Renci.SshNet;
using System.ComponentModel;
using Renci.SshNet.Sftp;
using System.IO;
using Renci.SshNet.Common;
using System.Threading;
using System.Net.Sockets;
using System.Timers;

namespace GCWZeroManager
{
    /// <summary>
    /// Interaction logic for TransferProgressWindow.xaml
    /// </summary>
    public partial class TransferProgressWindow : Window
    {
        struct WorkerThreadArgsUpload
        {
            public string RemotePath;
            public TransferDirectory TransferDirectory;
        }

        struct WorkerThreadArgsDownload
        {
            public string SourcePath;
            public List<string> Files;
            public string LocalPath;
        }

        BackgroundWorker workerThread;
        ScpClient scpClient = null;
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

        public bool UploadFiles(List<OPKFile> files, string remotePath)
        {
            TransferDirectory dir = new TransferDirectory("");

            foreach (OPKFile opk in files)
            {
                TransferFile file = new TransferFile(opk.LocalPath, opk.Size.Bytes);
                dir.AddFile(file);
            }

            return UploadFiles(dir, remotePath);
        }

        public bool UploadFiles(TransferDirectory dir, string remotePath)
        {
            int totalFiles = dir.GetTotalFiles();
            long totalSize = dir.GetTotalSize();

            if (totalFiles == 0)
            {
                MessageBox.Show("No files to upload", "No files", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            labelTotalBytesData.Content = HelperTools.GetFormattedSize(totalSize);
            labelTotalFilesData.Content = totalFiles;
            labelFilesRemainingData.Content = totalFiles;

            if (!ConnectionManager.Instance.IsConnected)
            {
                if (!ConnectionManager.Instance.Connect())
                {
                    MessageBox.Show("Connection failed!", "Connection failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            WorkerThreadArgsUpload args = new WorkerThreadArgsUpload();
            args.RemotePath = remotePath;
            args.TransferDirectory = dir;

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

        public void DownloadFiles(string sourcePath, List<string> files, string localPath)
        {
            if (files.Count == 0)
            {
                MessageBox.Show("No files to download", "No files", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!ConnectionManager.Instance.IsConnected)
            {
                if (!ConnectionManager.Instance.Connect())
                {
                    MessageBox.Show("Connection failed!", "Connection failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            WorkerThreadArgsDownload args = new WorkerThreadArgsDownload();
            args.SourcePath = sourcePath;
            args.Files = files;
            args.LocalPath = localPath;

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

        bool DoUploadFiles(TransferState state, string remotePath, TransferDirectory directory)
        {
            // Append folder to path. If name is empty, it's the root directory of this transfer.
            if (!string.IsNullOrEmpty(remotePath))
                remotePath = Path.Combine(remotePath, directory.Name).Replace('\\', '/');

            // Create directory if needed
            if (!state.Sftp.Exists(remotePath))
            {
                try
                {
                    state.Sftp.CreateDirectory(remotePath);
                }
                catch (Exception)
                {
                    state.StopWithError("Failed to create directory");
                    return false;
                }
            }

            // Get a list of files in the target directory to determine if a file already exists.
            // It's faster this way rather than doing it per file.
            List<string> existingFiles = new List<string>();
            try
            {
                foreach (SftpFile file in state.Sftp.ListDirectory(remotePath))
                {
                    if (file.IsRegularFile)
                        existingFiles.Add(file.Name);
                }
            }
            catch (SshConnectionException)
            {
                ConnectionManager.Instance.Disconnect(false);
                MessageBox.Show("Connection lost!", "Connection lost", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Transfer each file in the directory object
            foreach (var file in directory.Files)
            {
                if (state.CancellationPending)
                {
                    state.Cancel();
                    return true;
                }

                if (existingFiles.Contains(file.Name))
                {
                    var overwritePrompt = MessageBox.Show("File " + file.Name + " already exists, do you want to overwrite?", "File exists", MessageBoxButton.YesNo, MessageBoxImage.Question); // TODO replace all
                    if (overwritePrompt == MessageBoxResult.No)
                    {
                        state.Progress.FilesRemaining--;
                        state.BytesLeft -= file.Size;
                        continue;
                    }
                }

                if (state.CancellationPending)
                {
                    state.Cancel();
                    return true;
                }

                try
                {
                    int totalPercent;

                    state.Scp.Uploading += new EventHandler<ScpUploadEventArgs>(delegate (object _sender, ScpUploadEventArgs _e)
                    {
                        if (okToUpdate)
                        {
                            state.Progress.CurFileProgressPercent = _e.Size == 0 ? 100 : (int)((_e.Uploaded * 100) / _e.Size);
                            totalPercent = 100 - (state.TotalBytes == 0 ? 0 : (int)(((state.BytesLeft - _e.Uploaded) * 100) / state.TotalBytes));
                            state.Worker.ReportProgress(totalPercent, state.Progress);

                            okToUpdate = false;
                        }
                    });

                    bool errorHandled = false;
                    state.Scp.ErrorOccurred += new EventHandler<Renci.SshNet.Common.ExceptionEventArgs>(delegate (object _sender, Renci.SshNet.Common.ExceptionEventArgs _e)
                    {
                        if (errorHandled)
                            return;

                        errorHandled = true;
                        // Try to clean up
                        try
                        {
                            ConnectionManager.Instance.DeleteFile(Path.Combine(remotePath, file.Name).Replace('\\', '/'));
                        }
                        catch (Exception)
                        { }
                    });

                    state.Scp.Upload(new FileInfo(file.SourcePath), remotePath);

                    state.Progress.FilesRemaining--;
                    state.BytesLeft -= file.Size;
                    totalPercent = 100 - (state.TotalBytes == 0 ? 0 : (int)((state.BytesLeft * 100) / state.TotalBytes));
                    state.Worker.ReportProgress(totalPercent, state.Progress);
                }
                catch (ScpException se)
                {
                    state.StopWithError(se.Message, false);
                    return false;
                }
                catch (SocketException)
                {
                    // Was this due to a cancellation?
                    if (state.CancellationPending)
                    {
                        state.Cancel();
                        return true;
                    }
                    else
                    {
                        state.StopWithError("Error");
                        return false;
                    }
                }
                catch (SshOperationTimeoutException se)
                {
                    state.StopWithError(se.Message);
                    return false;
                }
                catch (SshConnectionException)
                {
                    state.StopWithError("Error");
                    return false;
                }
            }

            bool result = true;

            // Recurse for each subfolder
            foreach (var subDir in directory.Directories)
            {
                result = result && DoUploadFiles(state, remotePath, subDir);

                if (state.IsCancelled)
                    return true;

                if (!result)
                    return false;
            }

            return result;
        }

        bool DoDownloadFiles(TransferState state, string localPath, TransferDirectory directory)
        {
            // Append folder to path. If name is empty, it's the root directory of this transfer.
            if (!string.IsNullOrEmpty(localPath))
                localPath = Path.Combine(localPath, directory.Name);

            // Create directory if needed
            if (!Directory.Exists(localPath))
            {
                try
                {
                    Directory.CreateDirectory(localPath);
                }
                catch (Exception)
                {
                    state.StopWithError("Failed to create directory", false);
                    return false;
                }
            }

            // Transfer each file in the directory object
            foreach (var file in directory.Files)
            {
                if (state.CancellationPending)
                {
                    state.Cancel();
                    return true;
                }

                if (File.Exists(Path.Combine(localPath, file.Name)))
                {
                    var overwritePrompt = MessageBox.Show("File " + file.Name + " already exists, do you want to overwrite?", "File exists", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (overwritePrompt == MessageBoxResult.No)
                    {
                        state.Progress.FilesRemaining--;
                        state.BytesLeft -= file.Size;
                        continue;
                    }
                }

                if (state.CancellationPending)
                {
                    state.Cancel();
                    return true;
                }

                try
                {
                    int totalPercent;

                    state.Scp.Downloading += new EventHandler<ScpDownloadEventArgs>(delegate (object _sender, ScpDownloadEventArgs _e)
                    {
                        if (okToUpdate)
                        {
                            state.Progress.CurFileProgressPercent = _e.Size == 0 ? 100 : (int)((_e.Downloaded * 100) / _e.Size);
                            totalPercent = 100 - (state.TotalBytes == 0 ? 0 : (int)(((state.BytesLeft - _e.Downloaded) * 100) / state.TotalBytes));
                            state.Worker.ReportProgress(totalPercent, state.Progress);

                            okToUpdate = false;
                        }
                    });

                    bool errorHandled = false;
                    state.Scp.ErrorOccurred += new EventHandler<Renci.SshNet.Common.ExceptionEventArgs>(delegate (object _sender, Renci.SshNet.Common.ExceptionEventArgs _e)
                    {
                        if (errorHandled)
                            return;

                        errorHandled = true;
                        // Try to clean up
                        try
                        {
                            if (File.Exists(Path.Combine(localPath, file.Name)))
                                File.Delete(Path.Combine(localPath, file.Name));
                        }
                        catch (Exception)
                        { }
                    });

                    state.Scp.Download(file.SourcePath, new FileInfo(Path.Combine(localPath, file.Name)));

                    state.Progress.FilesRemaining--;
                    state.BytesLeft -= file.Size;
                    totalPercent = 100 - (state.TotalBytes == 0 ? 0 : (int)((state.BytesLeft * 100) / state.TotalBytes));
                    state.Worker.ReportProgress(totalPercent, state.Progress);
                }
                catch (ScpException se)
                {
                    state.StopWithError(se.Message, false);
                    return false;
                }
                catch (SocketException)
                {
                    // Was this due to a cancellation?
                    if (state.CancellationPending)
                    {
                        state.Cancel();
                        return true;
                    }
                    else
                    {
                        state.StopWithError("Error");
                        return false;
                    }
                }
                catch (SshOperationTimeoutException se)
                {
                    state.StopWithError(se.Message);
                    return false;
                }
                catch (SshConnectionException)
                {
                    state.StopWithError("Error");
                    return false;
                }
            }

            bool result = true;

            // Recurse for each subfolder
            foreach (var subDir in directory.Directories)
            {
                result = result && DoDownloadFiles(state, localPath, subDir);

                if (state.IsCancelled)
                    return true;

                if (!result)
                    return false;
            }

            return result;
        }

        TransferDirectory CollectFilesForDownload(string sourcePath, List<string> files, SftpClient sftp)
        {
            var directory = new TransferDirectory("");

            var filesInChosenDir = sftp.ListDirectory(sourcePath);
            foreach (var file in filesInChosenDir)
            {
                if (file.Name == ".." || file.Name == ".")
                    continue;

                if (files.Contains(file.Name))
                {
                    if (file.IsDirectory)
                    {

                        var subDir = new TransferDirectory(file.Name);
                        directory.AddDirectory(subDir);
                        CollectFilesForDownload(sourcePath, subDir, sftp);
                    }
                    else if (file.IsRegularFile)
                    {
                        var transferFile = new TransferFile(file.FullName, file.Length);
                        directory.AddFile(transferFile);
                    }
                }
            }

            return directory;
        }

        void CollectFilesForDownload(string sourcePath, TransferDirectory directory, SftpClient sftp)
        {
            if (!string.IsNullOrEmpty(sourcePath))
                sourcePath = Path.Combine(sourcePath, directory.Name).Replace('\\', '/');

            var files = sftp.ListDirectory(sourcePath);
            foreach (var file in files)
            {
                if (file.Name == ".." || file.Name == ".")
                    continue;

                if (file.IsDirectory)
                {
                    var subDir = new TransferDirectory(file.Name);
                    directory.AddDirectory(subDir);
                    CollectFilesForDownload(sourcePath, subDir, sftp);
                }
                else if (file.IsRegularFile)
                {
                    var transferFile = new TransferFile(file.FullName, file.Length);
                    directory.AddFile(transferFile);
                }
            }
        }

        private void workerThread_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;
            var uploadDir = (e.Argument as WorkerThreadArgsUpload?)?.TransferDirectory;

            ProgressState progress = new ProgressState();
            progress.CurFileProgressPercent = 0;
            if (uploadDir != null)
            {
                // This is only set for upload here since we don't know these things for download yet, we have to scan the directories first, which is done further down.
                // Also, it's done here so the values are updated immediately, and not delayed as they would be if they were updated after connecting.
                int totalFiles = uploadDir.GetTotalFiles();
                long totalSize = uploadDir.GetTotalSize();
                progress.FilesRemaining = totalFiles;
                progress.TotalFiles = totalFiles;
                progress.TotalBytes = totalSize;
            }

            progress.Status = "Connecting...";
            workerThread.ReportProgress(0, progress);

            var scp = ConnectionManager.Instance.ConnectWithActiveConnectionSCP();

            if (scp == null || !scp.IsConnected)
            {
                MessageBox.Show("Connection failed!", "Connection failed (2)", MessageBoxButton.OK, MessageBoxImage.Error);

                e.Result = new WorkerCompletedArgs(scp, false, "Connection Failed!");
                return;
            }

            scpClient = scp;

            var sftp = ConnectionManager.Instance.GetActiveSftpConnection();

            System.Timers.Timer updateTimer = new System.Timers.Timer();
            updateTimer.Elapsed += OnUpdateTimer;
            updateTimer.Interval = 20;
            updateTimer.Enabled = true;

            if (e.Argument is WorkerThreadArgsUpload)
            {
                progress.Status = "Transferring...";
                workerThread.ReportProgress(0, progress);

                WorkerThreadArgsUpload args = (WorkerThreadArgsUpload)e.Argument;
                string remotePath = args.RemotePath;
                long totalBytes = uploadDir.GetTotalSize();
                var state = new TransferState() { Worker = worker, WorkerEventArgs = e, Progress = progress, UpdateTimer = updateTimer, Scp = scp, Sftp = sftp, TotalBytes = totalBytes, BytesLeft = totalBytes };

                var result = DoUploadFiles(state, remotePath, args.TransferDirectory);
                if (!result)
                    return;
            }
            else if (e.Argument is WorkerThreadArgsDownload)
            {
                progress.Status = "Scanning folders...";
                workerThread.ReportProgress(0, progress);

                WorkerThreadArgsDownload args = (WorkerThreadArgsDownload)e.Argument;
                var transferDirectory = CollectFilesForDownload(args.SourcePath, args.Files, sftp);

                int totalFiles = transferDirectory.GetTotalFiles();
                long totalSize = transferDirectory.GetTotalSize();

                progress.FilesRemaining = totalFiles;
                progress.TotalFiles = totalFiles;
                progress.TotalBytes = totalSize;
                progress.Status = "Transferring...";
                workerThread.ReportProgress(0, progress);

                long totalBytes = transferDirectory.GetTotalSize();
                var state = new TransferState() { Worker = worker, WorkerEventArgs = e, Progress = progress, UpdateTimer = updateTimer, Scp = scp, Sftp = sftp, TotalBytes = totalBytes, BytesLeft = totalBytes };

                var result = DoDownloadFiles(state, args.LocalPath, transferDirectory);
                if (!result)
                    return;
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
            labelTotalBytesData.Content = HelperTools.GetFormattedSize(progress.TotalBytes);
            labelTotalFilesData.Content = progress.TotalFiles;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (workerThread != null && workerThread.IsBusy)
            {
                workerThread.CancelAsync();
                if (scpClient != null && scpClient.IsConnected)
                {
                    scpClient.Disconnect();
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
        public int CurFileProgressPercent { get; set; }
        public string Status { get; set; }
        public int FilesRemaining { get; set; }
        public int TotalFiles { get; set; }
        public long TotalBytes { get; set; }

        public ProgressState()
        {
        }

        public ProgressState(int curFileProgressPercent, string status, int filesRemaining, int totalFiles, long totalBytes)
        {
            CurFileProgressPercent = curFileProgressPercent;
            Status = status;
            FilesRemaining = filesRemaining;
            TotalFiles = totalFiles;
            TotalBytes = totalBytes;
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
