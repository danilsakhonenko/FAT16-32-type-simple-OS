using Chaos.SystemComponents.Commands;

namespace Chaos.SystemComponents
{
    internal class CommandManager
    {
        private readonly IEnumerable<ICommand> commandlist;
        public CommandManager(IEnumerable<ICommand> cmdlist)
        {
            commandlist = cmdlist;
        }
        public async Task Handle(string? command)
        {
            if (command == null) return;
            if (command == "help")
            {
                commandlist.ToList().ForEach(a => a.Info()); return;
            }
            var process = commandlist.FirstOrDefault(a => a.isExecutable(command));
            if (process != null)
            {
                try
                {
                    await process.Run(command);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else Console.WriteLine($"{command}: неизвестная команда");
        }
    }
}
