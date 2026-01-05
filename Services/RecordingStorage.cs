using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RecPlay.Models;

namespace RecPlay.Services;

internal sealed class RecordingStorage
{
    private readonly string _recordingPath;

    public RecordingStorage()
    {
        _recordingPath = Path.Combine(AppPaths.DataDirectory, "recording.json");
    }

    public IReadOnlyList<InputEvent> Load()
    {
        try
        {
            if (!File.Exists(_recordingPath))
            {
                return new List<InputEvent>();
            }

            var json = File.ReadAllText(_recordingPath);
            var events = JsonSerializer.Deserialize<List<InputEvent>>(json);
            return events ?? new List<InputEvent>();
        }
        catch
        {
            return new List<InputEvent>();
        }
    }

    public void Save(IReadOnlyList<InputEvent> events)
    {
        Directory.CreateDirectory(AppPaths.DataDirectory);
        var json = JsonSerializer.Serialize(events, new JsonSerializerOptions
        {
            WriteIndented = true,
        });
        File.WriteAllText(_recordingPath, json);
    }
}
