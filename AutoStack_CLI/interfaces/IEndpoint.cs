namespace AutoStack_CLI.interfaces;

public interface IEndpoint<in TParameters, TResult>
{
    Task<TResult?> ExecuteAsync(TParameters parameters);
}