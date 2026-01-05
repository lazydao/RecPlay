using System;
using System.IO;
using System.Text.Json;
using RecPlay.Models;

namespace RecPlay.Services;

internal sealed class SettingsService
{
    private readonly string _settingsPath;

    public SettingsService()
    {
        _settingsPath = Path.Combine(AppPaths.DataDirectory, "settings.json");
    }

    public UserSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return new UserSettings();
            }

            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<UserSettings>(json);
            return settings ?? new UserSettings();
        }
        catch
        {
            return new UserSettings();
        }
    }

    public void Save(UserSettings settings)
    {
        Directory.CreateDirectory(AppPaths.DataDirectory);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true,
        });
        File.WriteAllText(_settingsPath, json);
    }
}
