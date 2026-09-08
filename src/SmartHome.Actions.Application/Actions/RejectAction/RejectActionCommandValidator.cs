using FluentValidation;

namespace SmartHome.Actions.Application.Actions.RejectAction;

public class RejectActionCommandValidator : AbstractValidator<RejectActionCommand>
{
    public RejectActionCommandValidator()
    {
        RuleFor(c => c.ActionId).NotEmpty();
    }
}
