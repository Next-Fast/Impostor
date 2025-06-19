using System.Collections.Generic;
using System.Linq;

namespace Impostor.Api.Events;

public interface IEventResult;

public enum EventResultType
{
    Default,
    Cancelled,
    Error,
    Success,
}

public class NumberEventOutcome<T>(T result) : IEventResult where T : unmanaged
{
    public T Result { get; init; } = result;

    public static implicit operator T(NumberEventOutcome<T> result)
    {
        return result.Result;
    }
}

public class EventOutcome<T>(T result) : IEventResult
{
    public T Result { get; init; } = result;

    public static implicit operator T(EventOutcome<T> result)
    {
        return result.Result;
    }
}

public class EventResultCollection(IEnumerable<IEventResult> result) : IEventResult
{
    public List<IEventResult> Results { get; init; } = result.ToList();

    public T Result<T>(int index) where T : IEventResult
    {
        return (T)Results[index];
    }
}

public class EventTypeResult(EventResultType resultType) : IEventResult
{
    public EventResultType Type { get; init; } = resultType;

    public string? Message { get; set; }

    public static implicit operator EventResultType(EventTypeResult result)
    {
        return result.Type;
    }
}
