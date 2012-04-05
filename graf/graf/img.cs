using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using sio = System.IO;
using sdr = System.Drawing;

namespace Persona3Graphics
{
    public enum RawImgTypes
    {
        unknown = -1,
        p3tmx = 0,
        p3ps2 = 1
    }

    public abstract class RawImgClass : BaseImgClass
    {
        public readonly RawImgTypes Type = RawImgTypes.unknown;
        public PngClass png;

        public abstract void ToPng(string fileName);

        public static RawImgClass FromPng(string fileName)
        {
            throw new NotSupportedException("You must hide this method - RawImgClass.FromPng() - in derived classes");
        }

        public override void Save(string fileName)
        {
            throw new NotImplementedException();
        }

        public static RawImgTypes GetImgType(string fileName)
        {
            sio.MemoryStream ms;

            ms = TmxClass.CheckAndLoadTmx(fileName);
            if (ms != null)
            {
                ms.Close();
                return RawImgTypes.p3tmx;
            }

            return RawImgTypes.unknown;
        }
    }

    public abstract class BaseImgClass : Md5Class
    {
        public sio.MemoryStream rawMs;
        public int Width;
        public int Height;
        public int BitCount;
        public sdr.Imaging.PixelFormat PixelFormat;
        public byte[] imgData, palData;
        public string name;

        public virtual void CalcMd5()
        {
            long prevPos;

            prevPos = rawMs.Position;
            rawMs.Position = 0;
            CalcMd5(rawMs);
            rawMs.Position = prevPos;
        }

        public abstract void Save(string fileName);
    }
}
