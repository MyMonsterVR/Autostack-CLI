namespace AutoStack_CLI.models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
}
