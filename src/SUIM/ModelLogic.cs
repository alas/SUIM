namespace SUIM;

using System.Text.Json;
using System.Xml.Linq;

public static class ModelLogic
{
    public static dynamic Create(object model)
    {
        if (model is ObservableObject oo) return oo;

        var observable = new ObservableObject();
        observable.Initialize(model);
        return observable;
    }

    public static dynamic? ExtractModel(XElement root, dynamic? model)
    {
        var modelElement = root.Elements()
            .FirstOrDefault(e => e.Name.LocalName.Equals("model", StringComparison.OrdinalIgnoreCase));

        if (modelElement != null)
        {
            // Get the content of the model element
            var content = modelElement.Value.Trim();

            if (!string.IsNullOrEmpty(content))
            {
                model = MergeModels(model, content);
            }
        }

        return model;
    }

    private static dynamic? MergeModels(dynamic? existingModel, string modelJson)
    {
        try
        {
            // Parse JSON into a dictionary
            var options = new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip };
            var jsonObject = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(modelJson, options);
            if (jsonObject == null)
            {
                return existingModel;
            }

            // Convert JsonElement objects to standard .NET types
            var modelDict = ConvertJsonElementDictionary(jsonObject);

            // If no existing model, create from JSON
            if (existingModel == null)
            {
                return CreateDynamicFromDictionary(modelDict);
            }

            // Merge: extract properties from existing model, then add JSON values
            var mergedDict = ExtractPropertiesAsDictionary(existingModel);
            foreach (var kvp in modelDict)
            {
                // If the existing model already has this property, keep its value; otherwise use JSON value
                if (!mergedDict.ContainsKey(kvp.Key))
                {
                    mergedDict[kvp.Key] = kvp.Value;
                }
            }

            // Preserve the source object if we had one
            object? source = null;
            if (existingModel is ObservableObject oo)
            {
                var sourceField = typeof(ObservableObject).GetField("_source", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                source = sourceField?.GetValue(oo);
            }

            return CreateDynamicFromDictionary(mergedDict, source);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse model JSON: {ex.Message}", ex);
        }
    }

    private static Dictionary<string, object?> ConvertJsonElementDictionary(Dictionary<string, JsonElement> jsonObject)
    {
        var result = new Dictionary<string, object?>();
        foreach (var kvp in jsonObject)
        {
            result[kvp.Key] = ConvertJsonElement(kvp.Value);
        }
        return result;
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var intVal) ? intVal : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToArray(),
            JsonValueKind.Object => ConvertJsonElementDictionary(
                element.EnumerateObject().ToDictionary(p => p.Name, p => p.Value)
            ),
            _ => null
        };
    }

    private static Dictionary<string, object?> ExtractPropertiesAsDictionary(dynamic? model)
    {
        var dict = new Dictionary<string, object?>();
        if (model == null)
        {
            return dict;
        }

        // If it's an ObservableObject, try to extract its properties
        if (model is ObservableObject)
        {
            var modelType = model.GetType();
            var propertiesField = modelType.GetField("_properties",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (propertiesField?.GetValue(model) is Dictionary<string, object?> properties)
            {
                return new Dictionary<string, object?>(properties);
            }
        }

        // Otherwise, extract using reflection
        foreach (var prop in model.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            if (prop.CanRead)
            {
                dict[prop.Name] = prop.GetValue(model);
            }
        }

        return dict;
    }

    private static dynamic CreateDynamicFromDictionary(Dictionary<string, object?> dict, object? source = null)
    {
        var observable = new ObservableObject();
        if (source != null)
        {
            observable.Initialize(source);
        }
        
        // Set properties directly into the observable (this overrides/sets dictionary values)
        foreach (var kvp in dict)
        {
            observable.SetValue(kvp.Key, kvp.Value);
        }
        return observable;
    }
}
