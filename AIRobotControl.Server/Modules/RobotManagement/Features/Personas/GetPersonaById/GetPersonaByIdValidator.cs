using FluentValidation;

namespace AIRobotControl.Server.Modules.RobotManagement.Features.Personas.GetPersonaById;

public class GetPersonaByIdValidator : AbstractValidator<GetPersonaByIdRequest>
{
    public GetPersonaByIdValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id must be a positive integer");
    }
}
