#if false
using AIRobotControl.Server.Data;
using AIRobotControl.Server.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;
using AIRobotControl.Server.Modules.RobotManagement.Features.Personas.Shared;

namespace AIRobotControl.Server.Modules.RobotManagement.Features.Personas.GetAllPersonas;

public class GetAllPersonasHandler : IHandler<NoRequest, GetPersonasResponse>
{
    private readonly ApplicationDbContext _dbContext;

    public GetAllPersonasHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetPersonasResponse> Handle(NoRequest request, CancellationToken cancellationToken)
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
