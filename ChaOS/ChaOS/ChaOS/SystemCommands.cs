using System.Text.RegularExpressions;
using Chaos.Structures;
using Chaos.Tools;
using Chaos.Tools.Exceptions;
using System.Collections;

namespace Chaos.SystemComponents.Commands
{
    internal interface ICommand
    {
        public void Info();
        public bool isExecutable(string command);
        public Task Run(string command);
    }
    internal class DiskFormatCommand : ICommand
    {
        private readonly FileSystem fs;
        private Session session;
        public bool isExecutable(string command) => Regex.IsMatch(command, "format");
        public DiskFormatCommand(FileSystem fs, Session session)
        {
            this.fs = fs;
            this.session = session;
        }
        public async Task Run(string command)
        {
            session.RequestForAccess();
            await fs.DiskFormat();
            session.User = null;
            session.CurrentPath = "";
            session.IsRoot = false;
        }
        public void Info()
        {
            Console.WriteLine("format\t - форматирование файловой системы");
        }
    }
    internal class AddUserCommand : ICommand
    {
        private readonly Session session;
        private readonly FileSystem fs;
        public AddUserCommand(FileSystem fs, Session session)
        {
            this.session = session;
            this.fs = fs;
        }
        public bool isExecutable(string command) => Regex.IsMatch(command, "adduser [A-Za-z]+ [A-Za-z1-9]+");
        public async Task Run(string command)
        {
            var arr = command.Split(' ');
            session.RequestForAccess();
            var usersData = await fs.ReadData(Superblock.UsersAreaStart, Superblock.UsersAreaSize);
            var userList = usersData.AsUserList();
            if (userList.FirstOrDefault(x => x.Name == arr[1]) != null)
                throw new UsernameException();
            var user = new User()
            {
                Id = GenerateID(userList),
                Name = arr[1],
                Password = arr[2]
            };
            var cluster = await fs.GetCluster();
            var userDirectory = new FileRecord()
            {
                Name = user.Name,
                Attributes = 160,
                Rights = 224,
                DateTime = DateTime.Now.ToLong(),
                Cluster = cluster,
                Size = 0,
                UserId = user.Id
            };
            var recordcluster = await fs.FindLastCluster(1);
            await fs.WriteData(fs.EOF.ToBytes(), Superblock.TablesStart + cluster * 2);
            await fs.WriteData(fs.EOF.ToBytes(), Superblock.TablesStart + Superblock.FATSize + cluster * 2);
            await fs.WriteData((byte[])user, Superblock.UsersAreaStart, Superblock.DataAreaStart);
            await fs.WriteData((byte[])userDirectory, Superblock.DataAreaStart + recordcluster * 2 - 2,
                Superblock.DataAreaStart + recordcluster * 2 - 2 + Superblock.ClusterSize);
            Console.WriteLine("Пользователь успешно создан.");
        }
        public void Info()
        {
            Console.WriteLine("adduser [имя польз.] [пароль]\t - создание нового пользователя");
        }

        private short GenerateID(List<User> list)
        {
            short id = 2;
            for (int i = 1; i < list.Count; i++)
            {
                if (id == list[i].Id)
                    id++;
            }
            return id;
        }
    }
    internal class LogInCommand : ICommand
    {
        private readonly FileSystem fs;
        private readonly Session session;
        public bool isExecutable(string command) => Regex.IsMatch(command, "login [A-Za-z1-9]+ [A-Za-z1-9]+");
        public LogInCommand(FileSystem fs, Session session)
        {
            this.fs = fs;
            this.session = session;
        }
        public async Task Run(string command)
        {
            if (session.User != null)
                throw new SessionLoginException();
            var arr = command.Split(' ');
            var data = await fs.ReadData(Superblock.UsersAreaStart, Superblock.UsersAreaSize);
            var usersData = await fs.ReadData(Superblock.UsersAreaStart, Superblock.UsersAreaSize);
            var userList = usersData.AsUserList();
            for (int i = 0; i < userList.Count; i++)
            {
                if (userList[i].Name == arr[1] &&
                    userList[i].Password == arr[2])
                {
                    session.User = new User()
                    {
                        Id = userList[i].Id,
                        Name = userList[i].Name,
                        Password = userList[i].Password
                    };
                    session.IsRoot = session.User.Name == "root";
                    session.CurrentPath = Session.RootDirectory;
                    Console.WriteLine("Авторизация выполнена успешно.");
                    return;
                }
            }
            Console.WriteLine("Ошибка. Неверное имя пользователя или пароль");
        }
        public void Info()
        {
            Console.WriteLine("login [имя польз.] [пароль]\t - авторизация пользователя");
        }

    }

