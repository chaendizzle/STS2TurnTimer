using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;

namespace STS2TurnTimer.Patches;

/// <summary>
/// Injects turn timer settings (timer duration paginator and always-show checkbox)
/// into the mod info panel when STS2TurnTimer is selected in the Modding Screen.
/// </summary>
[HarmonyPatch(typeof(NModInfoContainer), nameof(NModInfoContainer.Fill))]
public static class ModSettingsPatch
{
    private const string SettingsContainerName = "STS2TurnTimerSettings";

    [HarmonyPostfix]
    public static void Postfix(NModInfoContainer __instance, Mod mod)
    {
        // Remove any previously injected settings panel
        var existing = __instance.GetNodeOrNull(SettingsContainerName);
        if (existing != null)
            existing.QueueFree();

        // Only inject for our mod
        if (mod.manifest?.id != "STS2TurnTimer") return;

        try
        {
            InjectSettings(__instance);
        }
        catch (System.Exception e)
        {
            MainFile.Logger.Error($"[ModSettingsPatch] Failed to inject settings: {e}");
        }
    }

    private static void InjectSettings(NModInfoContainer container)
    {
        var descriptionNode = container.GetNodeOrNull<RichTextLabel>("ModDescription");
        if (descriptionNode == null)
        {
            MainFile.Logger.Error("[ModSettingsPatch] ModDescription node not found");
            return;
        }

        var descText = descriptionNode.Text;
        var descLeft = descriptionNode.OffsetLeft;
        var descTop = descriptionNode.OffsetTop;
        var descRight = descriptionNode.OffsetRight;
        var descBottom = container.OffsetBottom - container.OffsetTop - 20;

        descriptionNode.Visible = false;

        var scrollContainer = new ScrollContainer();
        scrollContainer.Name = SettingsContainerName;
        scrollContainer.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        scrollContainer.OffsetLeft = descLeft;
        scrollContainer.OffsetTop = descTop;
        scrollContainer.OffsetRight = descRight;
        scrollContainer.OffsetBottom = descBottom;
        scrollContainer.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        scrollContainer.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());

        var contentBox = new VBoxContainer();
        contentBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        contentBox.AddThemeConstantOverride("separation", 8);

        // Recreate description
        var cleanedText = System.Text.RegularExpressions.Regex.Replace(
            descText, @"\[/?\w+\]", "");
        var descLabel = new RichTextLabel();
        descLabel.BbcodeEnabled = false;
        descLabel.Text = cleanedText;
        descLabel.FitContent = true;
        descLabel.ScrollActive = false;
        descLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        descLabel.AddThemeColorOverride("default_color", new Color("E8DCBE"));
        descLabel.AddThemeFontSizeOverride("normal_font_size", 20);
        descLabel.AddThemeFontSizeOverride("bold_font_size", 20);
        contentBox.AddChild(descLabel);

        contentBox.AddChild(CreateDivider());

        // Settings header
        var headerLabel = new Label();
        headerLabel.Text = "Mod Settings";
        headerLabel.HorizontalAlignment = HorizontalAlignment.Left;
        headerLabel.AddThemeColorOverride("font_color", new Color("EFC851"));
        headerLabel.AddThemeFontSizeOverride("font_size", 22);
        contentBox.AddChild(headerLabel);

        // Auto End Turn
        contentBox.AddChild(CreateSettingsRow(
            "Auto End Turn",
            "Automatically end your turn when the timer expires",
            TimerConfig.AutoEndTurn,
            (toggled) =>
            {
                TimerConfig.AutoEndTurn = toggled;
                TimerConfig.Save();
                MainFile.Logger.Info($"[ModSettings] AutoEndTurn set to {toggled}");
            }));

        contentBox.AddChild(CreateDivider());

        // Timer Duration (10-120 seconds, default 45)
        contentBox.AddChild(CreatePaginatorRow(
            "Timer Duration (seconds)",
            10, 120, TimerConfig.TimerDurationSeconds, 5,
            (value) =>
            {
                TimerConfig.TimerDurationSeconds = value;
                TimerConfig.Save();
                MainFile.Logger.Info($"[ModSettings] TimerDurationSeconds set to {value}");
            }));

        contentBox.AddChild(CreateDivider());

        // Start Timer From Turn Start
        contentBox.AddChild(CreateSettingsRow(
            "Start Timer From Turn Start",
            "Start the timer at the beginning of each turn instead of when others are waiting",
            TimerConfig.StartTimerFromTurnStart,
            (toggled) =>
            {
                TimerConfig.StartTimerFromTurnStart = toggled;
                TimerConfig.Save();
                MainFile.Logger.Info($"[ModSettings] StartTimerFromTurnStart set to {toggled}");
            }));

