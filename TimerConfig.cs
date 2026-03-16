using System.Reflection;
using BaseLib.Config;
using Godot;

namespace STS2TurnTimer;

public class TimerConfig : ModConfig
{
    /// <summary>
    /// How long the countdown timer lasts in seconds.
    /// Default: 30.
    /// </summary>
    public static int TimerDurationSeconds { get; set; } = 30;

    /// <summary>
    /// When true, automatically ends the local player's turn when the timer expires.
    /// Default: true.
    /// </summary>
    public static bool AutoEndTurn { get; set; } = true;

    /// <summary>
    /// When true, the timer starts at the beginning of every player turn.
    /// When false (default), the timer only starts when all other players
    /// are waiting for the local player to end their turn.
    /// </summary>
    public static bool StartTimerFromTurnStart { get; set; } = false;

    public TimerConfig() : base("STS2TurnTimer.cfg") { }

    public override void SetupConfigUI(Control optionContainer)
    {
        var options = new VBoxContainer();
        options.Size = optionContainer.Size;
        options.AddThemeConstantOverride("separation", 8);
        optionContainer.AddChild(options);

        foreach (var property in ConfigProperties)
        {
            if (property.PropertyType == typeof(bool))
            {
                MakeToggleOption(options, property);
            }
            else if (property.PropertyType == typeof(int))
            {
                MakePaginatorOption(options, property);
            }
        }
    }

    private void MakePaginatorOption(Control parent, PropertyInfo property)
    {
        var (min, max, step) = property.Name switch
        {
            nameof(TimerDurationSeconds) => (10, 120, 5),
            _ => (1, 10, 1)
        };

        var container = MakeOptionContainer(parent, "Paginator_" + property.Name, property.Name);

        var paginatorBox = new HBoxContainer();
        paginatorBox.AddThemeConstantOverride("separation", 8);
        paginatorBox.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        paginatorBox.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;

        int currentVal = Math.Clamp((int)(property.GetValue(null) ?? min), min, max);

        var valueLabel = new Label();
        valueLabel.Text = currentVal.ToString();
        valueLabel.CustomMinimumSize = new Vector2(40, 0);
        valueLabel.HorizontalAlignment = HorizontalAlignment.Center;
        valueLabel.AddThemeColorOverride("font_color", new Color("E8DCBE"));
        valueLabel.AddThemeFontSizeOverride("font_size", 20);

        var leftBtn = new Button();
        leftBtn.Text = "<";
        leftBtn.CustomMinimumSize = new Vector2(36, 36);
        leftBtn.AddThemeColorOverride("font_color", new Color("EFC851"));
        leftBtn.AddThemeFontSizeOverride("font_size", 20);
        leftBtn.Flat = true;

        var rightBtn = new Button();
        rightBtn.Text = ">";
        rightBtn.CustomMinimumSize = new Vector2(36, 36);
        rightBtn.AddThemeColorOverride("font_color", new Color("EFC851"));
        rightBtn.AddThemeFontSizeOverride("font_size", 20);
        rightBtn.Flat = true;

        leftBtn.Pressed += () =>
        {
            currentVal -= step;
            if (currentVal < min) currentVal = max;
            valueLabel.Text = currentVal.ToString();
            property.SetValue(null, currentVal);
            Changed();
        };

        rightBtn.Pressed += () =>
        {
            currentVal += step;
            if (currentVal > max) currentVal = min;
            valueLabel.Text = currentVal.ToString();
            property.SetValue(null, currentVal);
            Changed();
        };

        paginatorBox.AddChild(leftBtn);
        paginatorBox.AddChild(valueLabel);
        paginatorBox.AddChild(rightBtn);
        container.AddChild(paginatorBox);
    }
}
