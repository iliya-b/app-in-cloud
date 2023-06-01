using AppInCloud.Services;

namespace AppInCloud.Models;

public class Device
{
    public string Id { get; set; }
    public int Memory { get; set; }
    public bool IsActive { get; set; }
    public Statuses Status { get; set; }
    public Targets Target {get; set; }
    public List<ApplicationUser> Users  { get; set; } = new List<ApplicationUser>();

    public DateTime? StartedAt {get; set; }
    public int getCuttlefishNumber()
    {
        if(!Id.StartsWith("cvd-")) throw new InvalidOperationException("Not a cuttlefish device");
        return int.Parse(Id.Replace("cvd-", ""));
    }
    /**
        Get "serial number" to use with adb -s <serial>.
        Actually it's an address localhost:port, where port is determined by cuttlefish, see docs.
    */
    public string getSerialNumber() {
        int port = 6519 + getCuttlefishNumber();
        return "0.0.0.0:" + port; // todo configure adb to use localhost
    }
    
    public static CuttlefishLaunchOptions? GetLaunchOptions (IEnumerable<Device> devices) {
        var supportedTarget = Targets._13_x86_64;
        if(devices.Count() == 1 && devices.First().Target == Targets._12_x86_64) return GetLaunchOptions(devices.First());
        if(devices.Count() > 1 && devices.Any(device => device.Target != supportedTarget)) return null;
        if(devices.Any(device => !device.IsActive)) return null;
        // launch options for multiple devices with same target (android 13)
        var nums = devices.Select(d => d.getCuttlefishNumber());
        var rams = devices.Select(d => d.Memory);
        return new CuttlefishLaunchOptions {
            InstancesNumber = null,
            InstanceNumbers = nums, 
            Memory = rams,
            InstanceBaseNumber = null
        };
    }

    public bool IsRanOutLimit(ApplicationUser user){
        return StartedAt + user.DailyLimit < DateTime.Now;
    }
    public static CuttlefishLaunchOptions? GetLaunchOptions (Device device) {

        return device.Target switch {
            Targets._13_x86_64 => GetLaunchOptions(new Device[] { device }),
            Targets._12_x86_64 => new CuttlefishLaunchOptions {
                InstancesNumber = 1, 
                Memory = new int[] {device.Memory},
                InstanceBaseNumber = device.getCuttlefishNumber()
            },
            _ => null
        };
    }

    public enum Statuses {
        ENABLE,
        DISABLE
        
    }
    public enum Targets {
        _13_x86_64, 
        _12_x86_64
    }

    public static Targets ParseTarget(string target){
        return target switch {
            "_13_x86_64" => Targets._13_x86_64,
            "_12_x86_64" => Targets._12_x86_64,
            _ => throw new Exception()
        };
    }

}