        scrollContainer.AddChild(contentBox);
        container.AddChild(scrollContainer);
    }

    private static ColorRect CreateDivider()
    {
        var divider = new ColorRect();
        divider.CustomMinimumSize = new Vector2(0, 2);
        divider.Color = new Color(0.91f, 0.86f, 0.75f, 0.25f);
        divider.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        return divider;
    }

    private static MarginContainer CreateSettingsRow(string label, string description, bool initialValue, System.Action<bool> onToggled)
    {
        var margin = new MarginContainer();
        margin.CustomMinimumSize = new Vector2(0, 56);
        margin.AddThemeConstantOverride("margin_left", 4);
        margin.AddThemeConstantOverride("margin_right", 4);
        margin.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        var hbox = new HBoxContainer();
        hbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        hbox.AddThemeConstantOverride("separation", 12);
        hbox.Alignment = BoxContainer.AlignmentMode.Center;

        var textColumn = new VBoxContainer();
        textColumn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        textColumn.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        textColumn.AddThemeConstantOverride("separation", 2);

        var nameLabel = new Label();
        nameLabel.Text = label;
        nameLabel.AddThemeColorOverride("font_color", new Color("E8DCBE"));
        nameLabel.AddThemeFontSizeOverride("font_size", 18);
        textColumn.AddChild(nameLabel);

        var descLabel = new Label();
        descLabel.Text = description;
        descLabel.AddThemeColorOverride("font_color", new Color("E8DCBE80"));
        descLabel.AddThemeFontSizeOverride("font_size", 14);
        descLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        textColumn.AddChild(descLabel);

        hbox.AddChild(textColumn);

        var tickboxWrapper = CreateGameTickbox(initialValue, onToggled);
        hbox.AddChild(tickboxWrapper);

        margin.AddChild(hbox);
        return margin;
    }

    private static MarginContainer CreatePaginatorRow(string label, int min, int max, int currentValue, int step, System.Action<int> onValueChanged)
    {
        var margin = new MarginContainer();
        margin.CustomMinimumSize = new Vector2(0, 56);
        margin.AddThemeConstantOverride("margin_left", 4);
        margin.AddThemeConstantOverride("margin_right", 4);
        margin.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        var hbox = new HBoxContainer();
        hbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        hbox.AddThemeConstantOverride("separation", 12);
        hbox.Alignment = BoxContainer.AlignmentMode.Center;

        var nameLabel = new Label();
        nameLabel.Text = label;
        nameLabel.AddThemeColorOverride("font_color", new Color("E8DCBE"));
        nameLabel.AddThemeFontSizeOverride("font_size", 18);
        nameLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        nameLabel.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        hbox.AddChild(nameLabel);

        var paginatorBox = new HBoxContainer();
        paginatorBox.AddThemeConstantOverride("separation", 8);
        paginatorBox.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;

        int currentVal = System.Math.Clamp(currentValue, min, max);

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
            onValueChanged(currentVal);
        };

        rightBtn.Pressed += () =>
        {
            currentVal += step;
            if (currentVal > max) currentVal = min;
            valueLabel.Text = currentVal.ToString();
            onValueChanged(currentVal);
        };

        paginatorBox.AddChild(leftBtn);
        paginatorBox.AddChild(valueLabel);
        paginatorBox.AddChild(rightBtn);
        hbox.AddChild(paginatorBox);

        margin.AddChild(hbox);
        return margin;
    }

    private static Control CreateGameTickbox(bool initialValue, System.Action<bool> onToggled)
    {
        var tickboxScene = PreloadManager.Cache.GetScene("res://scenes/ui/tickbox.tscn");
        var tickboxVisuals = tickboxScene.Instantiate<Control>();

        var tickedImage = tickboxVisuals.GetNode<Control>("Ticked");
        var notTickedImage = tickboxVisuals.GetNode<Control>("NotTicked");

        bool isTicked = initialValue;
        tickedImage.Visible = isTicked;
        notTickedImage.Visible = !isTicked;

        var button = new Button();
        button.CustomMinimumSize = new Vector2(64, 64);
        button.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        button.Flat = true;
        button.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
        button.AddThemeStyleboxOverride("hover", new StyleBoxEmpty());
        button.AddThemeStyleboxOverride("pressed", new StyleBoxEmpty());
        button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());

        button.AddChild(tickboxVisuals);

        button.Pressed += () =>
        {
            isTicked = !isTicked;
            tickedImage.Visible = isTicked;
            notTickedImage.Visible = !isTicked;
            onToggled(isTicked);
        };

        return button;
    }
}
