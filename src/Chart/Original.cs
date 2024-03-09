using Godot;
using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace Phi.Chart.Original;

public partial class OriginalPlayer : Node2D, IChartPlayer
{
    OriginalChart _chart;
    List<OriginalJudgeLineInstance> _judgeLineInstances = [];
    AudioStreamPlayer streamPlayer = new();
    double _timeBegin;
    double _timeDelay = 0;
    bool _playingAudio = false;

    public OriginalPlayer(string _chartPath, string _musicPath=null)
    {
        _chart = OriginalLoader.Load(_chartPath);

        if (_musicPath is not null)
        {
            if (_musicPath.GetExtension().Equals("mp3", StringComparison.OrdinalIgnoreCase))
                streamPlayer.Stream = AudioLoader.LoadMP3(_musicPath);

            else if (_musicPath.GetExtension().Equals("wav", StringComparison.OrdinalIgnoreCase))
                streamPlayer.Stream = AudioLoader.LoadWAV(_musicPath);

            else if (_musicPath.GetExtension().Equals("ogg", StringComparison.OrdinalIgnoreCase))
                streamPlayer.Stream = AudioLoader.LoadOGG(_musicPath);

            else
                throw new IOException("Audio extension not supported");
            
            streamPlayer.Autoplay = false;
        }
        
    }

    public void Play()
    {
        AddChild(streamPlayer);

        foreach (var lineData in _chart.JudgeLineList)
        {
            var line = new OriginalJudgeLineInstance(lineData);
            AddChild(line);
            _judgeLineInstances.Add(line);
        }

        _timeBegin = Time.GetTicksUsec();
    }

    public void Pause()
    {

    }

    public void Stop()
    {

    }

    public override void _Process(double delta)
    {
        double timeSecs = (Time.GetTicksUsec() - _timeBegin) / 1000000.0d;
        timeSecs = Math.Max(0.0d, timeSecs - _timeDelay);

        if (timeSecs >= _chart.Offset && !_playingAudio)
        {
            _playingAudio = true;
            _timeDelay = AudioServer.GetTimeToNextMix() + AudioServer.GetOutputLatency();
            streamPlayer.Play();
        }

        foreach (var line in _judgeLineInstances)
            line.DoUpdate(timeSecs);
        
        // GD.Print($"time: {timeSecs}");
    }
}

public partial class OriginalJudgeLineInstance : Node2D
{
    Line2D _drawLine;
    OriginalJudgeLine _data;

    public OriginalJudgeLineInstance(OriginalJudgeLine data)
    {

    }

    public override void _Ready()
    {
        _drawLine = new()
        {
            Width = 5,
        };
        _drawLine.AddPoint(new(-1000, 0));
        _drawLine.AddPoint(new(1000, 0));
        AddChild(_drawLine);
        // Position = _data.JudgeLineMoveEvents[0];
    }

    public void DoUpdate(double tick)
    {

    }
}

public class OriginalLoader
{
    public static OriginalChart Load(string _chartPath)
    {
        var global_path = ProjectSettings.GlobalizePath(_chartPath);
        if (!File.Exists(global_path))
            throw new IOException("Chart File Not Found");
        
        var text = File.ReadAllText(global_path);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var res = JsonSerializer.Deserialize<OriginalChart>(text, options);

        return res;
    }
}

public class OriginalChart
{
    public int FormatVersion {get; set;}
    public float Offset {get; set;}
    // public int NumOfNotes;
    public OriginalJudgeLine[] JudgeLineList {get; set;}
}

public class OriginalJudgeLine
{
    public float Bpm {get; set;}
    public OriginalNote[] NotesAbove {get; set;}
    public OriginalNote[] NotesBelow {get; set;}
    public Dictionary<string, float>[] SpeedEvents {get; set;}
    public Dictionary<string, float>[] JudgeLineDisappearEvents {get; set;}
    public Dictionary<string, float>[] JudgeLineMoveEvents {get; set;}
    public Dictionary<string, float>[] JudgeLineRotateEvents {get; set;}
}

public class OriginalJudgeLineEvent
{
    public int StartTime {get; set;}
    public int EndTime {get; set;}
    public float Start {get; set;}
    public float End {get; set;}
    public float? Start2 {get; set;}
    public float? End2 {get; set;}
}

public class OriginalNote
{
    public NoteType Type {get; set;}
    public int Time {get; set;}
    public float PositionX {get; set;}
    public float HoldTime {get; set;} // Should be int
    public float Speed {get; set;}
    public double FloorPosition {get; set;}
}

public enum NoteType
{
    Tap = 1,
    Drag,
    Hold,
    Flick,
}