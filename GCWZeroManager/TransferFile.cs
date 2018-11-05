using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GCWZeroManager
{
    public class TransferFile
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public string SourcePath { get; set; }

        public TransferFile(string sourcePath, long size)
        {
            Name = Path.GetFileName(sourcePath);
            Size = size;
            SourcePath = sourcePath;
        }
    }
}
