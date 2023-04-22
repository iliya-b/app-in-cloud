using AppInCloud.Jobs;

namespace AppInCloud.Services;

public class CuttlefishLaunchOptions {
    public static int BaseNumber = 90;
    public int InstancesNumber {get; set;} = 2;
    public IEnumerable<int> Memory {get; set;} = new int[]{1024, 1024};
}


[Hangfire.Queue("cuttlefish")]
public class CuttlefishService
{
    private readonly IConfiguration _config;
    private readonly ICommandRunner _commandRunner;
    public CuttlefishService(ICommandRunner commandRunner, IConfiguration config) =>
                (_commandRunner, _config) = (commandRunner, config);


    private async Task<CommandResult> run(string script, IEnumerable<string>? args=null){
        string basePath = _config["Emulator:BasePath"];
        string command = basePath + "/bin/" + script;
        if(args is null){
            args = new string[]{};
        }
        return _commandRunner.run(command, args, new Dictionary<string, string>() {
            {"HOME", basePath}
        } );
    }



    [ErrorOn(type: typeof(CommandResult.Error))]
    [Hangfire.AutomaticRetry(Attempts = 0, OnAttemptsExceeded = Hangfire.AttemptsExceededAction.Delete)]
    // we suppose cuttlefish hasn't been ran if stop gets failed. So, delete if it fails
    public async Task<object> Stop(){
        return await run("stop_cvd");
    }

    /** Restarts and waits for completion */
    public async Task<object> Restart(int N){
        return await run("restart_cvd", new [] {"--instance_num", N.ToString()});
    }

    /** Resets, restarts and waits completion */
    public async Task<object> Powerwash(int N){
        return await run("powerwash_cvd", new []{"--instance_num", N.ToString()});
    }

    [ErrorOn(type: typeof(CommandResult.Error))]
    public async Task<object> Launch(CuttlefishLaunchOptions options){
        if (options.Memory.Count() > 1 && options.Memory.Count() != options.InstancesNumber){
            /**
                case memory = [] -> use system defaults
                case memory = [1024] -> use 1024 mb RAM for all devices
                case memory = [1024, 2048, 512] -> use 1024 mb for #1 device, 2048 mb for #2, etc.
            */
            throw new Exception("Provide memory parameter for each device or set general one");
        }
        return await run(
            "launch_cvd", 
            new string[]{
                "--daemon", 
                "--num_instances", options.InstancesNumber.ToString(),
                "--base_instance_num", CuttlefishLaunchOptions.BaseNumber.ToString(), 
                "-memory_mb",  string.Join(',', options.Memory)
            }
        );
    }
}
