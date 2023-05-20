using System.Diagnostics;
using Renci.SshNet;

namespace AppInCloud.Services;





public class LocalCommandRunner: ICommandRunner {

    IConfiguration _config;
    public LocalCommandRunner(IConfiguration config) => _config = config;
    public CommandResult run(string program, IEnumerable<string>? arguments, IDictionary<string, string>? env, int timeout=System.Threading.Timeout.Infinite)
    {
            Console.WriteLine("running " + program + " with args: " + string.Join(' ',  arguments) );
            var cmd = new Process();
            cmd.StartInfo.FileName = program;
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            
            if(env is not null){
                foreach(var entry in env) cmd.StartInfo.Environment.Add(entry.Key, entry.Value);
            }
            if(arguments is not null){
                foreach(var arg in arguments) cmd.StartInfo.ArgumentList.Add(arg);
            }
            var cmdExited = new CmdExitedTaskWrapper();
            cmd.EnableRaisingEvents = true;
            cmd.Exited += cmdExited.EventHandler;
            cmd.Start();
            var isComplete = cmdExited.Task.Wait(timeout);

            if(!isComplete) {
                return new CommandResult.Error(124, new string[]{});
            }
            if(cmd.ExitCode != 0) {
                return new CommandResult.Error(cmd.ExitCode, new []{cmd.StandardOutput.ReadLine()!});
            }
            List<string> result = new List<string> ();
            while (true){
                string? s = cmd.StandardOutput.ReadLine();
                if(s == null) break;
                result.Add(s);
            }
            return new CommandResult.Success(result);

    }



    private class CmdExitedTaskWrapper
    {
        private TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();
        public void EventHandler(object sender, EventArgs e) => _tcs.SetResult(true);
        public Task Task => _tcs.Task;
    }
}