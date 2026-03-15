using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace STS2TurnTimer;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    private const string ModId = "STS2TurnTimer";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Logger.Info("STS2TurnTimer: Turn Timer mod initializing...");

        TimerConfig.Load();

        Harmony harmony = new(ModId);
        harmony.PatchAll();

        Logger.Info("STS2TurnTimer: Harmony patches applied.");
    }
}
