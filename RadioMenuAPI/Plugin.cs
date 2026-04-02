using System;
using LabApi.Events.CustomHandlers;
using LabApi.Features;
using LabApi.Loader.Features.Plugins;

namespace RadioMenuAPI;

public class RadioMenuAPI : Plugin
{
    private RadioMenuEventHandler? _eventHandler;
    public override string Name => "RadioMenuAPI";
    public override string Description => "API for creating custom radio menus.";
    public override string Author => "MedveMarci";
    public override Version Version { get; } = new(1, 1, 0);
    public override Version RequiredApiVersion { get; } = new(LabApiProperties.CompiledVersion);

    public override void Enable()
    {
        _eventHandler = new RadioMenuEventHandler();
        CustomHandlersManager.RegisterEventsHandler(_eventHandler);
    }

    public override void Disable()
    {
        if (_eventHandler != null)
            CustomHandlersManager.UnregisterEventsHandler(_eventHandler);
        _eventHandler = null;
        RadioMenuManager.ClearAll();
    }
}