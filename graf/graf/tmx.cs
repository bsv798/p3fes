#define NORMALIZE_ALPHA

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using sio = System.IO;
using sdr = System.Drawing;
using ste = System.Text.Encoding;

namespace Persona3Graphics
{
    public class TmxClass : RawImgClass
    {
        public readonly new RawImgTypes Type = RawImgTypes.p3tmx;
        private sio.BinaryReader tmxBr;
        private bool converted;
        private static ste japEnc = ste.GetEncoding(932);

        public TmxClass()
        {

        }

        public TmxClass(string fileName)
        {
            rawMs = CheckAndLoadTmx(fileName);
            if (rawMs == null)
                throw new ArgumentException(string.Format("File '{0}' is not tmx", fileName));

            tmxBr = new sio.BinaryReader(rawMs);
            Width = tmxBr.ReadInt16();
            Height = tmxBr.ReadInt16();
            switch (tmxBr.ReadInt16())
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
            //if (tmxBr.ReadUInt32() != 0xff000000)
            //    throw new ArgumentException("Wrong ff flag");
            tmxBr.ReadInt32();
            tmxBr.ReadInt32();
            tmxBr.ReadInt32();
            name = rawMs.ReadNullTerminatedString(0x1c, japEnc);

            converted = false;
        }

        public static sio.MemoryStream CheckAndLoadTmx(string fileName)
        {
            sio.MemoryStream ms;
            sio.BinaryReader br;

            sio.FileInfo fi;

            fi = new sio.FileInfo(fileName);
            if (!fi.Exists)
                throw new sio.FileNotFoundException(string.Format("File '{0}' not found", fi.FullName));

            ms = new sio.MemoryStream(sio.File.ReadAllBytes(fileName), 0, (int)fi.Length, false, true);
            br = new sio.BinaryReader(ms);

            if (ms.Length > 0x10)
                if (br.ReadInt32() == 0x2)
                    if (br.ReadInt32() != 0xff) //if (br.ReadInt32() <= ms.Length)
                        if (br.ReadInt32() == 0x30584D54)
                            if (br.ReadInt32() == 0)
                                if (br.ReadByte() < 0x2)
                                    if (br.ReadByte() == 0)
                                        return ms;

            return null;
        }

        public override void ToPng(string fileName)
        {
            if (!converted)
            {
                if (BitCount < 0x09)
                    SetPaletteFromTmx();
                else
                    palData = new byte[] { };
                SetDataFromTmx();
                converted = true;

                tmxBr.Close();
                rawMs.Close();

                png = new PngClass(this);
            }
            png.Save(fileName);
        }

        private void SetPaletteFromTmx()
        {
            uint col;
            int a;
            int shift;
            bool cms;
            int i, c;

            i = 0;
            Func<int, bool> SetColor = (shft) =>
                {
                    col = tmxBr.ReadUInt32();
                    a = (byte)(col >> 0x18);
#if NORMALIZE_ALPHA
                    a = NormalyzeAlpha(a);
#endif
                    palData[i + shft] = (byte)a;
                    palData[i + shft + 1] = (byte)col;
                    palData[i + shft + 2] = (byte)(col >> 0x08);
                    palData[i + shft + 3] = (byte)(col >> 0x10);

                    return true;
                };

            palData = new byte[(int)Math.Pow(2, BitCount) << 0x2];
            rawMs.Position = 0x40;

            for (i = 0; i < 0x20; i += 0x4)
                SetColor(0);

            if (BitCount == 0x08)
            {
                c = 0;
                shift = 0;
                cms = false;
                for (i = 0x20; i < palData.Length - 0x20; i += 0x4)
                {
                    if (c % 0x20 == 0)
                        shift = -32;
                    if (c % 0x40 == 0)
                    {
                        cms = !cms;
                        shift = 0;
                    }
                    if (c % 0x80 == 0)
                        shift = 0x20;
                    if (!cms)
                        shift = 0;
                    SetColor(shift);
                    c += 4;
                }
            }

            for (i = palData.Length - 0x20; i < palData.Length; i += 0x4)
                SetColor(0);
        }

        private void SetDataFromTmx()
        {
            int b;

            imgData = new byte[(int)(Width * Height * ((float)BitCount / 8))];
            Array.Copy(rawMs.GetBuffer(), 0x40 + palData.Length, imgData, 0, imgData.Length);

            if (BitCount == 0x04)
                for (int i = 0; i < imgData.Length; i++)
                {
                    b = imgData[i];
                    imgData[i] = (byte)(((b >> 0x04) & 0x0f) | ((b & 0x0f) << 0x04));
                }
#if NORMALIZE_ALPHA
            else if (BitCount == 0x20)
                for (int i = 3; i < imgData.Length; i += 4)
                    imgData[i] = (byte)NormalyzeAlpha(imgData[i]);
#endif
        }

#if NORMALIZE_ALPHA
        private int NormalyzeAlpha(int a)
        {
            if (a == 0x80)
                return 0xff;
            else if (a == 0xff)
                return 0xff;
            else
                return (a & 0x7f) << 1;

            throw new Exception("Wrong alpha");
        }

