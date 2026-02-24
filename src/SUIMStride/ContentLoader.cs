namespace SUIMStride;

using System.Collections.Concurrent;
using System.IO;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Sprites;

/// <summary>
/// Helper to load sprite and font assets with a simple memory cache.
/// This centralizes loading and makes it easy to extend fallback loading from disk later.
/// </summary>
public static class ContentLoader
{
    private static readonly ConcurrentDictionary<string, ISpriteProvider?> SpriteCache = new();
    private static readonly ConcurrentDictionary<string, SpriteFont?> FontCache = new();

    /// <summary>
    /// Load an <see cref="ISpriteProvider"/> (typically a <see cref="Sprite"/>) using the provided <see cref="ContentManager"/>.
    /// Returns null if the path is null/empty or loading fails. Results are cached by path.
    /// </summary>
    public static ISpriteProvider? LoadSprite(ContentManager? contentManager, string? path, Game? game)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (SpriteCache.TryGetValue(path, out var cached)) return cached;

        ISpriteProvider? result = null;

        if (!IsImageExtension(path))
        {
            if (contentManager != null)
            {
                try
                {
                    result = contentManager.Load<ISpriteProvider>(path);
                }
                catch { }
            }
        }

        if (result == null && game != null && File.Exists(path))
        {
            try
            {
                using var stream = File.Open(path, FileMode.Open, FileAccess.Read);
                var texture = Texture.Load(game.GraphicsDevice, stream, loadAsSRGB: true);
                result = new SpriteFromTexture { Texture = texture };
            }
            catch { }
        }

        SpriteCache[path] = result;
        return result;
    }

    /// <summary>
    /// Determines if a path refers to a common image file format.
    /// </summary>
    private static bool IsImageExtension(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;

        var extension = Path.GetExtension(path).ToLowerInvariant();
        var imageExtensions = new HashSet<string>
        {
            ".png", ".jpg", ".jpeg", ".bmp", ".tga", ".gif", ".webp", ".tif", ".tiff"
        };
        return imageExtensions.Contains(extension);
    }

    /// <summary>
    /// Load a <see cref="SpriteFont"/> using the provided <see cref="ContentManager"/>.
    /// Returns null if the name is null/empty or loading fails. Results are cached by name.
    /// </summary>
    public static SpriteFont? LoadFont(ContentManager? contentManager, string? name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        if (FontCache.TryGetValue(name, out var cached)) return cached;

        SpriteFont? result = null;
        try
        {
            if (contentManager != null)
            {
                result = contentManager.Load<SpriteFont>(name);
            }
        }
        catch
        {
            // Ignore; return null and cache it
        }

        FontCache[name] = result;
        return result;
    }
}
