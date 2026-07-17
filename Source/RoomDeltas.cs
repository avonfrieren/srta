using System;
using System.Collections.Generic;
using Celeste.Mod.SpeedrunTool;
using Celeste.Mod.SpeedrunTool.Message;
using Celeste.Mod.SpeedrunTool.RoomTimer;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.ModInterop;

namespace Celeste.Mod.SrtAdditions;

// Phase 2: per-room delta against the PB split, drawn under SpeedrunTool's room timer.
// SpeedrunTool only shows a comparison once the run is completed; this shows one on every
// room change, like a LiveSplit split.
internal static class RoomDeltas {
    private static SrtaSettings Settings => SrtaModule.Settings;
    private static SpeedrunToolSettings SrtSettings => SpeedrunToolSettings.Instance;

    // greens match SpeedrunTool's "finished" colors; reds are their hue-flipped counterpart
    private static readonly Color AheadColor1 = Calc.HexToColor("6ded87");
    private static readonly Color AheadColor2 = Calc.HexToColor("43d14c");
    private static readonly Color BehindColor1 = Calc.HexToColor("ed6d6d");
    private static readonly Color BehindColor2 = Calc.HexToColor("d14343");

    // mutated during gameplay ⇒ registered with SpeedrunTool's save states below,
    // so the displayed delta follows save/load like the timer itself
    private static int lastRoomNumber;
    private static string lastTimeKeyPrefix = "";
    private static RoomTimerType lastTimerType = RoomTimerType.Off;
    private static string deltaText = "";
    private static bool deltaIsAhead;

    // settings mirrors, deliberately NOT registered with save states: settings are not
    // rolled back on load, so these trackers must not be either
    private static int lastNumberOfRooms;
    private static DeltaMode lastDeltaMode;

    private static object saveLoadAction;

    // fields are filled at runtime by ModInterop()
#pragma warning disable CS0649
    [ModImportName("SpeedrunTool.SaveLoad")]
    private static class SaveLoadImports {
        public static Func<Type, string[], object> RegisterStaticTypes;
        public static Action<object> Unregister;
    }
#pragma warning restore CS0649

    public static void Load() {
        On.Celeste.Level.Update += LevelOnUpdate;
        On.Celeste.SpeedrunTimerDisplay.Render += SpeedrunTimerDisplayOnRender;

        typeof(SaveLoadImports).ModInterop();
        saveLoadAction = SaveLoadImports.RegisterStaticTypes?.Invoke(typeof(RoomDeltas),
            [nameof(lastRoomNumber), nameof(lastTimeKeyPrefix), nameof(lastTimerType), nameof(deltaText), nameof(deltaIsAhead)]);
    }

    public static void Unload() {
        On.Celeste.Level.Update -= LevelOnUpdate;
        On.Celeste.SpeedrunTimerDisplay.Render -= SpeedrunTimerDisplayOnRender;

        if (saveLoadAction != null) {
            SaveLoadImports.Unregister?.Invoke(saveLoadAction);
            saveLoadAction = null;
        }
    }

    private static void LevelOnUpdate(On.Celeste.Level.orig_Update orig, Level self) {
        // SpeedrunTool updates its room timer inside orig (srta loads after it, so this hook
        // is outermost) ⇒ after orig the timer data of this frame is final
        orig(self);

        if (!self.Paused && Settings.ToggleRoomDeltas.Pressed) {
            Settings.ShowRoomDeltas = !Settings.ShowRoomDeltas;
            SrtaModule.Instance.SaveSettings();
            string state = Dialog.Clean(Settings.ShowRoomDeltas ? DialogIds.On : DialogIds.Off);
            PopupMessageUtils.ShowOptionState(Dialog.Clean("MODOPTIONS_SRTA_SHOWROOMDELTAS"), state);
        }

        UpdateDelta();
    }

