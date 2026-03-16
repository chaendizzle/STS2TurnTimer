using BaseLib.Config;
using Godot;

namespace STS2TurnTimer;

public enum TimerDuration
{
    Sec10 = 10, Sec15 = 15, Sec20 = 20, Sec25 = 25, Sec30 = 30,
    Sec35 = 35, Sec40 = 40, Sec45 = 45, Sec50 = 50, Sec55 = 55,
    Sec60 = 60, Sec65 = 65, Sec70 = 70, Sec75 = 75, Sec80 = 80,
    Sec85 = 85, Sec90 = 90, Sec95 = 95, Sec100 = 100, Sec105 = 105,
    Sec110 = 110, Sec115 = 115, Sec120 = 120
}

public class TimerConfig : SimpleModConfig
{
    public static TimerDuration TimerDurationSeconds { get; set; } = TimerDuration.Sec30;
    public static bool AutoEndTurn { get; set; } = true;
    public static bool StartTimerFromTurnStart { get; set; } = false;

    public TimerConfig() { }

    public static int TimerDurationSecondsInt => (int)TimerDurationSeconds;

    public override void SetupConfigUI(Control optionContainer)
    {
        foreach (var child in optionContainer.GetChildren())
            child.Free();

        base.SetupConfigUI(optionContainer);
    }
}