    internal class LogOutCommand : ICommand
    {
        private readonly FileSystem fs;
        private readonly Session session;
        public LogOutCommand(FileSystem fs, Session session)
        {
            this.fs = fs;
            this.session = session;
        }
        public bool isExecutable(string command) => Regex.IsMatch(command, "logout");

        public async Task Run(string command)
        {
            if (session.User == null)
                throw new NotInSessionException();
            session.User = null;
            session.CurrentPath = "";
            session.IsRoot = false;
        }
        public void Info()
        {
            Console.WriteLine("logout\t - выход из учетной записи");
        }
    }

    internal class CreateDirCommand : ICommand
    {
        private readonly FileSystem fs;
        private readonly Session session;
        public CreateDirCommand(FileSystem fs, Session
       session)
        {
            this.fs = fs;
            this.session = session;
        }
        public bool isExecutable(string command) =>
       Regex.IsMatch(command, "crdir ([A-Za-zА-Яа-я1-9\\ ])+");
        public async Task Run(string command)
        {
            if (session.User == null)
                throw new AccessException();
            var arr = command.Split(' ');
            var path = arr[1].GetPath();
            if (path.Filename.Length > 11)
                throw new FilenameException();
            var dir = await fs.GetDir(path.DirectoryPath ??
                session.CurrentPath.Remove(session.CurrentPath.Length - 1));
            session.isWritable(dir);
            var list = await fs.GetAllFiles(dir.Cluster);
            list.ForEach(x =>
            {
                if (x.Name == path.Filename)
                    throw new FileExistException();
            });
            var cluster = await fs.GetCluster();
            var newDir = new FileRecord()
            {
                Name = path.Filename,
                Attributes = 128,
                Rights = 224,
                DateTime = DateTime.Now.ToLong(),
                Cluster = cluster,
                Size = 0,
                UserId = session.User.Id
            };
            var recordcluster = await fs.FindLastCluster(dir.Cluster);
            await fs.WriteData(fs.EOF.ToBytes(), Superblock.TablesStart + cluster * 2);
            await fs.WriteData(fs.EOF.ToBytes(), Superblock.TablesStart + Superblock.FATSize + cluster * 2);
            await fs.WriteData((byte[])newDir, recordcluster * Superblock.ClusterSize - Superblock.ClusterSize
                + Superblock.DataAreaStart, recordcluster * Superblock.ClusterSize + Superblock.DataAreaStart);
        }
        public void Info()
        {
            Console.WriteLine("crdir [имя каталога]\t - создание нового каталога");
        }
    }

    internal class OpenDirCommand : ICommand
    {
        private readonly FileSystem fs;
        private readonly Session session;
        public OpenDirCommand(FileSystem fs, Session session)
        {
            this.fs = fs;
            this.session = session;
        }

