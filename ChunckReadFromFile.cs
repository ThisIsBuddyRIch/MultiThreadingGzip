using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace MultiThreadingGzipCompressor
{
    public class ChunckReadFromFile
    {
        private object _sync;
        private string _filename;

        

        public ChunckReadFromFile(object sync, string filename)
        {
            _sync = sync;
            _filename = filename;
        }

        public ChunckQueue ChunckQueue { get; set; } = new ChunckQueue(Constants.queueSize);

        public void Read()
        {
            var count = 0;
            using (var reader = new BinaryReader(File.Open(_filename, FileMode.Open)))
            {
                while (true)
                {
                    Monitor.Enter(_sync);
                    if (ChunckQueue.IsFull())
                    {
                        Monitor.Exit(_sync);
                        Thread.Sleep(Constants.threadSleep);

                    }
                    else
                    {
                        Monitor.Exit(_sync);
                        var arr = reader.ReadBytes(Constants.bufferSize);
                       
                        if (arr.Length == 0)
                        {
                            return;
                        }

                        Monitor.Enter(_sync);
                        ChunckQueue.Enqueue(new Chunck { Data = arr });
                        count++;
                        Monitor.Exit(_sync);
                    }
                }
            }

        }
    }
}

