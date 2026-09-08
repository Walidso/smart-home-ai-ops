using FluentValidation;

namespace SmartHome.Actions.Application.Actions.ProposeAction;

public class ProposeActionCommandValidator : AbstractValidator<ProposeActionCommand>
{
    public ProposeActionCommandValidator()
    {
        RuleFor(c => c.Description).NotEmpty().MaximumLength(500);
    }
}
