using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace Insiyab.Avalonia.Gallery.Droid;

[Application]
public class GalleryApplication : AvaloniaAndroidApplication<App>
{
    protected GalleryApplication(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    {
    }
}

// Rotation, size and night-mode changes reach the running view instead of restarting the activity.
[Activity(Label = "انسياب", Theme = "@style/InsiyabGallery", MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.UiMode | ConfigChanges.Density,
    WindowSoftInputMode = Android.Views.SoftInput.AdjustResize)]
public class MainActivity : AvaloniaMainActivity
{
}
