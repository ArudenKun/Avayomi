using System;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Avayomi.Controls;

/// <summary>
/// Represents a media item (track) with metadata and cached images used by
/// the UI. The model exposes properties for serialization and runtime-only
/// properties for bitmaps and actions that should not be persisted.
/// </summary>
public partial class MediaItem : ObservableObject, IDisposable
{
    private Bitmap? _coverBitmap;
    private Bitmap? _wallpaperBitmap;
    private Bitmap? _screenshotBitmap;

    // Persisted metadata

    [JsonPropertyName("FileName")]
    public string? FileName
    {
        get;
        set => SetProperty(ref field, value);
    }

    [JsonPropertyName("Title")]
    public string? Title
    {
        get;
        set => SetProperty(ref field, value);
    }

    [JsonPropertyName("Artist")]
    public string? Artist
    {
        get;
        set => SetProperty(ref field, value);
    }

    [JsonPropertyName("Album")]
    public string? Album
    {
        get;
        set => SetProperty(ref field, value);
    }

    [JsonPropertyName("Track")]
    public uint Track
    {
        get;
        set => SetProperty(ref field, value);
    }

    [JsonPropertyName("Year")]
    public uint Year
    {
        get;
        set => SetProperty(ref field, value);
    }

    [JsonPropertyName("Duration")]
    public double Duration
    {
        get;
        set => SetProperty(ref field, value);
    }

    [JsonPropertyName("Lyrics")]
    public string? Lyrics
    {
        get;
        set => SetProperty(ref field, value);
    }

    [JsonPropertyName("Genre")]
    public string? Genre
    {
        get;
        set => SetProperty(ref field, value);
    }

    [JsonPropertyName("Comment")]
    public string? Comment
    {
        get;
        set => SetProperty(ref field, value);
    }

    [JsonPropertyName("ReplayGainTrackGain")]
    public double ReplayGainTrackGain
    {
        get;
        set => SetProperty(ref field, value);
    }

    [JsonPropertyName("ReplayGainAlbumGain")]
    public double ReplayGainAlbumGain
    {
        get;
        set => SetProperty(ref field, value);
    }

    [JsonPropertyName("LocalCoverPath")]
    public string? LocalCoverPath
    {
        get;
        set => SetProperty(ref field, value);
    }

    [JsonPropertyName("VideoUrl")]
    public string? VideoUrl
    {
        get;
        set => SetProperty(ref field, value);
    }

    // Runtime-only flags and resources (not serialized)

    [JsonIgnore]
    public bool IsLoadingCover
    {
        get;
        set => SetProperty(ref field, value);
    }

    [JsonIgnore]
    public bool CoverFound
    {
        get;
        set => SetProperty(ref field, value);
    }

    [XmlIgnore]
    [JsonIgnore]
    public (string, string)? OnlineUrls
    {
        get;
        set => SetProperty(ref field, value);
    }

    [XmlIgnore]
    [JsonIgnore]
    public Bitmap? CoverBitmap
    {
        get => _coverBitmap;
        set => SetProperty(ref _coverBitmap, value);
    }

    [XmlIgnore]
    [JsonIgnore]
    public int Index
    {
        get;
        set => SetProperty(ref field, value);
    }

    [XmlIgnore]
    [JsonIgnore]
    public Bitmap? ScreenshotBitmap
    {
        get => _screenshotBitmap;
        set
        {
            var old = _screenshotBitmap;
            if (SetProperty(ref _screenshotBitmap, value))
                old?.Dispose();
        }
    }

    [XmlIgnore]
    [JsonIgnore]
    public Bitmap? WallpaperBitmap
    {
        get => _wallpaperBitmap;
        set
        {
            var old = _wallpaperBitmap;
            if (SetProperty(ref _wallpaperBitmap, value))
                old?.Dispose();
        }
    }

    [XmlIgnore]
    [JsonIgnore]
    public bool IsRenaming
    {
        get;
        set => SetProperty(ref field, value);
    }

    [XmlIgnore]
    [JsonIgnore]
    public bool IsNameInvalid
    {
        get;
        set => SetProperty(ref field, value);
    }

    [XmlIgnore]
    [JsonIgnore]
    public string? NameInvalidMessage
    {
        get;
        set => SetProperty(ref field, value);
    }

    [XmlIgnore]
    [JsonIgnore]
    public Action<MediaItem>? SaveCoverBitmapAction
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Disposes runtime bitmap resources that are uniquely owned by this model.
    /// </summary>
    public void Dispose()
    {
        // Cover bitmaps are commonly shared (default placeholders and metadata cache entries)
        // and can still be referenced by UI Image controls when items are removed/replaced.
        // Do not dispose here to avoid invalidating shared sources during layout.
        _coverBitmap = null;

        _wallpaperBitmap?.Dispose();
        _wallpaperBitmap = null;

        _screenshotBitmap?.Dispose();
        _screenshotBitmap = null;
    }

    [RelayCommand]
    private void SaveCoverBitmap()
    {
        SaveCoverBitmapAction?.Invoke(this);
        CoverFound = false;
    }

    [RelayCommand]
    private void Cancel()
    {
        CoverFound = false;
    }
}
