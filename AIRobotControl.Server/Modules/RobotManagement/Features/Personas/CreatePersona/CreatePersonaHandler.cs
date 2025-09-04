using AIRobotControl.Server.Data;
using AIRobotControl.Server.Modules.RobotManagement.Domain;
using AIRobotControl.Server.Shared.Abstractions;

namespace AIRobotControl.Server.Modules.RobotManagement.Features.Personas.CreatePersona;

public class CreatePersonaHandler : IHandler<CreatePersonaRequest, int>
{
    private readonly ApplicationDbContext _dbContext;

    public CreatePersonaHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> Handle(CreatePersonaRequest request, CancellationToken cancellationToken)
    {
        var persona = new Persona
        {
            Name = request.Name,
            Description = request.Description,
            Instructions = request.Instructions,
            Tags = request.Tags
        };

        _dbContext.Personas.Add(persona);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return persona.Id;
    }
}