        private int UnNormalyzeAlpha(int a)
        {
            if (a == 0xff)
                return 0x80;
            else
                return a >> 1;

        }
#endif

        public static new TmxClass FromPng(string fileName)
        {
            PngClass png;
            TmxClass tmx;
            sio.FileInfo fi;

            fi = new sio.FileInfo(fileName);
            if (!fi.Exists)
                throw new sio.FileNotFoundException(string.Format("File '{0}' not found", fi.FullName));

            png = new PngClass(fileName, (RawImgClass)new TmxClass());
            tmx = (TmxClass)png.p3img;
            tmx.converted = false;

            return tmx;
        }

        private void ConvertPaletteFromPng()
        {
            sio.MemoryStream ms;
            sio.BinaryReader br;
            byte[] buf;
            int shift;
            bool cms;
            uint col;
            int a;
            int i, c;

            i = 0;
            br = null;
            Func<int, bool> SetColor = (shft) =>
                {
                    col = br.ReadUInt32();
                    a = (byte)col;
#if NORMALIZE_ALPHA
                    a = UnNormalyzeAlpha(a);
#endif
                    palData[i + shft] = (byte)(col >> 0x08);
                    palData[i + shft + 1] = (byte)(col >> 0x10);
                    palData[i + shft + 2] = (byte)(col >> 0x18);
                    palData[i + shft + 3] = (byte)a;

                    return true;
                };

            buf = new byte[palData.Length];
            Array.Copy(palData, buf, palData.Length);
            ms = new sio.MemoryStream(buf);
            br = new sio.BinaryReader(ms);

            for (i = 0; i < 0x20; i += 0x4)
                SetColor(0);

            if (BitCount == 0x08)
            {
                c = 0;
                shift = 0;
                cms = false;
                for (i = 0x20; i < palData.Length - 0x20; i += 0x4)
                {
                    if (c % 0x20 == 0)
                        shift = -32;
                    if (c % 0x40 == 0)
                    {
                        cms = !cms;
                        shift = 0;
                    }
                    if (c % 0x80 == 0)
                        shift = 0x20;
                    if (!cms)
                        shift = 0;
                    SetColor(shift);
                    c += 4;
                }
            }

            for (i = palData.Length - 0x20; i < palData.Length; i += 0x4)
                SetColor(0);

            br.Close();
            ms.Close();
        }

        private void ConvertDataFromPng()
        {
            int b;

            if (BitCount == 0x04)
                for (int i = 0; i < imgData.Length; i++)
                {
                    b = imgData[i];
                    imgData[i] = (byte)(((b >> 0x04) & 0x0f) | ((b & 0x0f) << 0x04));
                }
#if NORMALIZE_ALPHA
            else if (BitCount == 0x20)
                for (int i = 3; i < imgData.Length; i += 4)
                    imgData[i] = (byte)UnNormalyzeAlpha(imgData[i]);
#endif
        }

        public override void Save(string fileName)
        {
            sio.FileStream tmxFs;
            sio.BinaryWriter tmxBw;

            if (!converted)
            {
                if (BitCount < 0x09)
                    ConvertPaletteFromPng();
                else
                    palData = new byte[] { };
                ConvertDataFromPng();
                converted = true;
            }

            tmxFs = new sio.FileStream(fileName, sio.FileMode.Create, sio.FileAccess.Write);
            tmxBw = new sio.BinaryWriter(tmxFs);

            tmxBw.Write(0x2);
            tmxBw.Write(palData.Length + imgData.Length + 0x40);
            tmxBw.Write(0x30584D54);
            tmxBw.Write(0);
            tmxBw.Write((short)(BitCount < 9 ? 1 : 0));
            tmxBw.Write((short)Width);
            tmxBw.Write((short)Height);
            switch (BitCount)
            {
                case 0x04:
                    tmxBw.Write((short)0x14);
                    break;
                case 0x08:
                    tmxBw.Write((short)0x13);
                    break;
                case 0x18:
                    tmxBw.Write((short)0x01);
                    break;
                case 0x20:
                    tmxBw.Write((short)0x00);
                    break;
                default:
                    throw new ArgumentException("Wrong color depth");
            }
            tmxBw.Write(0xff000000);
            tmxBw.Write(0);
            tmxBw.Write(0);
            tmxFs.WriteNullTerminatedString(name, 0x1c, japEnc);
            tmxBw.Write(palData);
            tmxBw.Write(imgData);

            tmxBw.Close();
            tmxFs.Close();
        }
    }
}
