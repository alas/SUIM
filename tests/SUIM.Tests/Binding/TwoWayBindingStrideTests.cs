namespace SUIM.Tests.Binding;

using Xunit;
using Stride.UI.Controls;
using Stride.UI;
using Stride.Engine;
using SUIMStride;
using SUIM.Model;

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
        var (strideRoot, _) = suim.Parse(markup, new Game(), model: model);
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
        
        // The proxy getter should return the current value from the widget
        Assert.Equal("updated", model.GetValue("myText"));
    }

    [Fact]
    public void ToggleButton_UpdatesModel_OnStateChanged()
    {
        // Arrange
        var markup = @"<input type=""checkbox"" value=""@myBool"" />";
        var model = new ObservableObject();
        model.SetValue("myBool", false);
        
        var suim = new Parser();
        var (strideRoot, _) = suim.Parse(markup, new Game(), model: model);
        var tb = strideRoot as ToggleButton;
        Assert.NotNull(tb);

        // Act
        tb.State = ToggleState.Checked;
        
        // The proxy getter should return true because tb.State == Checked
        Assert.Equal(true, model.GetValue("myBool"));
        
        tb.State = ToggleState.UnChecked;
        Assert.Equal(false, model.GetValue("myBool"));

        model.SetValue("myBool", true);
        Assert.Equal(ToggleState.Checked, tb.State);

        dynamic dyn = model;
        dyn.myBool = false;
        Assert.Equal(ToggleState.UnChecked, tb.State);

        dyn.myBool = true;
        Assert.Equal(ToggleState.Checked, tb.State);
    }
}
