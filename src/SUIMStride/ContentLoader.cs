namespace SUIMStride;

using System.Collections.Concurrent;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using Stride.Graphics;

/// <summary>
/// Helper to load sprite and font assets with a simple memory cache.
/// This centralizes loading and makes it easy to extend fallback loading from disk later.
/// </summary>
public static class ContentLoader
{
    private static readonly ConcurrentDictionary<string, Sprite?> SpriteCache = new();
    private static readonly ConcurrentDictionary<string, SpriteFont?> FontCache = new();

    /// <summary>
    /// Load an <see cref="ISpriteProvider"/> (typically a <see cref="Sprite"/>) using the provided <see cref="ContentManager"/>.
    /// Returns null if the path is null/empty or loading fails. Results are cached by path.
    /// </summary>
    public static Sprite? LoadSprite(ContentManager? contentManager, string? path, Game? game)
    {
        if (string.IsNullOrEmpty(path)) return null;

        if (SpriteCache.TryGetValue(path, out var cached)) return cached;

        Sprite? result = null;
        try
        {
            if (contentManager != null)
            {
                // Primary attempt: use content pipeline
                result = contentManager.Load<Sprite>(path);
            }
        }
        catch
        {
            // Ignore and fall through to caching null; can extend with disk-based loader here
        }

        if (result == null && game != null)
        {
            try
            {
                using var stream = File.Open(path, FileMode.Open);
                var texture = Texture.Load(game.GraphicsDevice, stream);
                result = new Sprite
                {
                    Texture = texture,
                    Region = new Stride.Core.Mathematics.RectangleF(0, 0, texture.Width, texture.Height),
                    Center = new Stride.Core.Mathematics.Vector2(texture.Width / 2f, texture.Height / 2f)
                };
            }
            catch
            {
                // Ignore and fall through to caching null; can extend with disk-based loader here
            }
        }

        SpriteCache[path] = result;
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
