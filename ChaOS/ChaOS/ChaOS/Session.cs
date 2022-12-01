using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chaos.Structures;
using Chaos.Tools;
using Chaos.Tools.Exceptions;

namespace Chaos.SystemComponents
{
    internal class Session
    {
        public User User { get; set; }
        public bool IsRoot { get; set; } = false;
        public string CurrentPath { get; set; } = "";
        public const string RootDirectory = "root\\";
        public override string ToString()
        {
            return "[" + User?.Name + "]" + CurrentPath + ">";
        }
        public void RequestForAccess()
        {
            if (User?.Name != "root")
            {
                throw new Exception("Ошибка права доступа. Вы не являетесь администратором"); //!!!!!!!
            }
            Console.WriteLine("Введите пароль: ");
            Console.Write(this);
            if (User.Password != Console.ReadLine())
            {
                throw new Exception("Ошибка доступа. Пароль был указан неверно."); //!!!!!!!!
            }
        }
        public void isWritable(FileRecord file)
        {
            var rights = file.Rights.AsAttributesArray();
            if (IsRoot)
                return;
            if (User == null)
                throw new AccessException();
            if (User.Id == file.UserId)
            {
                if (!rights[1])
                    throw new AccessException();
                return;
            }
            if (!rights[4])
            {
                throw new AccessException();
            }
        }
        public void isReadable(FileRecord file)
        {
            var rights = file.Rights.AsAttributesArray();
            if (IsRoot)
                return;
            if (User == null)
                throw new AccessException();
            if (User?.Id == file.UserId)
            {
                if (!rights[0])
                    throw new AccessException();
                return;
            }
            if (!rights[3])
            {
                throw new AccessException();
            }
        }
    }
}
