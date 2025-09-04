using System.Net;
using System.Net.Http.Json;
using AIRobotControl.Server.Modules.RobotManagement.Domain;
using AIRobotControl.Server.Tests.Shared;
using FluentAssertions;

namespace AIRobotControl.Server.Tests.Integration.Modules.RobotManagement.Features.Personas;

public class GetPersonaEndpointTests : IntegrationTestBase
{
    public GetPersonaEndpointTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetPersonaById_WithExistingPersona_ShouldReturnPersona()
    {
        var persona = new Persona
        {
            Name = "Test Persona",
            Description = "Test description",
            Instructions = "Test instructions",
            Tags = "test,sample",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            UpdatedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        DbContext!.Personas.Add(persona);
        await DbContext.SaveChangesAsync();

        var response = await Client.GetAsync($"/api/personas/{persona.Id}");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<GetPersonaResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(persona.Id);
        result.Name.Should().Be("Test Persona");
        result.Description.Should().Be("Test description");
        result.Instructions.Should().Be("Test instructions");
        result.Tags.Should().Be("test,sample");
        result.CreatedAt.Should().BeCloseTo(persona.CreatedAt, TimeSpan.FromSeconds(1));
        result.UpdatedAt.Should().BeCloseTo(persona.UpdatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetPersonaById_WithNonExistentId_ShouldReturnNotFound()
    {
        var response = await Client.GetAsync("/api/personas/999");
        
        await AssertProblemDetails(response, 404);
    }

    [Fact]
    public async Task GetPersonaById_WithInvalidId_ShouldReturnBadRequest()
    {
        var response = await Client.GetAsync("/api/personas/invalid");
        
        await AssertProblemDetails(response, 400);
    }

    [Fact]
    public async Task GetAllPersonas_WithNoPersonas_ShouldReturnEmptyList()
    {
        var response = await Client.GetAsync("/api/personas");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<GetPersonasResponse>();
        result.Should().NotBeNull();
        result!.Personas.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllPersonas_WithMultiplePersonas_ShouldReturnAllPersonas()
    {
        var personas = new[]
        {
            new Persona 
            { 
                Name = "Persona 1", 
                Instructions = "Instructions 1",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
                UpdatedAt = DateTimeOffset.UtcNow.AddDays(-2)
            },
            new Persona 
            { 
                Name = "Persona 2", 
                Instructions = "Instructions 2",
                Description = "Description 2",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
                UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new Persona 
            { 
                Name = "Persona 3", 
                Instructions = "Instructions 3",
                Tags = "tag1,tag2",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        };

        DbContext!.Personas.AddRange(personas);
        await DbContext.SaveChangesAsync();

        var response = await Client.GetAsync("/api/personas");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<GetPersonasResponse>();
        result.Should().NotBeNull();
        result!.Personas.Should().HaveCount(3);
        result.Personas.Select(p => p.Name).Should().BeEquivalentTo(new[] { "Persona 1", "Persona 2", "Persona 3" });
    }

    private class GetPersonaResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string Instructions { get; set; } = "";
        public string? Tags { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    private class GetPersonasResponse
    {
        public List<GetPersonaResponse> Personas { get; set; } = new();
    }
}