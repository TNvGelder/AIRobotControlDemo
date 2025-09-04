using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using AIRobotControl.Server.Data;

namespace AIRobotControl.Server.Tests.Shared;

public abstract class IntegrationTestBase : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    protected readonly TestWebApplicationFactory Factory;
    protected readonly HttpClient Client;
    protected IServiceScope? Scope;
    protected ApplicationDbContext? DbContext;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected IntegrationTestBase(TestWebApplicationFactory factory)
    {
        Factory = factory;
        Client = Factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        Scope = Factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (DbContext != null)
        {
            await DbContext.Database.EnsureDeletedAsync();
            await DbContext.Database.EnsureCreatedAsync();
        }
        
        Scope?.Dispose();
    }

    protected async Task<T?> GetAsync<T>(string url)
    {
        var response = await Client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
    }

    protected async Task<HttpResponseMessage> PostAsync(string url, object content)
    {
        return await Client.PostAsJsonAsync(url, content, _jsonOptions);
    }

    protected async Task<T?> PostAsync<T>(string url, object content)
    {
        var response = await PostAsync(url, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
    }

    protected async Task AssertProblemDetails(HttpResponseMessage response, int expectedStatus, string? expectedDetail = null)
    {
        response.StatusCode.Should().Be((System.Net.HttpStatusCode)expectedStatus);
        
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>(_jsonOptions);
        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be(expectedStatus);
        
        if (expectedDetail != null)
        {
            problemDetails.Detail.Should().Contain(expectedDetail);
        }
    }

    protected class ProblemDetailsResponse
    {
        public string? Type { get; set; }
        public string? Title { get; set; }
        public int? Status { get; set; }
        public string? Detail { get; set; }
        public object? Errors { get; set; }
    }
}