using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace STS2TurnTimer.UI;

/// <summary>
/// A countdown timer bar that appears below the End Turn button.
/// Shows remaining seconds and a shrinking progress bar.
/// Styled to match the Slay the Spire aesthetic.
/// When AutoEndTurn is enabled, automatically ends the local player's turn on expiry.
/// </summary>
public partial class NTurnTimerBar : Control
{
    private static NTurnTimerBar? _instance;
    public static NTurnTimerBar? Instance => _instance;

    private enum TimerState
    {
        Idle,
        Running,
        Expired
    }

    private TimerState _state = TimerState.Idle;
    private double _elapsed;
    private double _duration;
    private bool _autoEndTriggered;

    // Bar dimensions — shorter width to avoid discard pile overlap
    private const float BarWidth = 260f;
    private const float BarHeight = 40f;
    private const float InnerPadding = 4f;

    // Colors — subtle, less contrast between elements
    private static readonly Color OuterBorderColor = new(0.12f, 0.10f, 0.08f, 0.95f);
    private static readonly Color InnerBorderLight = new(0.38f, 0.33f, 0.26f, 0.7f);
    private static readonly Color InnerBorderDark = new(0.22f, 0.18f, 0.14f, 0.7f);
    private static readonly Color BackgroundColor = new(0.13f, 0.11f, 0.09f, 0.92f);
    private static readonly Color FillColorFull = new("D4A830");
    private static readonly Color FillColorMid = new("E87820");
    private static readonly Color FillColorLow = new("C03030");
    private static readonly Color FillHighlight = new(1f, 1f, 0.85f, 0.06f);
    private static readonly Color FillShadow = new(0f, 0f, 0f, 0.06f);
    private static readonly Color TextColor = new("E8DCBE");
    private static readonly Color TextOutlineColor = new(0f, 0f, 0f, 1f);
    private static readonly Color ExpiredTextColor = new("FF4040");

    public override void _Ready()
    {
        _instance = this;
        CustomMinimumSize = new Vector2(BarWidth, BarHeight);
        Size = new Vector2(BarWidth, BarHeight);
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;
        ZIndex = 10;
    }

    public override void _ExitTree()
    {
        if (_instance == this) _instance = null;
    }

    public override void _Process(double delta)
    {
        if (_state == TimerState.Running)
        {
            _elapsed += delta;
            if (_elapsed >= _duration)
            {
                _elapsed = _duration;
                _state = TimerState.Expired;
                TryAutoEndTurn();
            }
            QueueRedraw();
        }
    }

    /// <summary>
    /// When AutoEndTurn is enabled, automatically ends the local player's turn
    /// by calling CallReleaseLogic() on the parent NEndTurnButton — the exact same
    /// path as a manual button press. This atomically disables the button and
    /// enqueues EndPlayerTurnAction, preventing any double-end-turn race.
    /// </summary>
    private void TryAutoEndTurn()
    {
        if (_autoEndTriggered) return;
        if (!TimerConfig.AutoEndTurn) return;

        try
        {
            var endTurnButton = GetParent<NEndTurnButton>();
            if (endTurnButton == null) return;

            var combatManager = CombatManager.Instance;
            if (combatManager == null || !combatManager.IsInProgress) return;

            var localPlayer = LocalContext.GetMe(combatManager._state);
            if (localPlayer == null) return;

            if (combatManager.IsPlayerReadyToEndTurn(localPlayer)) return;

            _autoEndTriggered = true;
            endTurnButton.CallReleaseLogic();

            MainFile.Logger.Info("[TurnTimerBar] Auto-ended turn via CallReleaseLogic");
        }
        catch (System.Exception e)
        {
            MainFile.Logger.Error($"[TurnTimerBar] Failed to auto-end turn: {e}");
        }
    }