    // poll RoomTimerData instead of hooking transitions: roomNumber only moves once
    // SpeedrunTool has recorded the split, whatever triggered it (transition, summit flag,
    // heart/cassette, level complete)
    private static void UpdateDelta() {
        if (SrtSettings is null) {
            return;
        }

        RoomTimerData data = RoomTimerManager.Data_Auto;
        int roomNumber = data.roomNumber;
        string prefix = data.TimeKeyPrefix;

        // timer type switch = Data_Auto may point to the other instance; prefix change = other
        // map or fresh attempt; roomNumber going back = timer reset or save state load;
        // timed-room count or comparison mode change = the displayed delta no longer means
        // anything, show none until the next room
        bool resync = SrtSettings.RoomTimerType != lastTimerType
                      || prefix != lastTimeKeyPrefix
                      || roomNumber < lastRoomNumber
                      || SrtSettings.NumberOfRooms != lastNumberOfRooms
                      || Settings.RoomDeltasMode != lastDeltaMode;
        lastTimerType = SrtSettings.RoomTimerType;
        lastTimeKeyPrefix = prefix;
        lastNumberOfRooms = SrtSettings.NumberOfRooms;
        lastDeltaMode = Settings.RoomDeltasMode;

        if (resync) {
            lastRoomNumber = roomNumber;
            deltaText = "";
            return;
        }

        if (roomNumber == lastRoomNumber) {
            return;
        }
        lastRoomNumber = roomNumber;

        if (!SrtSettings.Enabled || SrtSettings.RoomTimerType == RoomTimerType.Off) {
            deltaText = "";
            return;
        }

        // the split SpeedrunTool just recorded belongs to the previous room number;
        // compare against the PB split as it was when the attempt started (like its own
        // end-of-run comparison), since PbTimes may already contain this run's time
        string key = prefix + (roomNumber - 1);
        if (data.ThisRunTimes.TryGetValue(key, out long split) &&
            data.lastPbTimes.TryGetValue(key, out long pbSplit) && pbSplit > 0) {
            long time = split;
            long pb = pbSplit;
            if (Settings.RoomDeltasMode == DeltaMode.Room) {
                // segment times: subtract the previous room's split on both sides
                // (missing key = first timed room = 0, like SpeedrunTool's prevRoomTime)
                string prevKey = prefix + (roomNumber - 2);
                time -= data.ThisRunTimes.GetValueOrDefault(prevKey, 0);
                pb -= data.lastPbTimes.GetValueOrDefault(prevKey, 0);
            }
            deltaText = RoomTimerManager.ComparePb(time, pb);
            deltaIsAhead = time <= pb;
        }
        else {
            // nothing to compare against yet (first attempt through this room)
            deltaText = "";
        }
    }

    private static void SpeedrunTimerDisplayOnRender(On.Celeste.SpeedrunTimerDisplay.orig_Render orig, SpeedrunTimerDisplay self) {
        orig(self);

        if (!Settings.ShowRoomDeltas || deltaText.Length == 0 || RoomTimerTweaks.HideRoomTimer) {
            return;
        }

        if (SrtSettings is not { Enabled: true } || SrtSettings.RoomTimerType == RoomTimerType.Off) {
            return;
        }

        // once completed SpeedrunTool draws its own comparison next to the final time
        if (RoomTimerManager.Data_Auto.IsCompleted || self.DrawLerp <= 0f) {
            return;
        }

        DrawDelta(self);
    }

    // third row below SpeedrunTool's time + PB rows, same background and sliding animation
    private static void DrawDelta(SpeedrunTimerDisplay self) {
        const float topTimeHeight = 38f;
        const float timeMarginLeft = 32f;
        const float scale = 0.6f;

        MTexture bg = GFX.Gui["strawberryCountBG"];
        float x = -300f * Ease.CubeIn(1f - self.DrawLerp);
        float y = self.Y + topTimeHeight + bg.Height * scale + 1f;
        float width = 60 + Math.Max(0, 18 * (deltaText.Length - 5));

        Draw.Rect(x, y - 1f, width + bg.Width * scale, 1f, Color.Black);
        Draw.Rect(x, y, width + 2, bg.Height * scale + 1f, Color.Black);
        bg.Draw(new Vector2(x + width, y), Vector2.Zero, Color.White, scale);

        Color color1 = deltaIsAhead ? AheadColor1 : BehindColor1;
        Color color2 = deltaIsAhead ? AheadColor2 : BehindColor2;
        DrawTime(new Vector2(x + timeMarginLeft, y + 28.4f), deltaText, scale, color1, color2);
    }

    // char-by-char like SpeedrunTool's DrawTime, but with our own colors; per-char advance
    // via the public GetTimeWidth to avoid publicizing Celeste's spacerWidth/numberWidth
    private static void DrawTime(Vector2 position, string text, float scale, Color color1, Color color2) {
        PixelFont font = Dialog.Languages["english"].Font;
        float fontFaceSize = Dialog.Languages["english"].FontFaceSize;
        float x = position.X;

        foreach (char ch in text) {
            Color color = ch is ':' or '.' ? color2 : color1;
            float advance = (SpeedrunTimerDisplay.GetTimeWidth(ch.ToString()) + 4f) * scale;
            font.DrawOutline(fontFaceSize, ch.ToString(), new Vector2(x + advance / 2f, position.Y),
                new Vector2(0.5f, 1f), Vector2.One * scale, color, 2f, Color.Black);
            x += advance;
        }
    }
}
