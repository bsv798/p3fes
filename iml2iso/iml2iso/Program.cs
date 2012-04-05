using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iml2iso
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
            try
            {
                if (args.Length > 1)
                    new imlClass(args[0], args[1]).Rebuild();
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


        static void ShowUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("\timl2iso.exe iml_name iso_name");
            throw new ArgumentException("Parameters are not set");
        }
    }
}
