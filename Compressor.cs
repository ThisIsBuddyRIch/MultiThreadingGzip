using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;

namespace MultiThreadingGzipCompressor
{

    public class Compressor
    {
        private object _sync = new object();
        private volatile bool _stopWorkers = false;
        private ChunckReadFromFile _reader;
        private Queue<byte[]> queueForWrite = new Queue<byte[]>();
        private readonly string _filePath;


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
                else
                {
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
                while (true)
                {
                    Monitor.Enter(_sync);
                    if (_reader.ChunckQueue.IsEmpty())
                    {
                        Monitor.Exit(_sync);
                        if (_stopWorkers)
                        {
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
                        var isFull = _reader.ChunckQueue.IsFull();
                        var chunck = _reader.ChunckQueue.Dequeue();
                        if (_reader.ChunckQueue.QueueIsFull)
                        {
                            _reader.ChunckQueue.QueueIsFull = false;
                            Console.WriteLine("Wake up");
                            Monitor.Pulse(_sync);
                        }

                        Monitor.Exit(_sync);

                        writer.Write(chunck.Data);
                        writer.Flush();
                    }
                }
            }
        }
    }
}
