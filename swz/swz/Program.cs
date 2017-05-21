using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using sio = System.IO;

namespace swz
{
    class Program
    {
        static int[,] col32 = {{  0,  1,  4,  5,  8,  9, 12, 13,
                                  2,  3,  6,  7, 10, 11, 14, 15 },
                               {  0,  1,  4,  5,  8,  9, 12, 13,
                                  2,  3,  6,  7, 10, 11, 14, 15 }};
        static int[] col32unswz;
        static int[] page32 = { 0,  1,  4,  5, 16, 17, 20, 21,
	                            2,  3,  6,  7, 18, 19, 22, 23,
	                            8,  9, 12, 13, 24, 25, 28, 29,
	                           10, 11, 14, 15, 26, 27, 30, 31 };


        static void Main(string[] args)
        {
            byte[] res;
            Ps2Swizzler swz;

            //swzCol32(sio.File.ReadAllBytes(@"e:\Project\cs\p3fes\swz\col32.bin "), out res);
            //saveArray(res, @"e:\Project\cs\p3fes\swz\col32.swz ");

            //swzPage32(sio.File.ReadAllBytes(@"e:\Project\cs\p3fes\swz\page32.bin "), out res);
            //saveArray(res, @"e:\Project\cs\p3fes\swz\page32.swz ");

            //new swz().writeTexPSMCT32(sio.File.ReadAllBytes(@"f:\Soft\Backup\vs10\vcs\p3fes\swz\p3_title_32_org_unswz.bin "), 640, 448, out res);
            //saveArray(res, @"f:\Soft\Backup\vs10\vcs\p3fes\swz\swz.bin ");

            //new swz().readTexPSMCT32(sio.File.ReadAllBytes(@"f:\Soft\Backup\vs10\vcs\p3fes\swz\p3_title_32_org_unswz.bin "), 640, 448, out res);
            //saveArray(res, @"f:\Soft\Backup\vs10\vcs\p3fes\swz\swz.bin ");

            swz = new Ps2Swizzler();

            //swz.Swizzle32(sio.File.ReadAllBytes(@"f:\Soft\Backup\vs10\vcs\p3fes\swz\p3_title_32_org_unswz.bin "), 640, 448, out res);
            //saveArray(res, @"f:\Soft\Backup\vs10\vcs\p3fes\swz\swz.bin ");

            //swz.Swizzle32(sio.File.ReadAllBytes(@"f:\Soft\Backup\vs10\vcs\p3fes\swz\p3_title_32_org_unswz.bin "), 640, 448, out res);
            //saveArray(res, @"f:\Soft\Backup\vs10\vcs\p3fes\swz\swz.bin ");

            //swz.Swizzle8(sio.File.ReadAllBytes(@"f:\Soft\Backup\vs10\vcs\p3fes\swz\p3_08_unswz.bin "), 512, 512, out res);
            //saveArray(res, @"f:\Soft\Backup\vs10\vcs\p3fes\swz\p3_08_swz1.bin ");

            swz.UnSwizzle32_8(sio.File.ReadAllBytes(@"e:\Temp\swizzling\PS2Textures\fdsfd\noname.bin "), 256, 128, out res);
            saveArray(res, @"e:\Temp\swizzling\PS2Textures\fdsfd\FNT2_unsw.bin ");

            //swz.UnSwizzle32_4(sio.File.ReadAllBytes(@"e:\Temp\swizzling\PS2Textures\fdsfd\noname.bin "), 256, 128, out res);
            //saveArray(res, @"e:\Temp\swizzling\PS2Textures\fdsfd\FNT2_unsw.bin ");

            //swz.Swizzle4_32(sio.File.ReadAllBytes(@"e:\Project\cs\p3fes\swz\FNT2_unsw1.bin "), 512, 256, out res);
            //saveArray(res, @"e:\Project\cs\p3fes\swz\FNT21.bin ");
        }

        static void swzCol32(byte[] src, out byte[] dst)
        {
            int dstPos;
            int i;

            dst = new byte[src.Length];

            for (i = 0; i < src.Length; i += 4)
            {
                dstPos = col32[0, i >> 2] << 2;
                Array.Copy(src, i, dst, dstPos, 4);
            }
        }

        static void swzPage32(byte[] src, out byte[] dst)
        {
            const int bpp = 4;
            const int colWdtPix = 8;
            const int colWdt = colWdtPix * bpp;
            const int colHgt = 2;
            const int blockHgt = colHgt * 4;
            const int pageWdtCols = 8;
            const int pageWdtPix = pageWdtCols * colWdtPix;
            const int pageWdt = pageWdtPix * bpp;
            const int pageHgt = blockHgt * 4;

            int wdt, hgt;
            int lineWdt;
            int lineEven;
            int srcPos;
            int dstPos;
            int i, j, k;
            byte[] tmp1, tmp2;
            int blkIdx;

            dst = new byte[src.Length];
            tmp1 = new byte[colWdt * colHgt];
            hgt = 32; wdt = 64;
            lineWdt = wdt * bpp;
            wdt *= bpp;
            for (j = 0; j < hgt - blockHgt; j += colHgt)
            {
                for (i = 0; i < wdt; i += colWdt)
                {
                    srcPos = (j * lineWdt) + i;
                    for (k = 0; k < colHgt; k++)
                        Array.Copy(src, srcPos + (lineWdt * k), tmp1, colWdt * k, colWdt);
                    swzCol32(tmp1, out tmp2);

                    //dstPos = col32[(j >> 1) & 1, (i % pageWdt) / colWdt];
                    //if (dstPos > pageWdtCols - 1)
                    //    dstPos = ((dstPos - pageWdtCols) * bpp) + lineWdt;
                    //else
                    //    dstPos = dstPos * bpp;
                    blkIdx = page32[((j % pageHgt) / blockHgt * pageWdtCols) + ((i % pageWdt) / colWdt)];
                    dstPos = ((blkIdx / pageWdtCols) * lineWdt) + ((blkIdx & (pageWdtCols - 1)) * colWdt);
                    dstPos += (j * lineWdt) + (i & ~(pageWdt - 1));
                    k = 0;

                    for (k = 0; k < colHgt; k++)
                        Array.Copy(tmp2, colWdt * k, dst, dstPos + (lineWdt * k), colWdt);
                }
            }
        }

        static void calcCol32UnSwz()
        {
            col32unswz = new int[col32.Length];

            for (int i = 0; i < col32unswz.Length; i++)
            {
                col32unswz[col32[0, i]] = i;
            }
        }

        static void saveArray(byte[] arr, string path)
        {
            sio.FileStream fs = new sio.FileStream(path, sio.FileMode.Create, sio.FileAccess.Write);
            fs.Write(arr, 0, arr.Length);
            fs.Close();
        }
    }
}
