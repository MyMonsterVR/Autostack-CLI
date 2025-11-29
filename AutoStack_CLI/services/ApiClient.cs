using AutoStack_CLI.models;
using System.Net.Http.Json;
using AutoStack_CLI.models.parameters;

namespace AutoStack_CLI.services;

public class ApiClient(ApiConfiguration config)
{
    private readonly HttpClient client = CreateHttpClient(config);

    private static HttpClient CreateHttpClient(ApiConfiguration config)
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
        var loginRequest = new LoginParameters(username, password);
        var response = await client.PostAsJsonAsync("login", loginRequest);

        if (response.IsSuccessStatusCode)
        {
            var loginData = await response.Content.ReadFromJsonAsync<ApiResponse<Login>>();
            return new Token(loginData.Data.AccessToken, loginData.Data.RefreshToken);
        }

        return null;
    }
}