        public bool isExecutable(string command) =>
       (Regex.IsMatch(command, "opendir ([A-Za-zА-Яа-я1-9\\ ])+") ||
       Regex.IsMatch(command, "opendir .."));
        public async Task Run(string command)
        {
            var path = command.Split(' ')[1];
            if (path == "..")
            {
                if (session.CurrentPath == Session.RootDirectory)
                    return;
                int x = session.CurrentPath.Length - 1;
                session.CurrentPath = session.CurrentPath.Remove(x);
                var index = session.CurrentPath.GetLastCharIndex('\\');
                session.CurrentPath = session.CurrentPath.Substring(0, index + 1);
            }
            else if (path.Contains('\\'))
            {
                await fs.GetDir(path);
                session.CurrentPath = path;
            }
            else
            {
                if (session.CurrentPath == Session.RootDirectory && path == "root")
                    throw new NoFileException();
                await fs.GetDir(session.CurrentPath + path);
                session.CurrentPath = session.CurrentPath + path + "\\";
            }
        }
        public void Info()
        {
            Console.WriteLine("opendir [имя каталога]\t - Переход в каталог");
        }
    }

    internal class DirListCommand : ICommand
    {
        private readonly FileSystem fs;
        private readonly Session session;
        public DirListCommand(FileSystem fs, Session session)
        {
            this.fs = fs;
            this.session = session;
        }
        public bool isExecutable(string command) =>
       Regex.IsMatch(command, "dirlist( [/A-Za-z1-9]+)?");
        public async Task Run(string command)
        {
            var arr = command.Split(' ');
            FileRecord dir;
            if (arr.Length == 1)
            {
                dir = await
               fs.GetDir(session.CurrentPath.Remove(session.CurrentPath.Length - 1));
            }
            else
            {
                dir = await fs.GetDir(arr[1]);
            }
            var usersData = await fs.ReadData(Superblock.UsersAreaStart, Superblock.UsersAreaSize);
            var userList = usersData.AsUserList();
            var list = await fs.GetAllFiles(dir.Cluster);
            Console.WriteLine("File\tUser\tAttr\tRights\tDate - Time\t\tSize");
            list.ForEach(x =>
            {
                if (!x.Attributes.AsAttributesArray()[1])
                    Console.WriteLine(x.ToString(userList));
            });
        }
        public void Info()
        {
            Console.WriteLine("dirlist [имя каталога]\t - вывод содержимого каталога");
        }
    }


    internal class RenameCommand : ICommand
    {
        private readonly FileSystem fs;
        private readonly Session session;
        public RenameCommand(FileSystem fs, Session session)
        {
            this.fs = fs;
            this.session = session;
        }
        public bool isExecutable(string command) =>
       Regex.IsMatch(command, "rename ([A-Za-zА-Яа-я1-9])+ ([AZa-zА-Яа-я1-9])");
        public async Task Run(string command)
        {
            if (session.User == null)
                throw new AccessException();
            var arr = command.Split(' ');
            if (arr[1].Length > 11 || arr[2].Length > 11)
                throw new FilenameException();
            var oldName = arr[1];
            var newName = arr[2];
            var parentdir = await fs.GetDir(session.CurrentPath.Remove(session.CurrentPath.Length - 1));
            session.isWritable(parentdir);
            var info = await fs.FindFile(oldName, parentdir.Cluster) ??
                throw new NoFileException();
            if (((FileRecord)info[0]).Attributes.AsAttributesArray()[2])
                throw new ReadOnlyException();
            session.isWritable((FileRecord)info[0]);
            var newNameBytes = new byte[11];
            newName.ToBytes().CopyTo(newNameBytes, 0);
            await fs.WriteData(newNameBytes, Superblock.DataAreaStart + ((short)info[1] - 1) *
                Superblock.ClusterSize + ((int)info[2]) * FileRecord.RecordSize);
        }
        public void Info()
        {
            Console.WriteLine("rename [имя файла]\t - переименования файла/каталога");
        }
    }

