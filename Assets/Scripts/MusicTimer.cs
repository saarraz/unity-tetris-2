#nullable enable
using UnityEngine;
using System.Collections;
using System;

// Ticks according to the Tetris theme
public class MusicTimer : MonoBehaviour
{
    public const float OriginalTempo = 0.857142f;

    public const float OriginalLength = 68.62367f;

    public readonly float[] OriginalBeatTimes =
    {
        0f,
        0.428571f,
        0.6428564999999999f,
        0.857142f,
        1.0714275f,
        1.17857025f,
        1.2857129999999999f,
        1.4999984999999998f,
        1.7142839999999997f,
        2.1428549999999995f,
        2.3571404999999994f,
        2.5714259999999993f,
        2.999996999999999f,
        3.214282499999999f,
        3.428567999999999f,
        4.071424499999999f,
        4.285709999999999f,
        4.714280999999999f,
        5.142851999999999f,
        5.5714229999999985f,
        5.999993999999998f,
        6.857135999999998f,
        7.499992499999998f,
        7.7142779999999975f,
        8.142848999999998f,
        8.357134499999999f,
        8.57142f,
        9.2142765f,
        9.428562000000001f,
        9.857133000000001f,
        10.071418500000002f,
        10.285704000000003f,
        10.714275000000002f,
        10.928560500000003f,
        11.142846000000004f,
        11.571417000000004f,
        11.999988000000004f,
        12.428559000000003f,
        12.857130000000003f,
        13.714272000000003f,
        14.142843000000003f,
        14.357128500000004f,
        14.571414000000004f,
        14.785699500000005f,
        14.892842250000005f,
        14.999985000000004f,
        15.214270500000005f,
        15.428556000000006f,
        15.857127000000006f,
        16.071412500000005f,
        16.285698000000004f,
        16.714269000000005f,
        16.928554500000004f,
        17.142840000000003f,
        17.785696500000004f,
        17.999982000000003f,
        18.428553000000004f,
        18.857124000000006f,
        19.285695000000008f,
        19.71426600000001f,
        20.57140800000001f,
        21.21426450000001f,
        21.42855000000001f,
        21.85712100000001f,
        22.07140650000001f,
        22.285692000000008f,
        22.92854850000001f,
        23.142834000000008f,
        23.57140500000001f,
        23.78569050000001f,
        23.999976000000007f,
        24.42854700000001f,
        24.642832500000008f,
        24.857118000000007f,
        25.28568900000001f,
        25.71426000000001f,
        26.14283100000001f,
        26.571402000000013f,
        26.999973000000015f,
        28.285686000000016f,
        29.142828000000016f,
        29.999970000000015f,
        30.857112000000015f,
        31.714254000000015f,
        32.571396000000014f,
        33.42853800000002f,
        33.857109000000015f,
        35.14282200000002f,
        35.99996400000002f,
        36.85710600000002f,
        37.714248000000026f,
        38.142819000000024f,
        38.57139000000002f,
        39.428532000000025f,
        40.28567400000003f,
        41.57138700000003f,
        41.78567250000003f,
        41.999958000000035f,
        42.21424350000004f,
        42.32138625000004f,
        42.42852900000004f,
        42.64281450000004f,
        42.857100000000045f,
        43.28567100000004f,
        43.499956500000046f,
        43.71424200000005f,
        44.14281300000005f,
        44.35709850000005f,
        44.57138400000005f,
        45.21424050000005f,
        45.428526000000055f,
        45.85709700000005f,
        46.28566800000005f,
        46.71423900000005f,
        47.14281000000005f,
        47.99995200000005f,
        48.64280850000005f,
        48.85709400000005f,
        49.28566500000005f,
        49.499950500000054f,
        49.71423600000006f,
        50.35709250000006f,
        50.57137800000006f,
        50.99994900000006f,
        51.21423450000006f,
        51.42852000000006f,
        51.85709100000006f,
        52.07137650000006f,
        52.285662000000066f,
        52.714233000000064f,
        53.14280400000006f,
        53.57137500000006f,
        53.99994600000006f,
        54.428517000000056f,
        55.28565900000005f,
        55.499944500000055f,
        55.71423000000006f,
        55.92851550000006f,
        56.03565825000006f,
        56.14280100000006f,
        56.357086500000065f,
        56.57137200000007f,
        56.999943000000066f,
        57.21422850000007f,
        57.42851400000007f,
        57.85708500000007f,
        58.07137050000007f,
        58.285656000000074f,
        58.928512500000075f,
        59.14279800000008f,
        59.571369000000075f,
        59.99994000000007f,
        60.42851100000007f,
        60.85708200000007f,
        61.71422400000007f,
        62.35708050000007f,
        62.571366000000076f,
        62.999937000000074f,
        63.214222500000076f,
        63.42850800000008f,
        64.07136450000007f,
        64.28565000000008f,
        64.71422100000008f,
        64.92850650000008f,
        65.14279200000009f,
        65.57136300000009f,
        65.7856485000001f,
        65.9999340000001f,
        66.4285050000001f,
        66.8570760000001f,
        67.28564700000011f,
        67.71421800000012f
    };
    
