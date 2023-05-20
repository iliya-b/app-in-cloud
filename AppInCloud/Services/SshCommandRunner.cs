using Renci.SshNet;

namespace AppInCloud.Services;





public class SshCommandRunner: ICommandRunner {

    IConfiguration _config;
    public SshCommandRunner(IConfiguration config) => _config = config;
    public CommandResult run(string program, IEnumerable<string>? arguments, IDictionary<string, string>? env, int timeout=0)
    {
        var builder  = new LinuxCommandBuilder(); // escape arguments
        builder.AppendArgument(program);
        foreach(var arg in arguments) builder.AppendArgument(arg);

        var fullCommand = builder.ToString();
        using (var client = new SshClient(_config["AppInCloud:RPC:Host"], _config["AppInCloud:RPC:User"], (_config["AppInCloud:RPC:Password"])))
        {
            client.Connect();
            var cmd = client.RunCommand(fullCommand);

            var output = cmd.Execute().Split("\n");
            var status = cmd.ExitStatus;

            Console.WriteLine(fullCommand);
            Console.Write(" ===> " + status);
            foreach(var s in output) Console.WriteLine(s);
            if(status == 0) return new CommandResult.Success(output);
            return new CommandResult.Error(status, output);
        }
    }
}