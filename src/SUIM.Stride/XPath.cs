namespace SUIM.StrideIntegration;

using Stride.UI;

public static class XPath
{
    public static UIElement? Find(UIElement root, string path)
    {
        return XPathHelper.FindElementByPath(root, path) as UIElement;
    }
}
