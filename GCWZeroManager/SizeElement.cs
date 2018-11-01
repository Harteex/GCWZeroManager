using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GCWZeroManager
{
    public class SizeElement : IComparable
    {
        public long Bytes { get; private set; }

        public SizeElement(long bytes)
        {
            this.Bytes = bytes;
        }

        public override string ToString()
        {
            if (Bytes == -1)
                return "";

            long size = Bytes;
            if (size < 1024)
                return "" + size + " B";

            size /= 1024;
            if (size < 1024)
                return "" + size + " KB";

            size /= 1024;
            return "" + size + " MB";
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            SizeElement s = obj as SizeElement;
            if ((object)s == null)
                return false;

            return (this.Bytes == s.Bytes);
        }

        public override int GetHashCode()
        {
            return (int)Bytes;
        }

        public int CompareTo(object other)
        {
            SizeElement o = (SizeElement)other;
            if (Bytes > o.Bytes)
                return 1;

            if (Bytes == o.Bytes)
                return 0;

            return -1;
        }
    }
}
