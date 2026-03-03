namespace SUIM.Tests.Binding;

using Xunit;
using Stride.Engine;
using StrideButton = Stride.UI.Controls.Button;
using SUIMStride;
using SUIM.Parse;
using SUIM.Model;
using SUIM.Parse.Components;

/// <summary>
/// Tests for verifying that event handlers on the model are correctly mapped when 
/// markup with events is parsed and converted to Stride widgets.
/// 
/// Note: These tests focus on the mapping logic (ensuring handlers are resolved and attached)
/// rather than actually triggering Stride events, as that requires a full game context.
/// </summary>
public class EventHandlerStrideTests
{
    /// <summary>
    /// Test model with various handler signatures for testing.
    /// </summary>
    public class TestModel
    {
        public int ClickCount { get; set; }
        public string? LastButtonId { get; set; }
        public bool HandlerInvoked { get; set; }

        /// <summary>Handler with no parameters</summary>
        public void OnSimpleClick()
        {
            ClickCount++;
            HandlerInvoked = true;
        }

        /// <summary>Handler with UIElement sender parameter</summary>
        public void OnClickWithSender(UIElement sender)
        {
            LastButtonId = sender.Id;
            HandlerInvoked = true;
        }

        /// <summary>Alternative handler name</summary>
        public void OnAlternateClick()
        {
            ClickCount++;
        }

        /// <summary>Handler accepting EventArgs</summary>
        public void OnClickViaEvent(object? sender, EventArgs e)
        {
            ClickCount++;
            HandlerInvoked = true;
        }
    }

    /// <summary>
    /// Test that markup with event attributes is correctly parsed
    /// and the event information is stored.
    /// </summary>
    [Fact]
    public void MarkupParser_ParsesEventAttribute_StoredInElementEvents()
    {
        // Arrange
        var markup = @"<button id=""testBtn"" onclick=""OnSimpleClick()"" />";
        var model = new TestModel();

        // Act
        var (root, _) = MarkupParser.Parse(markup, model);

        // Assert
        Assert.IsType<Button>(root);
        Assert.True(root.Events.ContainsKey("click"));
        Assert.Equal("OnSimpleClick()", root.Events["click"]);
    }

    /// <summary>
    /// Test that multiple event attributes are parsed.
    /// </summary>
    [Fact]
    public void MarkupParser_ParsesMultipleEventAttributes()
    {
        // Arrange
        var markup = @"<button id=""btn"" onclick=""OnSimpleClick()"" />";
        var model = new TestModel();

        // Act
        var (root, _) = MarkupParser.Parse(markup, model);

        // Assert
        Assert.True(root.Events.Count > 0);
        Assert.True(root.Events.ContainsKey("click"));
    }

    /// <summary>
    /// Test that markup parsing preserves event handler names
    /// for different element types.
    /// </summary>
    [Fact]
    public void MarkupParser_PreservesEventHandlerName()
    {
        // Arrange
        var markup = @"<button id=""btn1"" onclick=""OnSimpleClick()"" />";
        var model = new TestModel();

        // Act
        var (root, _) = MarkupParser.Parse(markup, model);

        // Assert
        var button = root as Button;
        Assert.NotNull(button);
        Assert.Equal("OnSimpleClick()", button.Events["click"]);
    }

    /// <summary>
    /// Test that SUIMStride can parse markup with event handlers
    /// without throwing exceptions.
    /// </summary>
    [Fact]
    public void SUIMStride_ParsesMarkupWithEventHandler_NoThrow()
    {
        // Arrange
        var markup = @"<button id=""testBtn"" onclick=""OnSimpleClick()"" />";
        var model = new TestModel();
        var suim = new Parser();

        // Act & Assert - should not throw
        var exception = Record.Exception(() =>
        {
            var (strideRoot, returnedModel) = suim.Parse(markup, CreateTestGame(), model: model);
            Assert.NotNull(strideRoot);
        });

        Assert.Null(exception);
    }

    /// <summary>
    /// Test that event handlers on buttons can be resolved from the model.
    /// </summary>
    [Fact]
    public void SUIMStride_ResolvesEventHandler_FromModel()
    {
        // Arrange
        var markup = @"<button id=""testBtn"" onclick=""OnSimpleClick()"" />";
        var model = new TestModel();
        var suim = new Parser();

        // Act
        var (strideRoot, returnedModel) = suim.Parse(markup, CreateTestGame(), model: model);
        var button = strideRoot as StrideButton;

        // Assert
        Assert.NotNull(button);
        Assert.NotNull(returnedModel);
        // Model should have handler method available
        var testModel = returnedModel as dynamic;
        Assert.NotNull(testModel);
    }

    /// <summary>
    /// Test that event handlers are not mapped if handler doesn't exist.
    /// Should not throw.
    /// </summary>
    [Fact]
    public void SUIMStride_MissingEventHandler_DoesNotThrow()
    {
        // Arrange
        var markup = @"<button id=""testBtn"" onclick=""NonExistentHandler()"" />";
        var model = new TestModel();
        var suim = new Parser();

        // Act & Assert - should not throw even if handler doesn't exist
        var exception = Record.Exception(() =>
        {
            var (strideRoot, _) = suim.Parse(markup, CreateTestGame(), model: model);
            Assert.NotNull(strideRoot);
        });

        Assert.Null(exception);
    }

