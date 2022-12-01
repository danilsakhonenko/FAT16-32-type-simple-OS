using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chaos.Tools
{
    internal class StreamOperations : IDisposable
    {
        private FileStream fstream;
        public void Dispose()
        {
            fstream?.Close();
            fstream?.Dispose();
        }
        public void Open(string filename)
        {
            fstream = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        }
        public async Task Write(byte[] buffer, long position)
        {
            fstream.Position = position;
            await fstream.WriteAsync(buffer, 0, buffer.Length);
            await fstream.FlushAsync();
        }
        public async Task Read(byte[] buffer, long position)
        {
            fstream.Position = position;
            await fstream.ReadAsync(buffer, 0, buffer.Length);
        }
    }
}