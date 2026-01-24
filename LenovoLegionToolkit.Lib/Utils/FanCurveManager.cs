using LenovoLegionToolkit.Lib.Controllers.Sensors;
using LenovoLegionToolkit.Lib.Features;
using LenovoLegionToolkit.Lib.Listeners;
using LenovoLegionToolkit.Lib.Messaging;
using LenovoLegionToolkit.Lib.Messaging.Messages;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UniversalFanControl.Lib;
using UniversalFanControl.Lib.Generic.Api;

namespace LenovoLegionToolkit.Lib.Utils;

public partial class FanCurveManager : IDisposable
{
    private readonly SensorsGroupController _sensors;
    private readonly FanCurveSettings _settings;
    private readonly PowerModeFeature _powerModeFeature;
    private readonly PowerModeListener _powerModeListener;

    private PowerModeState _cachedPowerState = PowerModeState.Balance;

    private readonly FanControl _fanHardware = new();
    private readonly Dictionary<FanType, IFanControlView> _activeViewModels = new();
    private readonly Dictionary<FanType, FanCurveController> _activeControllers = new();

    private readonly HashSet<string> _loggedConfigs = new();

    private CancellationTokenSource? _cts;

    public int LogicInterval { get; set; } = 500;

    private bool _isEnabled;

    public FanCurveManager(
        SensorsGroupController sensors,
        FanCurveSettings settings,
        PowerModeFeature powerModeFeature,
        PowerModeListener powerModeListener)
    {
        _sensors = sensors;
        _settings = settings;
        _powerModeFeature = powerModeFeature;
        _powerModeListener = powerModeListener;
    }

    public void Initialize(bool enabled)
    {
        if (_isEnabled) return;
        _isEnabled = enabled;

        if (!_isEnabled) return;

        InitializeCustomInternal();
    }

    // This partial method will be implemented in FanCurveManager.Custom.cs
    // If the file is missing, the compiler will ignore calls to it.
    partial void InitializeCustomInternal();

    private void OnPowerModeChanged(object? sender, PowerModeListener.ChangedEventArgs e)
    {
        _cachedPowerState = e.State;
    }

    public void RegisterViewModel(FanType type, IFanControlView vm)
    {
        if (!_isEnabled) return;
        lock (_activeViewModels)
        {
            _activeViewModels[type] = vm;
        }
    }

    public void UnregisterViewModel(FanType type, IFanControlView vm)
    {
        if (!_isEnabled) return;
        lock (_activeViewModels)
        {
            if (_activeViewModels.TryGetValue(type, out var current) && current == vm) _activeViewModels.Remove(type);
        }
    }

    public async Task SetRegister(bool flag = false)
    {
        if (!_isEnabled) return;
        await SetRegisterInternal(flag);
    }

    // Provide a fallback if Custom implementation is missing
    private Task SetRegisterInternal(bool flag)
    {
        try 
        {
            return SetRegisterCustom(flag);
        }
        catch (NotImplementedException)
        {
            return Task.CompletedTask;
        }
    }

    private partial Task SetRegisterCustom(bool flag);

    private partial void StartControlLoop();

    public partial FanCurveEntry? GetEntry(FanType type);

    public partial void AddEntry(FanCurveEntry entry);

    public partial void UpdateGlobalSettings(FanCurveEntry sourceEntry);

    public partial void UpdateConfig(FanType type, FanCurveEntry entry);

    private partial (ushort Val1, ushort Val2, ushort Val3) GetHardwareConfig(MachineInformation? mi, FanType fanType, bool isControlConfig);

    public partial void Dispose();
}
