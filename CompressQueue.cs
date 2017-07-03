using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiThreadingGzipCompressor
{
    public class ChunckQueue
    {

        private List<Chunck> _list = new List<Chunck>();
        private int _size;

        public ChunckQueue(int size)
        {
            _size = size;
        }

        public bool IsEmpty()
        {
            return _list.Count == 0;
        }

        public bool IsFull()
        {
            return _list.Count == _size;
        }

        public void Enqueue(Chunck chunck)
        {
            _list.Add(chunck);
        }

        public Chunck Dequeue()
        {
            var item = _list[0];
            _list.RemoveAt(0);
            return item;
        }

        public Chunck Peek()
        {
            return _list[0];
        }

        public Chunck GetUncompressChunck()
        {
            return _list.FirstOrDefault(x => x.State == ChunckState.NoCompress);
        }
    }
}
