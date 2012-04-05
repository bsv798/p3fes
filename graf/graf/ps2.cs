using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using sio = System.IO;
using sdr = System.Drawing;

namespace Persona3Graphics
{
    public class Ps2Class : RawImgClass
    {
        public readonly new RawImgTypes Type = RawImgTypes.p3ps2;
        private sio.BinaryReader ps2Br;
        private bool converted;

        public static sio.MemoryStream CheckAndLoadPs2(string fileName)
        {
            sio.MemoryStream ms;
            sio.BinaryReader br;

            sio.FileInfo fi;

            fi = new sio.FileInfo(fileName);
            if (!fi.Exists)
                throw new sio.FileNotFoundException(string.Format("File '{0}' not found", fi.FullName));

            ms = new sio.MemoryStream(sio.File.ReadAllBytes(fileName), 0, (int)fi.Length, false, true);
            br = new sio.BinaryReader(ms);

            if (br.ReadInt32() != 0x15)
                throw new Exception("ps2 0x15");
            if (br.ReadInt32() != ms.Length)
                throw new Exception("ps2 size");
            br.ReadInt32();
            if (br.ReadInt32() != 1)
                throw new Exception("ps2 0x01");
            if (br.ReadInt32() != 0x08)
                throw new Exception("ps2 0x08");
            br.ReadInt32();
            if (br.ReadInt32() != 0x00325350)
                throw new Exception("ps2 magic");

            return ms;
            //return null;
        }

        public Ps2Class(string fileName)
        {
            int len;

            rawMs = CheckAndLoadPs2(fileName);
            if (rawMs == null)
                throw new ArgumentException(string.Format("File '{0}' is not ps2", fileName));

            rawMs.Position = 0x20;
            ps2Br = new sio.BinaryReader(rawMs);
            if (ps2Br.ReadInt32() == 0x02)
            {
                len = ps2Br.ReadInt32();
              //  name = rawMs.ReadNullTerminatedString(len);
            }
            else
                name = "";
            ps2Br.ReadInt16();
            Width = ps2Br.ReadInt16();
            Height = ps2Br.ReadInt16();
            switch (ps2Br.ReadInt16())
            {
                case 0x13:
                    BitCount = 0x08;
                    PixelFormat = sdr.Imaging.PixelFormat.Format8bppIndexed;
                    break;
                case 0x14:
                    BitCount = 0x04;
                    PixelFormat = sdr.Imaging.PixelFormat.Format4bppIndexed;
                    break;
                case 0x01:
                    BitCount = 0x18;
                    PixelFormat = sdr.Imaging.PixelFormat.Format24bppRgb;
                    break;
                case 0x00:
                    BitCount = 0x20;
                    PixelFormat = sdr.Imaging.PixelFormat.Format32bppArgb;
                    break;
                default:
                    throw new ArgumentException("Wrong color depth");
            }
            if (ps2Br.ReadUInt32() != 0xff000000)
                throw new ArgumentException("Wrong color depth");
            ps2Br.ReadInt32();
            ps2Br.ReadInt32();
          //  name = rawMs.ReadNullTerminatedString(0x1c);

            converted = false;
        }

        public override void ToPng(string fileName)
        {
            throw new NotImplementedException();
        }

        public static new Ps2Class FromPng(string fileName)
        {
            throw new NotImplementedException();
        }

        public override void Save(string fileName)
        {
            throw new NotImplementedException();
        }
    }
}
