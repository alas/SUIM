namespace SUIM;

using System;
using System.Collections.Generic;
using SUIM.Components;

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

    public static bool IsRegistered(string tag)
    {
        return _fileRegistrations.ContainsKey(tag) || _factoryRegistrations.ContainsKey(tag);
    }

    public static UIElement Create(string tag)
    {
        if (_factoryRegistrations.TryGetValue(tag, out var factory))
        {
            var element = factory();
            return element;
        }

        if (_fileRegistrations.TryGetValue(tag, out var filePath))
        {
            return new CustomComponent(tag) { Source = filePath };
        }

        throw new NotSupportedException($"Unknown custom tag: {tag}");
    }
}