    internal class ChangeAttributesCommand : ICommand
    {
        private readonly FileSystem fs;
        private readonly Session session;
        public ChangeAttributesCommand(FileSystem fs, Session session)
        {
            this.fs = fs;
            this.session = session;
        }
        public bool isExecutable(string command) => Regex.IsMatch(command, "chattr ([A-Za-z1-9\\ ])+ (0|1)(0|1)");
        public async Task Run(string command)
        {
            if (session.User == null)
                throw new AccessException();
            var arr = command.Split(' ');
            if (arr[2].Length != 2)
                throw new ChattrException();
            var path = arr[1].GetPath();
            var dir = await fs.GetDir(path.DirectoryPath ??
                session.CurrentPath.Remove(session.CurrentPath.Length - 1));
            var info = await fs.FindFile(path.Filename, dir.Cluster) ??
                throw new NoFileException();
            session.isWritable((FileRecord)info[0]);
            var attr = ((FileRecord)info[0]).Attributes.AsAttributesArray();
            attr[1] = arr[2].Substring(0, 1) == "1";
            attr[2] = arr[2].Substring(1, 1) == "1";
            ((FileRecord)info[0]).Attributes = attr.ToByte();
            await fs.WriteData((byte[])(FileRecord)info[0], Superblock.DataAreaStart + ((short)info[1] - 1) *
                Superblock.ClusterSize + (int)info[2] * FileRecord.RecordSize);
        }
        public void Info()
        {
            Console.WriteLine("chattr [имя файла] [права доступа]\t - задание новых разрешений пользователй");
        }
    }

    internal class ChangeRightsCommand : ICommand
    {
        private readonly FileSystem fs;
        private readonly Session session;
        public ChangeRightsCommand(FileSystem fs,
       Session session)
        {
            this.fs = fs;
            this.session = session;
        }
        public bool isExecutable(string command) =>
       Regex.IsMatch(command, "chmod ([A-Za-z1-9\\ ])+ (0|1)(0|1)(0|1)(0|1)(0|1)(0|1)");
        public async Task Run(string command)
        {
            if (session.User == null)
                throw new AccessException();
            var arr = command.Split(' ');
            if (arr[2].Length != 6)
                throw new ChmodException();
            var path = arr[1].GetPath();
            var dir = await fs.GetDir(path.DirectoryPath ??
                session.CurrentPath.Remove(session.CurrentPath.Length - 1));
            var info = await fs.FindFile(path.Filename, dir.Cluster) ??
                throw new NoFileException();
            session.isWritable((FileRecord)info[0]);
            ((FileRecord)info[0]).Rights = arr[2].ToByte();
            await fs.WriteData((byte[])(FileRecord)info[0], Superblock.DataAreaStart + ((short)info[1] - 1) *
                Superblock.ClusterSize + (int)info[2] * FileRecord.RecordSize);
        }
        public void Info()
        {
            Console.WriteLine("chmod [имя файла] [права доступа]\t - задание новых разрешений пользователй");
        }
    }

    internal class UserListCommand : ICommand
    {
        private readonly FileSystem fs;
        public UserListCommand(FileSystem fs)
        {
            this.fs = fs;
        }
        public bool isExecutable(string command) => command == "userlist";
        public async Task Run(string command)
        {
            var data = await fs.ReadData(Superblock.UsersAreaStart, Superblock.UsersAreaSize);
            var list = data.AsUserList();
            Console.WriteLine("ID\t Name");
            list.ForEach(x => Console.WriteLine(x));
        }
        public void Info()
        {
            Console.WriteLine("userlist\t - информация о всех пользователях");
        }
    }

