using FluentValidation;

namespace SmartHome.Actions.Application.Actions.ApproveAction;

public class ApproveActionCommandValidator : AbstractValidator<ApproveActionCommand>
{
    public ApproveActionCommandValidator()
    {
        RuleFor(c => c.ActionId).NotEmpty();
    }
}
