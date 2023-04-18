
using System.Security.Cryptography;
using AppInCloud.Models;
using Hangfire;

namespace AppInCloud;

public class InvalidFileException : Exception {}

public class InstallationService {

    private ADB _adb;
    public InstallationService( ADB adb)
    {
        
        _adb = adb;
    }

    public async Task<string> CopyInstaller(IFormFile file){
        if (file.Length == 0) throw new InvalidFileException() ;
        Models.AppTypes type;
        if (file.FileName.EndsWith(".aab")){
            type = Models.AppTypes.AAB;
        }else if (file.FileName.EndsWith(".apk")){
            type = Models.AppTypes.APK;
        }else {
            throw new InvalidFileException();
        }
        var filePath = Path.GetTempFileName() + "." + type.ToString();
        using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }
        return filePath;
    }
}