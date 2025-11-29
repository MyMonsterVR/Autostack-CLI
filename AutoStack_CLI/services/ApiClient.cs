using AutoStack_CLI.models;
using System.Net.Http.Json;

namespace AutoStack_CLI.services;

public class ApiClient(AppConfiguration config)
{
    private readonly HttpClient client = CreateHttpClient(config);

    private static HttpClient CreateHttpClient(AppConfiguration config)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(config.ApiBaseUrl)
        };
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        httpClient.DefaultRequestHeaders.Add("User-Agent", "AutoStack-CLI/1.0");
        return httpClient;
    }

    public async Task<Stack?> GetStackAsync(Guid stackId)
    {
        try
        {
            var response = await client.GetFromJsonAsync<ApiResponse<Stack>>($"stack/getstack?id={stackId}");
            return response?.Data;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"API Error: {ex.Message}");
            return null;
        }
    }
    
    public async Task<List<Stack?>> GetStacksAsync()
    {
        try
        {
            var response = await client.GetFromJsonAsync<ApiResponse<PagedResponse<Stack>>>($"stack/getstacks");
            return response?.Data?.Items;
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
        var response = await client.PostAsJsonAsync("login", loginRequest);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Token>();
        }

        return null;
    }
}