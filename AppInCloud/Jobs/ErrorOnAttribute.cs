namespace AppInCloud.Jobs;
using Hangfire.Common;
using Hangfire.Logging;
using Hangfire.Server;

public class ErrorOnAttribute : JobFilterAttribute, IServerFilter
{
    private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

    private readonly Type _type;
    public ErrorOnAttribute(Type type) => _type = type;
    public void OnPerforming(PerformingContext context)
    {
        Logger.InfoFormat("Starting to perform job `{0}`", context.BackgroundJob.Id);
    }
    
    public void OnPerformed(PerformedContext context)
    {
        if(context.Result is not null && _type is not null && context.Result.GetType() == _type){
            throw new Exception(context.Result.ToString());
        }
        Logger.InfoFormat("Job `{0}` has been performed", context.BackgroundJob.Id);
    }
}