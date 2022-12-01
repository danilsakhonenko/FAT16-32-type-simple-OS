using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chaos.Tools;
using Chaos.Tools.Exceptions;

namespace Chaos.Structures
{
    internal class User
    {
        public static int SizeInBytes => 22;
        private string name;
        private string pass;
        public short Id { get; set; }
        public string Name
        {
            get => name; set
            {
                if (value.Length < 10) name = value;
                else throw new UserLengthException();
            }
        }
        public string Password
        {
            get => pass; set
            {
                if (value.Length < 10) pass = value;
                else throw new UserLengthException(); 
            } 
        }
        public static explicit operator byte[](User user)
        {
            var arr = new byte[22];
            user.Id.ToBytes().CopyTo(arr, 0);
            user.Name.ToBytes().CopyTo(arr, 2);
            user.Password.ToBytes().CopyTo(arr, 12);
            return arr;
        }

        public override string ToString()
        {
            return $"{Id}\t{Name}\t";
        }

    }
}
