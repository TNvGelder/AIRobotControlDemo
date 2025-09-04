using System.Net;
using System.Net.Http.Json;
using AIRobotControl.Server.Tests.Shared;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AIRobotControl.Server.Tests.Integration.Modules.RobotManagement.Features.Personas;

public class CreatePersonaEndpointTests : IntegrationTestBase
{
    public CreatePersonaEndpointTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreatePersona_WithValidData_ShouldReturnCreatedWithLocationHeader()
    {
        var request = new
        {
            Name = "Test Persona",
            Description = "A test persona description",
            Instructions = "Be helpful and friendly",
            Tags = "test,sample"
        };

        var response = await PostAsync("/api/personas", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Match("/api/personas/*");

        var persona = await DbContext!.Personas.FirstOrDefaultAsync();
        persona.Should().NotBeNull();
        persona!.Name.Should().Be("Test Persona");
        persona.Description.Should().Be("A test persona description");
        persona.Instructions.Should().Be("Be helpful and friendly");
        persona.Tags.Should().Be("test,sample");
        persona.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        persona.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreatePersona_WithMinimalData_ShouldReturnCreated()
    {
        var request = new
        {
            Name = "Minimal Persona",
            Instructions = "Basic instructions"
        };

        var response = await PostAsync("/api/personas", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var persona = await DbContext!.Personas.FirstOrDefaultAsync();
        persona.Should().NotBeNull();
        persona!.Name.Should().Be("Minimal Persona");
        persona.Instructions.Should().Be("Basic instructions");
        persona.Description.Should().BeNull();
        persona.Tags.Should().BeNull();
    }

    [Fact]
    public async Task CreatePersona_WithMissingName_ShouldReturnBadRequest()
    {
        var request = new
        {
            Instructions = "Some instructions"
        };

        var response = await PostAsync("/api/personas", request);

        await AssertProblemDetails(response, 400);
        
        var count = await DbContext!.Personas.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task CreatePersona_WithMissingInstructions_ShouldReturnBadRequest()
    {
        var request = new
        {
            Name = "Test Persona"
        };

        var response = await PostAsync("/api/personas", request);

        await AssertProblemDetails(response, 400);
        
        var count = await DbContext!.Personas.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task CreatePersona_WithEmptyName_ShouldReturnBadRequest()
    {
        var request = new
        {
            Name = "",
            Instructions = "Some instructions"
        };

        var response = await PostAsync("/api/personas", request);

        await AssertProblemDetails(response, 400);
        
        var count = await DbContext!.Personas.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task CreatePersona_WithEmptyInstructions_ShouldReturnBadRequest()
    {
        var request = new
        {
            Name = "Test Persona",
            Instructions = ""
        };

        var response = await PostAsync("/api/personas", request);

        await AssertProblemDetails(response, 400);
        
        var count = await DbContext!.Personas.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task CreatePersona_WithTooLongName_ShouldReturnBadRequest()
    {
        var request = new
        {
            Name = new string('a', 101),
            Instructions = "Some instructions"
        };

        var response = await PostAsync("/api/personas", request);

        await AssertProblemDetails(response, 400);
        
        var count = await DbContext!.Personas.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task CreatePersona_WithTooLongDescription_ShouldReturnBadRequest()
    {
        var request = new
        {
            Name = "Test Persona",
            Description = new string('a', 501),
            Instructions = "Some instructions"
        };

        var response = await PostAsync("/api/personas", request);

        await AssertProblemDetails(response, 400);
        
        var count = await DbContext!.Personas.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task CreatePersona_WithTooLongTags_ShouldReturnBadRequest()
    {
        var request = new
        {
            Name = "Test Persona",
            Instructions = "Some instructions",
            Tags = new string('a', 201)
        };

        var response = await PostAsync("/api/personas", request);

        await AssertProblemDetails(response, 400);
        
        var count = await DbContext!.Personas.CountAsync();
        count.Should().Be(0);
    }
}