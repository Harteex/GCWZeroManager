using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;
using System.Threading.Tasks;

namespace GCWZeroManager
{
    public enum ConnectionState { Connected, DisconnectedNormal, DisconnectedError }
    public class ConnectionStateEventArgs : EventArgs
    {
        public ConnectionStateEventArgs(ConnectionState state)
        {
            this.state = state;
        }
        private ConnectionState state;
        public ConnectionState ConnectionState
        {
            get { return state; }
        }
    }

    public class ConnectionManager
    {
        // Handles connection configurations and the current active connection

        private static ConnectionManager instance;
        public static ConnectionManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new ConnectionManager();

                return instance;
            }
        }

        private SftpClient activeSftp = null;
        private SshClient activeSsh = null;

        private ConnectionNodeHolder connections = new ConnectionNodeHolder();

        private ConnectionManager()
        {
        }

        private string opkDir = "/media/data/apps/";
        private string homeDir = "/usr/local/home/";
        private string username = "root";
        private string passphrase = "";
        private bool passphraseOk = false;
        private string lastError = "No error";

        private TimeSpan connectingTimeout = new TimeSpan(0, 0, 10);
        private TimeSpan operationTimeout = new TimeSpan(0, 0, 8);

        public event EventHandler<ConnectionStateEventArgs> ConnectionStateChanged;
        protected virtual void OnConnectionStateChanged(ConnectionStateEventArgs e)
        {
            if (ConnectionStateChanged != null)
                ConnectionStateChanged(this, e);
        }

        public void AddConnection(ConnectionNode cn)
        {
            connections.AddConnection(cn);
        }

        public ConnectionNodeHolder Connections
        {
            get { return connections; }
            set { connections = value; }
        }

        public string OPKDirectory
        {
            get { return opkDir; }
        }

        public string HomeDirectory
        {
            get { return homeDir; }
        }

        public bool IsConnected
        {
            get
            {
                if (activeSftp == null || activeSsh == null)
                    return false;

                if (!activeSftp.IsConnected || !activeSsh.IsConnected)
                {
                    Disconnect(true);
                    return false;
                }

                return true;
            }
        }

        public bool Connect()
        {
            ConnectionNode conn = connections.GetActiveConnection();
            if (conn == null)
            {
                MessageBox.Show("No active connection selected!", "No active connection", MessageBoxButton.OK, MessageBoxImage.Error); // FIXME this is probably more appropriate elsewhere?
                OnConnectionStateChanged(new ConnectionStateEventArgs(ConnectionState.DisconnectedError));
                return false;
            }

            if (activeSftp != null && activeSftp.IsConnected)
                activeSftp.Disconnect();
            if (activeSsh != null && activeSsh.IsConnected)
                activeSsh.Disconnect();

            activeSftp = ConnectSFTP(conn);
            if (activeSftp == null)
            {
                OnConnectionStateChanged(new ConnectionStateEventArgs(ConnectionState.DisconnectedError));
                return false;
            }

            activeSsh = ConnectSSH(conn);
            if (activeSsh == null)
            {
                activeSftp.Disconnect();
                activeSftp = null;
                OnConnectionStateChanged(new ConnectionStateEventArgs(ConnectionState.DisconnectedError));
                return false;
            }

            OnConnectionStateChanged(new ConnectionStateEventArgs(ConnectionState.Connected));

            return true;
        }

        private void DisconnectOld(SftpClient sftp, SshClient ssh)
        {
            try
            {
                if (sftp != null && sftp.IsConnected)
                {
                    sftp.Disconnect();
                }
            }
            catch (Exception)
            { }

            try
            {
                if (ssh != null && ssh.IsConnected)
                {
                    ssh.Disconnect();
                }
            }
            catch (Exception)
            { }
        }

        public void Disconnect(bool becauseError)
        {
            SftpClient oldSftp = activeSftp;
            SshClient oldSsh = activeSsh;

            activeSftp = null;
            activeSsh = null;

            Task disconnectOldTask = Task.Factory.StartNew(() => DisconnectOld(oldSftp, oldSsh));

            if (becauseError)
            {
                OnConnectionStateChanged(new ConnectionStateEventArgs(ConnectionState.DisconnectedError));
            }
            else
            {
                OnConnectionStateChanged(new ConnectionStateEventArgs(ConnectionState.DisconnectedNormal));
            }
        }

        public List<FileNode> ListFiles(string directory)
        {
            if (activeSftp == null || !activeSftp.IsConnected)
                return null;

            List<FileNode> list = new List<FileNode>();

            foreach (SftpFile file in activeSftp.ListDirectory(directory))
            {
                if (file.Name == "." || file.Name == "..")
                    continue;

                FileNode fileNode = new FileNode();
                fileNode.Filename = file.Name;
                fileNode.Size = new SizeElement(-1);

                if (file.IsDirectory)
                {
                    fileNode.FileType = FileType.Directory;
                }
                else if (file.IsSymbolicLink)
                {
                    fileNode.FileType = FileType.SymLink;
                }
                else if (file.IsRegularFile)
                {
                    fileNode.FileType = FileType.RegularFile;
                    fileNode.Size = new SizeElement(file.Length); // Only set file size for files
                }
                else
                {
                    fileNode.FileType = FileType.Other;
                }

                list.Add(fileNode);
            }

            return list;
        }

        public List<OPKFile> ListOPKs()
        {
            if (activeSftp == null || !activeSftp.IsConnected)
                return null;

            List<OPKFile> list = new List<OPKFile>();

            foreach (SftpFile file in activeSftp.ListDirectory(opkDir))
            {
                if (file.IsRegularFile)
                {
                    OPKFile opk = new OPKFile();
                    opk.Filename = file.Name;
                    opk.Title = file.Name; // FIXME load this somehow in a thread later somewhere
                    opk.Size = new SizeElement(file.Length);
                    list.Add(opk);
                }
            }

            return list;
        }

        public SftpClient GetActiveSftpConnection()
        {
            if (activeSftp != null && !activeSftp.IsConnected)
            {
                if (activeSsh != null && activeSsh.IsConnected)
                    activeSsh.Disconnect();

                activeSftp = null;
                activeSsh = null;
            }

            return activeSftp;
        }

        public SshClient GetActiveSshConnection()
        {
            if (activeSsh != null && !activeSsh.IsConnected)
            {
                if (activeSftp != null && activeSftp.IsConnected)
                    activeSftp.Disconnect();

                activeSftp = null;
                activeSsh = null;
            }

            return activeSsh;
        }

        public bool CreateFolder(string path, string folderName)
        {
            if (activeSftp == null || !activeSftp.IsConnected)
                return false;

            string pathToCreate = path + (path.EndsWith("/") ? "" : "/") + folderName;

            activeSftp.CreateDirectory(pathToCreate);

            return true;
        }

        public bool DeleteFile(string path)
        {
            if (activeSftp == null || !activeSftp.IsConnected)
                return false;

            activeSftp.DeleteFile(path);
            return true;
        }

        public bool DeleteDirectory(string path)
        {
            if (activeSftp == null || !activeSftp.IsConnected)
                return false;

            activeSftp.DeleteDirectory(path);
            return true;
        }

        public bool DeleteFiles(List<OPKFile> filesToDelete)
        {
            if (activeSftp == null || !activeSftp.IsConnected)
                return false;

            foreach (OPKFile file in filesToDelete)
            {
                activeSftp.DeleteFile(opkDir + file.Filename);
            }

            return true;
        }

        public bool DeleteFiles(List<string> paths)
        {
            if (activeSftp == null || !activeSftp.IsConnected)
                return false;

            foreach (string path in paths)
            {
                activeSftp.DeleteFile(path);
            }

            return true;
        }

        public bool DeleteDirectories(List<string> paths)
        {
            if (activeSftp == null || !activeSftp.IsConnected)
                return false;

            foreach (string path in paths)
            {
                activeSftp.DeleteDirectory(path);
            }

            return true;
        }

        public ConnectionInfo GetConnectionInfo(ConnectionNode conn)
        {
            ConnectionInfo connectionInfo = null;
            if (conn == null)
                return null;

            if (conn.AuthenticationMethod == AuthenticationMethod.PrivateKey)
            {
                try
                {
                    if (passphraseOk)
                        connectionInfo = new PrivateKeyConnectionInfo(conn.Host, username, new PrivateKeyFile(conn.PrivateKey, passphrase));
                    else
                        connectionInfo = new PrivateKeyConnectionInfo(conn.Host, username, new PrivateKeyFile(conn.PrivateKey));
                }
                catch (SshPassPhraseNullOrEmptyException)
                {
                    while (true)
                    {
                        PasswordInputDialog dialog = new PasswordInputDialog();
                        dialog.ShowDialog();
                        if (dialog.DialogResult.HasValue && dialog.DialogResult.Value)
                        {
                            passphrase = dialog.Passphrase;
                        }
                        else
                        {
                            return null;
                        }

                        try
                        {
                            connectionInfo = new PrivateKeyConnectionInfo(conn.Host, username, new PrivateKeyFile(conn.PrivateKey, passphrase));
                        }
                        catch (Exception)
                        {
                            continue;
                        }

                        passphraseOk = true;

                        break;
                    }
                }
            }
            else
            {
                connectionInfo = new PasswordConnectionInfo(conn.Host, username, conn.Password); // FIXME Handle ArgumentException
            }

            connectionInfo.Timeout = TimeSpan.FromSeconds(10);

            return connectionInfo;
        }

        public ScpClient ConnectSCP(ConnectionNode conn)
        {
            return ConnectSCP(GetConnectionInfo(conn));
        }

        public ScpClient ConnectSCP(ConnectionInfo connectionInfo)
        {
            if (connectionInfo == null)
                return null;

            ScpClient scp = new ScpClient(connectionInfo);
            // OperationTimeout didn't seem to work here?
            scp.BufferSize = 8192;

            try
            {
                scp.Connect();
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                return null;
            }

            return scp;
        }

        public SftpClient ConnectSFTP(ConnectionNode conn)
        {
            return ConnectSFTP(GetConnectionInfo(conn));
        }

        public SftpClient ConnectSFTP(ConnectionInfo connectionInfo)
        {
            if (connectionInfo == null)
                return null;

            connectionInfo.Timeout = connectingTimeout;
            SftpClient sftp = new SftpClient(connectionInfo);
            sftp.OperationTimeout = operationTimeout;
            sftp.BufferSize = 8192;

            try
            {
                sftp.Connect();
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                return null;
            }

            return sftp;
        }

        public SshClient ConnectSSH(ConnectionNode conn)
        {
            return ConnectSSH(GetConnectionInfo(conn));
        }

        public SshClient ConnectSSH(ConnectionInfo connectionInfo)
        {
            if (connectionInfo == null)
                return null;

            connectionInfo.Timeout = connectingTimeout;
            SshClient ssh = new SshClient(connectionInfo);

            try
            {
                ssh.Connect();
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                return null;
            }

            return ssh;
        }

        public bool TestConnection(ConnectionNode cn)
        {
            bool ret;
            ScpClient scp = ConnectSCP(cn);
            if (scp == null || !scp.IsConnected)
            {
                ret = false;
            }
            else
            {
                ret = true;
                scp.Disconnect();
            }

            return ret;
        }

        public ScpClient ConnectWithActiveConnectionSCP()
        {
            ConnectionNode conn = connections.GetActiveConnection();
            if (conn == null)
            {
                MessageBox.Show("No active connection selected!", "No active connection", MessageBoxButton.OK, MessageBoxImage.Error); // FIXME this is probably more appropriate elsewhere?
                return null;
            }

            return ConnectSCP(conn);
        }

        public SftpClient ConnectWithActiveConnectionSFTP()
        {
            ConnectionNode conn = connections.GetActiveConnection();
            if (conn == null)
            {
                MessageBox.Show("No active connection selected!", "No active connection", MessageBoxButton.OK, MessageBoxImage.Error); // FIXME this is probably more appropriate elsewhere?
                return null;
            }

            return ConnectSFTP(conn);
        }

        public SshClient ConnectWithActiveConnectionSSH()
        {
            ConnectionNode conn = connections.GetActiveConnection();
            if (conn == null)
            {
                MessageBox.Show("No active connection selected!", "No active connection", MessageBoxButton.OK, MessageBoxImage.Error); // FIXME this is probably more appropriate elsewhere?
                return null;
            }

            return ConnectSSH(conn);
        }

        private List<string> GetOPKFilenameList(SftpClient sftp)
        {
            if (sftp == null || !sftp.IsConnected)
                return null;

            List<string> list = new List<string>();

            foreach (SftpFile file in sftp.ListDirectory(opkDir))
            {
                if (file.IsRegularFile)
                    list.Add(file.Name);
            }

            return list;
        }

        public List<OPKFile> GetOPKList()
        {
            SftpClient sftp = ConnectWithActiveConnectionSFTP();
            if (sftp == null || !sftp.IsConnected)
                return null;

            List<OPKFile> list = new List<OPKFile>();

            foreach (SftpFile file in sftp.ListDirectory(opkDir))
            {
                if (file.IsRegularFile)
                {
                    OPKFile opk = new OPKFile();
                    opk.Filename = file.Name;
                    opk.Title = file.Name; // FIXME load this somehow in a thread later somewhere
                    opk.Size = new SizeElement(file.Length);
                    list.Add(opk);
                }
            }

            sftp.Disconnect();

            return list;
        }

        public bool InstallPublicKey(ConnectionNode cn, string publicKey, out string result)
        {
            SshClient ssh = ConnectSSH(cn);
            if (ssh == null || !ssh.IsConnected)
            {
                result = "Connection failed (" + lastError + ")";
                return false;
            }

            try
            {
                string pubkey = File.ReadAllText(publicKey);
                pubkey = pubkey.Trim();
                if (pubkey.Contains("\n"))
                {
                    result = "Unhandled public key format, the key should be a single line of text";
                    return false;
                }

                pubkey = pubkey.Replace("\\", "\\\\");
                pubkey = pubkey.Replace("\"", "\\\"");

                SshCommand cmd1 = ssh.CreateCommand("mkdir ~/.ssh");
                cmd1.Execute();
                SshCommand cmd2 = ssh.CreateCommand("chmod 700 ~/.ssh");
                cmd2.Execute();
                SshCommand cmd3 = ssh.CreateCommand("echo \"" + pubkey + "\" >> ~/.ssh/authorized_keys");
                cmd3.Execute();
                if (cmd3.Error != "")
                {
                    result = "Error while writing key: " + cmd3.Error;
                    return false;
                }
                SshCommand cmd4 = ssh.CreateCommand("chmod 600 ~/.ssh/authorized_keys");
                cmd4.Execute();
            }
            catch (Exception e)
            {
                result = "Exception (" + e.Message + ")";
                return false;
            }

            result = "OK";
            return true;
        }
    }
}