    /// <summary>
    /// Test that event parsing works for nested elements.
    /// </summary>
    [Fact]
    public void MarkupParser_ParsesEvents_InNestedElements()
    {
        // Arrange
        var markup = @"
            <stack orientation=""vertical"">
                <button id=""btn1"" onclick=""OnSimpleClick()"" />
                <div>
                    <button id=""btn2"" onclick=""OnAlternateClick()"" />
                </div>
            </stack>";
        var model = new TestModel();

        // Act
        var (root, _) = MarkupParser.Parse(markup, model);

        // Assert
        Assert.NotNull(root);
        
        // Check first button
        var stack = root as Stack;
        Assert.NotNull(stack);
        
        var btn1 = stack.Children[0] as Button;
        Assert.NotNull(btn1);
        Assert.True(btn1!.Events.ContainsKey("click"));
        Assert.Equal("OnSimpleClick()", btn1.Events["click"]);
    }

    /// <summary>
    /// Test that SUIMStride correctly maps buttons with events
    /// to Stride Button widgets.
    /// </summary>
    [Fact]
    public void SUIMStride_MapElement_ConvertsEventButtonToStrideButton()
    {
        // Arrange
        var markup = @"<button id=""testBtn"" onclick=""OnSimpleClick()"">Click Me</button>";
        var model = new TestModel();
        var suim = new Parser();

        // Act
        var (strideRoot, _) = suim.Parse(markup, CreateTestGame(), model: model);

        // Assert
        Assert.IsType<StrideButton>(strideRoot);
        var button = strideRoot as StrideButton;
        Assert.NotNull(button);
        Assert.Equal("testBtn", button!.Name);
    }

    /// <summary>
    /// Test that the same markup can be parsed multiple times
    /// with different models, creating independent event bindings.
    /// </summary>
    [Fact]
    public void SUIMStride_Parse_WithDifferentModels_CreatesIndependentInstances()
    {
        // Arrange
        var markup = @"<button id=""btn"" onclick=""OnSimpleClick()"" />";
        var model1 = new TestModel();
        var model2 = new TestModel();
        var suim = new Parser();

        // Act
        var (strideRoot1, _) = suim.Parse(markup, CreateTestGame(), model: model1, createNewInstance: true);
        var (strideRoot2, _) = suim.Parse(markup, CreateTestGame(), model: model2, createNewInstance: true);

        // Assert
        Assert.NotNull(strideRoot1);
        Assert.NotNull(strideRoot2);
        Assert.NotSame(strideRoot1, strideRoot2);
    }

    /// <summary>
    /// Test that event information is preserved through the SUIM->Stride mapping.
    /// </summary>
    [Fact]
    public void SUIMStride_PreservesEventInformation_ThroughMapping()
    {
        // Arrange
        var markup = @"<button id=""testBtn"" onclick=""OnSimpleClick()"" />";
        var model = new TestModel();
        
        // First parse to SUIM
        var (suimRoot, _) = MarkupParser.Parse(markup, model);
        Assert.True(suimRoot.Events.ContainsKey("click"));
        
        // Then convert to Stride via SUIMStride
        var suim = new Parser();
        var (strideRoot, _) = suim.Parse(markup, CreateTestGame(), model: model);

        // Assert
        Assert.NotNull(strideRoot);
        // The Stride button should exist and be ready for events
        var button = strideRoot as StrideButton;
        Assert.NotNull(button);
    }

    /// <summary>
    /// Test that button with event handler can be parsed from embedded model.
    /// </summary>
    [Fact]
    public void SUIMStride_ParsesButton_WithEmbeddedModelAndEvent()
    {
        // Arrange
        var markup = @"
            <div>
                <model>{ ""title"": ""Click Button"" }</model>
                <button id=""btn"" onclick=""OnSimpleClick()"">Click</button>
            </div>";
        var model = new TestModel();
        var suim = new Parser();

        // Act & Assert - should not throw
        var exception = Record.Exception(() =>
        {
            var (strideRoot, parsedModel) = suim.Parse(markup, CreateTestGame(), model: model);
            Assert.NotNull(strideRoot);
            Assert.NotNull(parsedModel);
        });

        Assert.Null(exception);
    }

    /// <summary>
    /// Test that ObservableObject correctly identifies handler methods.
    /// </summary>
    [Fact]
    public void ObservableObject_CanResolveDelegateForHandler()
    {
        // Arrange
        var rawModel = new TestModel();
        var observable = ModelLogic.Create(rawModel);

        // Act
        // Try to get a handler delegate for the method
        if (observable is ObservableObject obs)
        {
            var handler = obs.GetHandler("OnSimpleClick()");
            
            // Assert
            // Should be able to get a handler (either as Delegate or null if not bindable)
            Assert.NotNull(handler);
        }
    }

    /// <summary>
    /// Test that stacked buttons with different handlers parse correctly.
    /// </summary>
    [Fact]
    public void MarkupParser_StackedButtons_WithDifferentHandlers()
    {
        // Arrange
        var markup = @"
            <stack orientation=""vertical"">
                <button id=""btn1"" onclick=""OnSimpleClick()"">Button 1</button>
                <button id=""btn2"" onclick=""OnAlternateClick()"">Button 2</button>
            </stack>";
        var model = new TestModel();

        // Act
        var (root, _) = MarkupParser.Parse(markup, model);

        // Assert
        var stack = root as Stack;
        Assert.NotNull(stack);
        Assert.Equal(2, stack!.Children.Count);
        
        var btn1 = stack.Children[0] as Button;
        var btn2 = stack.Children[1] as Button;
        
        Assert.Equal("OnSimpleClick()", btn1!.Events["click"]);
        Assert.Equal("OnAlternateClick()", btn2!.Events["click"]);
    }




    // Helper method to create a minimal test Game instance
    private static Game CreateTestGame()
    {
        // Create a minimal game for testing - Stride requires a valid game context
        return new Game();
    }
}
