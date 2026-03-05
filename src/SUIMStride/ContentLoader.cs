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
    public static ISpriteProvider? LoadSprite(ContentManager? contentManager, string? sprite, Game? game)
    {
        if (string.IsNullOrEmpty(sprite)) return null;
        if (SpriteCache.TryGetValue(sprite, out var cached)) return cached;

        ISpriteProvider? result = null;

        var isUrl = sprite.StartsWith("url(");
        if (isUrl && game != null)
        {
            try
            {
                var path = sprite["url(".Length..^1].Trim('"', '\'');
                if (File.Exists(path))
                {
                    using var stream = File.Open(path, FileMode.Open, FileAccess.Read);
                    var texture = Texture.Load(game.GraphicsDevice, stream, loadAsSRGB: true);
                    result = new SpriteFromTexture { Texture = texture };
                }
            }
            catch { }
        }
        
        if (result == null)
        {
            if (contentManager != null)
            {
                try
                {
                    result = contentManager.Load<ISpriteProvider>(sprite);
                }
                catch { }
            }
        }

        SpriteCache[sprite] = result;
        return result;
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
