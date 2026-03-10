namespace SUIMStride;

using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using Stride.Graphics;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;
using StrideButton = Stride.UI.Controls.Button;
using StrideUIElement = Stride.UI.UIElement;
using SUIM;
using SUIM.Flexbox;
using SUIM.Parse;
using SUIM.Parse.Components;
using SUIMButton = SUIM.Parse.Components.Button;
using SUIMElement = SUIM.Parse.Components.UIElement;

public class Parser
{
    private ContentManager? ContentManager = null;
    private readonly Dictionary<string, SpriteFont> Fonts = [];
    private readonly Dictionary<string, (SUIMElement SuimRoot, StrideUIElement StrideRoot, dynamic? Model)> _parseCache = [];
    public string? RootPath { get; set; }

    public (StrideUIElement StrideRoot, dynamic? Model) GetView(string viewName, Game game, int defaultFontSize = 16, bool fullscreen = false, object? model = null, bool createNewInstance = false)
    {
        return GetNamed(true, viewName, game, defaultFontSize, fullscreen, model, createNewInstance);
    }

    public (StrideUIElement StrideRoot, dynamic? Model) GetComponent(string viewName, Game game, int defaultFontSize = 16, bool fullscreen = false, object? model = null, bool createNewInstance = false)
    {
        return GetNamed(false, viewName, game, defaultFontSize, fullscreen, model, createNewInstance);
    }

    private (StrideUIElement StrideRoot, dynamic? Model) GetNamed(bool isView, string name, Game game, int defaultFontSize, bool fullscreen, object? model, bool createNewInstance)
    {
        if (string.IsNullOrEmpty(RootPath)) throw new InvalidOperationException("RootPath must be set before calling.");
        
        var project = new SUIMProject(RootPath);
        var viewPath = Path.Combine(RootPath, isView ? "views" : "components", $"{name}.suim");
        if (!File.Exists(viewPath)) throw new FileNotFoundException($"View not found: {viewPath}");

        var markup = File.ReadAllText(viewPath);
        project.ResolveDependencies(markup);

        return DoParse(markup, game, defaultFontSize, fullscreen, model, createNewInstance, RootPath, name);
    }

    public (StrideUIElement StrideRoot, dynamic? Model) Parse(string markup, Game game, int defaultFontSize = 16, bool fullscreen = false, object? model = null, bool createNewInstance = false)
    {
        return DoParse(markup, game, defaultFontSize, fullscreen, model, createNewInstance, null);
    }

    private (StrideUIElement StrideRoot, dynamic? Model) DoParse(string markup, Game game, int defaultFontSize, bool fullscreen, object? model, bool createNewInstance, string? basePath, string? viewName = null)
    {
        ArgumentNullException.ThrowIfNull(markup);

        // Return cached instance when available and caller doesn't request a new one
        lock (_parseCache)
        {
            if (_parseCache.TryGetValue(markup, out var cached))
            {
                if (!createNewInstance)
                {
                    return (cached.StrideRoot, cached.Model);
                }

                // createNewInstance==true -> return a fresh Stride tree by remapping the cached SUIM tree
                return (MapElement(cached.SuimRoot, game), cached.Model);
            }
        }

        ContentManager = game.Content;

        // Not cached: parse markup, map and store the canonical instance
        Text.MeasureFunc = (node, width, widthMode, height, heightMode) =>
        {
            var text = (Text)node.Context!;
            var fontSize = text.FontSize != null && float.TryParse(text.FontSize.AsSpan()[..^2], out var f) ? f : 0f;
            if (fontSize <= 0f)
            {
                fontSize = defaultFontSize; // Default font size if not specified or invalid
            }
            var fontName = text.Font ?? "StrideDefaultFont";
            var sf = Fonts.TryGetValue(fontName, out SpriteFont? value) ? value : null;
            if (sf == null && !Fonts.ContainsKey(fontName))
            {
                sf = ContentLoader.LoadFont(ContentManager, fontName);
                if (sf != null)
                {
                    Fonts[fontName] = sf;
                }
            }
            if (sf != null)
            {
                var size = sf.MeasureString(text.Value ?? "");
                return new Size(size.X, size.Y);
            }
            return new Size(0, 0);
        };
        var (suimRoot, model2) = MarkupParser.Parse(markup, model, basePath: basePath, componentName: viewName);
        Layout(suimRoot, game, fullscreen);
        var strideRoot = MapElement(suimRoot, game);

        lock (_parseCache)
        {
            _parseCache[markup] = (suimRoot, strideRoot, model2);
        }

        return (strideRoot, model2);
    }

