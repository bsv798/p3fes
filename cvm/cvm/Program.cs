using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using sio = System.IO;

namespace cvm
{
    class Program
    {
        static int Main(string[] args)
        {
#if DEBUG
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Debug version");
            Console.ForegroundColor = ConsoleColor.Gray;
#endif

            CvmClass cvm;
#if DEBUG
            {
                System.Environment.SetEnvironmentVariable("gamedir", @"e:\Trance\p3fes\tools\..\game");
                System.Environment.SetEnvironmentVariable("resdir", @"e:\Trance\p3fes\tools\..\res");
                cvm = new CvmClass(@"e:\Trance\p3fes\tools\..\game", "DATA.CVM", @"e:\Trance\p3fes\tools\..\res");
                cvm.Pack();
                return 0;
            }
#endif

            try
            {
                if (CheckArgs(ref args))
                {
                    AddClosingDirSep(ref args[1]);
                    AddClosingDirSep(ref args[3]);
                    cvm = new CvmClass(args[1], args[2], args[3]);
                    if (args[0] == "-e")
                        cvm.Unpack();
                    else
                        cvm.Pack();
                    Console.WriteLine("Done");
                }
                else
                    ShowUsage();
                return 0;
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(e.Message);
                Console.ForegroundColor = ConsoleColor.Gray;
                return -1;
            }
        }

        static bool CheckArgs(ref string[] args)
        {
            for (int i = 0; i < args.Length; i++)
                args[i] = args[i].ToLower();

            if (args.Length == 0)
                ShowUsage();
            else if (args[0] == "-e" || args[0] == "-r")
            {
                if (args.Length < 4)
                {
                    if (CheckVars())
                    {
                        Array.Resize<string>(ref args, 4);
                        args[2] = args[1];
                        args[1] = System.Environment.GetEnvironmentVariable("gamedir");
                        args[3] = System.Environment.GetEnvironmentVariable("resdir");
                        return true;
                    }
                    else
                        ShowUsage();
                }
                else
                    return true;
            }
            else
                ShowUsage();

            return false;
        }

        static bool CheckVars()
        {
            bool res;

            res = true;
            res &= System.Environment.GetEnvironmentVariables().Contains("gamedir");
            res &= System.Environment.GetEnvironmentVariables().Contains("resdir");

            return res;
        }

        static void ShowUsage()
        {
            Console.WriteLine("Usage:");
            throw new ArgumentException("Parameters are not set");
        }

        static void AddClosingDirSep(ref string path)
        {
            if (path.LastIndexOf(sio.Path.DirectorySeparatorChar) < path.Length - 1)
                path += sio.Path.DirectorySeparatorChar;
        }

        //System.IO.FileStream fs;
        //PathTableRecords pts;
        //VolumeDescriptor vdr;
        //ISO9660 iso;

        ////fs = new System.IO.FileStream(@"e:\Trance\p3fes\game\DATA.CVM ", System.IO.FileMode.Open, System.IO.FileAccess.Read);

        ////fs.Position = 0xA800;
        ////pts = new PathTableRecords(fs, 1562);
        ////pts = new PathTableRecords(config.impPath);

        ////fs.Position = 0x9800;
        ////vdr = new PrimaryVolumeDescriptor(fs);

        ////iso = new ISO9660("e:\\Trance\\p3fes\\game\\DATA.CVM ", 3);
        ////iso.ExtractPartition("e:\\Trance\\p3fes\\DATA_CVM ");
        ////iso = new ISO9660("e:\\Trance\\p3fes\\img\\org\\Shin Megami Tensei Persona 3 FES [NTSC-ENG].iso ", 0);
        ////iso.ExtractPartition("e:\\Trance\\p3fes\\DATA_CVM ");
        //iso = new ISO9660("e:\\Temp\\DATA_NEW_CVM");
        //iso.RebuildPartition("e:\\Trance\\p3fes\\data_new.cvm");
        ////iso = new ISO9660("e:\\Trance\\p3fes\\data_new.cvm ", 0);
        ////iso.ExtractPartition("e:\\Temp\\DATA_NEW_CVM ");


        ////fs.Close();
    }
}
