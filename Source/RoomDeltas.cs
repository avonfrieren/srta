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
    private static int deltaRoom; // room the delta row refers to (< 1 = none yet)
    private static string deltaText = "";
    private static bool deltaIsAhead;

    // settings mirror, deliberately NOT registered with save states: settings are not
    // rolled back on load, so this tracker must not be either
    private static int lastNumberOfRooms;

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
            [nameof(lastRoomNumber), nameof(lastTimeKeyPrefix), nameof(lastTimerType), nameof(deltaRoom), nameof(deltaText), nameof(deltaIsAhead)]);
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

        if (!self.Paused) {
            if (Settings.ToggleRoomDeltas.Pressed) {
                Settings.ShowRoomDeltas = !Settings.ShowRoomDeltas;
                SrtaModule.Instance.SaveSettings();
                string state = Dialog.Clean(Settings.ShowRoomDeltas ? DialogIds.On : DialogIds.Off);
                PopupMessageUtils.ShowOptionState(Dialog.Clean("MODOPTIONS_SRTA_SHOWROOMDELTAS"), state);
            }

            if (Settings.SwitchRoomDeltasMode.Pressed) {
                Settings.RoomDeltasMode = (DeltaMode)(((int)Settings.RoomDeltasMode + 1) % Enum.GetNames(typeof(DeltaMode)).Length);
                SrtaModule.Instance.SaveSettings();
                PopupMessageUtils.ShowOptionState(Dialog.Clean("MODOPTIONS_SRTA_ROOMDELTASMODE"),
                    Dialog.Clean("MODOPTIONS_SRTA_ROOMDELTASMODE_" + Settings.RoomDeltasMode.ToString().ToUpper()));
            }
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

        // roomNumber moving forward = SpeedrunTool just recorded a split, which belongs to
        // the previous room number; timer type switch = Data_Auto may point to the other
        // instance; prefix change = other map or fresh attempt; roomNumber going back =
        // timer reset — in every case re-anchor on the last split this instance recorded
        // (after a reset ThisRunTimes is empty, so the row stays hidden until the next split)
        if (SrtSettings.RoomTimerType != lastTimerType
            || prefix != lastTimeKeyPrefix
            || roomNumber != lastRoomNumber) {
            deltaRoom = roomNumber - 1;
        }
        lastTimerType = SrtSettings.RoomTimerType;
        lastTimeKeyPrefix = prefix;
        lastRoomNumber = roomNumber;

        // scrolling the timed-room count re-targets the row to the selected room, mirroring
        // how SpeedrunTool re-points its own display at PbTimes[prefix + NumberOfRooms]
        if (SrtSettings.NumberOfRooms != lastNumberOfRooms) {
            if (lastNumberOfRooms != 0) {
                deltaRoom = SrtSettings.NumberOfRooms;
            }
            lastNumberOfRooms = SrtSettings.NumberOfRooms;
        }

        // recomputed every frame so the row reacts instantly to count scrolling and mode
        // switches: shown iff both this run and the PB have a time for deltaRoom
        deltaText = "";
        if (deltaRoom < 1 || !SrtSettings.Enabled || SrtSettings.RoomTimerType == RoomTimerType.Off) {
            return;
        }

        // compare against the PB split as it was when the attempt started (like SpeedrunTool's
        // own end-of-run comparison), since PbTimes may already contain this run's time
        string key = prefix + deltaRoom;
        if (data.ThisRunTimes.TryGetValue(key, out long split) &&
            data.lastPbTimes.TryGetValue(key, out long pbSplit) && pbSplit > 0) {
            long time = split;
            long pb = pbSplit;
            if (Settings.RoomDeltasMode == DeltaMode.Room) {
                // segment times: subtract the previous room's split on both sides
                // (missing key = first timed room = 0, like SpeedrunTool's prevRoomTime)
                string prevKey = prefix + (deltaRoom - 1);
                time -= data.ThisRunTimes.GetValueOrDefault(prevKey, 0);
                pb -= data.lastPbTimes.GetValueOrDefault(prevKey, 0);
            }
            deltaText = RoomTimerManager.ComparePb(time, pb);
            deltaIsAhead = time <= pb;
        }
    }

    private static void SpeedrunTimerDisplayOnRender(On.Celeste.SpeedrunTimerDisplay.orig_Render orig, SpeedrunTimerDisplay self) {
        orig(self);

        if (!Settings.ShowRoomDeltas || deltaText.Length == 0 || RoomTimerTweaks.HideRoomTimer) {
            return;
        }

        if (SrtSettings is not { Enabled: true } || SrtSettings.RoomTimerType == RoomTimerType.Off || self.DrawLerp <= 0f) {
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
