
using Microsoft.EntityFrameworkCore;

namespace AppInCloud.Services;


class DevicesService {
    IEnumerable<Models.Device> _db;
    ADB _adb;
    public DevicesService(IEnumerable<Models.Device> db, ADB adb) => (_db, _adb) = (db, adb);

    public async void Reboot()
    {
        {
            var tasks = _db.ToList().Select(device => _adb.Reboot(device.getSerialNumber()));
            await Task.WhenAll(tasks);
        }
        // wait for devices to reboot
        {
            IEnumerable<Task<bool>> tasks;
            do{
                tasks = _db.ToList().Select(device => _adb.HealthCheck(device.getSerialNumber()));
                await Task.WhenAll(tasks);
            }while(!tasks.All(t => t.Result));
        }
    }
}