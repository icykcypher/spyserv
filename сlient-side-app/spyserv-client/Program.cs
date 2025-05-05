using spyserv.Core;
using spyserv.Infrastructure;

namespace spyserv
{
    /// <summary>
    /// Class representing assembly application in CLI
    /// </summary>
    public class Program
    {
        static void Main(string[] args)
        {
            CliProviderService.ConfigureCommands(args);
        }
    }
}