    public override void _Draw()
    {
        if (_state == TimerState.Idle) return;

        float ratio = 1.0f - (float)(_elapsed / _duration);
        ratio = Mathf.Clamp(ratio, 0f, 1f);

        // === Outer border ===
        var outerRect = new Rect2(0, 0, BarWidth, BarHeight);
        DrawRect(outerRect, OuterBorderColor);

        // === Inner bevel — subtle light top/left, dark bottom/right ===
        DrawLine(new Vector2(1, 1), new Vector2(BarWidth - 1, 1), InnerBorderLight, 1f);
        DrawLine(new Vector2(1, 1), new Vector2(1, BarHeight - 1), InnerBorderLight, 1f);
        DrawLine(new Vector2(1, BarHeight - 2), new Vector2(BarWidth - 1, BarHeight - 2), InnerBorderDark, 1f);
        DrawLine(new Vector2(BarWidth - 2, 1), new Vector2(BarWidth - 2, BarHeight - 1), InnerBorderDark, 1f);

        // === Background ===
        var innerRect = new Rect2(InnerPadding, InnerPadding,
            BarWidth - InnerPadding * 2, BarHeight - InnerPadding * 2);
        DrawRect(innerRect, BackgroundColor);

        // === Fill bar ===
        if (ratio > 0f)
        {
            float fillWidth = (BarWidth - InnerPadding * 2) * ratio;
            float fillHeight = BarHeight - InnerPadding * 2;
            var fillRect = new Rect2(InnerPadding, InnerPadding, fillWidth, fillHeight);

            Color fillColor;
            if (ratio > 0.5f)
                fillColor = FillColorFull;
            else if (ratio > 0.2f)
                fillColor = FillColorMid.Lerp(FillColorFull, (ratio - 0.2f) / 0.3f);
            else
                fillColor = FillColorLow.Lerp(FillColorMid, ratio / 0.2f);

            DrawRect(fillRect, fillColor);

            // Subtle top highlight
            var highlightRect = new Rect2(InnerPadding, InnerPadding, fillWidth, fillHeight * 0.3f);
            DrawRect(highlightRect, FillHighlight);

            // Subtle bottom shadow
            var shadowRect = new Rect2(InnerPadding, InnerPadding + fillHeight * 0.8f,
                fillWidth, fillHeight * 0.2f);
            DrawRect(shadowRect, FillShadow);
        }

        // === Seconds text — thick black outline + main color ===
        int secondsLeft = Mathf.CeilToInt((float)(_duration - _elapsed));
        if (secondsLeft < 0) secondsLeft = 0;
        string text = $"{secondsLeft}s";

        Color textCol = _state == TimerState.Expired ? ExpiredTextColor : TextColor;
        var font = ThemeDB.FallbackFont;
        int fontSize = 22;
        var textSize = font.GetStringSize(text, HorizontalAlignment.Center, -1, fontSize);
        var textPos = new Vector2(
            (BarWidth - textSize.X) / 2f,
            (BarHeight + textSize.Y) / 2f - 6f
        );

        // Black outline — draw text offset in 8 directions
        for (int dx = -2; dx <= 2; dx++)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                DrawString(font, textPos + new Vector2(dx, dy), text,
                    HorizontalAlignment.Left, -1, fontSize, TextOutlineColor);
            }
        }
        // Main text on top
        DrawString(font, textPos, text, HorizontalAlignment.Left, -1, fontSize, textCol);
    }

    /// <summary>
    /// Starts or restarts the countdown timer.
    /// </summary>
    public void StartTimer()
    {
        _duration = TimerConfig.TimerDurationSecondsInt;
        _elapsed = 0;
        _autoEndTriggered = false;
        _state = TimerState.Running;
        Visible = true;
        QueueRedraw();
        MainFile.Logger.Info($"[TurnTimerBar] Timer started ({_duration}s)");
    }

    /// <summary>
    /// Stops and hides the timer.
    /// </summary>
    public void StopTimer()
    {
        if (_state == TimerState.Idle) return;
        _state = TimerState.Idle;
        _elapsed = 0;
        Visible = false;
        QueueRedraw();
        MainFile.Logger.Info("[TurnTimerBar] Timer stopped");
    }

    /// <summary>
    /// Returns true if the timer is currently running or expired (visible).
    /// </summary>
    public bool IsActive => _state != TimerState.Idle;
}
