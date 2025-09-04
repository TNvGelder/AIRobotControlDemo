#if false
using AIRobotControl.Server.Data;
using AIRobotControl.Server.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;
using AIRobotControl.Server.Modules.RobotManagement.Features.Personas.GetPersonaById;
using AIRobotControl.Server.Modules.RobotManagement.Features.Personas.Shared;

namespace AIRobotControl.Server.Modules.RobotManagement.Features.Personas.GetPersonaById;

public class GetPersonaByIdHandler : IHandler<GetPersonaByIdRequest, GetPersonaResponse?>
{
    private readonly ApplicationDbContext _dbContext;

    public GetPersonaByIdHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetPersonaResponse?> Handle(GetPersonaByIdRequest request, CancellationToken cancellationToken)
    {
        var persona = await _dbContext.Personas
            .Where(p => p.Id == request.Id)
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
}
#endif
