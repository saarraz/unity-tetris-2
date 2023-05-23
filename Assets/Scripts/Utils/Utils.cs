#nullable enable

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System;
using Pixelplacement;

public static class Utils
{
    public static IEnumerator WhilePlaying(this Animation animation)
    {
        do
        {
            yield return null;
        } while (animation.isPlaying);
    }

    public static IEnumerator PlayUntilEnd(this Animation animation, string animationName, float? duration = null)
    {
        yield return animation.PlayAndWaitForStart(animationName, duration);
        yield return animation.WhilePlaying();
    }

    private static AnimationState? FindAnimationState(this Animation animation, string animationName)
    {
        return (from AnimationState animationState in animation where animationState.clip.name == animationName && animationState.enabled select animationState).FirstOrDefault();
    }

    public static IEnumerator PlayAndWaitForStart(this Animation animation, string animationName, float? duration = null, Promise<AnimationState>? animationState = null)
    {
        animation.PlayQueued(animationName);
        if (duration == null && animationState == null)
        {
            yield break;
        }
        // Wait for animation to start
        AnimationState? state = null;
        yield return new WaitUntil(() =>
        {
            state = animation.FindAnimationState(animationName);
            return state != null;
        });
        if (duration != null)
        {
            state!.speed = state!.clip.length / duration.Value;
        }
        animationState?.Resolve(state!);
    }


    public static IEnumerator PlayAndWaitForEvent(this Animation animation, string animationName, AnimationEvent animationEvent, float? duration = null)
    {
        var state = new Promise<AnimationState>();
        yield return PlayAndWaitForStart(animation, animationName, animationState:state);

        // Wait until event has fired.
        yield return new WaitUntil(() => state.Value.time >= animationEvent.time);
    }

    public static IEnumerator PlayAndWaitForEvent(this Animation animation, string animationName, int eventIndex, float? durationUntilEvent = null)
    {
        AnimationClip clip = animation.GetClip(animationName);
        AnimationEvent animationEvent = clip.events[0];
        yield return PlayAndWaitForEvent(animation, animationName, animationEvent, durationUntilEvent * (clip.length / animationEvent.time));
    }

    public static IEnumerator Animate(float duration, params Action<float>[] renderFrames)
    {
        yield return Animate(duration, null, null, renderFrames);
    }

    public static IEnumerator Animate(float duration, AnimationCurve? curve, params Action<float>[] renderFrames)
    {
        yield return Animate(duration, curve, null, renderFrames);
    }

    public static IEnumerator Animate(float duration, Func<bool> shouldCancel, params Action<float>[] renderFrames)
    {
        yield return Animate(duration, null, shouldCancel, renderFrames);
    }

    public static IEnumerator Animate(float duration, AnimationCurve? curve, Func<bool>? shouldCancel, params Action<float>[] renderFrames)
    {
        var startTime = Time.fixedTime;
        for (var elapsed = 0f; elapsed <= duration; elapsed = Math.Min(duration, Time.fixedTime - startTime))
        {
            var progress = shouldCancel?.Invoke() == true ? 1f : elapsed / duration;
            if (curve != null)
            {
                progress = curve.Evaluate(progress);
            }
            foreach (var renderFrame in renderFrames)
            {
                renderFrame(progress);
            }
            yield return new WaitForFixedUpdate();
            if (progress == 1)
            {
                break;
            }
        }
    }

    public class Promise<T>
    {
        private List<Action<T>>? _then;
        public bool Resolved { get; private set; }
#pragma warning disable CS8653 // A default expression introduces a null value for a type parameter.
        public T _value = default;
#pragma warning restore CS8653 // A default expression introduces a null value for a type parameter.
        public T Value
        {
            get {
                Debug.Assert(Resolved);
                return _value;
            }
        }


        public void Resolve(T value)
        {
            Debug.Assert(!Resolved);
            Resolved = true;
            _value = value;
            if (_then == null)
            {
                return;
            }
            foreach (var thenHandler in _then)
            {
                thenHandler(value);
            }
            _then.Clear();
        }

        public Promise<T> Then(Action<T> then)
        {
            if (Resolved)
            {
                then(_value);
                return this;
            }
            if (_then == null)
            {
                _then = new List<Action<T>>();
            }
            _then!.Add(then);
            return this;
        }
    }
}