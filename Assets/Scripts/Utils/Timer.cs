using UnityEngine;

interface ITimer
{

    // Returns the no. of ticks since the last time OnUpdate was called.
    public int OnUpdate();
    public void Pause();
    public void Resume();
}

public class Timer : ITimer
{
    public float LastTickTime { get; private set; }
    public float Frequency
    {
        get => _frequency;

        set
        {
            _remainingDuration -= _frequency - value;
            _frequency = value;
        }
    }
    public bool Running { get; private set; }
    public bool Paused { get; private set; }

    private float _referenceTime;
    private float _remainingDuration;
    private float _frequency;


    public void Start()
    {
        Reset();
        Running = true;
    }

    public void Pause()
    {
        if (Paused) {
            return;
        }
        Paused = true;
        if (Running)
        {
            _remainingDuration -= Time.time - _referenceTime;
        }
    }

    public void Resume()
    {
        if (!Paused)
        {
            return;
        }
        Paused = false;
        if (Running)
        {
            _referenceTime = Time.time;
        }
    }

    public Timer(float frequency = 1f, bool start = true)
    {
        Frequency = frequency;
        Running = start;
    }

    public int OnUpdate()
    {
        if (!Running || Paused)
        {
            return 0;
        }
        if ((Time.time - _referenceTime) < _remainingDuration)
        {
            return 0;
        }
        LastTickTime = Time.time;
        Reset();
        return 1;
    }

    public void Reset(bool stop = false)
    {
        _referenceTime = Time.time;
        _remainingDuration = Frequency;
        Running = !stop;
    }
}
