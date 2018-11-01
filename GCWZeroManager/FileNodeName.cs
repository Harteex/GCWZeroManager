using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GCWZeroManager
{
    // The purpose of this class is to allow sorting to work correctly. We want folders to be sorted before any other type.
    public class FileNodeName : IComparable
    {
        public string Name { get; private set; }
        public FileType Type { get; private set; }

        public FileNodeName(string name, FileType type)
        {
            this.Name = name ?? "";
            this.Type = type;
        }

        public override string ToString()
        {
            return Name;
        }

        public int CompareTo(object other)
        {
            var o = other as FileNodeName;

            // Currently we treat symlinks as folders, which is not always the case.
            // This needs to be fixed throughout the application.
            if ((Type == FileType.Directory || Type == FileType.SymLink) && (o.Type != FileType.Directory && o.Type != FileType.SymLink))
                return -1;
            if ((Type != FileType.Directory && Type != FileType.SymLink) && (o.Type == FileType.Directory || o.Type == FileType.SymLink))
                return 1;

            return Name.CompareTo(o.Name);
        }
    }
}
