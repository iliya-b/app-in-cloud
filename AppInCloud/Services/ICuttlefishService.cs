namespace AppInCloud.Services;


interface ICuttlefishService {
    public Task<object> Stop();

    public Task<object> Restart(int N);

    public Task<object> Powerwash(int N);

    public Task<object> Launch(CuttlefishLaunchOptions options);
}