using FluentValidation;

namespace SmartHome.Application.Sensors.GetSensorHistory;

public class GetSensorHistoryQueryValidator : AbstractValidator<GetSensorHistoryQuery>
{
    public GetSensorHistoryQueryValidator()
    {
        // A Guid.Empty SensorId parses just fine as a Guid (model binding won't catch it), but
        // it can never match a real sensor — that's exactly the kind of "syntactically valid,
        // semantically meaningless" input FluentValidation is for.
        RuleFor(q => q.SensorId).NotEmpty();
    }
}
