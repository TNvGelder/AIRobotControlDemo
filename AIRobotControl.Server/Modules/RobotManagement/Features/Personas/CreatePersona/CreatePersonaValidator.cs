using FluentValidation;

namespace AIRobotControl.Server.Modules.RobotManagement.Features.Personas.CreatePersona;

public class CreatePersonaValidator : AbstractValidator<CreatePersonaRequest>
{
    public CreatePersonaValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Instructions)
            .NotEmpty().WithMessage("Instructions are required");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Tags)
            .MaximumLength(200).WithMessage("Tags must not exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Tags));
    }
}