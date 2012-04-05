using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using sio = System.IO;
using sirs = System.Runtime.InteropServices;
using ste = System.Text.Encoding;

namespace cvm
{
    public static class misc
    {
        public static T ReadStruct<T>(sio.FileStream fs)
        {
            byte[] buf;
            sirs.GCHandle gch;

            buf = new byte[GetStructSize<T>()];
            fs.Read(buf, 0, buf.Length);
            gch = sirs.GCHandle.Alloc(buf, sirs.GCHandleType.Pinned);
            T res = (T)sirs.Marshal.PtrToStructure(gch.AddrOfPinnedObject(), typeof(T));
            gch.Free();

            return res;
        }

        public static void WriteStruct(sio.FileStream fs, object str)
        {
            byte[] buf;
            sirs.GCHandle gch;

            buf = new byte[sirs.Marshal.SizeOf(str)];
            gch = sirs.GCHandle.Alloc(buf, sirs.GCHandleType.Pinned);
            sirs.Marshal.StructureToPtr(str, gch.AddrOfPinnedObject(), true);
            fs.Write(buf, 0, buf.Length);
            gch.Free();
        }

        public static int GetStructSize<T>()
        {
            return sirs.Marshal.SizeOf(typeof(T));
        }

        public static T AllocStruct<T>()
        {
            byte[] buf;
            sirs.GCHandle gch;

            buf = new byte[GetStructSize<T>()];
            gch = sirs.GCHandle.Alloc(buf, sirs.GCHandleType.Pinned);
            T res = (T)sirs.Marshal.PtrToStructure(gch.AddrOfPinnedObject(), typeof(T));
            gch.Free();

            return res;
        }

        public static string ReadString(this sio.FileStream fs, int len)
        {
            byte[] buf;

            if (len < 1)
                return "";

            buf = new byte[len];
            fs.Read(buf, 0, buf.Length);
            if (buf[0] == 0)
                return "";
            else
                return ste.ASCII.GetString(buf);
        }

        public static void WriteString(this sio.FileStream fs, string str)
        {
            byte[] buf;

            if (str.Length > 0)
                buf = ste.ASCII.GetBytes(str);
            else
                buf = new byte[1];
            fs.Write(buf, 0, buf.Length);
        }

        public static int ChangeEndian(this int i)
        {
            int res;

            res = i << 0x18;
            res |= (i & 0xff00) << 0x08;
            res |= (i >> 0x08) & 0xff00;
            res |= (i >> 0x18) & 0xff;

            return res;
        }

        public static short ChangeEndian(this short i)
        {
            int res;

            res = (i & 0xff) << 0x8;
            res |= (i >> 0x08) & 0xff;

            return (short)res;
        }

        public static string ToStr(this char[] cs)
        {
            return new string(cs);
        }

        public static string CheckEndSep(this string str)
        {
            if (str[str.Length - 1] != config.dirSep)
                return str + config.dirSep;
            else
                return str;
        }

        public static sio.DirectoryInfo DICreateAndCheck(string path)
        {
            sio.DirectoryInfo di;

            path = path.CheckEndSep();
            di = new sio.DirectoryInfo(path);
            if (!di.Exists)
                di.Create();

            return di;
        }

        public static void Extract(this sio.FileStream fs, string path, long start, long length)
        {
            const int bufLen = 0x1000;
            byte[] buf;
            int bufCnt;
            long remSize;
            sio.FileStream fw;

            buf = new byte[bufLen];
            bufCnt = bufLen;
            remSize = length;

            fw = new sio.FileStream(path, sio.FileMode.Create, sio.FileAccess.Write);
            fs.Position = start;
            while (remSize > 0)
            {
                if (remSize < bufLen)
                    bufCnt = (int)remSize;
                fs.Read(buf, 0, bufCnt);
                fw.Write(buf, 0, bufCnt);
                remSize -= bufCnt;
            }
            fw.Close();
        }

        public static void Insert(this sio.FileStream fs, string path, int align)
        {
            const int bufLen = 0x1000;
            byte[] buf;
            int bufCnt;
            long remSize;
            sio.FileStream str;

            str = new sio.FileStream(path, sio.FileMode.Open, sio.FileAccess.Read);
            buf = new byte[bufLen];
            bufCnt = bufLen;
            remSize = str.Length;

            while (remSize > 0)
            {
                if (remSize < bufLen)
                    bufCnt = (int)remSize;
                str.Read(buf, 0, bufCnt);
                fs.Write(buf, 0, bufCnt);
                remSize -= bufCnt;
            }

            if (align > 0)
            {
                remSize = fs.Position & (align - 1);
                if (remSize > 0)
                    remSize = align - remSize;
                fs.Write(new byte[remSize], 0, (int)remSize);
            }
            str.Close();
        }

        public static int CompareDirs(PathTableRecord x, PathTableRecord y)
        {
            int i, j, k;
            Func<PathTableRecord, int> getSepCount = z =>
            {
                i = 0;
                k = z.fullName.IndexOf(config.dirSep);
                while (k > -1)
                {
                    i++;
                    k = z.fullName.IndexOf(config.dirSep, k + 1);
                }
                return i;
            };

            j = getSepCount(y);
            i = getSepCount(x);

            if ((x.fullName.IndexOf(y.fullName) == 0) && (j == i + 1))
                return 1;
            if ((y.fullName.IndexOf(x.fullName) == 0) && (i == j + 1))
                return -1;


            return x.fullName.Replace('_', 'z').CompareTo(y.fullName.Replace('_', 'z'));
        }

        public static int CompareFiles(FileDirectoryDescriptor x, FileDirectoryDescriptor y)
        {
            return x.name.Replace('_', 'z').CompareTo(y.name.Replace('_', 'z'));
        }

        public static int CompareLocs(FileDirectoryDescriptor x, FileDirectoryDescriptor y)
        {
            if (x.hdr.locL < y.hdr.locL)
                return -1;
            else if (x.hdr.locL > y.hdr.locL)
                return 1;
            else
                return 0;
        }

        public static byte GetLocalUtc15()
        {
            return (byte)(TimeZoneInfo.Local.BaseUtcOffset.TotalMinutes / 15);
        }

        public static char[] GetPadResChars(string str, int strLen)
        {
            if (str.Length < strLen)
                str = str.PadRight(strLen);
            else
                str = str.Substring(0, strLen);

            return str.ToCharArray();
        }

        public static string GetZeroPaddedString(string str, int strLen)
        {
            if (str.Length < strLen)
                str = str.PadRight(strLen, '\0');
            else
                str = str.Substring(0, strLen);

            return str;
        }

        public static int GetLen(ref char[] arr)
        {
            return ((sirs.MarshalAsAttribute)Attribute.GetCustomAttribute(arr.GetType(), typeof(sirs.MarshalAsAttribute))).SizeConst;
        }

        public static int CondAlign(this int num, int alg, int cond)
        {
            int mod;

            if ((mod = (num + cond) & (alg - 1)) < cond)
                num += cond - mod;

            return num;
        }

        public static void Align(this sio.FileStream fs, int alg)
        {
            long mod;

            mod = fs.Position & (alg - 1);
            if (mod > 0)
                fs.Position += alg - mod;
        }

        public static void CondAlign(this sio.FileStream fs, int alg, int cond)
        {
            long mod;

            if ((mod = (fs.Position + cond) & (alg - 1)) < cond)
                fs.Position += cond - mod;
        }
    }
}
