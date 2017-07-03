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
            
            using (var reader = new BinaryReader(File.Open(_filename, FileMode.Open)))
            {
                while (true)
                {
                    var arr = reader.ReadBytes(Constants.bufferSize);

                    if (arr.Length == 0)
                    {
                        return;
                    }

                    Monitor.Enter(_sync);
                    if (ChunckQueue.IsFull())
                    {
                        ChunckQueue.QueueIsFull = true;
                        Console.WriteLine("Уснул на переменной");
                        Monitor.Wait(_sync);
                    }

                    ChunckQueue.Enqueue(new Chunck { Data = arr });
                    Monitor.Exit(_sync);
                }
                
            }

        }
    }
}

