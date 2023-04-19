namespace AppInCloud.Models;



public class Device
{
    public string Id { get; set; }
    public int Memory { get; set; }
    public List<ApplicationUser> Users  { get; set; } = new List<ApplicationUser>();

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

}

