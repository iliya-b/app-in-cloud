namespace AppInCloud.Models;



public class Device
{
    public string Id { get; set; }
    public List<ApplicationUser> Users  { get; set; }


    /**
        Get "serial number" to use with adb -s <serial>.
        Actually it's an address localhost:port, where port is determined by cuttlefish, see docs.
    */
    public string getSerialNumber() {
        int N = int.Parse(Id.Replace("cvd-", ""));
        int port = 6519 + N;
        return "0.0.0.0:" + port; // todo configure adb to use localhost
    }

}

