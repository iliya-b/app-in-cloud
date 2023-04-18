using System.Diagnostics;

namespace AppInCloud;


public class PackageInfo {
    public string Name {get; set; }
    public string InstallerHashSum {get; set; }

    public string Type {get;set;}
}

public class ADB
{
        private readonly IConfiguration _config;

        public string Serial {get; set;}

        public ADB(IConfiguration config) =>
                _config = config;



        private async Task<string[]> run(string command){
            return await run(Serial, command);
        }
        private async Task<string[]> run(string deviceSerial, string command){
            string path =  _config["Emulator:BasePath"] + "/" + _config["Emulator:AdbPath"];
            var cmd = new Process();
            cmd.StartInfo.FileName = path;
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.Arguments =  command;
            if(deviceSerial.Length > 0) {
                cmd.StartInfo.Arguments = "-s " + deviceSerial + " " + cmd.StartInfo.Arguments;
            }
            var cmdExited = new CmdExitedTaskWrapper();
            cmd.EnableRaisingEvents = true;
            cmd.Exited += cmdExited.EventHandler;
            cmd.Start();
            await cmdExited.Task;
            if(cmd.ExitCode != 0) throw new Exception(cmd.StandardOutput.ReadLine());
            List<string> result = new List<string> ();
            while (true){
                string? s = cmd.StandardOutput.ReadLine();
                if(s == null) break;
                result.Add(s);
            }
            return result.ToArray();
        }
        public async Task delete(string serial, string package){
            await run(serial, "uninstall " + package);
        }
        
        public async Task install(string apkPath){
            await run("install " + apkPath);
        }
        public async Task install(string deviceSerial, string apkPath){
            await run(deviceSerial, "install " + apkPath);
        }

        public async Task reboot(string deviceSerial){
            await run(deviceSerial, "reboot");
        }

        public async Task<bool> healthCheck(string deviceSerial){
            try{
                return "test" == (await run(deviceSerial, "echo test"))[0];
            }catch(Exception){
                return false;
            }
        }
        public async Task<PackageInfo[]> getPackages(){
            return await getPackages(Serial);
        }
        public async Task<PackageInfo[]> getPackages(string serial){
            string[] result  = await run(serial, "shell pm list packages -f -3 ");
            return result.Select(async line => {
                if(!line.StartsWith("package:")) throw new Exception("no package in output");
                var delimiter = line.LastIndexOf('=');
                var package = line[(delimiter + 1)..];
                var filename = line[8..delimiter];
                var type = filename.Split('.').Last().ToLowerInvariant(); // apk or aab
                string[] sha256sum = await run(serial, "shell sha256sum " + filename) ;
                if(sha256sum.Length == 0) throw new Exception("sha256sum error");
                string hash = sha256sum[0].Split(' ', 2) [0];
                return new PackageInfo {Name=package, InstallerHashSum=hash, Type=type};
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
