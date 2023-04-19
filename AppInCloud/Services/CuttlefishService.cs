using System.Diagnostics;

namespace AppInCloud.Services;



public class CuttlefishLaunchOptions {
    public static int BaseNumber = 90;
    public int InstancesNumber {get; set;} = 2;
    public IEnumerable<int> Memory {get; set;} = new int[]{1024, 1024};
}

public class CuttlefishService
{
    private readonly IConfiguration _config;
    public CuttlefishService(IConfiguration config) =>
                _config = config;

    private async Task<string[]> run(string script){
        return await run(script, "");
    }

    private async Task<string[]> run(string script, string args){
        string basePath = _config["Emulator:BasePath"];
        string path = basePath + "/bin/" + script;
        var cmd = new Process();
        cmd.StartInfo.FileName = path;
        cmd.StartInfo.RedirectStandardInput = true;
        cmd.StartInfo.RedirectStandardOutput = true;
        cmd.StartInfo.CreateNoWindow = true;
        cmd.StartInfo.UseShellExecute = false;
        cmd.StartInfo.Arguments =  args;
        cmd.StartInfo.Environment["HOME"]=basePath;

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


    public async Task<object> Stop(){
        return await run("stop_cvd");
    }

    /** Restarts and waits for completion */
    public async Task<object> Restart(int N){
        return await run("restart_cvd", "--instance_num " + N);
    }

    /** Resets, restarts and waits completion */
    public async Task<object> Powerwash(int N){
        return await run("powerwash_cvd", "--instance_num " + N);
    }

    public async Task<object> Launch(CuttlefishLaunchOptions options){
        if (options.Memory.Count() > 1 && options.Memory.Count() != options.InstancesNumber){
            /**
                case memory = [] -> use system defaults
                case memory = [1024] -> use 1024 mb RAM for all devices
                case memory = [1024, 2048, 512] -> use 1024 mb for #1 device, 2048 mb for #2, etc.
            */
            throw new Exception("Provide memory parameter for each device or set general one");
        }
        return await run("launch_cvd", 
            string.Format(
                "-daemon --num_instances {0} --base_instance_num {1} -memory_mb {2}", 
                options.InstancesNumber, CuttlefishLaunchOptions.BaseNumber, string.Join(',', options.Memory)
            )
        );
    }
    private class CmdExitedTaskWrapper
    {
        private TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();
        public void EventHandler(object sender, EventArgs e) => _tcs.SetResult(true);
        public Task Task => _tcs.Task;
    }
}
