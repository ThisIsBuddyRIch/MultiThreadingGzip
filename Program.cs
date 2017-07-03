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
            compressor.Compress(8);

            sw.Stop();
            Console.WriteLine((sw.ElapsedMilliseconds / 100.0).ToString());

            Console.WriteLine("Finish");

            Console.ReadKey();

        }
    }

    public class Compressor
    {
        private object _sync = new object();
        private volatile bool _stopWorkers = false;
        private ChunckReadFromFile _reader;
        private Queue<byte[]> queueForWrite = new Queue<byte[]>();
        private readonly string _filePath ;
        

        public Compressor(string file)
        {
            _filePath = file;
            if (!File.Exists(_filePath))
            {
                throw new ArgumentException("File not found, name : " + _filePath);
            }

            _reader = new ChunckReadFromFile(_sync, _filePath);
        }


        public void Compress(int workers)
        {
            var threadForRead = new Thread(_reader.Read);
            threadForRead.Start();
            List<Thread> workersThread = new List<Thread>();


            foreach (var i in Enumerable.Range(0, workers))
            {
                var item = new Thread(CompressForOneWorker);
                workersThread.Add(item);
                item.Start();
            }

            var threadForWrite = new Thread(Write);
            threadForWrite.Start();

            threadForRead.Join();
            RequestStopWorkers();

            foreach (var item in workersThread)
            {
                item.Join();
            }
            threadForWrite.Join();
        }

        private void RequestStopWorkers()
        {
            _stopWorkers = true;
        }
        

        private void CompressForOneWorker()
        {
            var count = 0;
            while (true)
            {
                Monitor.Enter(_sync);
                var chunck = _reader.ChunckQueue.GetUncompressChunck();
                if (chunck == null)
                {
                    Monitor.Exit(_sync);
                    if (_stopWorkers)
                    {
                        Console.WriteLine("Worker finish Job : " + Thread.CurrentThread.ManagedThreadId);
                        Console.WriteLine("Chunck is " + count);
                        return;
                    }
                    //Console.WriteLine("Worker has slept : " + Thread.CurrentThread.ManagedThreadId);
                    Thread.Sleep(Constants.threadSleep);
                }
                else {
                    count++;
                    chunck.State = ChunckState.InProgress;
                    Monitor.Exit(_sync);

                    chunck.Data = CompressToByteArray(chunck.Data);
                    chunck.State = ChunckState.Compressed;
                }
            }
        }

        private byte[] CompressToByteArray(byte[] raw)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(memory,
                    CompressionMode.Compress, true))
                {
                    gzip.Write(raw, 0, raw.Length);
                }
                return memory.ToArray();
            }
        }

        private void Write()
        {
            using (var writer = new BinaryWriter(File.Open(_filePath + ".gz", FileMode.Create)))
            {
                var count = 0;
                while (true)
                {
                    Monitor.Enter(_sync);
                    if (_reader.ChunckQueue.IsEmpty())
                    {
                        Monitor.Exit(_sync);
                        if (_stopWorkers)
                        {
                            Console.WriteLine("Count write" + count);
                            return;
                        }
                        Thread.Sleep(Constants.threadSleep);
                    }
                    else if (_reader.ChunckQueue.Peek().State != ChunckState.Compressed)
                    {
                        Monitor.Exit(_sync);
                        Thread.Sleep(Constants.threadSleep);
                    }

                    else
                    {
                        var chunck = _reader.ChunckQueue.Dequeue();
                        count++;
                        Monitor.Exit(_sync);

                        writer.Write(chunck.Data);
                        writer.Flush();
                    }
                }
            }
    }
}

  

    public class Constants
    {
        public const int bufferSize = 1024;

        public const int queueSize = 1000;

        public const int threadSleep = 5;
    }
}