    internal class DeleteUserCommand : ICommand
    {
        private readonly FileSystem fs;
        private readonly Session session;
        public DeleteUserCommand(FileSystem fs, Session session)
        {
            this.fs = fs;
            this.session = session;
        }
        public bool isExecutable(string command) => Regex.IsMatch(command, "deluser [A-Za-z]+");
        public async Task Run(string command)
        {
            var arr = command.Split(' ');
            var user = arr[1];
            if (user == "root") throw new AccessException();
            session.RequestForAccess();
            var data = await fs.ReadData(Superblock.UsersAreaStart, Superblock.UsersAreaSize);
            var userlist = data.AsUserList();
            for (int i = 0; i < userlist.Count; i++)
            {
                if (userlist[i].Name == user)
                {
                    var info = await fs.FindFile(user, 1) ??
                        throw new NoFileException();
                    var newName = user;
                    if (newName.Length > 7)
                        newName.Remove(6);
                    newName += DateTime.Now.Date.ToString().Remove(5).Replace(".", "");
                    ((FileRecord)info[0]).Name = newName;
                    ((FileRecord)info[0]).DateTime = DateTime.Now.ToLong();
                    ((FileRecord)info[0]).Attributes = 224;
                    ((FileRecord)info[0]).UserId = 1;
                    await fs.WriteData((byte[])(FileRecord)info[0], Superblock.DataAreaStart + ((short)info[1] - 1) *
                        Superblock.ClusterSize + (int)info[2] * FileRecord.RecordSize);
                    await fs.WriteData(new byte[User.SizeInBytes], Superblock.UsersAreaStart + i * User.SizeInBytes);
                    Console.WriteLine("Пользователь удален.");
                    return;
                }
            }
            throw new Exception("Пользователь не найден.");
        }
        public void Info()
        {
            Console.WriteLine("deluser [имя польз.]\t delete users");
        }
    }

    internal class CreateFileCommand : ICommand
    {
        private readonly FileSystem fs;
        private readonly Session session;
        public CreateFileCommand(FileSystem fs, Session session)
        {
            this.fs = fs;
            this.session = session;
        }

        public bool isExecutable(string command) => Regex.IsMatch(command, "crfile ([A-Za-zА-Яа-я1-9\\ ])+");
        public async Task Run(string command)
        {
            if (session.User == null)
                throw new AccessException();
            var arr = command.Split(' ');
            var path = arr[1].GetPath();
            var dir = await fs.GetDir(path.DirectoryPath ??
                session.CurrentPath.Remove(session.CurrentPath.Length - 1));
            session.isWritable(dir);
            var list = await fs.GetAllFiles(dir.Cluster);
            list.ForEach(x =>
            {
                if (x.Name == path.Filename)
                    throw new FileExistException();
            });
            var cluster = await fs.GetCluster();
            var newFile = new FileRecord()
            {
                Name = path.Filename,
                Attributes = 0,
                Rights = 192,
                DateTime = DateTime.Now.ToLong(),
                Cluster = cluster,
                Size = 0,
                UserId = session.User.Id
            };
            var recordcluster = await fs.FindLastCluster(dir.Cluster);
            await fs.WriteData(fs.EOF.ToBytes(), Superblock.TablesStart + cluster * 2);
            await fs.WriteData(fs.EOF.ToBytes(), Superblock.TablesStart + Superblock.FATSize + cluster * 2);
            await fs.WriteData((byte[])newFile, recordcluster * Superblock.ClusterSize - Superblock.ClusterSize
                 + Superblock.DataAreaStart, recordcluster * Superblock.ClusterSize + Superblock.DataAreaStart);
            await fs.ResizeDirectories(path.DirectoryPath, 32);
        }

