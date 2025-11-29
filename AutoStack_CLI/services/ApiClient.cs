using AutoStack_CLI.models;
using System.Net.Http.Json;

namespace AutoStack_CLI.services;

public class ApiClient
{
    private static readonly HttpClient Client = new();

    public ApiClient(AppConfiguration config)
    {
        Client.BaseAddress = new Uri(config.ApiBaseUrl);
        Client.DefaultRequestHeaders.Add("Accept", "application/json");
        Client.DefaultRequestHeaders.Add("User-Agent", "AutoStack-CLI/1.0");
    }

    public async Task<Stack?> GetStackAsync(Guid stackId)
    {
        try
        {
            var response = await Client.GetFromJsonAsync<ApiResponse<Stack>>($"stack/getstack?id={stackId}");
            return response?.Data;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"API Error: {ex.Message}");
            return null;
        }
    }

    public async Task<Token?> LoginAsync(string username, string password)
    {
        var loginRequest = new { Username = username, Password = password };
        var response = await Client.PostAsJsonAsync("login", loginRequest);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Token>();
        }

        return null;
    }
}