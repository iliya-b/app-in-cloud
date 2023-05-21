using AppInCloud.Jobs;

namespace AppInCloud.Services;

public class CuttlefishLaunchOptions {
    public static int BaseNumber;
    public int? InstanceBaseNumber = 1;
    public int? InstancesNumber = 1; // (InstanceBaseNumber, InstancesNumber) and InstanceNumbers are mutually excluding
    public IEnumerable<int>? InstanceNumbers  = null;
    public IEnumerable<int> Memory  = new int[]{1024, 1024};
    public static int getBaseNumber(AppInCloud.Models.Device.Targets target){
        return target switch
        {
            Models.Device.Targets._13_x86_64 => 90,
            Models.Device.Targets._12_x86_64 => 1,
            _ => throw new Exception()
        };
    }
}
//-report_anonymous_usage_stats=n

[Hangfire.Queue("cuttlefish")]
[ErrorOn(type: typeof(CommandResult.Error))]
public class CuttlefishService 
{
    private readonly string _path;
    private readonly ICommandRunner _commandRunner;
    public CuttlefishService(ICommandRunner commandRunner, string path = "") =>
                (_commandRunner, _path) = (commandRunner, path);


    private async Task<CommandResult> run(string script, IEnumerable<string>? args=null){
        string basePath = _path; //_config["Emulator:BasePath"];
        string command = basePath + "/bin/" + script;
        if(args is null){
            args = new string[]{};
        }
        return _commandRunner.run(command, args, new Dictionary<string, string>() {
            {"HOME", basePath}
        } );
    }


    [Hangfire.AutomaticRetry(Attempts = 0, OnAttemptsExceeded = Hangfire.AttemptsExceededAction.Delete)]
    // we suppose cuttlefish hasn't been ran if stop gets failed. So, delete if it fails
    public async Task<CommandResult> Stop(){
        return await run("stop_cvd");
    }

    /** Restarts and waits for completion */
    public async Task<CommandResult> Restart(int instanceNumber){
        return await run("restart_cvd", new [] {"--instance_num", instanceNumber.ToString()});
    }

    /** Resets, restarts and waits completion */
    public async Task<CommandResult> Powerwash(int instanceNumber){
        return await run("powerwash_cvd", new []{"--instance_num", instanceNumber.ToString()});
    }


    // public async Task<object> Start(int N){
    //     return await run("powerwash_cvd", new []{"--instance_num", N.ToString()});
    // }

    public async Task<CommandResult> Launch(CuttlefishLaunchOptions options){
        if (
            options.Memory.Count() > 1 && options.InstancesNumber is not null && options.Memory.Count() != options.InstancesNumber 
            ||
            options.Memory.Count() > 1 && options.InstanceNumbers is not null && options.Memory.Count() != options.InstanceNumbers.Count()){
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
                "-report_anonymous_usage_stats=Y",
                "--daemon", 
                "-memory_mb",  string.Join(',', options.Memory),
            }.Concat((options.InstancesNumber, options.InstanceNumbers) switch {
                (null, null) or (not null, not null) or (0, _) => throw new Exception(),
                (null, var instanceNumbers) =>  new string[] {"--instance_nums", string.Join(',', instanceNumbers.Select(n => n.ToString()))},
                (var instancesNumber , null) =>  new string[] {"--num_instances", ""+instancesNumber, "--base_instance_num", options.InstanceBaseNumber.ToString()},
            })
        );
    }
}


[Hangfire.Queue("cuttlefish")]
[ErrorOn(type: typeof(CommandResult.Error))]
public class VirtualDeviceService
{
    private readonly Func<int, string> _basePath;
    private readonly ICommandRunner _commandRunner;
    public VirtualDeviceService(ICommandRunner commandRunner, Func<int, string> pathResolver) =>
                (_commandRunner, _basePath) = (commandRunner, pathResolver);


    public Task Powerwash(int instanceNumber) {
        return new CuttlefishService(_commandRunner, _basePath(instanceNumber)).Powerwash(instanceNumber); 
    }

    [Hangfire.AutomaticRetry(Attempts = 0, OnAttemptsExceeded = Hangfire.AttemptsExceededAction.Delete)]
    public Task Stop(int instanceNumber) {
        return new CuttlefishService(_commandRunner, _basePath (instanceNumber)).Stop(); 
        // path is like ~/cuttlefish12/cf12
    }
    public Task Launch(int instanceNumber, CuttlefishLaunchOptions options) {
        return new CuttlefishService(_commandRunner, _basePath (instanceNumber)).Launch(options); 
    }
}
