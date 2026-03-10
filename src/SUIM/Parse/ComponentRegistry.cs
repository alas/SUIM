namespace SUIM.Parse;

using System;
using System.Collections.Generic;
using SUIM.Parse.Components;

public static class ComponentRegistry
{
    private static readonly Dictionary<string, string> _fileRegistrations = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Func<UIElement>> _factoryRegistrations = new(StringComparer.OrdinalIgnoreCase);

    public static void Register(string tag, string filePath)
    {
        _fileRegistrations[tag] = filePath;
    }

    public static void Register(string tag, Func<UIElement> factory)
    {
        _factoryRegistrations[tag] = factory;
    }

    public static void Register<T>() where T : UIComponent, new()
    {
        var name = typeof(T).Name;
        Register(name, () => new T());
    }

    public static bool IsRegistered(string tag)
    {
        return _fileRegistrations.ContainsKey(tag) || _factoryRegistrations.ContainsKey(tag);
    }

    public static bool IsRegisteredFactory(string tag)
    {
        return _factoryRegistrations.ContainsKey(tag);
    }

    public static UIElement Create(string tag, bool onlyFactory = false)
    {
        if (_factoryRegistrations.TryGetValue(tag, out var factory))
        {
            return factory();
        }

        if (!onlyFactory && _fileRegistrations.TryGetValue(tag, out var filePath))
        {
            var result = new VirtualComponent(tag);
            result.SetAttribute("source", filePath);
            return result;
        }

        throw new NotSupportedException($"Unknown custom tag: {tag}");
    }
}
