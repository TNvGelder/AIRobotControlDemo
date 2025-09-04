#if false
using AIRobotControl.Server.Data;
using AIRobotControl.Server.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AIRobotControl.Server.Modules.RobotManagement.Features.Personas.GetPersona;

public class GetPersonaHandler
{
    private readonly ApplicationDbContext _dbContext;

    public GetPersonaHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetPersonaResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var persona = await _dbContext.Personas
            .Where(p => p.Id == id)
            .Select(p => new GetPersonaResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Instructions = p.Instructions,
                Tags = p.Tags,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return persona;
    }

    public async Task<GetPersonasResponse> GetAllAsync(CancellationToken cancellationToken)
    {
        var personas = await _dbContext.Personas
            .Select(p => new GetPersonaResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Instructions = p.Instructions,
                Tags = p.Tags,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new GetPersonasResponse { Personas = personas };
    }
}
#endif