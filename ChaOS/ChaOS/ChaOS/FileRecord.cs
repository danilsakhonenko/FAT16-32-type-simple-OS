using Chaos.Tools;

namespace Chaos.Structures
{
    internal class FileRecord
    {
        public static int RecordSize => 32;
        public string Name { get; set; }
        public byte Attributes { get; set; }
        public byte Rights { get; set; }
        public long DateTime { get; set; }
        public short Cluster { get; set; }
        public int Size { get; set; }
        public short UserId { get; set; }

        public static explicit operator byte[](FileRecord file)
        {
            var arr = new byte[RecordSize];
            file.Name.ToBytes().CopyTo(arr, 0);
            arr[11] = file.Attributes;
            arr[15] = file.Rights;
            file.DateTime.ToBytes().CopyTo(arr, 16);
            file.Cluster.ToBytes().CopyTo(arr, 24);
            file.Size.ToBytes().CopyTo(arr, 26);
            file.UserId.ToBytes().CopyTo(arr, 30);
            return arr;
        }

        public string ToString(List<User> userList)
        {
            if (Cluster == 1 && Name == "root")
                return "";

            var str = string.Empty;
            str += Name + "\t";
            str += (userList.FirstOrDefault(x => x.Id ==UserId)?.Name) + "\t";
            var bits = Attributes.AsAttributesArray();
            for(int i = 0; i < 3; i++)
            {
                if (bits[i])
                    str += '1';
                else
                    str += '0';
            }
            str += "\t";
            bits = Rights.AsAttributesArray();
            for (int i = 0; i < 6; i++)
            {
                if (bits[i])
                    str += '1';
                else
                    str += '0';
            }
            str += "\t";
            var date = DateTime.ToString();
            var dateArr = date.Insert(date.Length - 4, ":").Split(":");
            dateArr[0] = dateArr[0].Insert(dateArr[0].Length - 4, ".");
            dateArr[0] = dateArr[0].Insert(dateArr[0].Length - 7, ".");
            dateArr[1] = dateArr[1].Insert(2, ":");
            str += dateArr[0] + " - ";
            str += dateArr[1] + "\t";
            str += Size;
            return str;
        }

    }
    internal class Path
    {
        public string DirectoryPath { get; set; }
        public string Filename { get; set; }
    }
}
