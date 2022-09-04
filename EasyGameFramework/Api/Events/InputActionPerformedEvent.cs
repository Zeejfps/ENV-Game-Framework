namespace EasyGameFramework.Api.Events;

internal readonly struct InputActionPerformedEvent
{
    public string ActionName { get; init; }
}