namespace SUIMStride;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using Stride.Graphics;
using Stride.UI;
using Stride.UI.Controls;
using StrideUIElement = Stride.UI.UIElement;
using StrideButton = Stride.UI.Controls.Button;
using Stride.UI.Panels;
using SUIM;
using SUIM.Layout;
using SUIM.Model;
using SUIM.Parse;
using SUIM.Parse.Components;
using SUIMElement = SUIM.Parse.Components.UIElement;
using SUIM.Parse.Components.Attributes;

public class Parser
{
    // Test hook: keep track of click handlers bound to Stride Buttons so tests can simulate clicks.
    private readonly Dictionary<StrideButton, Delegate> _clickHandlers = [];
    private ContentManager? ContentManager = null;
    private readonly Dictionary<string, SpriteFont> Fonts = [];
    private readonly Dictionary<string, (SUIMElement SuimRoot, StrideUIElement StrideRoot, dynamic? Model)> _parseCache = [];
    private dynamic? _currentModel;
    private Game? _game;
    public string? RootPath { get; set; }

    public (StrideUIElement StrideRoot, dynamic? Model) Parse(string markup, Game game, int defaultFontSize = 16, bool fullscreen = false, object? model = null, bool createNewInstance = false)
    {
        return DoParse(markup, game, defaultFontSize, fullscreen, model, createNewInstance, null);
    }

    public (StrideUIElement StrideRoot, dynamic? Model) GetView(string viewName, Game game, int defaultFontSize = 16, bool fullscreen = false, object? model = null, bool createNewInstance = false)
    {
        if (string.IsNullOrEmpty(RootPath)) throw new InvalidOperationException("RootPath must be set before calling GetView.");
        
        var project = new SUIMProject(RootPath);
        var viewPath = Path.Combine(RootPath, "views", $"{viewName}.suim");
        if (!File.Exists(viewPath)) throw new FileNotFoundException($"View not found: {viewPath}");

        var markup = File.ReadAllText(viewPath);
        project.ResolveDependencies(markup);

        return DoParse(markup, game, defaultFontSize, fullscreen, model, createNewInstance, RootPath, viewName);
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

        _game = game;
        ContentManager = game.Content;

        // Not cached: parse markup, map and store the canonical instance
        var (suimRoot, model2) = MarkupParser.Parse(markup, model, basePath: basePath, componentName: viewName);
        Layout(suimRoot, game, defaultFontSize, fullscreen);
        _currentModel = model2;
        var strideRoot = MapElement(suimRoot, game);
        _currentModel = null;

        lock (_parseCache)
        {
            _parseCache[markup] = (suimRoot, strideRoot, model2);
        }

        return (strideRoot, model2);
    }