        public void Info()
        {
            Console.WriteLine("crfile [имя/путь файла]\t - создание файла ");
        }
    }
    internal class FileWriteCommand : ICommand
    {
        private readonly FileSystem fs;
        private readonly Session session;
        private ArrayList fileinfo;
        public FileWriteCommand(FileSystem fs, Session session)
        {
            this.fs = fs;
            this.session = session;
        }
        public bool isExecutable(string command) => Regex.IsMatch(command, "filewrite (-a |-r )([A-Za-zА-Яа-я1-9\\ ])+");
        public async Task Run(string command)
        {
            if (session.User == null)
                throw new AccessException();
            var arr = command.Split(' ');
            var path = arr[2].GetPath();
            var dir = await fs.GetDir(path.DirectoryPath ?? session.CurrentPath.Remove(session.CurrentPath.Length - 1));
            fileinfo = await fs.FindFile(path.Filename, dir.Cluster) ??
                throw new NoFileException();
            if (((FileRecord)fileinfo[0]).Attributes.AsAttributesArray()[0])
                throw new NoFileException();
            session.isWritable((FileRecord)fileinfo[0]);
            var inputData = string.Empty;
            Console.WriteLine("(Чтобы остановить ввод нажмите Num0)Ввод данных:");
            inputData += Console.ReadLine();
            while (Console.ReadKey().Key != ConsoleKey.NumPad0)
            {
                inputData += Console.ReadLine();
            }
            ((FileRecord)fileinfo[0]).Size = 0;
            if (arr[1] == "-r")
                await Rewrite(inputData.ToBytes());
            else
                await Append(inputData.ToBytes());
            await fs.WriteData(((FileRecord)fileinfo[0]).Size.ToBytes(), Superblock.DataAreaStart +
                ((short)fileinfo[1] - 1) * Superblock.ClusterSize + ((int)fileinfo[2]) * FileRecord.RecordSize + 26);
            await fs.ResizeDirectories(path.DirectoryPath, ((FileRecord)fileinfo[0]).Size);
        }
        private async Task Rewrite(byte[] data)
        {
            var cluster = ((FileRecord)fileinfo[0]).Cluster;
            var next = cluster;
            do
            {
                cluster = next;
                await fs.WriteData(new byte[Superblock.ClusterSize], Superblock.DataAreaSize + Superblock.ClusterSize * cluster);
                next = await fs.GetNextCluster(cluster);
                await fs.WriteData(new byte[2], Superblock.TablesStart + (cluster) * 2);
                await fs.WriteData(new byte[2], Superblock.TablesStart + Superblock.FATSize + (cluster) * 2);
            } while (cluster != next);
            await WriteData(((FileRecord)fileinfo[0]).Cluster, data);
        }
        private async Task Append(byte[] data)
        {
            var last = await fs.FindLastCluster(((FileRecord)fileinfo[0]).Cluster);
            var oldDataBytes = await fs.ReadData(Superblock.DataAreaStart + Superblock.ClusterSize * (last - 1), Superblock.ClusterSize);
            var oldData = oldDataBytes.ToStr().Trim('\0').ToBytes();
            var newData = new byte[oldData.Length + data.Length];
            oldData.CopyTo(newData, 0);
            data.CopyTo(newData, oldData.Length);
            await WriteData(last, newData);
        }
        private async Task WriteData(int cluster, byte[] data)
        {
            var newCluster = -1;
            for (int i = 0; i < data.Length; i += Superblock.ClusterSize)
            {
                if (newCluster != -1)
                {
                    await fs.WriteData(newCluster.ToBytes(), Superblock.TablesStart + cluster * 2);
                    await fs.WriteData(newCluster.ToBytes(), Superblock.TablesStart + Superblock.FATSize + cluster * 2);
                    cluster = newCluster;
                }
                var partData = new byte[Superblock.ClusterSize];
                var size = (data.Length - i) >= Superblock.ClusterSize ? Superblock.ClusterSize : data.Length - i;
                Array.Copy(data, i, partData, 0, size);
                var startPosition = Superblock.DataAreaStart + Superblock.ClusterSize * (cluster - 1);
                await fs.WriteData(partData, startPosition);
                await fs.WriteData(fs.EOF.ToBytes(), Superblock.TablesStart + cluster * 2);
                await fs.WriteData(fs.EOF.ToBytes(), Superblock.TablesStart + Superblock.FATSize + cluster * 2);
                ((FileRecord)fileinfo[0]).Size += size;
                newCluster = await fs.GetCluster();
            }
        }
        public void Info()
        {
            Console.WriteLine("filewrite [имя/путь файла]\t - запить в файл");
        }
    }

