using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using sio = System.IO;
using ssc = System.Security.Cryptography;
using ste = System.Text.Encoding;

namespace Persona3Graphics
{
    public class Md5Class
    {
        public const int hashLength = 0x10;

        private static ssc.MD5 hasher = ssc.MD5.Create();

        public byte[] md5bytes;
        public string md5string { get { string s = ""; for (int i = 0; i < hashLength; i++) s += string.Format("{0:x2}", md5bytes[i]); return s; } }

        public void CalcMd5(sio.Stream stream)
        {
            md5bytes = Md5Class.hasher.ComputeHash(stream);
        }

        public void CalcMd5(byte[] array)
        {
            md5bytes = Md5Class.hasher.ComputeHash(array);
        }

        public void CalcMd5(byte[] array, int start, int length)
        {
            md5bytes = Md5Class.hasher.ComputeHash(array, start, length);
        }

        public void SetMd5(string md5)
        {
            if (md5.Length < hashLength * 2)
                throw new ArgumentException("Wrong md5 length");

            md5bytes = new byte[hashLength];
            for (int i = 0; i < hashLength * 2; i += 2)
                md5bytes[i >> 1] = Convert.ToByte(md5.Substring(i, 2), 16);
        }
    }
}