    private static void Layout(SUIMElement root, Game game, int defaultFontSize, bool fullscreen)
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
        LayoutEngine.Layout(root, defaultFontSize, preferredWidth, preferredHeight);
    }

    /// <summary>
    /// Maps an already-parsed and laid-out SUIM element tree to Stride UI elements.
    /// It is public for testing or when you have a SUIM tree that's already been processed.
    /// </summary>
    public StrideUIElement MapElement(SUIMElement element, Game? game)
    {
        StrideUIElement strideElement = element switch
        {
            SUIM.Parse.Components.Button b => MapButton(b, game),
            Text t => MapText(t),
            Input i => MapInput(i),
            SUIM.Parse.Components.Image img => MapImage(img, game),
            SUIM.Parse.Components.Border br => MapBorder(br, game),
            BackgroundImage bg => MapBackgroundImage(bg, game),
            _ => new Canvas() // Fallback
        };

        ApplyCommonProperties(element, strideElement);
        
        TransferBindings(element, strideElement);

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

    private StrideButton MapButton(SUIM.Parse.Components.Button button, Game? game)
    {
        var btn = new StrideButton();
        
        if (!string.IsNullOrEmpty(button.MouseOverImage))
        {
            var loaded = ContentLoader.LoadSprite(ContentManager, button.MouseOverImage, game);
            if (loaded != null) btn.MouseOverImage = loaded;
        }
        if (!string.IsNullOrEmpty(button.NotPressedImage))
        {
            var loaded = ContentLoader.LoadSprite(ContentManager, button.NotPressedImage, game);
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
        var fontSize = text.FontSize != null ? text.ToPixels(UnitValue.Parse(text.FontSize)) : 0f;
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
        var borderElem = new Stride.UI.Controls.Border
        {
            BorderThickness = ThicknessToStride(border.Thickness, border)
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
        var vis = SUIM.Parse.Components.Attributes.Visibility.Parse(suim.Visibility);
        stride.Visibility = vis switch
        {
            SUIM.Parse.Components.Attributes.Visibility.Hidden => Stride.UI.Visibility.Hidden,
            SUIM.Parse.Components.Attributes.Visibility.Collapsed => Stride.UI.Visibility.Collapsed,
            _ => Stride.UI.Visibility.Visible,
        };

        // Use calculated dimensions
        stride.SetCanvasAbsolutePosition(new Vector3(
            FractionalUnit.Sanitize(suim.ActualX),
            FractionalUnit.Sanitize(suim.ActualY), 0));
        stride.Width = FractionalUnit.Sanitize(suim.ActualWidth);
        stride.Height = FractionalUnit.Sanitize(suim.ActualHeight);

        // Use the computed margins from SUIM, which represent the actual margin values applied to the element
        stride.Margin = new Stride.UI.Thickness(
            FractionalUnit.Sanitize(suim.ComputedMarginLeft),
            FractionalUnit.Sanitize(suim.ComputedMarginTop),
            FractionalUnit.Sanitize(suim.ComputedMarginRight),
            FractionalUnit.Sanitize(suim.ComputedMarginBottom));

        if (stride is ContentControl cc)
        {
            cc.Padding = ThicknessToStride(suim.Padding, suim);
        }
        if (suim.BackgroundColor != null)
        {
            stride.BackgroundColor = ParseColor(suim.BackgroundColor);
        }

        if (string.Equals(suim.StopClicks, "true", StringComparison.OrdinalIgnoreCase))
        {
            stride.CanBeHitByUser = true;
        }
    }

    private static Stride.UI.Thickness ThicknessToStride(string? thicknessString, SUIMElement suim)
    {
        var thickness = SUIM.Parse.Components.Attributes.Thickness.Parse(thicknessString);
        return new Stride.UI.Thickness(
            suim.ToPixels(thickness.Left),
            suim.ToPixels(thickness.Top),
            suim.ToPixels(thickness.Right),
            suim.ToPixels(thickness.Bottom));
    }

    private static Color ParseColor(string colorStr)
    {
        var pc = BackendHelpers.ParseColor(colorStr);
        return new Color(pc.R, pc.G, pc.B, pc.A);
    }
    
    private void TransferBindings(SUIMElement suimElement, StrideUIElement strideElement)
    {
        var model = suimElement.GetEffectiveModel();
        
        foreach (var binding in suimElement.Bindings)
        {
            if (model == null)
            {
                if (suimElement.IsComponentRoot || suimElement.Parent?.GetEffectiveModel() == null)
                    throw new InvalidOperationException($"Binding '{binding.ModelPropertyName}' found on tag '{suimElement.TagName}' but no model context is available.");
                continue;
            }
            SetupBinding(model, binding.ModelPropertyName, binding.TargetPropertyName, strideElement);
        }

        TransferEvents(suimElement, strideElement);
    }

    private void TransferEvents(SUIMElement suimElement, StrideUIElement strideElement)
    {
        if (suimElement.Events.Count == 0) return;
        var model = suimElement.GetEffectiveModel();

        foreach (var kvp in suimElement.Events)
        {
            var eventName = kvp.Key;
            var handlerName = kvp.Value;

            if (model == null)
                throw new InvalidOperationException($"Event '{eventName}' found on tag '{suimElement.TagName}' but no model context is available.");

            // Resolve handler using the effective model for this element (components must be isolated)
            Delegate? handler = null;

            if (!string.IsNullOrEmpty(handlerName) && handlerName.StartsWith('@'))
            {
                var propName = handlerName[1..];
                if (model is ObservableObject mOO)
                {
                    var val = mOO.GetValue(propName);
                    if (val is Delegate d)
                    {
                        handler = d;
                    }
                    else if (val is string s)
                    {
                        // Try to interpret the string as a handler expression on the component model (or parent proxy)
                        handler = mOO.GetHandler(s) ?? BackendHelpers.ResolveEventAction(s, (object)model, suimElement) ?? ResolveMethodAsDelegate(s, model);
                    }
                }
            }
            else
            {
                if (model is ObservableObject oo)
                {
                    handler = oo.GetHandler(handlerName);
                }
                else
                {
                    // Try reflection on raw object using shared helper
                    handler = BackendHelpers.ResolveEventAction(handlerName, (object)model, suimElement);
                    // If generic resolver didn't find anything, try Stride-specific resolver that understands RoutedEventArgs
                    handler ??= ResolveMethodAsDelegate(handlerName, model);
                }
            }

            if (handler != null)
            {
                // Map to Stride event
                if (string.Equals(eventName, "click", StringComparison.OrdinalIgnoreCase) && strideElement is StrideButton btn)
                {
                    BindClickHandler(btn, handler, suimElement);
                }
                // Add more event types here as needed
            }
        }
    }

    private void BindClickHandler(StrideButton btn, Delegate handler, SUIMElement suimElement)
    {
        // Support multiple handler types for click events
        if (handler is EventHandler<Stride.UI.Events.RoutedEventArgs> routedHandler)
        {
            btn.Click += routedHandler;
            _clickHandlers[btn] = routedHandler;
        }
        else if (handler is EventHandler eh)
        {
            EventHandler<Stride.UI.Events.RoutedEventArgs> wrappedHandler = (s, e) => eh(s, e);
            btn.Click += wrappedHandler;
            _clickHandlers[btn] = wrappedHandler;
        }
        else if (handler is Action<SUIMElement> actionWithElement)
        {
            EventHandler<Stride.UI.Events.RoutedEventArgs> wrappedHandler = (s, e) => actionWithElement(suimElement);
            btn.Click += wrappedHandler;
            _clickHandlers[btn] = wrappedHandler;
        }
        else if (handler is Action a)
        {
            EventHandler<Stride.UI.Events.RoutedEventArgs> wrappedHandler = (s, e) => a();
            btn.Click += wrappedHandler;
            _clickHandlers[btn] = wrappedHandler;
        }
    }

    // Test helper to retrieve a bound click handler (returns null if none)
    public Delegate? GetBoundClickHandler(StrideButton btn)
    {
        return _clickHandlers.TryGetValue(btn, out var d) ? d : null;
    }

    /// <summary>
    /// Resolves a method name to a delegate using priority-based resolution.
    /// Priority: Parameterless -> UIElement parameter -> EventHandler pattern
    /// </summary>
    private static Delegate? ResolveMethodAsDelegate(string methodName, dynamic model)
    {
        // Keep a Stride-specific variant that supports RoutedEventArgs in addition to the generic helper
        if (model == null) return null;

        // Cast to object to avoid dynamic dispatch issues
        object targetObject = model;
        var methods = targetObject.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
        var matchingMethods = methods.Where(m => m.Name == methodName).ToList();
        if (matchingMethods.Count == 0)
            return null;

        // Try EventHandler<RoutedEventArgs> pattern (Stride specific)
        var routedHandlerMethod = matchingMethods.FirstOrDefault(m =>
        {
            var parms = m.GetParameters();
            return parms.Length == 2 &&
                parms[0].ParameterType == typeof(object) &&
                parms[1].ParameterType == typeof(Stride.UI.Events.RoutedEventArgs);
        });
        if (routedHandlerMethod != null)
        {
            try { return Delegate.CreateDelegate(typeof(EventHandler<Stride.UI.Events.RoutedEventArgs>), targetObject, routedHandlerMethod); }
            catch { /* Fall through */ }
        }

        return null;
    }
    
    private static void SetupBinding(dynamic? model, string modelPropertyName, string targetPropertyName, StrideUIElement strideElement)
    {
        if (model == null) return;
        
        // 2-way: UI -> model (via proxy)
        if (model is ObservableObject oo)
        {
            if (strideElement is EditText et && (targetPropertyName.Equals("text", StringComparison.OrdinalIgnoreCase) || targetPropertyName.Equals("value", StringComparison.OrdinalIgnoreCase)))
            {
                oo.SetProxy(modelPropertyName, () => et.Text, (val) => et.Text = val?.ToString() ?? "");
                et.TextChanged += (s, e) => oo.NotifyChanged(modelPropertyName);
                return; // Proxy handles everything
            }
            else if (strideElement is ToggleButton tb && (targetPropertyName.Equals("checked", StringComparison.OrdinalIgnoreCase) || targetPropertyName.Equals("value", StringComparison.OrdinalIgnoreCase)))
            {
                oo.SetProxy(modelPropertyName, 
                    () => tb.State == ToggleState.Checked, 
                    (val) => tb.State = (val is bool b && b) ? ToggleState.Checked : ToggleState.UnChecked);
                
                tb.Checked += (s, e) => oo.NotifyChanged(modelPropertyName);
                tb.Unchecked += (s, e) => oo.NotifyChanged(modelPropertyName);
                return; // Proxy handles everything
            }
        }

        // 1-way fallback (model -> UI)
        BackendHelpers.SetupPropertyBinding((object?)model, modelPropertyName, newValue => ApplyBindingValue(strideElement, targetPropertyName, newValue));
    }
    
    private static void ApplyBindingValue(StrideUIElement strideElement, string targetPropertyName, object? value)
    {
        try
        {
            // Handle Text property
            if (string.Equals(targetPropertyName, "text", StringComparison.OrdinalIgnoreCase) || string.Equals(targetPropertyName, "value", StringComparison.OrdinalIgnoreCase))
            {
                if (strideElement is TextBlock tb)
                    tb.Text = value?.ToString() ?? "";
                else if (strideElement is EditText et)
                    et.Text = value?.ToString() ?? "";
                else if (strideElement is StrideButton btn && btn.Content is TextBlock btnText)
                    btnText.Text = value?.ToString() ?? "";
            }
            // Handle other common properties
            else if (string.Equals(targetPropertyName, "visibility", StringComparison.OrdinalIgnoreCase))
            {
                if (value != null && Enum.TryParse<Stride.UI.Visibility>(value.ToString(), true, out var vis))
                    strideElement.Visibility = vis;
            }
            else if (string.Equals(targetPropertyName, "opacity", StringComparison.OrdinalIgnoreCase))
            {
                if (float.TryParse(value?.ToString() ?? "1", out var opacity))
                    strideElement.Opacity = opacity;
            }
        }
        catch { }
    }
}
