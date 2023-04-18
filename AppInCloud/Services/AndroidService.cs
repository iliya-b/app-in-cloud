using System.IO.Compression;
using System.Xml.Linq;
using AndroidXml;

namespace AppInCloud.Services;

public class AndroidService {

    public AndroidService (){

    }


    public string getInstallerPackageName(string filePath){
        string? manifestPath = null;
        string? package = null;
        using (ZipArchive zip = ZipFile.OpenRead(filePath))
        {
            foreach (ZipArchiveEntry entry in zip.Entries)
            {
                if(entry.FullName == "AndroidManifest.xml"){
                    manifestPath = Path.GetTempFileName();
                    entry.ExtractToFile(manifestPath, true);
                    break;
                }
            }
        }
        if(manifestPath is null) throw new Exception("Cannot find AndroidManifest.xml");

        using(var stream = File.OpenRead(manifestPath)){
            var reader = new AndroidXmlReader(stream);
            XDocument doc = XDocument.Load(reader);
            package = doc.Root!.Attribute("package")!.Value;
        }
        File.Delete(manifestPath);
        return package;

    }
}