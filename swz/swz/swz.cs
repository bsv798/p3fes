using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace swz
{
    public class swz
    {
        int[] block32 =
            {
	             0,  1,  4,  5, 16, 17, 20, 21,
	             2,  3,  6,  7, 18, 19, 22, 23,
	             8,  9, 12, 13, 24, 25, 28, 29,
	            10, 11, 14, 15, 26, 27, 30, 31
            };


        int[] columnWord32 =
            {
	             0,  1,  4,  5,  8,  9, 12, 13,
	             2,  3,  6,  7, 10, 11, 14, 15
            };

        public swz()
        {

        }

        public void writeTexPSMCT32(byte[] src, int rrw, int rrh, out byte[] dst)
        {
            int srcCnt;
            int dbw;

            dst = new byte[src.Length];
            srcCnt = 0;
            dbw = rrw / 64;
            for (int y = 0; y < rrh; y++)
            {
                for (int x = 0; x < rrw; x++)
                {
                    int pageX = x / 64; //х страницы в текстуре
                    int pageY = y / 32;
                    int page = pageX + pageY * dbw;

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

                    Array.Copy(src, srcCnt, dst, (page * 2048 + block * 64 + column * 16 + cw) * 4, 4);
                    srcCnt += 4;
                }
            }
        }

        public void readTexPSMCT32(byte[] src, int rrw, int rrh, out byte[] dst)
        {
            int srcCnt;
            int dbw;

            dst = new byte[src.Length];
            srcCnt = 0;
            dbw = rrw / 64;
            for (int y = 0; y < rrh; y++)
            {
                for (int x = 0; x < rrw; x++)
                {
                    int pageX = x / 64; //х страницы в текстуре
                    int pageY = y / 32;
                    int page = pageX + pageY * dbw;

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

                    Array.Copy(src, (page * 2048 + block * 64 + column * 16 + cw) * 4, dst, srcCnt, 4);
                    srcCnt += 4;
                }
            }
        }
    }
}
