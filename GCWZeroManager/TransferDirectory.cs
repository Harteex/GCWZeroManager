using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GCWZeroManager
{
    public class TransferDirectory
    {
        public string Name { get; set; }
        public List<TransferDirectory> Directories { get; set; }
        public List<TransferFile> Files { get; set; }

        public TransferDirectory(string name)
        {
            Name = name;
            Directories = new List<TransferDirectory>();
            Files = new List<TransferFile>();
        }

        public void AddDirectory(TransferDirectory directory)
        {
            Directories.Add(directory);
        }

        public void AddFile(TransferFile file)
        {
            Files.Add(file);
        }

        public int GetTotalFiles()
        {
            return Files.Count + Directories.Sum(x => x.GetTotalFiles());
        }

        public long GetTotalSize()
        {
            return Files.Sum(f => f.Size) + Directories.Sum(x => x.GetTotalSize());
        }
    }
}
