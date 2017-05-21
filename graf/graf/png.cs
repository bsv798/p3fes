using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using sdr = System.Drawing;
using sirs = System.Runtime.InteropServices;
using sio = System.IO;
using ssc = System.Security.Cryptography;

namespace Persona3Graphics
{
    public class PngClass : BaseImgClass
    {
        public sdr.Bitmap png;
        public RawImgClass p3img;

        public PngClass(RawImgClass p3img)
        {
            this.p3img = p3img;

            png = new sdr.Bitmap(p3img.Width, p3img.Height, p3img.PixelFormat);
            SetParams();
            ConvertFromRawImg();

            rawMs = new sio.MemoryStream();
            png.Save(rawMs, sdr.Imaging.ImageFormat.Png);

            CalcMd5();
        }

        public PngClass(string fileName)
        {
            rawMs = new sio.MemoryStream();
            png = LoadPng(fileName);
            png.Save(rawMs, sdr.Imaging.ImageFormat.Png);

            PixelFormat = png.PixelFormat;
            BitCount = -1;
            Width = png.Width;
            Height = png.Height;
        }

        public PngClass(string fileName, RawImgClass p3img)
            : this(fileName)
        {
            this.p3img = p3img;
            ConvertToRawImg(fileName);
        }

        private void SetParams()
        {
            PixelFormat = p3img.PixelFormat;
            BitCount = p3img.BitCount;
            Width = p3img.Width;
            Height = p3img.Height;
        }

        public override void Save(string fileName)
        {
            png.Save(fileName, sdr.Imaging.ImageFormat.Png);
        }

        private void ConvertFromRawImg()
        {
            if (p3img.BitCount < 0x09)
                SetPaletteToPng();
            SetImageDataToPng();
        }

        private void SetPaletteToPng()
        {
            int palPtr;
            sdr.Imaging.ColorPalette colors;

            colors = png.Palette;

            palPtr = 0;
            for (int i = 0; i < colors.Entries.Length; i++)
                colors.Entries[i] = sdr.Color.FromArgb(p3img.palData[palPtr++], p3img.palData[palPtr++], p3img.palData[palPtr++], p3img.palData[palPtr++]);
            png.Palette = colors;
        }

        private void SetImageDataToPng()
        {
            sdr.Imaging.BitmapData bmpData;

            bmpData = png.LockBits(new sdr.Rectangle(0, 0, p3img.Width, p3img.Height), sdr.Imaging.ImageLockMode.WriteOnly, p3img.PixelFormat);
            sirs.Marshal.Copy(p3img.imgData, 0, bmpData.Scan0, p3img.imgData.Length);
            png.UnlockBits(bmpData);
        }

        private void ConvertToRawImg(string fileName)
        {
            p3img.name = "";
            p3img.Width = png.Width;
            p3img.Height = png.Height;
            p3img.PixelFormat = png.PixelFormat;
            switch (p3img.PixelFormat)
            {
                case System.Drawing.Imaging.PixelFormat.Format4bppIndexed:
                    p3img.BitCount = 0x04;
                    break;
                case System.Drawing.Imaging.PixelFormat.Format8bppIndexed:
                    p3img.BitCount = 0x08;
                    break;
                case sdr.Imaging.PixelFormat.Format24bppRgb:
                    p3img.BitCount = 0x18;
                    break;
                case sdr.Imaging.PixelFormat.Format32bppArgb:
                    p3img.BitCount = 0x20;
                    break;
                default:
                    throw new ArgumentException(string.Format("Wrong bit depth in file '{0}'", fileName));
            }

            if (p3img.BitCount < 0x09)
                SetPaletteToRawImg(png, p3img);
            SetImageDataToRawImg(png, p3img);
        }

        private static sdr.Bitmap LoadPng(string fileName)
        {
            const int trns = 0x534E5274;
            sdr.Bitmap png;
            sdr.Imaging.ColorPalette colors;
            sio.FileStream fs;
            sio.BinaryReader br;
            sio.MemoryStream ms;
            sio.BinaryWriter bw;
            long fileLen;
            int chkLen;
            int chkName;
            byte[] alphas;
            int i;

            fs = new sio.FileStream(fileName, sio.FileMode.Open, sio.FileAccess.Read);
            br = new sio.BinaryReader(fs);
            fileLen = fs.Length;
            alphas = new byte[] { };

            if ((br.ReadInt32() == 0x474E5089) && (br.ReadInt32() == 0x0A1A0A0D))
            {
                ms = new sio.MemoryStream();
                bw = new sio.BinaryWriter(ms);

                bw.Write(0x474E5089);
                bw.Write(0x0A1A0A0D);

                while (fs.Position < fileLen)
                {
                    chkLen = br.ReadInt32().ChangeEndian();
                    chkName = br.ReadInt32();
                    if (chkName == trns)
                    {
                        alphas = new byte[chkLen];
                        for (i = 0; i < chkLen; i++)
                            alphas[i] = br.ReadByte();
                        br.ReadInt32();
                    }
                    else
                    {
                        bw.Write(chkLen.ChangeEndian());
                        bw.Write(chkName);
                        bw.Write(br.ReadBytes(chkLen + 0x4));
                    }
                }


                ms.Position = 0;
                png = (sdr.Bitmap)sdr.Image.FromStream(ms);
                if (alphas.Length > 0)
                {
                    colors = png.Palette;
                    for (i = 0; i < alphas.Length; i++)
                        colors.Entries[i] = sdr.Color.FromArgb(alphas[i], colors.Entries[i].R, colors.Entries[i].G, colors.Entries[i].B);
                    png.Palette = colors;
                }

                bw.Close();
                ms.Close();
                br.Close();
                fs.Close();

                return png;
            }
            br.Close();
            fs.Close();

            throw new NotSupportedException(string.Format("File '{0}' is not png"));
        }

        private static void SetPaletteToRawImg(sdr.Bitmap png, RawImgClass p3img)
        {
            int palPtr;
            sdr.Color[] cols;

            p3img.palData = new byte[png.Palette.Entries.Length << 0x2];
            cols = png.Palette.Entries; //effects performance VERY HEAVILY

            palPtr = 0;
            for (int i = 0; i < cols.Length; i++)
            {
                p3img.palData[palPtr++] = cols[i].A;
                p3img.palData[palPtr++] = cols[i].R;
                p3img.palData[palPtr++] = cols[i].G;
                p3img.palData[palPtr++] = cols[i].B;
            }
        }

        private static void SetImageDataToRawImg(sdr.Bitmap png, RawImgClass p3img)
        {
            sdr.Imaging.BitmapData bmpData;

            p3img.imgData = new byte[(int)(p3img.Width * p3img.Height * ((float)p3img.BitCount / 8))];
            bmpData = png.LockBits(new sdr.Rectangle(0, 0, png.Width, png.Height), sdr.Imaging.ImageLockMode.ReadOnly, png.PixelFormat);
            sirs.Marshal.Copy(bmpData.Scan0, p3img.imgData, 0, p3img.imgData.Length);
            png.UnlockBits(bmpData);
        }
    }
}
