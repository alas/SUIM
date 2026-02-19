namespace SUIM.StrideIntegration;

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
using Stride.UI.Panels;
using SUIM;
using StrideGrid = Stride.UI.Panels.Grid;

public class SUIMStride
{
    // Test hook: keep track of click handlers bound to Stride Buttons so tests can simulate clicks.
    private readonly Dictionary<Button, Delegate> _clickHandlers = [];
    private ContentManager? ContentManager = null;
    private readonly Dictionary<string, SpriteFont> Fonts = [];
    private readonly Dictionary<string, (Components.UIElement SuimRoot, UIElement StrideRoot, dynamic? Model)> _parseCache = [];
    private dynamic? _currentModel;
    public string? RootPath { get; set; }

    public (UIElement, dynamic?) ParseFromFile(string path, Game game, int defaultFontSize = 16, bool fullscreen = false, object? model = null, bool createNewInstance = false)
    {
        var markup = File.ReadAllText(path);
        var viewName = Path.GetFileNameWithoutExtension(path);
        return DoParse(markup, game, defaultFontSize, fullscreen, model, createNewInstance, Path.GetDirectoryName(path), viewName);
    }

    public (UIElement, dynamic?) Parse(string markup, Game game, int defaultFontSize = 16, bool fullscreen = false, object? model = null, bool createNewInstance = false)
    {
        return DoParse(markup, game, defaultFontSize, fullscreen, model, createNewInstance, null);
    }

    public (UIElement, dynamic?) GetView(string viewName, Game game, int defaultFontSize = 16, bool fullscreen = false, object? model = null, bool createNewInstance = false)
    {
        if (string.IsNullOrEmpty(RootPath)) throw new InvalidOperationException("RootPath must be set before calling GetView.");
        
        var project = new SUIMProject(RootPath);
        var viewPath = Path.Combine(RootPath, "views", $"{viewName}.suim");
        if (!File.Exists(viewPath)) throw new FileNotFoundException($"View not found: {viewPath}");

        var markup = File.ReadAllText(viewPath);
        project.ResolveDependencies(markup);

        return DoParse(markup, game, defaultFontSize, fullscreen, model, createNewInstance, RootPath, viewName);
    }

