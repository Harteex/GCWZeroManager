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
        public string Filename { get; set; }
        public SizeElement Size { get; set; }
        public FileType FileType { get; set; }

        private string GetIconPath(string filename)
        {
            return "Resources/Icons/" + filename;
        }

        public string Icon
        {
            get
            {
                if (FileType == GCWZeroManager.FileType.Directory)
                {
                    return GetIconPath("icon-folder.png");
                }

                if (FileType == GCWZeroManager.FileType.SymLink)
                {
                    return GetIconPath("icon-folder-symlink.png");
                }

                if (FileType == GCWZeroManager.FileType.RegularFile)
                {
                    if (Filename.EndsWith(".txt") || Filename.EndsWith(".log"))
                        return GetIconPath("icon-text.png");
                    if (Filename.EndsWith(".mp3") || Filename.EndsWith(".ogg") || Filename.EndsWith(".wav"))
                        return GetIconPath("icon-audio.png");
                    if (Filename.EndsWith(".bmp") || Filename.EndsWith(".png") || Filename.EndsWith(".jpg"))
                        return GetIconPath("icon-image.png");
                }

                return GetIconPath("icon-file.png");
            }
        }
    }
}
