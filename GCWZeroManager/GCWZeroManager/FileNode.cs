using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Reflection;
using System.IO;
using System.Windows.Resources;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
//using System.Drawing;

namespace GCWZeroManager
{
    public enum FileType { RegularFile, Directory, SymLink, Other };
    public class FileNode
    {
        //public string Icon { get; set; }
        public string Filename { get; set; }
        public SizeElement Size { get; set; }
        public FileType FileType { get; set; }

        private string Path(string filename)
        {
            return "Resources/Icons/" + filename;
        }

        public string Icon
        {
            get
            {
                if (FileType == GCWZeroManager.FileType.Directory)
                {
                    return Path("icon-folder.png");
                }

                if (FileType == GCWZeroManager.FileType.SymLink)
                {
                    return Path("icon-folder-symlink.png");
                }

                if (FileType == GCWZeroManager.FileType.RegularFile)
                {
                    if (Filename.EndsWith(".txt") || Filename.EndsWith(".log"))
                        return Path("icon-text.png");
                    if (Filename.EndsWith(".mp3") || Filename.EndsWith(".ogg") || Filename.EndsWith(".wav"))
                        return Path("icon-audio.png");
                    if (Filename.EndsWith(".bmp") || Filename.EndsWith(".png") || Filename.EndsWith(".jpg"))
                        return Path("icon-image.png");
                }

                return Path("icon-file.png");
            }
        }
    }
}
