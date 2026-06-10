using System;
using LabApi.Events.CustomHandlers;
using LabApi.Features;
using LabApi.Loader.Features.Plugins;
using LabApi.Loader.Features.Plugins.Enums;

namespace RadioMenuAPI;

public class RadioMenuAPI : Plugin<Config>
{
    internal static RadioMenuAPI Singleton = null!;
    private RadioMenuEventHandler? _eventHandler;
    public override string Name => "RadioMenuAPI";
    public override string Description => "API for creating custom radio menus.";
    public override string Author => "MedveMarci";
    public override Version Version { get; } = new(1, 3, 1);
    public override Version RequiredApiVersion { get; } = new(LabApiProperties.CompiledVersion);
    public override LoadPriority Priority => LoadPriority.Highest;

    public override void Enable()
    {
        Singleton = this;
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