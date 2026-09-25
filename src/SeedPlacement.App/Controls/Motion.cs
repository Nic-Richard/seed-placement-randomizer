using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;

namespace SeedPlacement.App.Controls;

public static class Ease
{
    public static double OutCubic(double t) => 1 - Math.Pow(1 - t, 3);

    public static double InOutCubic(double t) => t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;

    public static double Linear(double t) => t;
}

/// <summary>
/// Frame-synced tweens. Cancelling a tween jumps it to its final frame, so interrupting an animation
/// always leaves the view in its finished state.
/// </summary>
public static class Motion
{
    public static Task Tween(Visual host, TimeSpan duration, Action<double> step, CancellationToken cancel,
        Func<double, double>? ease = null, TimeSpan delay = default)
    {
        ease ??= Ease.OutCubic;
        var top = TopLevel.GetTopLevel(host);
        if (top is null || duration <= TimeSpan.Zero || ReducedMotion.IsRequested)
        {
            step(1);
            return Task.CompletedTask;
        }

        // Apply the starting state now, so nothing shows its old state while waiting for a frame or a delay.
        step(ease(0));
        var done = new TaskCompletionSource();
        var clock = Stopwatch.StartNew();
        var registration = cancel.Register(() =>
        {
            if (done.Task.IsCompleted) return;
            step(1);
            done.TrySetResult();
        });

        // Frame timestamps are not guaranteed to be wall-clock time on every platform, so tweens keep
        // their own clock and use frame callbacks only for pacing.
        void Frame(TimeSpan _)
        {
            if (done.Task.IsCompleted) return;
            var elapsed = clock.Elapsed - delay;
            var t = Math.Clamp(elapsed / duration, 0, 1);
            if (elapsed >= TimeSpan.Zero) step(ease(t));
            if (t >= 1)
            {
                done.TrySetResult();
                registration.Dispose();
                return;
            }
            top.RequestAnimationFrame(Frame);
        }

        top.RequestAnimationFrame(Frame);
        return done.Task;
    }

    public static async Task Delay(TimeSpan delay, CancellationToken cancel)
    {
        if (ReducedMotion.IsRequested) return;
        try
        {
            await Task.Delay(delay, cancel);
        }
        catch (OperationCanceledException)
        {
        }
    }
}
