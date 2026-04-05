using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using Avalonia.Controls;
using Avayomi.Core.Providers.Anime;
using CommunityToolkit.Mvvm.Input;
using Lucide.Avalonia;

namespace Avayomi.ViewModels.Pages;

public sealed partial class AnimePlayerPageViewModel : PageViewModel
{
    private readonly Dictionary<string, IAnimeProvider> _animeProviders;

    public AnimePlayerPageViewModel(IEnumerable<IAnimeProvider> animeProviders)
    {
        _animeProviders = animeProviders.ToDictionary(x => x.Name);
    }

    public override int Index => 3;
    public override LucideIconKind IconKind => LucideIconKind.Play;

    public override void OnLoaded()
    {
        base.OnLoaded();

        StartAsync().SafeFireAndForget();
    }

    public override void OnUnloaded()
    {
        base.OnUnloaded();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) { }
    }

    public const string Source =
        @"E:\Media\[Judas] Jujutsu Kaisen - Movie 01 - Jujutsu Kaisen 0 [BD 1080p][HEVC x265 10bit][Dual-Audio][Multi-Subs].mkv";

    private async Task StartAsync()
    {
        // if (MpvPlayer is null)
        // {
        //     Logger.LogInformation("MpvPlayer is null");
        //     return;
        // }
        //
        // await MpvPlayer.PlayFile(new MediaItem { FileName = source }, true);
        // MpvPlayer.SetProperty("sid", 1);
        // var animeProvider = _animeProviders.First(x => x.Key == "AniKai").Value;
        // var search = await animeProvider.SearchAsync("Love is war");
        // var anime = search.First();
        // var episodes = await animeProvider.GetEpisodesAsync(anime.Id);
        // var episode = episodes.First();
        // var videoServers = await animeProvider.GetVideoServersAsync(episode.Id);
        // var videoSources = await animeProvider.GetVideosAsync(videoServers.First());
        // var videoSource = videoSources.First();
        // Source =
        //     "https://test-videos.co.uk/vids/bigbuckbunny/mkv/360/Big_Buck_Bunny_360_10s_1MB.mkv";
    }

    [RelayCommand]
    public async Task PlayAsync() { }

    [RelayCommand]
    public void Stop() { }
}
