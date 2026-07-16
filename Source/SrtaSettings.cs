using Microsoft.Xna.Framework.Input;

namespace Celeste.Mod.SrtAdditions;

public class SrtaSettings : EverestModuleSettings {
    public bool ShowRoomTimer { get; set; } = true;

    public bool OnlyShowRoomTimerWhenCompleted { get; set; } = false;

    [DefaultButtonBinding(0, Keys.None)]
    public ButtonBinding ToggleRoomTimerVisibility { get; set; }

    [DefaultButtonBinding(0, Keys.None)]
    public ButtonBinding UndoRoomTimerPb { get; set; }
}
