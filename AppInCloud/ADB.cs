using System.Diagnostics;

namespace AppInCloud;

public class ADB
{
        private readonly IConfiguration _config;

        public ADB(IConfiguration config) =>
                _config = config;


        private async Task run(string command){
            string path =  _config["Emulator:BasePath"] + "/" + _config["Emulator:AdbPath"];
            var cmd = new Process();
            cmd.StartInfo.FileName = path;
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.Arguments = command;
            var cmdExited = new CmdExitedTaskWrapper();
            cmd.EnableRaisingEvents = true;
            cmd.Exited += cmdExited.EventHandler;
            cmd.Start();
            await cmdExited.Task;
        }
        public async Task install(string apkPath){
            await run("install " + apkPath);
        }


    private class CmdExitedTaskWrapper
    {

        private TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();

        public void EventHandler(object sender, EventArgs e)
        {
            _tcs.SetResult(true);
        }

        public Task Task => _tcs.Task;

    }
}
