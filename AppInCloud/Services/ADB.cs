namespace AppInCloud.Services;


public class PackageInfo {
    public string Name {get; set; }
    public string Type {get;set;}
}

public abstract record BridgeResponse {
    private BridgeResponse(){ }
    public record Success(string? output = null) : BridgeResponse {}
    public record Error(string? output = null) : BridgeResponse {};
}

public class ADB
{
        private readonly IConfiguration _config;
        private readonly ICommandRunner _commandRunner;
        public ADB(IConfiguration config, ICommandRunner commandRunner) => (_commandRunner, _config) = (commandRunner, config);

        private async Task<CommandResult> run(string deviceSerial, string command,  IEnumerable<string>? args=null){
            string path =  _config["Emulator:BasePath"] + "/" + _config["Emulator:AdbPath"];
            if(args is null){
                args = new List<string>{};
            }
            args = args.Prepend(command).Prepend(deviceSerial).Prepend("-s");
            
            return _commandRunner.run(path, args);
        }
        public async Task<BridgeResponse> Uninstall(string deviceSerial, string package){
            return await run(deviceSerial,  "uninstall" , new []{package}) switch {
                CommandResult.Success => new BridgeResponse.Success(),
                CommandResult.Error(int code, var output) => new BridgeResponse.Error(output is null ? null : output.First()),
                _ => new BridgeResponse.Error()
            };
        }
        
        public async Task<BridgeResponse> Install(string deviceSerial, string apkPath){
            return await run(deviceSerial, "install" , new[] {apkPath}) switch {
                CommandResult.Success => new BridgeResponse.Success(),
                CommandResult.Error(int code, var output) => new BridgeResponse.Error(output is null ? null : output.First()),
                _ => new BridgeResponse.Error()
            };
        }

        public async Task Reboot(string deviceSerial){
            await run(deviceSerial, "reboot");
        }

        public async Task<bool> HealthCheck(string deviceSerial){
            var result = await run(deviceSerial, "shell",  new[]{"echo", "test"});
            return result switch {
                CommandResult.Success(var output) => output.First() == "test",
                CommandResult.Error(int code, var output) => false,
                _ => false
            };
        }
        public async Task<PackageInfo[]?> getPackages(string deviceSerial){
            var result  = await run(deviceSerial, "shell", new[]{"pm", "list", "packages" , "-f", "-3"});
            return result switch {
                CommandResult.Success(var output) => 
                    output.Select(line => {
                        if(!line.StartsWith("package:")) throw new Exception("no package in output");
                        var delimiter = line.LastIndexOf('=');
                        var package = line[(delimiter + 1)..];
                        var filename = line[8..delimiter];
                        var type = filename.Split('.').Last().ToLowerInvariant(); // apk or aab
                        return new PackageInfo { Name=package, Type=type };
                    }).ToArray(),
                _ => null
            };
        }

        public async Task<BridgeResponse> Start(string deviceSerial, string package){
            return await run(deviceSerial, "shell", new []{"monkey", "-p", package, "1"}) switch {
                CommandResult.Success(var output) => new BridgeResponse.Success(),
                _ => new BridgeResponse.Error(),
            };
        }

        public void RebootAndWait(IEnumerable<string> serials)
        {
            RebootAndWait(serials, 90000);
        }
        public void RebootAndWait(IEnumerable<string> serials, int timeout)
        {

            var activeDevices = serials.Where(s => HealthCheck(s).Result);

            var rebootTasks = activeDevices.Select(Reboot);
            Task.WhenAll(rebootTasks).Wait();

            var allUp = SpinWait.SpinUntil(() => {
                var checkTasks = activeDevices.Select(HealthCheck);
                Task.WhenAll(checkTasks).Wait();
                return checkTasks.All(t => t.Result);
            }, timeout);
            // todo refactor with no exception
            if(!allUp) throw new Exception("devices not up");
        }
}