    internal class FileReadCommand : ICommand
    {
        private readonly FileSystem fs;
        private readonly Session session;
        public FileReadCommand(FileSystem fs, Session session)
        {
            this.fs = fs;
            this.session = session;
        }
        public bool isExecutable(string command) => Regex.IsMatch(command, "fileread [A-Za-zА-Яа-я1-9\\ ]+");
        public async Task Run(string command)
        {
            if (session.User == null)
                throw new AccessException();
            var path = command.Split(' ')[1].GetPath();
            var dir = await fs.GetDir(path.DirectoryPath ?? session.CurrentPath.Remove(session.CurrentPath.Length - 1));
            var list = await fs.GetAllFiles(dir.Cluster);
            var file = list.Find(x => x.Name == path.Filename &&
            !x.Attributes.AsAttributesArray()[0]) ??
                throw new NoFileException();
            session.isReadable(file);
            var startcluster = file.Cluster;
            var nextcluster = startcluster;
            do
            {
                startcluster = nextcluster;
                var fileData = await fs.ReadData(Superblock.DataAreaStart + Superblock.ClusterSize * (startcluster - 1), Superblock.ClusterSize);
                Console.Write(fileData.ToStr());
                nextcluster = await fs.GetNextCluster(startcluster);
            } while (startcluster != nextcluster);
            Console.WriteLine();
        }
        public void Info()
        {
            Console.WriteLine("fileread [имя/путь файла]\t - Вывод содержимого файла");
        }
    }

    internal class RemoveFileCommand : ICommand
    {
        private readonly FileSystem fs;
        private readonly Session session;
        private Task rmTask;
        public RemoveFileCommand(FileSystem fs, Session session)
        {
            this.fs = fs;
            this.session = session;
        }
        public bool isExecutable(string command) => Regex.IsMatch(command, "rmfile ([A-Za-z1-9\\ ]+)");
        public async Task Run(string command)
        {
            if (session.User == null)
                throw new AccessException();
            var pathStr = command.Split(' ')[1];
            var path = pathStr.GetPath();
            var dir = await fs.GetDir(path.DirectoryPath ?? session.CurrentPath.Remove(session.CurrentPath.Length - 1));
            var info =await fs.FindFile(path.Filename,dir.Cluster) ?? 
                throw new NoFileException();
            if (((FileRecord)info[0]).Attributes.AsAttributesArray()[0])
                throw new NoFileException();
            if (((FileRecord)info[0]).Attributes.AsAttributesArray()[2])
                throw new ReadOnlyException();
            session.isWritable(dir);
            session.isWritable((FileRecord)info[0]);
            await fs.WriteData(new byte[FileRecord.RecordSize], Superblock.DataAreaStart + 
                ((short)info[1] - 1) * Superblock.ClusterSize + ((int)info[2]) * FileRecord.RecordSize);
            var cluster = ((FileRecord)info[0]).Cluster;
            var next = cluster;
            do
            {
                cluster = next;
                next = await fs.GetNextCluster(cluster);
                await fs.WriteData(new byte[2], Superblock.TablesStart + cluster * 2);
                await fs.WriteData(new byte[2], Superblock.TablesStart + Superblock.FATSize + cluster * 2);
                await fs.WriteData(new byte[Superblock.ClusterSize], Superblock.DataAreaStart + (cluster-1) * Superblock.ClusterSize);
            } while (cluster != next);
            await fs.ResizeDirectories(path.DirectoryPath, -32 - ((FileRecord)info[0]).Size);
        }
        public void Info()
        {
            Console.WriteLine("rmfile [имя/путь файла]\t - удаление файла");
        }
    }

