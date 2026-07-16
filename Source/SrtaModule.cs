using System;

namespace Celeste.Mod.SrtAdditions;

public class SrtaModule : EverestModule {
    public static SrtaModule Instance { get; private set; }

    public override Type SettingsType => typeof(SrtaSettings);
    public static SrtaSettings Settings => (SrtaSettings)Instance._Settings;

    public SrtaModule() {
        Instance = this;
    }

    public override void Load() {
        RoomTimerTweaks.Load();
    }

    public override void Unload() {
        RoomTimerTweaks.Unload();
    }
}
