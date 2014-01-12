using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GCWZeroManager
{
    public enum FileType { RegularFile, Directory, SymLink, Other };
    public class FileNode
    {
        public string Filename { get; set; }
        public SizeElement Size { get; set; }
        public FileType FileType { get; set; }
    }
}
