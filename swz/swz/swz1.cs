using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace swz
{
    public class Ps2Swizzler
    {
        static readonly int[] block32 =
            {
	             0,  1,  4,  5, 16, 17, 20, 21,
	             2,  3,  6,  7, 18, 19, 22, 23,
	             8,  9, 12, 13, 24, 25, 28, 29,
	            10, 11, 14, 15, 26, 27, 30, 31
            };


        static readonly int[] columnWord32 =
            {
	             0,  1,  4,  5,  8,  9, 12, 13,
	             2,  3,  6,  7, 10, 11, 14, 15
            };

        int[] swz32;

        const int bufferSize = 0x4 * 0x400 * 0x400;
        byte[] buffer;

        public Ps2Swizzler()
        {
            InitSwizzle32();
            InitSwizzle8();
            InitSwizzle4();
        }

        private void InitSwizzle32()
        {
            int swzCnt;

            swz32 = new int[2048];
            swzCnt = 0;

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    int pageX = x / 64; //х страницы в текстуре
                    int pageY = y / 32;

                    int px = x - (pageX * 64); //х относительно страницы
                    int py = y - (pageY * 32);

                    int blockX = px / 8; //х блока в странице
                    int blockY = py / 8;
                    int block = block32[blockX + blockY * 8];

                    int bx = px - blockX * 8; //х относительно блока
                    int by = py - blockY * 8;

                    int column = by / 2;

                    int cx = bx;
                    int cy = by - column * 2; //у относительно колонны
                    int cw = columnWord32[cx + cy * 8];

                    swz32[swzCnt++] = (block * 64 + column * 16 + cw) * 4;
                }
            }
        }

        public void Swizzle32(byte[] src, int width, int height, out byte[] dst)
        {
            int dbw;
            int page;
            int srcCnt;

            dbw = width / 64;
            dst = new byte[src.Length];

            srcCnt = 0;
            for (int pageY = 0; pageY < height; pageY += 32)
            {
                for (int y = 0; y < 32; y++)
                {
                    for (int pageX = 0; pageX < width; pageX += 64)
                    {
                        page = (pageX / 64 + (pageY / 32 * dbw)) * 8192;
                        for (int x = 0; x < 64; x++)
                        {
                            Array.Copy(src, srcCnt, dst, page + swz32[x + y * 64], 4);
                            srcCnt += 4;
                        }
                    }
                }
            }
        }

        public void UnSwizzle32(byte[] src, int width, int height, out byte[] dst)
        {
            int dbw;
            int page;
            int srcCnt;

            dbw = width / 64;
            dst = new byte[src.Length];

            srcCnt = 0;
            for (int pageY = 0; pageY < height; pageY += 32)
            {
                for (int y = 0; y < 32; y++)
                {
                    for (int pageX = 0; pageX < width; pageX += 64)
                    {
                        page = (pageX / 64 + (pageY / 32 * dbw)) * 8192;
                        for (int x = 0; x < 64; x++)
                        {
                            Array.Copy(src, page + swz32[x + y * 64], dst, srcCnt, 4);
                            srcCnt += 4;
                        }
                    }
                }
            }
        }

        static readonly int[] block8 =
            {
                 0,  1,  4,  5, 16, 17, 20, 21,
                 2,  3,  6,  7, 18, 19, 22, 23,
                 8,  9, 12, 13, 24, 25, 28, 29,
                10, 11, 14, 15, 26, 27, 30, 31
            };

        //static readonly int[] block8 =
        //    {
        //         0,  1,  2,  3,  4,  5,  6,  7,
        //         8,  9, 10, 11, 12, 13, 14, 15,
        //        16, 17, 18, 19, 20, 21, 22, 23,
        //        24, 25, 26, 27, 28, 29, 30, 31
        //    };

        static readonly int[,] columnWord8 = 
            {
                {
                     0,  1,  4,  5,  8,  9, 12, 13,   0,  1,  4,  5,  8,  9, 12, 13,
                     2,  3,  6,  7, 10, 11, 14, 15,   2,  3,  6,  7, 10, 11, 14, 15,

                     8,  9, 12, 13,  0,  1,  4,  5,   8,  9, 12, 13,  0,  1,  4,  5,
                    10, 11, 14, 15,  2,  3,  6,  7,  10, 11, 14, 15,  2,  3,  6,  7
                },
                {
                     8,  9, 12, 13,  0,  1,  4,  5,   8,  9, 12, 13,  0,  1,  4,  5,
                    10, 11, 14, 15,  2,  3,  6,  7,  10, 11, 14, 15,  2,  3,  6,  7,

                     0,  1,  4,  5,  8,  9, 12, 13,   0,  1,  4,  5,  8,  9, 12, 13,
                     2,  3,  6,  7, 10, 11, 14, 15,   2,  3,  6,  7, 10, 11, 14, 15
                }
            };

        //static readonly int[,] columnWord8 = 
        //    {
        //        {
        //             0,  1,  2,  3,  4,  5,  6,  7,  0,  1,  2,  3,  4,  5,  6,  7,
        //             8,  9, 10, 11, 12, 13, 14, 15,  8,  9, 10, 11, 12, 13, 14, 15,

        //             4,  5,  6,  7,  0,  1,  2,  3,  4,  5,  6,  7,  0,  1,  2,  3,
        //            12, 13, 14, 15,  8,  9, 10, 11, 12, 13, 14, 15,  8,  9, 10, 11
        //        },
        //        {
        //             4,  5,  6,  7,  0,  1,  2,  3,  4,  5,  6,  7,  0,  1,  2,  3,
        //            12, 13, 14, 15,  8,  9, 10, 11, 12, 13, 14, 15,  8,  9, 10, 11,

        //             0,  1,  2,  3,  4,  5,  6,  7,  0,  1,  2,  3,  4,  5,  6,  7,
        //             8,  9, 10, 11, 12, 13, 14, 15,  8,  9, 10, 11, 12, 13, 14, 15
        //        }
        //    };

        static readonly int[] columnByte8 = 
            {
                0, 0, 0, 0, 0, 0, 0, 0,  2, 2, 2, 2, 2, 2, 2, 2,
                0, 0, 0, 0, 0, 0, 0, 0,  2, 2, 2, 2, 2, 2, 2, 2,

                1, 1, 1, 1, 1, 1, 1, 1,  3, 3, 3, 3, 3, 3, 3, 3,
                1, 1, 1, 1, 1, 1, 1, 1,  3, 3, 3, 3, 3, 3, 3, 3
            };

        //static readonly int[] columnByte8 = 
        //    {
        //        0, 0, 0, 0, 0, 0, 0, 0,  1, 1, 1, 1, 1, 1, 1, 1,
        //        2, 2, 2, 2, 2, 2, 2, 2,  3, 3, 3, 3, 3, 3, 3, 3,

        //        0, 0, 0, 0, 0, 0, 0, 0,  1, 1, 1, 1, 1, 1, 1, 1,
        //        2, 2, 2, 2, 2, 2, 2, 2,  3, 3, 3, 3, 3, 3, 3, 3,
        //    };

        int[] swz8;

        private void InitSwizzle8()
        {
            int swzCnt;
            byte[] swz81;

            swz8 = new int[8192];
            swz81 = new byte[swz8.Length];
            swzCnt = 0;

            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    int pageX = x / 128;
                    int pageY = y / 64;

                    int px = x - (pageX * 128);
                    int py = y - (pageY * 64);

                    int blockX = px / 16;
                    int blockY = py / 16;
                    int block = block8[blockX + blockY * 8];

                    int bx = px - (blockX * 16);
                    int by = py - (blockY * 16);

                    int column = by / 4;

                    int cx = bx;
                    int cy = by - column * 4;
                    int cw = columnWord8[column & 1, cx + cy * 16];
                    int cb = columnByte8[cx + cy * 16];

                    //if (swzCnt == 0x40)
                    //    swzCnt = swzCnt;

                    swz8[swzCnt++] = (block * 64 + column * 16 + cw) * 4 + cb;
                    //swz8[swzCnt++] = swz32[block * 64 + column * 16 + cw] + cb;
                    //swz8[swz32[block * 64 + column * 16 + cw] + cb] = swzCnt++;
                }
            }
            //for (int i = 0; i < swz8.Length; i++)
            //    swz81[i] = (byte)(swz8[i]);
            //Swizzle32(swz81, 64, 32, out swz81);
            //for (int i = 0; i < swz8.Length; i++)
            //    swz8[i] = swz81[i];

            //for (int i = 0; i < swz8.Length; i++)
            //    swz8[swz8[i]] = i;
        }

        public void Swizzle8_32(byte[] src, int width, int height, out byte[] dst)
        {
            int dbw;
            int page;
            int srcCnt;

            dbw = width / 128;
            dst = new byte[src.Length];

            srcCnt = 0;
            for (int pageY = 0; pageY < height; pageY += 64)
            {
                for (int y = 0; y < 64; y++)
                {
                    for (int pageX = 0; pageX < width; pageX += 128)
                    {
                        page = (pageX / 128 + (pageY / 64 * dbw)) * 8192;
                        for (int x = 0; x < 128; x++)
                        {
                            dst[page + swz8[x + y * 128]] = src[srcCnt++];
                        }
                    }
                }
            }
            UnSwizzle32(dst, width / 2, height / 2, out dst);
        }

        public void UnSwizzle32_8(byte[] src, int width, int height, out byte[] dst)
        {
            int dbw;
            int page;
            int srcCnt;

            dbw = width / 128;
            dst = new byte[src.Length];
            Swizzle32(src, width / 2, height / 2, out src);

            srcCnt = 0;
            for (int pageY = 0; pageY < height; pageY += 64)
            {
                for (int y = 0; y < 64; y++)
                {
                    for (int pageX = 0; pageX < width; pageX += 128)
                    {
                        page = (pageX / 128 + (pageY / 64 * dbw)) * 8192;
                        for (int x = 0; x < 128; x++)
                        {
                            dst[srcCnt++] = src[page + swz8[x + y * 128]];
                        }
                    }
                }
            }
        }

        static readonly int[] block4 =
            {
	             0,  2,  8, 10,
	             1,  3,  9, 11,
	             4,  6, 12, 14,
	             5,  7, 13, 15,
	            16, 18, 24, 26,
	            17, 19, 25, 27,
	            20, 22, 28, 30,
	            21, 23, 29, 31
            };

        static readonly int[,] columnWord4 =
            {
	            {
		             0,  1,  4,  5,  8,  9, 12, 13,   0,  1,  4,  5,  8,  9, 12, 13,   0,  1,  4,  5,  8,  9, 12, 13,   0,  1,  4,  5,  8,  9, 12, 13,
		             2,  3,  6,  7, 10, 11, 14, 15,   2,  3,  6,  7, 10, 11, 14, 15,   2,  3,  6,  7, 10, 11, 14, 15,   2,  3,  6,  7, 10, 11, 14, 15,

		             8,  9, 12, 13,  0,  1,  4,  5,   8,  9, 12, 13,  0,  1,  4,  5,   8,  9, 12, 13,  0,  1,  4,  5,   8,  9, 12, 13,  0,  1,  4,  5,
		            10, 11, 14, 15,  2,  3,  6,  7,  10, 11, 14, 15,  2,  3,  6,  7,  10, 11, 14, 15,  2,  3,  6,  7,  10, 11, 14, 15,  2,  3,  6,  7
	            },
	            {
		             8,  9, 12, 13,  0,  1,  4,  5,   8,  9, 12, 13,  0,  1,  4,  5,   8,  9, 12, 13,  0,  1,  4,  5,   8,  9, 12, 13,  0,  1,  4,  5,
		            10, 11, 14, 15,  2,  3,  6,  7,  10, 11, 14, 15,  2,  3,  6,  7,  10, 11, 14, 15,  2,  3,  6,  7,  10, 11, 14, 15,  2,  3,  6,  7,

		             0,  1,  4,  5,  8,  9, 12, 13,   0,  1,  4,  5,  8,  9, 12, 13,   0,  1,  4,  5,  8,  9, 12, 13,   0,  1,  4,  5,  8,  9, 12, 13,
		             2,  3,  6,  7, 10, 11, 14, 15,   2,  3,  6,  7, 10, 11, 14, 15,   2,  3,  6,  7, 10, 11, 14, 15,   2,  3,  6,  7, 10, 11, 14, 15
	            }
            };

        static readonly int[] columnByte4 =
            {
	            0, 0, 0, 0, 0, 0, 0, 0,  2, 2, 2, 2, 2, 2, 2, 2,  4, 4, 4, 4, 4, 4, 4, 4,  6, 6, 6, 6, 6, 6, 6, 6,
	            0, 0, 0, 0, 0, 0, 0, 0,  2, 2, 2, 2, 2, 2, 2, 2,  4, 4, 4, 4, 4, 4, 4, 4,  6, 6, 6, 6, 6, 6, 6, 6,

	            1, 1, 1, 1, 1, 1, 1, 1,  3, 3, 3, 3, 3, 3, 3, 3,  5, 5, 5, 5, 5, 5, 5, 5,  7, 7, 7, 7, 7, 7, 7, 7,
	            1, 1, 1, 1, 1, 1, 1, 1,  3, 3, 3, 3, 3, 3, 3, 3,  5, 5, 5, 5, 5, 5, 5, 5,  7, 7, 7, 7, 7, 7, 7, 7
            };

        int[] swz4;

        public void InitSwizzle4()
        {
            int swzCnt;

            swz4 = new int[128 * 128];
            swzCnt = 0;

            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    int pageX = x / 128;
                    int pageY = y / 128;

                    int px = x - (pageX * 128);
                    int py = y - (pageY * 128);

                    int blockX = px / 32;
                    int blockY = py / 16;
                    int block = block4[blockX + blockY * 4];

                    int bx = px - blockX * 32;
                    int by = py - blockY * 16;

                    int column = by / 4;

                    int cx = bx;
                    int cy = by - column * 4;
                    int cw = columnWord4[column & 1, cx + cy * 32];
                    int cb = columnByte4[cx + cy * 32];

                    //if ((((block * 64 + column * 16 + cw) * 4) << 3) == 0x1000)
                    //    pageX = 0;
                    swz4[swzCnt++] = (((block * 64 + column * 16 + cw) * 4) << 3) | (cb & 0x7); // +(cb >> 1);
                }
            }
        }

        public void Swizzle4_32(byte[] src, int width, int height, out byte[] dst)
        {
            int dbw;
            int page;
            int srcCnt;
            int cb;
            int b;
            bool odd;
            int dstPos;

            dbw = width / 128;
            dst = new byte[src.Length];

            odd = false;
            srcCnt = 0;
            for (int pageY = 0; pageY < height; pageY += 128)
            {
                for (int y = 0; y < 128; y++)
                {
                    for (int pageX = 0; pageX < width; pageX += 128)
                    {
                        page = (pageX / 128 + (pageY / 128 * dbw)) * 8192;
                        for (int x = 0; x < 128; x++)
                        {
                            dstPos = swz4[x + y * 128];
                            cb = dstPos & 0x7;
                            dstPos = page + (dstPos >> 0x3);
                            b = dst[dstPos + (cb >> 1)];
                            if ((cb & 1) == 1)
                            {
                                if (odd)
                                    b = (b & 0x0f) | (src[srcCnt] & 0xf0);
                                else
                                    b = (b & 0x0f) | ((src[srcCnt] << 4) & 0xf0);
                            }
                            else
                            {
                                if (odd)
                                    b = (b & 0xf0) | ((src[srcCnt] >> 4) & 0x0f);
                                else
                                    b = (b & 0xf0) | (src[srcCnt] & 0x0f);
                            }

                            dst[dstPos + (cb >> 1)] = (byte)b;
                            if (odd)
                                srcCnt++;
                            odd = !odd;
                        }
                    }
                }
            }
            UnSwizzle32(dst, width / 2, height / 4, out dst);
        }

        public void UnSwizzle32_4(byte[] src, int width, int height, out byte[] dst)
        {
            int dbw;
            int page;
            int srcCnt;
            int cb;
            int b;
            bool odd;
            int dstPos;

            dbw = width / 128;
            dst = new byte[src.Length];
            Swizzle32(src, width / 2, height / 4, out src);

            odd = false;
            srcCnt = 0;
            for (int pageY = 0; pageY < height; pageY += 128)
            {
                for (int y = 0; y < 128; y++)
                {
                    for (int pageX = 0; pageX < width; pageX += 128)
                    {
                        page = (pageX / 128 + (pageY / 128 * dbw)) * 8192;
                        for (int x = 0; x < 128; x++)
                        {
                            dstPos = swz4[x + y * 128];
                            cb = dstPos & 0x7;
                            dstPos = page + (dstPos >> 0x3);
                            b = dst[srcCnt];
                            if ((cb & 1) == 1)
                            {
                                if (odd)
                                    b = (b & 0x0f) | (src[dstPos + (cb >> 1)] & 0xf0);
                                else
                                    b = (b & 0xf0) | ((src[dstPos + (cb >> 1)] >> 4) & 0x0f);
                            }
                            else
                            {
                                if (odd)
                                    b = (b & 0x0f) | ((src[dstPos + (cb >> 1)] << 4) & 0xf0);
                                else
                                    b = (b & 0xf0) | (src[dstPos + (cb >> 1)] & 0x0f);
                            }

                            dst[srcCnt] = (byte)b;
                            if (odd)
                                srcCnt++;
                            odd = !odd;
                        }
                    }
                }
            }
        }
    }


    public abstract class Ps2SwizzlerBase
    {
        public static readonly int PageWidth;
    }
}
