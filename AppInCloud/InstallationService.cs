
using System.Security.Cryptography;
using AppInCloud.Models;

namespace AppInCloud;

public class InvalidFileException : Exception {}

public class InstallationService {

    private ADB _adb;
    public InstallationService( ADB adb)
    {
        
        _adb = adb;
    }

    public async Task start(int N){
        return;
    }

    public async Task<PackageInfo> install(string filePath, string deviceSerial){

        _adb.Serial = deviceSerial;
        await _adb.install(filePath);    

        // now determine package name 
        // todo: use direct method involving reading AndroidManifest.xml
        var installerHash = "";
        using (var sha256 = SHA256.Create())
        {
            using (var stream = File.OpenRead(filePath))
            {
                installerHash = BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            }
        }
        PackageInfo[] packages = await _adb.getPackages();
        var package = packages.First(p => p.InstallerHashSum == installerHash);
        return package;
    }


public class InstallJob{

    InstallationService _installationService;
    Data.ApplicationDbContext _db;
    public InstallJob(InstallationService installationService, Data.ApplicationDbContext db){
        _installationService = installationService;
        _db = db;
    }

    public void Run (string filePath, int id, string serial){
        var task = _installationService.install(filePath, serial);
        task.Wait();
        var package = task.Result;
        Models.MobileApp m = _db.MobileApps.Where(f=>f.Id == id).First();
        m.PackageName = package.Name;
        m.Status = AppStatuses.Ready;
        m.Type = package.Type == "aab" ? AppTypes.AAB : AppTypes.APK;
        _db.MobileApps.Update(m);
        _db.SaveChanges();        
    }
}

}