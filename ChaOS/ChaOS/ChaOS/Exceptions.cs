using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chaos.Tools.Exceptions
{
    internal class NoFileException :Exception
    {
        public override string Message => "Нет соответствующих объектов.";
    }
    internal class UserLengthException : Exception
    {
        public override string Message => "Длина имени пользователя и пароля не должна привышать 10 символов";
    }

    internal class UsernameException : Exception
    {
        public override string Message => "Ошибка! Пользователь с таким именем уже существует.";
    }

    internal class NoFreeSpaceException : Exception
    {
        public override string Message => "Не хватает свободного места для записи.";
    }
    internal class SessionLoginException : Exception
    {
        public override string Message => "Ошибка! Вы уже находитесь в сессии.";
    }
    internal class AccessException : Exception
    {
        public override string Message => "Доступ запрещен.";
    }

    internal class NoFreeClusterException : Exception
    {
        public override string Message => "Нет свободных кластеров.";
    }

    internal class FileExistException : Exception
    {
        public override string Message => "Файл с таким именем уже существует.";
    }

    internal class NotInSessionException : Exception
    {
        public override string Message => "Ошибка! Вы не находитесь в сессии.";
    }

    internal class FilenameException : Exception
    {
        public override string Message => "Длина имени файла не должна привышать 11 символов";
    }

    internal class ChmodException : Exception
    {
        public override string Message => "Неверное количество разрешений chmod";
    }

    internal class ChattrException : Exception
    {
        public override string Message => "Неверное количество атрибутов chattr";
    }
    internal class ReadOnlyException : Exception
    {
        public override string Message => "Файл имеет атрибут только для чтения";
    }

}