    private static void Layout(SUIMElement root, Game game, bool fullscreen)
    {
        int preferredWidth;
        int preferredHeight;
        if (fullscreen)
        {
            var adapterOutput = game.GraphicsDevice.Adapter.Outputs[0];
            var currentMonitorResolution = adapterOutput.CurrentDisplayMode;
            preferredWidth = currentMonitorResolution.Width;
            preferredHeight = currentMonitorResolution.Height;
            game.Window.PreferredWindowedSize = new Int2(preferredWidth, preferredHeight);
            game.Window.FullscreenIsBorderlessWindow = true;
            game.Window.IsFullscreen = true;
        }
        else
        {
            preferredWidth = game.GraphicsDeviceManager.PreferredBackBufferWidth;
            preferredHeight = game.GraphicsDeviceManager.PreferredBackBufferHeight;
        }
        root.CalculateLayout(preferredWidth, preferredHeight);
    }

    // Test/helper: retrieve the SUIM root for a mapped Stride tree
    public SUIMElement? GetSuimRootFor(StrideUIElement strideRoot)
    {
        lock (_parseCache)
        {
            foreach (var entry in _parseCache.Values)
            {
                if (ReferenceEquals(entry.StrideRoot, strideRoot))
                {
                    return entry.SuimRoot;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Maps an already-parsed and laid-out SUIM element tree to Stride UI elements.
    /// It is public for testing or when you have a SUIM tree that's already been processed.
    /// </summary>
    public StrideUIElement MapElement(SUIMElement element, Game? game)
    {
        StrideUIElement strideElement = element switch
        {
            SUIMButton b => MapButton(b, game),
            Text t => MapText(t),
            Input i => MapInput(i),
            SUIM.Parse.Components.Image img => MapImage(img, game),
            SUIM.Parse.Components.Border br => MapBorder(br, game),
            BackgroundImage bg => MapBackgroundImage(bg, game),
            _ => new Canvas() // Fallback
        };

        ApplyCommonProperties(element, strideElement);
        
        Bindings.TransferBindings(element, strideElement);

        // Handle Children for generic containers if not already handled
        if (strideElement is Panel panel && element.Children.Count > 0)
        {
            foreach (var child in element.Children)
            {
                panel.Children.Add(MapElement(child, game));
            }
        }
        else if (strideElement is ContentControl contentControl && element.Children.Count > 0)
        {
            if (element.Children.Count == 1)
            {
                contentControl.Content = MapElement(element.Children[0], game);
            }
            else
            {
                var canvas = new Canvas();
                foreach (var child in element.Children)
                {
                    canvas.Children.Add(MapElement(child, game));
                }
                contentControl.Content = canvas;
            }
        }

        return strideElement;
    }

    private StrideButton MapButton(SUIMButton button, Game? game)
    {
        var btn = new StrideButton();
        
        if (!string.IsNullOrEmpty(button.HoverImage))
        {
            var loaded = ContentLoader.LoadSprite(ContentManager, button.HoverImage, game);
            if (loaded != null) btn.MouseOverImage = loaded;
        }
        if (!string.IsNullOrEmpty(button.NormalImage))
        {
            var loaded = ContentLoader.LoadSprite(ContentManager, button.NormalImage, game);
            if (loaded != null) btn.NotPressedImage = loaded;
        }
        if (!string.IsNullOrEmpty(button.PressedImage))
        {
            var loaded = ContentLoader.LoadSprite(ContentManager, button.PressedImage, game);
            if (loaded != null) btn.PressedImage = loaded;
        }

        // Click handler will be bound in TransferEvents
        return btn;
    }

    private TextBlock MapText(Text text)
    {
        var fontSize = text.FontSize != null && float.TryParse(text.FontSize.AsSpan()[..^2], out var f) ? f : 0f;
        if (fontSize <= 0f)
        {
            fontSize = 16f; // Default font size if not specified or invalid
        }
        var tb = new TextBlock
        {
            Text = text.Value ?? "",
            TextSize = fontSize,
        };

        // Resolve SpriteFont by name (from style/attribute) using optional resolver.
        // Consumers (e.g. example app) should set SUIMStride.FontResolver = name => game.Content.Load<SpriteFont>(name);
        var fontName = text.Font ?? "StrideDefaultFont";
        SpriteFont? sf = Fonts.TryGetValue(fontName, out SpriteFont? value) ? value : null;
        if (sf == null && !Fonts.ContainsKey(fontName))
        {
            sf = ContentLoader.LoadFont(ContentManager, fontName);
            if (sf != null)
            {
                Fonts[fontName] = sf;
            }
        }

        if (sf != null)
        {
            tb.Font = sf;
        }
        
        if (text.Wrap)
        {
            tb.WrapText = true;
        }

        if (text.Color != null)
        {
             tb.TextColor = ParseColor(text.Color);
        }

        return tb;
    }

    private static StrideUIElement MapInput(Input input)
    {
        // Map based on the input type
        return input.Type switch
        {
            InputType.Checkbox => new ToggleButton(),
            // SUIM.Components.InputType.Radio => new ToggleButton(), // Stride doesn't have a direct RadioButton in all versions; use ToggleButton for now
            _ => new EditText
            {
                Text = input.Value ?? ""
            },
        };
    }

    private ImageElement MapImage(SUIM.Parse.Components.Image image, Game? game)
    {
        var img = new ImageElement();

        if (!string.IsNullOrEmpty(image.Source))
        {
            var loaded = ContentLoader.LoadSprite(ContentManager, image.Source, game);
            if (loaded != null) img.Source = loaded;
        }

        var stretch = ImageStretchExtensions.FromString(image.Stretch);
        img.StretchType = stretch switch
        {
            ImageStretch.Uniform => StretchType.Uniform,
            ImageStretch.UniformToFill => StretchType.UniformToFill,
            ImageStretch.Fill => StretchType.Fill,
            ImageStretch.FillOnStretch => StretchType.FillOnStretch,
            _ => StretchType.None
        };

        return img;
    }

    private ContentDecorator MapBackgroundImage(BackgroundImage background, Game? game)
    {
        var decorator = new ContentDecorator();
        if (!string.IsNullOrEmpty(background.Source))
        {
            var loaded = ContentLoader.LoadSprite(ContentManager, background.Source, game);
            if (loaded != null) decorator.BackgroundImage = loaded;
        }
        return decorator;
    }

    private Stride.UI.Controls.Border MapBorder(SUIM.Parse.Components.Border border, Game? game)
    {
        var borderThickness = new Thickness();
        //var borderStyle = BorderStyle.None;
        var borderColor = Color.Transparent;
        if (!string.IsNullOrEmpty(border.Thickness))
        {
            //todo: medium|thin|thick|initial|inherit;
            var span = border.Thickness.EndsWith("px") ? border.Thickness.AsSpan()[..^2] : border.Thickness.AsSpan();
            var value = float.TryParse(span, out var f) ? f : 0f;
            borderThickness = new Thickness(value, value, value, value);
        }

        // todo
        //borderStyle = BorderStyle.Solid;

        if (!string.IsNullOrEmpty(border.Color))
        {
            borderColor = ParseColor(border.Color);
        }

        var borderElem = new Stride.UI.Controls.Border
        {
            BorderThickness = borderThickness,
            BorderColor = borderColor,
        };

        if (!string.IsNullOrEmpty(border.Color))
        {
            borderElem.BorderColor = ParseColor(border.Color);
        }

        // Handle Children
        if (border.Children.Count > 0)
        {
            if (border.Children.Count == 1)
            {
                borderElem.Content = MapElement(border.Children[0], game);
            }
            else
            {
                throw new NotSupportedException("Border element in SUIM supports only one child element for now.");
            }
        }

        return borderElem;
    }

    private static void ApplyCommonProperties(SUIMElement suim, StrideUIElement stride)
    {
        stride.Name = suim.Id;
        stride.Opacity = suim.Opacity == null ? 1 : Convert.ToSingle(suim.Opacity);

        // Use calculated dimensions
        stride.SetCanvasAbsolutePosition(new Vector3(
            suim.GetLeft(),
            suim.GetTop(), 0));
        stride.Width = SanitizeSizeForStride(suim.GetWidth());
        stride.Height = SanitizeSizeForStride(suim.GetHeight());

        if (suim.BackgroundColor != null)
        {
            stride.BackgroundColor = ParseColor(suim.BackgroundColor);
        }

        if (suim.Visibility != null && !suim.Visibility.StartsWith('@'))
        {
            if ("visible".Equals(suim.Visibility, StringComparison.OrdinalIgnoreCase))
            {
                stride.Visibility = Visibility.Visible;
            }
            else if ("hidden".Equals(suim.Visibility, StringComparison.OrdinalIgnoreCase))
            {
                stride.Visibility = Visibility.Hidden;
            }
            else if ("collapsed".Equals(suim.Visibility, StringComparison.OrdinalIgnoreCase))
            {
                stride.Visibility = Visibility.Collapsed;
            }
            else
            {
                throw new NotSupportedException($"Visibility: '{suim.Visibility}'");
            }
        }

        if (string.Equals(suim.StopClicks, "true", StringComparison.OrdinalIgnoreCase))
        {
            stride.CanBeHitByUser = true;
        }
    }

    private static Color ParseColor(string colorStr)
    {
        var pc = BackendHelpers.ParseColor(colorStr);
        return new Color(pc.R, pc.G, pc.B, pc.A);
    }
    
    private static float SanitizeSizeForStride(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value < 0 || value == float.MaxValue)
            return 0;

        return value;
    }
}
