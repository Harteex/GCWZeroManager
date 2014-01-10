using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace GCWZeroManager
{
    public class ConfigurationManager
    {
        private static ConfigurationManager instance;
        public static ConfigurationManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new ConfigurationManager();

                return instance;
            }
        }

        private string folder = "gcwzeromanager";
        private string filenameConnections = "connections.xml";

        private ConfigurationManager()
        {
        }

        public string GetSaveFilePath()
        {
            string appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(new string[] { appDataDir, folder });
        }

        public void LoadConnections()
        {
            string dir = GetSaveFilePath();
            string path = Path.Combine(new string[] { dir, filenameConnections });
            if (!File.Exists(path))
            {
                ConnectionManager.Instance.Connections = new ConnectionNodeHolder();
                return;
            }

            var serializer = new XmlSerializer(typeof(ConnectionNodeHolder));
            var stream = new FileStream(path, FileMode.Open);
            var container = serializer.Deserialize(stream) as ConnectionNodeHolder;
            stream.Close();

            ConnectionManager.Instance.Connections = container;
        }

        public void SaveConnections()
        {
            string dir = GetSaveFilePath();
            if (!Directory.Exists(dir))
            {
                // Create directory if it doesn't exist
                Directory.CreateDirectory(dir);
            }

            string path = Path.Combine(new string[] { dir, filenameConnections });

            var serializer = new XmlSerializer(typeof(ConnectionNodeHolder));
            var stream = new FileStream(path, FileMode.Create);
            serializer.Serialize(stream, ConnectionManager.Instance.Connections);
            stream.Close();
        }
    }
}
