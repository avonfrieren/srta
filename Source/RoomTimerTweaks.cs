using System.Collections.Generic;
using Celeste.Mod.SpeedrunTool;
using Celeste.Mod.SpeedrunTool.Message;
using Celeste.Mod.SpeedrunTool.RoomTimer;

namespace Celeste.Mod.SrtAdditions;

// Phase 1 features, implemented on top of the official SpeedrunTool instead of forking it.
// The room timer keeps running while hidden; only its display is suppressed.
internal static class RoomTimerTweaks {
    private static SrtaSettings Settings => SrtaModule.Settings;
    private static SpeedrunToolSettings SrtSettings => SpeedrunToolSettings.Instance;

    private static bool HideRoomTimer =>
        !Settings.ShowRoomTimer ||
        (Settings.OnlyShowRoomTimerWhenCompleted && !RoomTimerManager.Data_Auto.IsCompleted);

    public static void Load() {
        // added after SpeedrunTool's own hooks (dependency ⇒ it loads first), so these wrappers
        // run outermost and SpeedrunTool's display code sees RoomTimerType.Off while hidden
        On.Celeste.SpeedrunTimerDisplay.Update += SpeedrunTimerDisplayOnUpdate;
        On.Celeste.SpeedrunTimerDisplay.Render += SpeedrunTimerDisplayOnRender;
        On.Celeste.TotalStrawberriesDisplay.Update += TotalStrawberriesDisplayOnUpdate;
        On.Celeste.Level.Update += LevelOnUpdate;
    }

    public static void Unload() {
        On.Celeste.SpeedrunTimerDisplay.Update -= SpeedrunTimerDisplayOnUpdate;
        On.Celeste.SpeedrunTimerDisplay.Render -= SpeedrunTimerDisplayOnRender;
        On.Celeste.TotalStrawberriesDisplay.Update -= TotalStrawberriesDisplayOnUpdate;
        On.Celeste.Level.Update -= LevelOnUpdate;
    }

    private static void LevelOnUpdate(On.Celeste.Level.orig_Update orig, Level self) {
        orig(self);

        if (self.Paused) {
            return;
        }

        if (Settings.ToggleRoomTimerVisibility.Pressed) {
            Settings.ShowRoomTimer = !Settings.ShowRoomTimer;
            SrtaModule.Instance.SaveSettings();
            string state = Dialog.Clean(Settings.ShowRoomTimer ? DialogIds.On : DialogIds.Off);
            PopupMessageUtils.ShowOptionState(Dialog.Clean("MODOPTIONS_SRTA_SHOWROOMTIMER"), state);
        }

        if (Settings.UndoRoomTimerPb.Pressed) {
            UndoPbTimes();
            PopupMessageUtils.Show(Dialog.Clean("SRTA_UNDO_ROOM_TIMER_PB_TOOLTIP"), "SRTA_UNDO_ROOM_TIMER_PB_DIALOG");
        }
    }

    // roll PbTimes back to the values recorded when the current attempt started,
    // unlike SpeedrunTool's ResetRoomTimerPb which wipes everything
    private static void UndoPbTimes() {
        RestoreLastPbTimes(RoomTimerManager.CurrentRoomTimerData);
        RestoreLastPbTimes(RoomTimerManager.NextRoomTimerData);
    }

    private static void RestoreLastPbTimes(RoomTimerData data) {
        data.PbTimes.Clear();
        foreach (KeyValuePair<string, long> timePair in data.lastPbTimes) {
            data.PbTimes[timePair.Key] = timePair.Value;
        }
    }

    private static void SpeedrunTimerDisplayOnUpdate(On.Celeste.SpeedrunTimerDisplay.orig_Update orig, SpeedrunTimerDisplay self) {
        RoomTimerType saved = PretendRoomTimerOff();
        try {
            orig(self);
        }
        finally {
            RestoreRoomTimerType(saved);
        }
    }

    private static void SpeedrunTimerDisplayOnRender(On.Celeste.SpeedrunTimerDisplay.orig_Render orig, SpeedrunTimerDisplay self) {
        RoomTimerType saved = PretendRoomTimerOff();
        try {
            orig(self);
        }
        finally {
            RestoreRoomTimerType(saved);
        }
    }

    // SpeedrunTool pushes the strawberry counter down whenever the room timer is on
    private static void TotalStrawberriesDisplayOnUpdate(On.Celeste.TotalStrawberriesDisplay.orig_Update orig, TotalStrawberriesDisplay self) {
        RoomTimerType saved = PretendRoomTimerOff();
        try {
            orig(self);
        }
        finally {
            RestoreRoomTimerType(saved);
        }
    }

    private static RoomTimerType PretendRoomTimerOff() {
        if (SrtSettings is null) {
            return RoomTimerType.Off;
        }

        RoomTimerType current = SrtSettings.RoomTimerType;
        if (current != RoomTimerType.Off && HideRoomTimer) {
            SrtSettings.RoomTimerType = RoomTimerType.Off;
        }

        return current;
    }

    private static void RestoreRoomTimerType(RoomTimerType saved) {
        if (SrtSettings is not null) {
            SrtSettings.RoomTimerType = saved;
        }
    }
}