    internal class RemoveDirCommand : ICommand
    {
        private readonly FileSystem fs;
        private readonly Session session;
        public RemoveDirCommand(FileSystem fs, Session session)
        {
            this.fs = fs;
            this.session = session;
        }
        public bool isExecutable(string command) => Regex.IsMatch(command, "rmdir ([A-Za-zА-Яа-я1-9\\ ])+");
        public async Task Run(string command)
        {
            if (session.User == null)
                throw new AccessException();
            var pathStr = command.Split(' ')[1];
            var path = pathStr.GetPath();
            var dir = await fs.GetDir(path.DirectoryPath ?? session.CurrentPath.Remove(session.CurrentPath.Length - 1));
            session.isWritable(dir);
            //Считываем файл из диска
            var info = await fs.FindFile(path.Filename, dir.Cluster) ??
                throw new NoFileException();
            if (!((FileRecord)info[0]).Attributes.AsAttributesArray()[0])
                throw new NoFileException();
            if (((FileRecord)info[0]).Attributes.AsAttributesArray()[2])
                throw new ReadOnlyException();
            session.isWritable((FileRecord)info[0]);
            await RecursRemoval((FileRecord)info[0]);
            await fs.WriteData(new byte[FileRecord.RecordSize], Superblock.DataAreaStart +
                ((short)info[1] - 1) * Superblock.ClusterSize + ((int)info[2]) * FileRecord.RecordSize);
            await fs.ResizeDirectories(path.DirectoryPath, -((FileRecord)info[0]).Size);
        }
        public void Info()
        {
            Console.WriteLine("rmdir [имя/путь каталога]\t - удалить каталог");
        }
        private async Task RecursRemoval(FileRecord fl)
        {
            var cluster = fl.Cluster;
            var list = await fs.GetAllFiles(cluster);
            var next = cluster;
            do
            {
                cluster = next;
                next = await fs.GetNextCluster(cluster);
                await fs.WriteData(new byte[2], Superblock.TablesStart + cluster * 2);
                await fs.WriteData(new byte[2], Superblock.TablesStart + Superblock.FATSize + cluster * 2);
                await fs.WriteData(new byte[Superblock.ClusterSize], Superblock.DataAreaStart + (cluster - 1) * Superblock.ClusterSize);
            } while (cluster != next);
            if (fl.Attributes.AsAttributesArray()[0])
            {
                for(int i=0; i < list.Count; i++)
                    await RecursRemoval(list[i]);
            }
        }
    }
    internal class MoveCommand : ICommand
    {
        private readonly FileSystem fs;
        private readonly Session session;
        public MoveCommand(FileSystem fs, Session session)
        {
            this.fs = fs;
            this.session = session;
        }
        public bool isExecutable(string command) => 
            Regex.IsMatch(command, "move ([A-Za-z1-9\\ ]+)([A-Za-z1-9\\ ]+)");
        public async Task Run(string command)
        {
            if (session.User == null)
                throw new AccessException();
            var arr = command.Split(' ');
            var path1Str = arr[1];
            var path2Str = arr[2];
            var path1 = path1Str.GetPath();
            var path2 = path2Str.GetPath();
            //Проверяем правильность указанных путей
            var dir1 = await fs.GetDir(path1.DirectoryPath ?? session.CurrentPath.Remove(session.CurrentPath.Length - 1));
            var dir2 = await fs.GetDir(path2.DirectoryPath ?? session.CurrentPath.Remove(session.CurrentPath.Length - 1));
            session.isWritable(dir1);
            session.isReadable(dir1);
            session.isWritable(dir2);
            var info = await fs.FindFile(path1.Filename, dir1.Cluster) ??
                throw new NoFileException();
            if (((FileRecord)info[0]).Attributes.AsAttributesArray()[2])
                throw new ReadOnlyException();
            session.isWritable((FileRecord)info[0]);
            await fs.WriteData(new byte[FileRecord.RecordSize], Superblock.DataAreaStart +
                ((short)info[1] - 1) * Superblock.ClusterSize + ((int)info[2]) * FileRecord.RecordSize);
            var cluster = await fs.FindLastCluster(dir2.Cluster);
            await fs.WriteData((byte[])(FileRecord)info[0], Superblock.DataAreaStart + (cluster-1) *
                Superblock.ClusterSize, Superblock.DataAreaStart + (cluster - 1) * Superblock.ClusterSize + Superblock.ClusterSize);
            //await fs.ResizeDirectories(dir2, +FileModel.SizeInBytes);
            //await fs.ResizeParentDirectory(dir1, dir1.Size - FileModel.SizeInBytes);
        }
        public void Info()
        {
            Console.WriteLine("move [имя/путь файла] [путь каталога]\t - переместить файл в указаный путь");
        }
    }
}
