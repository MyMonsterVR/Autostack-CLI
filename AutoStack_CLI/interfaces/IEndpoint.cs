namespace AutoStack_CLI.interfaces;

public interface IEndpoint<T, T2>
{
    Task<T2?> ExecuteAsync(T parameters);
}