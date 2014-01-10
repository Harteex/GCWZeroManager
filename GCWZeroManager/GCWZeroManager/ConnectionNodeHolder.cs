using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace GCWZeroManager
{
    [XmlRoot("ConnectionNodeHolder")]
    public class ConnectionNodeHolder
    {
        [XmlArray("ConnectionNodes"), XmlArrayItem("ConnectionNode")]
        private List<ConnectionNode> connections = new List<ConnectionNode>();

        [XmlAttribute("ActiveConnectionIndex")]
        private int activeConnectionIndex;

        public ConnectionNodeHolder()
        {
        }

        public List<ConnectionNode> Connections
        {
            get { return connections; }
            set { connections = value; }
        }

        public int ActiveConnectionIndex
        {
            get { return activeConnectionIndex; }
            set { activeConnectionIndex = value; }
        }

        public ConnectionNode GetActiveConnection()
        {
            if (activeConnectionIndex >= connections.Count)
                return null;

            return connections[activeConnectionIndex];
        }

        public void SetActiveConnection(ConnectionNode cn)
        {
            if (cn == null)
                activeConnectionIndex = 0;

            for (int i = 0; i < connections.Count; i++)
            {
                if (connections[i] == cn)
                {
                    activeConnectionIndex = i;
                    break;
                }
            }
        }

        public void AddConnection(ConnectionNode cn)
        {
            connections.Add(cn);
            SetActiveConnection(cn);
        }

        public void DeleteActiveConnection()
        {
            connections.Remove(GetActiveConnection());
            activeConnectionIndex = 0;
        }
    }
}
