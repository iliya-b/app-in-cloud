using System.Diagnostics;

namespace AppInCloud;


public class PackageInfo {
    public string Name {get; set; }
    public string InstallerHashSum {get; set; }
}

public class ADB
{
        private readonly IConfiguration _config;

        public string Serial {get; set;}

        public ADB(IConfiguration config) =>
                _config = config;


        private async Task<string[]> run(string command){
            string path =  _config["Emulator:BasePath"] + "/" + _config["Emulator:AdbPath"];
            var cmd = new Process();
            cmd.StartInfo.FileName = path;
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.Arguments =  command;
            if(Serial.Length > 0) {
                cmd.StartInfo.Arguments = "-s " + Serial + " " + cmd.StartInfo.Arguments;
            }
            var cmdExited = new CmdExitedTaskWrapper();
            cmd.EnableRaisingEvents = true;
            cmd.Exited += cmdExited.EventHandler;
            cmd.Start();
            await cmdExited.Task;
            if(cmd.ExitCode != 0) throw new Exception();
            List<string> result = new List<string> ();
            while (true){
                string? s = cmd.StandardOutput.ReadLine();
                if(s == null) break;
                result.Add(s);
            }
            return result.ToArray();
        }
        public async Task install(string apkPath){
            await run("install " + apkPath);
        }

        public async Task<PackageInfo[]> getPackages(){
            string[] result  = await run("shell pm list packages -f -3 ");
            return result.Select(async line => {
                if(!line.StartsWith("package:")) throw new Exception("no package in output");
                var delimiter = line.LastIndexOf('=');
                var package = line[(delimiter + 1)..];
                var filename = line[8..delimiter];
                string[] sha256sum = await run("shell sha256sum " + filename) ;
                if(sha256sum.Length == 0) throw new Exception("sha256sum error");
                string hash = sha256sum[0].Split(' ', 2) [0];
                return new PackageInfo {Name=package, InstallerHashSum=hash};
            }).Select(t => t.Result).ToArray();
        }

        
        public async Task start(string package){
            await run("shell monkey -p  " + package + " 1");
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
