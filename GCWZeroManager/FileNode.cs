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
        public FileNodeName Filename { get; private set; }
        public SizeElement Size { get; private set; }
        public FileType FileType { get; private set; }

        public FileNode(string filename, long size, FileType fileType)
        {
            Filename = new FileNodeName(filename, fileType);
            Size = new SizeElement(size);
            FileType = fileType;
        }

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
                    if (Filename.Name.EndsWith(".txt") || Filename.Name.EndsWith(".log"))
                        return GetIconPath("icon-text.png");
                    if (Filename.Name.EndsWith(".mp3") || Filename.Name.EndsWith(".ogg") || Filename.Name.EndsWith(".wav"))
                        return GetIconPath("icon-audio.png");
                    if (Filename.Name.EndsWith(".bmp") || Filename.Name.EndsWith(".png") || Filename.Name.EndsWith(".jpg"))
                        return GetIconPath("icon-image.png");
                }

                return GetIconPath("icon-file.png");
            }
        }
    }
}
