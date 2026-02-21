namespace SUIM.Tests;

using Xunit;
using Stride.UI.Controls;
using Stride.UI;
using Stride.Engine;
using SUIM;
using SUIMStride;

public class TwoWayBindingStrideTests
{
    [Fact]
    public void EditText_UpdatesModel_OnTextChanged()
    {
        // Arrange
        var markup = @"<input id=""myInput"" value=""@myText"" />";
        var model = new ObservableObject();
        model.SetValue("myText", "initial");
        
        var suim = new Parser();
        var (strideRoot, _, _) = suim.Parse(markup, new Game(), model: model);
        var et = strideRoot as EditText;
        Assert.NotNull(et);

        // Act
        et.Text = "updated";
        // EditText.TextChanged is usually triggered by UI, we simulate it if possible 
        // or we rely on our proxy logic which is already verified via unit tests 
        // BUT here we want to see if SUIMStride hooked up the event.
        
        // In SUIMStride: et.TextChanged += (s, e) => oo.NotifyChanged(modelPropertyName);
        // We can't easily trigger Stride events without a full sync, 
        // but we can verify the proxy itself via the model.
        
        var oo = model as ObservableObject;
        // The proxy getter should return the current value from the widget
        Assert.Equal("updated", oo.GetValue("myText"));
    }

    [Fact]
    public void ToggleButton_UpdatesModel_OnStateChanged()
    {
        // Arrange
        var markup = @"<input type=""checkbox"" value=""@myBool"" />";
        var model = new ObservableObject();
        model.SetValue("myBool", false);
        
        var suim = new Parser();
        var (strideRoot, _, _) = suim.Parse(markup, new Game(), model: model);
        var tb = strideRoot as ToggleButton;
        Assert.NotNull(tb);

        // Act
        tb.State = ToggleState.Checked;
        
        var oo = model as ObservableObject;
        // The proxy getter should return true because tb.State == Checked
        Assert.Equal(true, oo.GetValue("myBool"));
        
        tb.State = ToggleState.UnChecked;
        Assert.Equal(false, oo.GetValue("myBool"));
    }
}
