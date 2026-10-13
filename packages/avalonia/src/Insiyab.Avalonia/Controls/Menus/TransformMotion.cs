using Avalonia.Animation;
using Avalonia.Media;
using Avalonia.Media.Transformation;

namespace Insiyab.Avalonia.Controls;

// Lets a keyframe animation move RenderTransform between two CSS-like transforms, as the web's keyframes do.
internal sealed class TransformOperationsAnimator : InterpolatingAnimator<ITransform?>
{
    private static bool s_registered;

    public static void Ensure()
    {
        if (s_registered) return;
        s_registered = true;
        Animation.RegisterCustomAnimator<ITransform?, TransformOperationsAnimator>();
    }

    public override ITransform? Interpolate(double progress, ITransform? oldValue, ITransform? newValue) =>
        TransformOperations.Interpolate(oldValue as TransformOperations ?? TransformOperations.Identity,
            newValue as TransformOperations ?? TransformOperations.Identity, progress);
}
