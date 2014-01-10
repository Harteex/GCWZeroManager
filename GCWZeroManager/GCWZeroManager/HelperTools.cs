using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GCWZeroManager
{
    public static class HelperTools
    {
        public static string GetFormattedSize(long bytes)
        {
            long size = bytes;
            if (size < 1024)
                return "" + size + " B";

            size /= 1024;
            if (size < 1024)
                return "" + size + " KB";

            size /= 1024;
            return "" + size + " MB";
        }
    }
}
