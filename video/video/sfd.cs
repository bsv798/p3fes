using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using sio = System.IO;

namespace video
{
    public static class SFD
    {
        public static void Demux(string inFile, string outFile)
        {
            const int bufLen = 0x800;
            sio.FileStream fr;
            long fileLen;
            sio.FileStream fwa, fwv;
            byte[] buf;
            int len;

            buf = new byte[bufLen];
            fr = new sio.FileStream(inFile, sio.FileMode.Open, sio.FileAccess.Read);
            fileLen = fr.Length;
            fwa = new sio.FileStream(outFile + ".adx", sio.FileMode.Create, sio.FileAccess.Write);
            fwv = new sio.FileStream(outFile + ".m1v", sio.FileMode.Create, sio.FileAccess.Write);

            Console.WriteLine("Extractig streams from '{0}'", inFile);
            while (fr.Position < fileLen)
            {
                fr.Read(buf, 0, bufLen);
                len = (buf[0x10] << 0x8) | buf[0x11];
                if (buf[0xf] == 0xff)
                    break;
                else
                    switch (buf[0xf])
                    {
                        case 0xbb:
                            break;
                        case 0xbf:
                            break;
                        case 0xc0:
                            fwa.Write(buf, 0x19, len - 0x7); //0x7e0
                            break;
                        case 0xe0:
                            fwv.Write(buf, 0x1e, len - 0xc); //0x7e2
                            break;
                        default:
                            if (fr != null) fr.Close();
                            if (fwa != null) fwa.Close();
                            if (fwv != null) fwv.Close();
                            throw new ArgumentException(string.Format("Unknown field type '{0:x2}' in '{1}'", buf[0xf], inFile));
                    }
            }
            Console.WriteLine("Extractig done");

            fwv.Close();
            fwa.Close();
            fr.Close();
        }
    }
}
