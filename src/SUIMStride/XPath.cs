namespace SUIMStride;

using Stride.UI;
using SUIM;

public static class XPath
{
    public static UIElement? Find(UIElement root, string path)
    {
        return XPathHelper.FindElementByPath(root, path) as UIElement;
    }
}
