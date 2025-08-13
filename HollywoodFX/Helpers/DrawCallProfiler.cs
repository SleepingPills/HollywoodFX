using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace HollywoodFX.Helpers;

public class DrawCallProfiler : MonoBehaviour
{
    private long _drawCallsMax;
    private double _drawCallsAvg;
    private List<ProfilerRecorderSample> _samples;
    private ProfilerRecorder _recorder;

    private void OnEnable()
    {
        _samples = new List<ProfilerRecorderSample>(100);
        _recorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 100);
    }

    private void OnDisable()
    {
        _recorder.Dispose();
    }

    private void Update()
    {
        _drawCallsMax = Math.Max(_recorder.CurrentValue, _drawCallsMax);

        _recorder.CopyTo(_samples);
        
        double sum = 0f;
        for(var i = 0; i < _samples.Count; i++)
            sum += _samples[i].Value;
        
        _drawCallsAvg = sum / _samples.Count;
    }

    private void OnGUI()
    {
        GUI.TextArea(new Rect(10, 30, 250, 50), $"{_recorder.CurrentValue}/{_drawCallsAvg:f2}/{_drawCallsMax}");
    }
}