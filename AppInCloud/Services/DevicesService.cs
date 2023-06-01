
using AppInCloud.Models;
using Microsoft.EntityFrameworkCore;

namespace AppInCloud.Services;


class DevicesService {
    private readonly Data.ApplicationDbContext _db;
    private readonly ADB _adb;
    private readonly VirtualDeviceService _cuttlefishService;
    private readonly ILogger<DevicesService> _logger;

    public DevicesService(ILogger<DevicesService> logger, Data.ApplicationDbContext db, ADB adb, VirtualDeviceService cuttlefishService) => (_db, _adb, _cuttlefishService, _logger) = (db, adb, cuttlefishService, logger);


    public async void Check(){
        foreach(var user in _db.Users.Include(u=>u.Devices).ToList()){
            foreach(var device in user.Devices){
                bool time_limit = device.StartedAt + user.DailyLimit < DateTime.Now;
                bool is_ran = device.Status == Device.Statuses.ENABLE && await _adb.HealthCheck(device.getSerialNumber());
                if(time_limit && is_ran){
                    await _cuttlefishService.Stop(device.getCuttlefishNumber());
                    _logger.LogWarning("stopped device");
                }
            }
        }
    }
}