    public AudioSource? AudioSource;

    public bool Paused { get; private set; }

    public float Speed { get => OriginalLength / AudioSource!.clip.length; }

    public int Ticks { get; private set; }

    public float Tempo { get => OriginalTempo / Speed; }

    public float Offset = 0f;

    public void Pause()
    {
        Paused = true;
    }

    public void Resume()
    {
        Paused = false;
    }

    public struct Beat
    {
        public float Start;
        public float End;
        public int Index;
    };

    private float GetBeatTime(int index)
    {
        return OriginalBeatTimes[index] / Speed + Offset;
    }

    public Beat GetBeatAtTime(float playbackTime)
    {
        float currentBeatTime = 0;
        float nextBeatTime = 0;
        var audioSpeed = Speed;
        int i;
        for (i = 0; i < OriginalBeatTimes.Length; ++i)
        {
            currentBeatTime = GetBeatTime(i);
            if (i == OriginalBeatTimes.Length - 1)
            {
                nextBeatTime = OriginalLength / audioSpeed + Offset;
            }
            else
            {
                nextBeatTime = GetBeatTime(i + 1);
            }
            if (playbackTime >= currentBeatTime && playbackTime < nextBeatTime)
            {
                break;
            }
        }
        return new Beat() { Start = currentBeatTime, End = nextBeatTime, Index = i };
    }

    public float TimeToNextBeat()
    {
        return TimeToFutureBeat(offset: +1);
    }

    public float TimeToNearestBeat(float seconds)
    {
        return GetBeatAtTime(playbackTime: AudioSource!.time + seconds).End - AudioSource!.time;
    }

    public float TimeToFutureBeat(int offset)
    {
        float playbackTime = AudioSource!.time;
        Beat currentBeat = GetBeatAtTime(playbackTime);
        float beatPlaybackTime = GetBeatTime((currentBeat.Index + offset) % OriginalBeatTimes.Length);
        if (beatPlaybackTime < playbackTime)
        {
            // Wraparound
            return AudioSource!.clip.length - playbackTime + beatPlaybackTime;
        }
        return beatPlaybackTime - playbackTime;
    }

    public IEnumerator WaitUntilNextBeat()
    {
        yield return new WaitForSeconds(TimeToNextBeat());
    }

    public bool IsOnBeat()
    {
        float playbackTime = AudioSource!.time;
        Debug.Log($"{playbackTime - GetBeatAtTime(playbackTime).Start}");
        return playbackTime - GetBeatAtTime(playbackTime).Start < 0.1f;
    }

    private Beat? _lastBeat = null;

    public void Update()
    {
        if (Paused)
        {
            Ticks = 0;
            return;
        }

        if (_lastBeat == null)
        {
            _lastBeat = GetBeatAtTime(AudioSource!.time);
            Ticks = 0;
            return;
        }
        Beat currentBeat = GetBeatAtTime(AudioSource!.time);
        int currentBeatIndex = currentBeat.Index;
        int lastBeatIndex = _lastBeat.Value.Index;
        if (currentBeatIndex < lastBeatIndex)
        {
            // Wrap around the end of the beat array.
            Ticks = (OriginalBeatTimes.Length - lastBeatIndex) + currentBeatIndex;
        }
        else
        {
            Ticks = currentBeatIndex - lastBeatIndex;
        }
        _lastBeat = currentBeat;
    }
}
