using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lucide.Avalonia;

namespace Avayomi.ViewModels.Pages;

public sealed partial class AnimePlayerPageViewModel : PageViewModel
{
    public override int Index => 3;
    public override LucideIconKind IconKind => LucideIconKind.Play;

    [ObservableProperty]
    public partial string TestString { get; set; } = string.Empty;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) { }
    }

    public const string Source =
        @"E:\Media\[Judas] Jujutsu Kaisen - Movie 01 - Jujutsu Kaisen 0 [BD 1080p][HEVC x265 10bit][Dual-Audio][Multi-Subs].mkv";

    [RelayCommand]
    private async Task PlayAsync() { }

    [RelayCommand]
    private void Stop() { }
}
