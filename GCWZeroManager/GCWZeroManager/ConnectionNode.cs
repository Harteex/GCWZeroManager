using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace GCWZeroManager
{
    public enum AuthenticationMethod { PrivateKey, Password };

    public class ConnectionNode
    {
        [XmlAttribute("Host")]
        private string host = null;

        [XmlAttribute("AuthenticationMethod")]
        private AuthenticationMethod authenticationMethod = 0;

        [XmlAttribute("PrivateKey")]
        private string privateKey = null;

        [XmlAttribute("Password")]
        private string password = null;

        public ConnectionNode()
        {
        }

        public string Host
        {
            get { return host; }
            set { host = value; }
        }

        public AuthenticationMethod AuthenticationMethod
        {
            get { return authenticationMethod; }
            set { authenticationMethod = value; }
        }

        public string PrivateKey
        {
            get { return privateKey; }
            set { privateKey = value; }
        }

        public string Password
        {
            get { return password; }
            set { password = value; }
        }

        public override string ToString()
        {
            return host;
        }
    }
}
