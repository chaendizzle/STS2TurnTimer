using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace STS2TurnTimer;

public static class TimerConfig
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

    private static string? _configPath;

    public static void Load()
    {
        try
        {
            var dllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (dllDir == null) return;
            _configPath = Path.Combine(dllDir, "timer_config.json");

            if (!File.Exists(_configPath))
            {
                MainFile.Logger.Info($"[TimerConfig] No config file at {_configPath}, using defaults. Creating default config.");
                Save();
                return;
            }

            var json = File.ReadAllText(_configPath);
            var data = JsonSerializer.Deserialize<ConfigData>(json);
            if (data == null) return;

            TimerDurationSeconds = Math.Clamp(data.TimerDurationSeconds, 10, 120);
            AutoEndTurn = data.AutoEndTurn;
            StartTimerFromTurnStart = data.StartTimerFromTurnStart;

            MainFile.Logger.Info($"[TimerConfig] Loaded: TimerDurationSeconds={TimerDurationSeconds}, AutoEndTurn={AutoEndTurn}, StartTimerFromTurnStart={StartTimerFromTurnStart}");
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"[TimerConfig] Failed to load config: {e.Message}");
        }
    }

    public static void Save()
    {
        try
        {
            if (_configPath == null) return;

            var data = new ConfigData
            {
                TimerDurationSeconds = TimerDurationSeconds,
                AutoEndTurn = AutoEndTurn,
                StartTimerFromTurnStart = StartTimerFromTurnStart,
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(_configPath, json);
            MainFile.Logger.Info($"[TimerConfig] Saved config to {_configPath}");
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"[TimerConfig] Failed to save config: {e.Message}");
        }
    }

    private class ConfigData
    {
        [JsonPropertyName("timerDurationSeconds")]
        public int TimerDurationSeconds { get; set; } = 30;

        [JsonPropertyName("autoEndTurn")]
        public bool AutoEndTurn { get; set; } = true;

        [JsonPropertyName("startTimerFromTurnStart")]
        public bool StartTimerFromTurnStart { get; set; } = false;
    }
}
