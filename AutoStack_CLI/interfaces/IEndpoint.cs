namespace AutoStack_CLI.interfaces;

public interface IEndpoint
{}

public interface IEndpoint<TResult>
{
    Task<TResult?> ExecuteAsync();
}

public interface IEndpoint<in TParameters, TResult>
{
    Task<TResult?> ExecuteAsync(TParameters parameters);
}