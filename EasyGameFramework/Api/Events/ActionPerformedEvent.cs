namespace EasyGameFramework.Api.Events;

internal readonly struct ActionPerformedEvent
{
    public string ActionName { get; init; }
}