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

namespace GCWZeroManager
{
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

        private ConnectionNodeHolder connections = new ConnectionNodeHolder();

        private ConnectionManager()
        {
        }

        private string opkDir = "/boot/apps/";
        private string username = "root";
        private string passphrase = "";
        private bool passphraseOk = false;

        public void AddConnection(ConnectionNode cn)
        {
            connections.AddConnection(cn);
        }

        public ConnectionNodeHolder Connections
        {
            get { return connections; }
            set { connections = value; }
        }

        public string OPKDir
        {
            get { return opkDir; }
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
                connectionInfo = new PasswordConnectionInfo(conn.Host, username, conn.Password);
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
            scp.BufferSize = 8192;

            try
            {
                scp.Connect();
            }
            catch (Exception)
            {
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

            SftpClient sftp = new SftpClient(connectionInfo);
            sftp.BufferSize = 8192;

            try
            {
                sftp.Connect();
            }
            catch (Exception)
            {
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

            SshClient ssh = new SshClient(connectionInfo);

            try
            {
                ssh.Connect();
            }
            catch (Exception)
            {
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

        public bool DeleteFiles(List<OPKFile> filesToDelete)
        {
            SftpClient sftp = ConnectWithActiveConnectionSFTP();
            if (sftp == null || !sftp.IsConnected)
                return false;

            foreach (OPKFile file in filesToDelete)
            {
                sftp.DeleteFile(opkDir + file.Filename);
            }

            sftp.Disconnect();

            return true;
        }

        public bool UploadFiles(List<OPKFile> filesToUpload)
        {
            SftpClient sftp = ConnectWithActiveConnectionSFTP();
            if (sftp == null || !sftp.IsConnected)
                return false;

            List<string> fileList = GetOPKFilenameList(sftp);

            sftp.Disconnect();

            ScpClient scp = ConnectWithActiveConnectionSCP();

            foreach (OPKFile opk in filesToUpload)
            {
                if (fileList.Contains(opk.Filename))
                {
                    MessageBoxResult result = MessageBox.Show("File " + opk.Filename + " already exists, do you want to overwrite?", "File exists", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.No)
                        continue;
                }

                Stream fs = File.OpenRead(opk.Path);

                try
                {
                    string tempFilename = opkDir + opk.Filename;
                    //scp.Upload(fs, tempFilename);
                    scp.Uploading += new EventHandler<ScpUploadEventArgs>(scp_Uploading);
                    scp.Upload(new FileInfo(opk.Path), opkDir);
                }
                catch (SshException se)
                {
                    MessageBox.Show("Error: " + se.Message);
                    return false;
                }
            }

            scp.Disconnect();

            return true;
        }

        void scp_Uploading(object sender, ScpUploadEventArgs e)
        {
            //Console.WriteLine("LOL");
        }

        public bool InstallPublicKey(ConnectionNode cn, string publicKey, out string result)
        {
            SshClient ssh = ConnectSSH(cn);
            if (ssh == null || !ssh.IsConnected)
            {
                result = "Connection failed";
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

        public List<FileNode> ListFiles(string directory)
        {
            SftpClient sftp = ConnectWithActiveConnectionSFTP();
            if (sftp == null || !sftp.IsConnected)
                return null;

            List<FileNode> list = new List<FileNode>();

            foreach (SftpFile file in sftp.ListDirectory(directory))
            {
                if (file.Name == "." || file.Name == "..")
                    continue;

                FileNode opk = new FileNode();
                opk.Filename = file.Name;
                opk.Size = new SizeElement(file.Length);

                if (file.IsRegularFile)
                    opk.FileType = FileType.RegularFile;
                else if (file.IsDirectory)
                    opk.FileType = FileType.Directory;
                else
                    opk.FileType = FileType.Other;

                list.Add(opk);
            }

            sftp.Disconnect();

            return list;
        }
    }
}
