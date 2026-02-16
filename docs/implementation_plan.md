# Fix Click Handlers and Event Resolution

The goal is to fix the reported issue where click handlers in SUIM markup (e.g., `onclick="QuitHandler"`) are not working. This is primarily caused by method ambiguity in `ObservableObject.GetHandler` and limited event mapping in `SUIMStride.TransferEvents`.

## User Review Required

> [!NOTE]
> I will be updating `ObservableObject` and `SUIMStride` to more intelligently resolve methods when multiple methods with the same name exist (overloading).

## Proposed Changes

### SUIM Core (`src/SUIM`)

#### [MODIFY] [ObservableObject.cs](file:///c:/Users/Office/source/repos/SUIM/src/SUIM/ObservableObject.cs)
- Improve `GetHandler`:
    - Replace `GetMethod` with `GetMethods` to avoid `AmbiguousMatchException`.
    - Implement a priority-based resolution:
        1. Parameterless methods (`Action`).
        2. Methods taking a single `UIElement` (`Action<UIElement>`).
        3. Methods following the `EventHandler` pattern.
    - Support creating generic `EventHandler<T>` delegates for specialized event args.

### SUIM Stride Integration (`src/SUIM.Stride`)

#### [MODIFY] [SUIMStride.cs](file:///c:/Users/Office/source/repos/SUIM/src/SUIM.Stride/SUIMStride.cs)
- Update `TransferEvents`:
    - Add support for `Action<SUIM.Components.UIElement>` handlers.
    - Improve reflection-based resolution for non-`ObservableObject` models to match the logic in `ObservableObject.GetHandler`.
    - Ensure `RoutedEventArgs` (Stride's default) is handled gracefully even when the model expects `EventArgs`.

## Verification Plan

### Automated Tests
- Update `EventBindingTests.cs` to include a test case with overloaded methods to verify the priority resolution.

### Manual Verification
- Run the `Chess3d` example.
- Click the "Quit" button and verify the confirmation popup appears (this verifies `public void QuitHandler()` is called).
- Click "YES" in the popup and verify the game exits (this verifies `private void QuitHandler(object sender, EventArgs e)` is called from code).
