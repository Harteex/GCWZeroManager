using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GCWZeroManager
{
    public class SizeElement : IComparable
    {
        private long bytes = 0;

        public long Bytes
        {
            get { return bytes; }
            set { bytes = value; }
        }

        public SizeElement(long bytes)
        {
            this.bytes = bytes;
        }

        public override string ToString()
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

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            SizeElement s = obj as SizeElement;
            if ((System.Object)s == null)
                return false;

            return (this.Bytes == s.Bytes);
        }

        public override int GetHashCode()
        {
            return (int)bytes;
        }

        public int CompareTo(object other)
        {
            SizeElement o = (SizeElement)other;
            if (this.Bytes > o.Bytes)
                return -1;

            if (this.Bytes == o.Bytes)
                return 0;

            return 1;
        }
    }
}
