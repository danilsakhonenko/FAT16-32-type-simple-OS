using System.Collections;
using Chaos.Structures;
using Chaos.Tools;
using Chaos.Tools.Exceptions;

namespace Chaos.SystemComponents
{
    internal class FileSystem
    {

        private readonly string disk = "disk";
        private readonly StreamOperations stream;
        private readonly Session currentSession;
        public int EOF { get; private set; } = 65535;
        public FileSystem(StreamOperations stream, Session session)
        {
            currentSession = session;
            this.stream = stream;
            if (!File.Exists(disk))
            {
                stream.Open(disk);
                DiskFormat().GetAwaiter();
            }
            else stream.Open(disk);
        }

        public async Task DiskFormat()
        {
            await stream.Write(new byte[Superblock.FSSize], 0);
            var i = 0;
            await stream.Write(Superblock.Title.ToBytes(), i);
            i += 5;
            await stream.Write(Superblock.ClusterSize.ToBytes(), i);
            i += 2;
            await stream.Write(Superblock.FATSize.ToBytes(), i);
            i += 2;
            await stream.Write(Superblock.FATCount.ToBytes(), i);
            i += 1;
            await stream.Write(Superblock.UsersAreaSize.ToBytes(), i);
            i += 2;
            await stream.Write(Superblock.DataAreaSize.ToBytes(), i);
            var rootuser = new User() { Id = 1, Name = "root", Password = "root" };
            await stream.Write((byte[])rootuser, Superblock.UsersAreaStart);
            var rootDir = new FileRecord()
            {
                Name = rootuser.Name,
                Attributes = 160,
                Rights = 252,
                DateTime = DateTime.Now.ToLong(),
                Cluster = 1,
                Size = 0,
                UserId = rootuser.Id
            };
            await stream.Write(EOF.ToBytes(), Superblock.TablesStart);
            await stream.Write(EOF.ToBytes(), Superblock.TablesStart + Superblock.FATSize);
            await stream.Write(EOF.ToBytes(), Superblock.TablesStart + 2);
            await stream.Write(EOF.ToBytes(), Superblock.TablesStart + 2 + Superblock.FATSize);
            await stream.Write((byte[])rootDir, Superblock.DataAreaStart);
        }
        public async Task WriteData(byte[] array, int startPosition, int endPosition)
        {
            for (int i = startPosition; i <= endPosition; i += array.Length)
            {
                var arr = new byte[array.Length];
                await stream.Read(arr, i);
                bool isClear = true;
                for (int j = 0; j < arr.Length; j++)
                {
                    if (arr[j] != '\0')
                    {
                        isClear = false;
                        break;
                    }
                }
                if (isClear)
                {
                    await stream.Write(array, i);
                    return;
                }
            }
            throw new NoFreeSpaceException();
        }

        public async Task<short> FindLastCluster(short start)
        {
            var next = start;
            var cell = new byte[2];
            do
            {
                start = next;
                await stream.Read(cell, Superblock.TablesStart + start * 2);
                next = cell.ToShort();
            } while (next != -1);
            return start;
        }

        public async Task<short> GetNextCluster(short start)
        {
            var cell = new byte[2];
            await stream.Read(cell, Superblock.TablesStart + start * 2);
            var next = cell.ToShort();
            if (next == -1)
                return start;
            else
                return next;
        }

        public async Task WriteData(byte[] array, int position)
        {
            await stream.Write(array, position);
        }

        public async Task<short> GetCluster()
        {
            for (int i = Superblock.TablesStart + 4; i < Superblock.TablesStart + Superblock.FATSize; i += 2)
            {
                var data = new byte[2];
                await stream.Read(data, i);
                bool empty = true;
                for (int j = 0; j < data.Length; j++)
                {
                    if (data[j] != 0)
                    {
                        empty = false;
                        break;
                    }
                }
                if (empty)
                    return (short)((i - Superblock.TablesStart) / 2);
            }
            throw new NoFreeClusterException();
        }
        public async Task<byte[]> ReadData(int startPosition, int size)
        {
            var bytes = new byte[size];
            await stream.Read(bytes, startPosition);
            return bytes;
        }

        public async Task<List<FileRecord>> GetAllFiles(short cluster)
        {
            var allfiles = new List<FileRecord>();
            short next;
            while (true)
            {
                var data = await ReadData(Superblock.DataAreaStart + (cluster - 1) *
                    Superblock.ClusterSize, Superblock.ClusterSize);
                var list = data.AsFileList();
                allfiles.AddRange(list);
                next = await GetNextCluster(cluster);
                if (next == cluster)
                    return allfiles;
                else
                    cluster = next;
            }
        }
        public async Task<ArrayList> FindFile(string name, short cluster)
        {
            ArrayList info = new ArrayList();
            short next = cluster;
            do
            {
                cluster = next;
                var data = await ReadData(Superblock.DataAreaStart + (cluster - 1) *
                    Superblock.ClusterSize, Superblock.ClusterSize);
                var list = data.AsFileList();
                var file = list.Find(x => x.Name == name);
                if (file == null)
                    next = await GetNextCluster(cluster);
                else
                {
                    info.Add(file);
                    info.Add(cluster);
                    info.Add(list.IndexOf(file));
                    return info;
                }
            } while (cluster != next);
            return null;
        }


        public async Task<FileRecord> GetDir(string filepath)
        {
            FileRecord? item = null;
            var directoryArray = filepath.Split('\\');
            var data = await ReadData(Superblock.DataAreaStart, Superblock.ClusterSize);
            var list = await GetAllFiles(1);
            if (filepath == "")
            {
                return new FileRecord() { Name = "" };
            }
            for (int i = 0; i < directoryArray.Length; i++)
            {
                item = list.Where(x => x.Name == directoryArray[i] &&
                x.Attributes.AsAttributesArray()[0]).FirstOrDefault();
                if (item == null) throw new NoFileException();
                currentSession.isReadable(item);
                var cluster = item?.Cluster ?? throw new NoFileException();
                list = await GetAllFiles(cluster);
            }
            if (item == null) throw new NoFileException();
            return item;
        }
        public async Task ResizeDirectories(string path,int changeSize)
        {
            if (path == null || path == String.Empty)
                path = currentSession.CurrentPath.Remove(currentSession.CurrentPath.Length - 1);
            var dirArr = path.Split('\\');
            path =dirArr[0];
            for (int i = 0; i < dirArr.Length-1; i++)
            {
                var dir = await GetDir(path);
                if (i + 1 == dirArr.Length)
                    break;
                var nextdir = await FindFile(dirArr[i+1],dir.Cluster);
                if (nextdir != null)
                {
                    ((FileRecord)nextdir[0]).Size = ((FileRecord)nextdir[0]).Size + changeSize;
                    await WriteData((byte[])(FileRecord)nextdir[0], Superblock.DataAreaStart + ((short)nextdir[1] - 1) *
                        Superblock.ClusterSize + (int)nextdir[2] * FileRecord.RecordSize);
                }
                path += "\\"+dirArr[i + 1];
            }
        }
    }
}

