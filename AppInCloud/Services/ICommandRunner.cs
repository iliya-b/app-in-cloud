namespace AppInCloud.Services;

public abstract record CommandResult
{   
    private CommandResult(){ }
    public record Success(IEnumerable<string> output): CommandResult { }

    public record Error(int exitStatus, IEnumerable<string> output) : CommandResult;
}

public interface ICommandRunner {
    public CommandResult run(string command, IEnumerable<string>? arguments = null, IDictionary<string, string>? env = null);
}