using Avalonia.Platform.Storage;

namespace Avayomi.Utilities;

public static class StorageProviderHelper
{
    public static FilePickerFileType All { get; } =
        new("All") { Patterns = ["*.*"], MimeTypes = ["*/*"] };

    public static FilePickerFileType Json { get; } =
        new("Json")
        {
            Patterns = ["*.json"],
            AppleUniformTypeIdentifiers = ["public.json"],
            MimeTypes = ["application/json"],
        };

    public static FilePickerFileType Image { get; } =
        new("Image")
        {
            Patterns = ["*.png", "*.jpg", "*.jpeg", "*.gif", "*.bmp", "*.webp", "*.svg"],
            AppleUniformTypeIdentifiers = ["public.image"],
            MimeTypes = ["image/*"],
        };

    public static FilePickerFileType Video { get; } =
        new("Video")
        {
            Patterns = ["*.mp4", "*.mov", "*.avi", "*.mkv", "*.wmv", "*.webm", "*.flv"],
            AppleUniformTypeIdentifiers = ["public.movie"], // 'public.movie' is Apple's base type for video/movie files
            MimeTypes = ["video/*"],
        };
}
