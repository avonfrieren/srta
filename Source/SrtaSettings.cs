using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.SrtAdditions;

// what the per-room delta is measured against (later: WR/TAS, once external data import is sorted out)
public enum DeltaMode {
    Split, // total time since timer start vs the PB split
    Room,  // this room's segment time vs the PB's room time
}

public class SrtaSettings : EverestModuleSettings {
    public bool ShowRoomTimer { get; set; } = true;

    public bool OnlyShowRoomTimerWhenCompleted { get; set; } = false;

    public bool ShowRoomDeltas { get; set; } = true;

    public DeltaMode RoomDeltasMode { get; set; } = DeltaMode.Split;

    [DefaultButtonBinding(0, Keys.None)]
    public ButtonBinding ToggleRoomTimerVisibility { get; set; }

    [DefaultButtonBinding(0, Keys.None)]
    public ButtonBinding UndoRoomTimerPb { get; set; }

    [DefaultButtonBinding(0, Keys.None)]
    public ButtonBinding ToggleRoomDeltas { get; set; }

    [DefaultButtonBinding(0, Keys.None)]
    public ButtonBinding SwitchRoomDeltasMode { get; set; }
}