    private (UIElement, dynamic?) DoParse(string markup, Game game, int defaultFontSize, bool fullscreen, object? model, bool createNewInstance, string? basePath, string? viewName = null)
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
                return (MapElement(cached.SuimRoot), cached.Model);
            }
        }

        ContentManager = game.Content;

        // Not cached: parse markup, map and store the canonical instance
        var (suimRoot, model2) = MarkupParser.Parse(markup, model, basePath: basePath, componentName: viewName);
        Layout(suimRoot, game, defaultFontSize, fullscreen);
        _currentModel = model2;
        var strideRoot = MapElement(suimRoot);
        _currentModel = null;

        lock (_parseCache)
        {
            _parseCache[markup] = (suimRoot, strideRoot, model2);
        }

        return createNewInstance ? (MapElement(suimRoot), model2) : (strideRoot, model2);
    }

    private static void Layout(Components.UIElement root, Game game, int defaultFontSize, bool fullscreen)
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
        SUIM.Layout.LayoutEngine.Layout(root, defaultFontSize, preferredWidth, preferredHeight);
    }

    /// <summary>
    /// Maps an already-parsed and laid-out SUIM element tree to Stride UI elements.
    /// It is public for testing or when you have a SUIM tree that's already been processed.
    /// </summary>
    public UIElement MapElement(Components.UIElement element)
    {
        UIElement strideElement = element switch
        {
            Components.Button b => MapButton(b),
            Components.Text t => MapText(t),
            Components.Stack s => MapStack(s),
            Components.Grid g => MapGrid(g),
            Components.Input i => MapInput(i),
            Components.Image img => MapImage(img),
            Components.Border br => MapBorder(br),
            _ => new StrideGrid() // Fallback
        };

        ApplyCommonProperties(element, strideElement);
        
        TransferBindings(element, strideElement);

        // Handle Children for generic containers if not already handled
        if (strideElement is Panel panel && element.Children.Count > 0 && element is not Components.Grid)
        {
            foreach (var child in element.Children)
            {
                panel.Children.Add(MapElement(child));
            }
        }
        else if (strideElement is ContentControl contentControl && element.Children.Count > 0)
        {
            if (element.Children.Count == 1)
            {
                contentControl.Content = MapElement(element.Children[0]);
            }
            else
            {
                var stack = new StackPanel { Orientation = Orientation.Vertical };
                foreach (var child in element.Children)
                {
                    stack.Children.Add(MapElement(child));
                }
                contentControl.Content = stack;
            }
        }

        return strideElement;
    }

    private static Button MapButton(Components.Button button)
    {
        var btn = new Button();
        // Click handler will be bound in TransferEvents
        return btn;
    }

    private TextBlock MapText(Components.Text text)
    {
        var tb = new TextBlock
        {
            Text = text.Value ?? "",
            TextSize = text.FontSize > 0f ? text.FontSize : 14f,
        };

        // Resolve SpriteFont by name (from style/attribute) using optional resolver.
        // Consumers (e.g. example app) should set SUIMStride.FontResolver = name => game.Content.Load<SpriteFont>(name);
        var fontName = text.Font ?? "StrideDefaultFont";
        SpriteFont? sf = Fonts.TryGetValue(fontName, out SpriteFont? value) ? value : null;
        if (sf == null && !Fonts.ContainsKey(fontName))
        {
            sf = ContentManager?.Load<SpriteFont>(fontName);
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

    private static UIElement MapInput(Components.Input input)
    {
        // Map based on the input type
        return input.Type switch
        {
            Components.InputType.Checkbox => new ToggleButton(),
            // Components.InputType.Radio => new ToggleButton(), // Stride doesn't have a direct RadioButton in all versions; use ToggleButton for now
            _ => new EditText
            {
                Text = input.Value ?? ""
            },
        };
    }

    private static ImageElement MapImage(Components.Image image)
    {
        var img = new ImageElement();

        if (!string.IsNullOrEmpty(image.Source))
        {
            //img.Source = image.Source;
        }

        img.StretchType = image.Stretch switch
        {
            Components.ImageStretch.Uniform => StretchType.Uniform,
            Components.ImageStretch.UniformToFill => StretchType.UniformToFill,
            Components.ImageStretch.Fill => StretchType.Fill,
            Components.ImageStretch.FillOnStretch => StretchType.FillOnStretch,
            _ => StretchType.None
        };

        return img;
    }

    private Border MapBorder(Components.Border border)
    {
        var borderElem = new Border
        {
            BorderThickness = ComponentsThicknessToStride(border.Thickness, border)
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
                borderElem.Content = MapElement(border.Children[0]);
            }
            else
            {
                throw new NotSupportedException("Border element in SUIM supports only one child element for now.");
            }
        }

        return borderElem;
    }

    private StrideGrid MapGrid(Components.Grid grid)
    {
        var g = new StrideGrid();

        foreach (var childContainer in grid.GridChildren)
        {
            var childStride = MapElement(childContainer.Element);
            childStride.SetGridRow(childContainer.Row);
            childStride.SetGridColumn(childContainer.Column);
            childStride.SetGridRowSpan(childContainer.RowSpan);
            childStride.SetGridColumnSpan(childContainer.ColumnSpan);
            g.Children.Add(childStride);
        }

        return g;
    }

    private static StackPanel MapStack(Components.Stack stack)
    {
        return new StackPanel
        {
            Orientation = stack.Orientation == Components.Orientation.Horizontal 
                ? Orientation.Horizontal 
                : Orientation.Vertical
        };
    }

    private static void ApplyCommonProperties(Components.UIElement suim, UIElement stride)
    {
        stride.Name = suim.Id;
        stride.Opacity = suim.Opacity;
        stride.Visibility = suim.Visibility == "hidden" ? Visibility.Hidden : (suim.Visibility == "collapse" ? Visibility.Collapsed : Visibility.Visible);
        
        // Start simple with margins/padding parsing
        stride.Margin = ComponentsThicknessToStride(suim.Margin, suim);
        if (stride is ContentControl cc)
        {
            cc.Padding = ComponentsThicknessToStride(suim.Padding, suim); // Only ContentControl has padding in Stride basic? No, wrappers do.
        }

        // Alignment
        stride.HorizontalAlignment = suim.HorizontalAlignment switch
        {
            Components.HorizontalAlignment.Center => HorizontalAlignment.Center,
            Components.HorizontalAlignment.Right => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Left
        };

        stride.VerticalAlignment = suim.VerticalAlignment switch
        {
            Components.VerticalAlignment.Center => VerticalAlignment.Center,
            Components.VerticalAlignment.Bottom => VerticalAlignment.Bottom,
            _ => VerticalAlignment.Top
        };

        // For overlays, always use ActualWidth/ActualHeight from layout
        if (suim is Components.Overlay)
        {
            if (!float.IsNaN(suim.ActualWidth) && suim.ActualWidth > 0)
                stride.Width = suim.ActualWidth;
            if (!float.IsNaN(suim.ActualHeight) && suim.ActualHeight > 0)
                stride.Height = suim.ActualHeight;
        }
        else
        {
            var width = suim.ToPixels(suim.Width);
            if (width != 0f)
            {
                stride.Width = width;
            }

            var height = suim.ToPixels(suim.Height);
            if (height != 0f)
            {
                stride.Height = height;
            }
        }

        if (suim.BackgroundColor != null)
        {
            stride.BackgroundColor = ParseColor(suim.BackgroundColor);
        }

        if (suim.StopClicks)
        {
            stride.CanBeHitByUser = true;
        }
    }

    private static Thickness ComponentsThicknessToStride(Layout.Thickness thickness, Components.UIElement suim)
    {
        return new Thickness(
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
    
    private void TransferBindings(Components.UIElement suimElement, UIElement strideElement)
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

    private void TransferEvents(Components.UIElement suimElement, UIElement strideElement)
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
                var propName = handlerName.Substring(1);
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
                if (string.Equals(eventName, "click", StringComparison.OrdinalIgnoreCase) && strideElement is Button btn)
                {
                    BindClickHandler(btn, handler, suimElement);
                }
                // Add more event types here as needed
            }
        }
    }

    private void BindClickHandler(Button btn, Delegate handler, Components.UIElement suimElement)
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
        else if (handler is Action<Components.UIElement> actionWithElement)
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
    public Delegate? GetBoundClickHandler(Button btn)
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
    
    private static void SetupBinding(dynamic? model, string modelPropertyName, string targetPropertyName, UIElement strideElement)
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
    
    private static void ApplyBindingValue(UIElement strideElement, string targetPropertyName, object? value)
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
                else if (strideElement is Button btn && btn.Content is TextBlock btnText)
                    btnText.Text = value?.ToString() ?? "";
            }
            // Handle other common properties
            else if (string.Equals(targetPropertyName, "visibility", StringComparison.OrdinalIgnoreCase))
            {
                if (value != null && Enum.TryParse<Visibility>(value.ToString(), out var vis))
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
