using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace GCWZeroManager
{
    public class ConfigurationSettings
    {
        [XmlAttribute("HasShownDragNDropNoticeInstallOpk")]
        private bool hasShownDragNDropNoticeInstallOpk;

        [XmlAttribute("HasShownDragNDropNoticeFiles")]
        private bool hasShownDragNDropNoticeFiles;

        public ConfigurationSettings()
        {
            hasShownDragNDropNoticeInstallOpk = false;
            hasShownDragNDropNoticeFiles = false;
        }

        public bool HasShownDragNDropNoticeInstallOpk
        {
            get { return hasShownDragNDropNoticeInstallOpk; }
            set { hasShownDragNDropNoticeInstallOpk = value; }
        }

        public bool HasShownDragNDropNoticeFiles
        {
            get { return hasShownDragNDropNoticeFiles; }
            set { hasShownDragNDropNoticeFiles = value; }
        }
    }
}
