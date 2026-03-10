namespace SUIM.Parse;

using System;
using System.Collections.Generic;
using SUIM.Parse.Components;

public static class ComponentRegistry
{
    private static readonly Dictionary<string, (bool, string)> _fileRegistrations = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, (bool, Func<UIElement>)> _factoryRegistrations = new(StringComparer.OrdinalIgnoreCase);

    public static void Register(string tag, bool isView, string filePath)
    {
        _fileRegistrations[tag] = (isView, filePath);
    }

    public static void Register(string tag, bool isView, Func<UIElement> factory)
    {
        _factoryRegistrations[tag] = (isView, factory);
    }

    public static void Register<T>(bool isView) where T : UIComponent, new()
    {
        var name = typeof(T).Name;
        Register(name, isView, () => new T());
    }

    public static bool IsRegistered(string tag)
    {
        return _fileRegistrations.ContainsKey(tag) || _factoryRegistrations.ContainsKey(tag);
    }

    public static UIElement Create(string tag)
    {
        if (_factoryRegistrations.TryGetValue(tag, out var data))
        {
            var element = data.Item2();
            if (element is VirtualComponent vc)
            {
                vc.IsView = data.Item1;
            }
            return element;
        }

        if (_fileRegistrations.TryGetValue(tag, out var data2))
        {
            var result = new VirtualComponent(tag);
            var filePath = data2.Item2;
            result.SetAttribute("source", filePath);
            return result;
        }

        throw new NotSupportedException($"Unknown custom tag: {tag}");
    }
}
