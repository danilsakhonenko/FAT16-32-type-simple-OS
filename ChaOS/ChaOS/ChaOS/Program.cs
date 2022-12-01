using Chaos.SystemComponents;
using Chaos.SystemComponents.Commands;
using Chaos.Tools;

var session = new Session();
var filesys = new FileSystem(new StreamOperations(), session);

List<ICommand> commands = new List<ICommand>()
{
    new DiskFormatCommand(filesys,session),
    new AddUserCommand(filesys,session),
    new LogInCommand(filesys,session),
    new LogOutCommand(filesys,session),
    new OpenDirCommand(filesys,session),
    new CreateDirCommand(filesys,session),
    new RenameCommand(filesys,session),
    new ChangeRightsCommand(filesys,session),
    new ChangeAttributesCommand(filesys,session),
    new DirListCommand(filesys,session),
    new UserListCommand(filesys),
    new DeleteUserCommand(filesys,session),
    new CreateFileCommand(filesys,session),
    new FileReadCommand(filesys,session),
    new FileWriteCommand(filesys,session),
    new RemoveFileCommand(filesys,session),
    new RemoveDirCommand(filesys,session),
    new MoveCommand(filesys,session)

};
var handler = new CommandManager(commands);

while (true)
{
    Console.Write(session.ToString());
    await handler.Handle(Console.ReadLine());
}
