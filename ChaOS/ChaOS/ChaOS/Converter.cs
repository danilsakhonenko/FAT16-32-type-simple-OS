using System.Collections;
using System.Text;
using Chaos.Structures;

namespace Chaos.Tools
{
    internal static class Converter
    {
        public static byte[] ToBytes(this string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        public static byte[] ToBytes(this int value)
        {
            return BitConverter.GetBytes(value);
        }
        public static byte[] ToBytes(this short value)
        {
            return BitConverter.GetBytes(value);
        }
        public static byte[] ToBytes(this long value)
        {
            return BitConverter.GetBytes(value);
        }
        public static byte ToByte(this BitArray bits)
        {
            byte[] arr = new byte[1];
            for (int i = 0; i < 4; i++)
            {
                bool bit = bits[i];
                bits[i] = bits[8 - i - 1];
                bits[8 - i - 1] = bit;
            }
            bits.CopyTo(arr, 0);
            return arr[0];
        }
        public static byte ToByte(this string str)
        {
            BitArray bits = new BitArray(8);
            for (int i =0; i < str.Length; i++)
            {
                bits[7-i] = str[i] == '1';
            }
            byte[] bt= new byte[1];
            bits.CopyTo(bt, 0);
            return bt[0];
        }
        public static string ToStr(this byte[] arr)
        {
            return Encoding.UTF8.GetString(arr);
        }
        public static string Clear(this string str)
        {
            return str.Replace("\0", "");
        }
        public static int ToInt(this byte[] arr)
        {
            return BitConverter.ToInt32(arr);
        }
        public static short ToShort(this byte[] arr)
        {
            return BitConverter.ToInt16(arr);
        }
        public static long ToLong(this byte[] arr)
        {
            return BitConverter.ToInt64(arr);
        }
        public static long ToLong(this DateTime dateTime)
        {
            var date = dateTime.ToShortDateString().Replace(".", "");
            if (date.Length == 7)
                date = date.Insert(0, "0");
            var time = dateTime.ToShortTimeString().Replace(".",
           "").Replace(":", "");
            if (time.Length == 3)
                time = time.Insert(0, "0");
            return long.Parse(date + time);
        }
        public static List<User> AsUserList(this byte[] data)
        {
            var list = new List<User>();
            for (int i = 0; i < data.Length; i += User.SizeInBytes)
            {
                var id = new byte[2];
                var name = new byte[10];
                var password = new byte[10];
                Array.Copy(data, i, id, 0, 2);
                Array.Copy(data, i + 2, name, 0, 10);
                Array.Copy(data, i + 12, password, 0, 10);
                var user = new User
                {
                    Id = id.ToShort(),
                    Name = name.ToStr().Clear(),
                    Password = password.ToStr().Clear()
                };
                if (user.Id != 0)
                    list.Add(user);
            }
            return list;
        }

        public static int GetLastCharIndex(this string str, char c)
        {
            var lastIndex = -1;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == c)
                    lastIndex = i;
            }
            return lastIndex;
        }
        public static Structures.Path GetPath(this string str)
        {
            var index1 = str.GetLastCharIndex('\\');
            var directoryStr = index1 != -1 ? str.Substring(0, index1) :
           null;
            var Filename = (index1 != -1 ? str.Remove(0, index1) : str).Trim('\\');
            return new Structures.Path
            {
                DirectoryPath = directoryStr,
                Filename = Filename
            };
        }

        public static BitArray AsAttributesArray(this byte val)
        {
            var arr = new BitArray(new byte[] { val });
            for(int i = 0; i < 4; i++) 
            {
                bool bit = arr[i];
                arr[i] = arr[8 - i - 1];
                arr[8 - i - 1] = bit;
            }
            return arr;
        }
        public static List<FileRecord> AsFileList(this byte[] data)
        {
            var list = new List<FileRecord>();
            for (int i = 0; i < data.Length; i += FileRecord.RecordSize)
            {
                if (data.Length - i < FileRecord.RecordSize)
                    return list;
                var name = new byte[11];
                var attr = new byte[1];
                var rights = new byte[1];
                var dateTime = new byte[8];
                var cluster = new byte[2];
                var size = new byte[4];
                var userId = new byte[2];
                Array.Copy(data, i, name, 0, 11);
                Array.Copy(data, i + 11, attr, 0, 1);
                Array.Copy(data, i + 15, rights, 0, 1);
                Array.Copy(data, i + 16, dateTime, 0, 8);
                Array.Copy(data, i + 24, cluster, 0, 2);
                Array.Copy(data, i + 26, size, 0, 4);
                Array.Copy(data, i + 30, userId, 0, 2);
                var file = new FileRecord
                {
                    Name = name.ToStr().Clear(),
                    Attributes = attr[0],
                    Rights = rights[0],
                    DateTime = dateTime.ToLong(),
                    Cluster = cluster.ToShort(),
                    Size = size.ToInt(),
                    UserId = userId.ToShort()
                };
                if (file.Name.Length != 0)
                    list.Add(file);
            }
            return list;
        }
       
    }
}
