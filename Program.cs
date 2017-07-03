using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Diagnostics;

namespace MultiThreadingGzipCompressor
{
    class Program
    {
        private static string directoryPath = @"C:\Temp";
        static void Main(string[] args)
        {
            var sw = new Stopwatch();
            sw.Start();
            var compressor = new Compressor(@"C:\Temp\Урок 14. Composite.mkv");
            compressor.Compress(4);

            sw.Stop();
            Console.WriteLine((sw.ElapsedMilliseconds / 100.0).ToString());

            Console.WriteLine("Finish");

            Console.ReadKey();

        }
    }


  

    public class Constants
    {
        public const int bufferSize = 1024;

        public const int queueSize = 1000;

        public const int threadSleep = 5;
    }
}

