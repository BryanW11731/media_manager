using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace media_manager
{
    internal abstract class Command
    {
        static Command[] sCommandList;
        static Command()
        {
            sCommandList = new Command[0];
            sCommandList = sCommandList.Append(new Command_import()).ToArray();
        }
        internal static void DisplayCommandList()
        {
            Console.WriteLine("media_manager {command} [args]");
            Console.WriteLine();
            Console.WriteLine("Available commands are");
            for (int i = 0; i < sCommandList.Length; i++)
            {
                Console.WriteLine("  -{0} : {1}", sCommandList[i].Name, sCommandList[i].Description);
            }
            Console.WriteLine();
            Console.WriteLine("To get usage information");
            Console.WriteLine("> media_manager {command} --help");
        }
        internal static Command? GetCommand(string[] args)
        {
            if (args == null)
            {
                return null;
            }
            if (args.Length < 1)
            {
                return null;
            }

            Command? result = null;
            string command = args[0];
            for (int i = 0; i < sCommandList.Length; i++)
            {
                if (command == ("-" + sCommandList[i].Name))
                {
                    result = new Command_import();
                }
            }
            return result;
        }

        internal abstract string Name { get; }
        internal abstract string Description { get; }
        internal abstract bool ParseArguments(string[] args);
        internal abstract void DisplayUsage();
        internal abstract void Run();
    }
    internal class Command_import : Command
    {
        internal override string Name { get { return "import"; } }
        internal override string Description { get { return "import media files from a specified folder"; } }
        internal override void DisplayUsage()
        {
            Console.WriteLine("Command_import.DisplayUsage: TBD");
        }
        internal override bool ParseArguments(string[] args)
        {
            throw new NotImplementedException();
        }
        internal override void Run()
        {
            throw new NotImplementedException();
        }
    }
}
