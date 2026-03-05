# SUIM 1.0 Technical Specification

## Overview

SUIM aims to reuse existing tags and concepts found in HTML and CSS while also incorporating foundational concepts from WinForms, such as explicit anchoring and docking logic, for desktop-grade application development.
SUIM aims to be familiar to web developers and easy to learn for winforms developers.

### SUIM officially supports:

#### Layout

* ✅ Custom Layout Tags Based on WinForms Box Model(Dock, Stack, Grid, Overlay)
* ✅ Flexbox
* ❌ Grid (maybe later)
* ❌ float / clear
* ❌ position: fixed

#### Styling

* ❌ advanced selectors
* ❌ complex inheritance

#### Behavior

* ✅ click / input events
* ❌ Javascript

---

## Structural & Layout Tags

### The <div> Tag

A simple container where children are arranged in a vertical stack by default. It also support flexbox layout.

### The <stack> Tag

Arranges children sequentially along a single axis.

* **Attributes:** orientation (horizontal, vertical).
* **Synonyms:**
* **<vstack>**, **<stackv>**, **<stack-v>** and **<vbox>**: Equivalent to <stack orientation="vertical">.
* **<hstack>**, **<stackh>**, **<stack-h>** and **<hbox>**: Equivalent to <stack orientation="horizontal">.

### The <grid> Tag

Divides space into a matrix.

* **Attributes:** columns, rows.
* **Child Logic:** grid.row, grid.column, grid.rowspan, grid.columnspan.

**Example 1: Explicit Attributes**
<grid columns="100, 500" rows="50, 500">
	<div grid.row="0" grid.column="0" bg="gray" />
	<div grid.row="0" grid.column="1" bg="silver" />
	<div grid.row="1" grid.column="0" grid.columnspan="2" bg="white" />
</grid>

**Example 2: Using <row>**
<grid>
	<row height="500px">
		<div width="100" bg="blue" />
		<div width="auto" bg="green" />
		<div width="auto" bg="red" />
	</row>
</grid>

**Example 3: Using <column>**
<grid columns="200, auto">
	<column>
		<div height="100" bg="blue" />
		<div height="auto" bg="green" />
		<div height="auto" bg="red" />
	</column>
</grid>

### The <dock> Tag

Pins children to edges. Mirrors WinForms **DockPanel** behavior.

* **Attributes:** lastchildfill (default true).
* **Child Logic:** dock.edge (left, right, top, bottom).

### The <overlay> Tag

Forces itself to parent size and intercepts all input. **Overlays always render on the highest global layer**.

---

## Content Tags

### The <button> Tag

Interactive element for triggering actions.

* `normal`: `string` - The sprite for the "Idle" state.
* `hover`: `string` - The sprite for "Hover" state.
* `pressed`: `string` - The sprite for "Pressed" state.
* `onclick`: `string` - Method name in the model to call.

### The <input> Tag

Data entry field.

* `type`: `string` - text, password, number, range, date, time, datetime, datetime-local, checkbox, radio, button.
* `mask`: `string` - C# regex.
* `placeholder`: `string` - placeholder.
* `step`: `integer` - step for number slider.
* `min`: `integer` - min for number slider.
* `max`: `integer` - max for number slider.

### The <textarea> Tag

Multi-line text input for long content.

### The <select> & <option> Tags

Dropdown menu. Supports **multiple** selection attribute.

### The <label> Tag

Text Display.

* `value`: `string` - The string to display.
* `font`: `string` - Asset name of the SpriteFont.
* `fontSize`: `integer` - Base size for measurement.
* `color`: `Color` - Text color.
* `wrap`: `bool` - Enable word wrapping.

### The <image> Tag

Graphic Display.

* `source`: `string` - Sprite or Texture name.
* `stretch`: `enum` - `None`, `Fill`, `Uniform`, `UniformToFill`.


## The scroll Attribute & Constraints

The **scroll** attribute triggers a structural transformation. The tag is wrapped in an outer scroll-viewport (the scroll component), which inherits **all of the tag's styling** (including size, background, borders, and padding). The original tag remains as the direct child of the scroll-viewport, containing all nested children. Example:

Original.suim
```xml
<dock class="myclass" scroll="vertical">
<style>
.myclass {
	width: 500;
	height: 400;
	scroll.width: 10000;
	scroll.height: 800;
}
</style>
	<label value="Inventory" />
</dock>

```

**Final C# Tree:**

* `Stackpanel (Orientation: "Vertical", width: 500, height: 400)`
* `Dockpanel (width: 10000, height: 800)`
* `Label` (Text: "Inventory")

### Allowed Variations

* **scroll="vertical"**: Outer wrapper is a vertical scroll-viewport.
* **scroll="horizontal"**: Outer wrapper is a horizontal scroll-viewport.
* **scroll="both"**: Outer wrapper is a horizontal and vertical scroll-viewport.

## The border Attribute

The **border** attribute triggers a structural transformation. The tag is wrapped in an outer border-element (the border component). The original tag remains as the direct child of the border, containing all nested children. Example:

Original.suim
```xml
<div class="myclass">
<style>
.myclass {
	width: 500;
	height: 400;
	border: 10 White;
}
</style>
	<label value="Inventory" />
</div>

```

**Final C# Tree:**

* `Border (Thicknes: 10, Color: "White")`
* `Div (width: 10000, height: 800)`
* `Label` (Text: "Inventory")

Original.suim
```xml
<div class="myclass">
<style>
.myclass {
	width: 500;
	height: 400;
	border: 10 5 0 2 White;
}
</style>
	<label value="Inventory" />
</div>

```

**Final C# Tree:**

* `Border (Thicknes: 10 5 0 2, Color: "White")`
* `Div (width: 10000, height: 800)`
* `Label` (Text: "Inventory")

---

#### Special Formatting Rules for Parser

 **Attribute Precedence:**
   When the same property is defined in multiple places, the following precedence applies (highest wins):
   * Inline attributes (e.g., `<div width="100">`) - **Highest priority**
   * ID selector in CSS (e.g., `#myDiv { width: 200; }`)
   * Class selector in CSS (e.g., `.myClass { width: 150; }`)
   * Tag selector in CSS (e.g., `div { width: 120; }`)
   * Universal selector in CSS (e.g., `* { width: 100; }`) - **Lowest priority**

 **The `@` Prefix:**
* If any attribute starts with `@`, the **Hydrator** must create a `PropertyBinding` instead of a static assignment.


---


### Control Flow (The `@` Directive)

Control flow is resolved during the **Expansion Pass** before the layout engine runs.
Supported syntax is:

@if identifierbool
{
	<label value="true" />
}

Produces the label if identifierbool exist in the model with a value of true.


@if identifierbool
{
	<label value="true" />
}
else
{
	<label value="not!" />
}

Produces the correct label depending on the value of identifierbool.

@if identifierbool
{
	<label value="true" />
}
else if identifierbool2
{
	<label value="true2" />
}
else
{
	<label value="not!" />
}

Produces the correct label depending on the values of identifierbool and identifierbool2.

@switch identifierany
{
    case "valuestring"
	{
        <p>Loading...</p>
    }
    case 500
	{
        <p>Loading 2...</p>
    }
    case @identifier2
	{
        <p>value of identifierany is equal to value of identifier2 !!</p>
    }
    default
	{
        <p>default case</p>
    }
}

Produces the correct p tag depending on the values of identifierany and identifier2.

@for i=0 count=100
{
	<label value="@i" />
}

The parser must clone the inner XML (label in this case) 100 times, i will go from 0 to 99 (100 different values).

@for i=0 count=100 step=-1
{
	<label value="@i" />
}

The parser must clone the inner XML (label in this case) 100 times, i will go from 0 to -99 (100 different values, each pass will add -1 to i).

@foreach myitem in Collection
{
	<label value="@myitem.Property" />
}

The parser must clone the inner XML for every item in the `Collection`.

@foreach i in 0..100
{
	<label value="@i" />
}

The parser must clone the inner XML (label in this case) 100 times, i will go from 0 to 99 (not inclusive end value).

Within the loop, `@i` or `@item` or `@item.Property` acts as a local binding, the engine will call .ToString() if the types dont match and the target type is string, otherwise it will fail.


### Custom Components (Tags)

* Any tag not in the primitive list is treated as a file-based component (e.g., `<MyButton />` and `<mybutton />` looks for `MyButton.suim`, everything is case insensitive).
* Attributes passed to custom tags override the local `<model>`.

Original.suim
```xml
<div>
	<inventory showTitle="false" />
</div>

```

Inventory.suim
```xml
<inventory>
	<model> { showTitle: true } </model>
	<vstack>
		@if (showTitle)
		{
			<label value="Inventory" />
		}
		<grid width="@invWidth" />
	</vstack>
</inventory>

```

Result in this output:
```xml
<div>
	<vstack>
		<grid width="@invWidth" />
	</vstack>
</div>

```


### Data Binding Syntax

* **Static:** `width="100"` (Immediate value)
* **Dynamic Binding:** `width="@currentWidth"` (Links to a Model Property via Reflection/Getters)


---

## Example Input/Output

**Input (`Inventory.suim`):**

```xml
<inventory>
	<model> { showTitle: true } </model>
	<vstack>
		@if (showTitle)
		{
			<label value="Inventory" />
		}
		<grid width="@invWidth" />
	</vstack>
</inventory>

```

**Intermediate Flat XML (after Processor):**

```xml
<inventory>
	<vstack>
		<label value="Inventory" />
		<grid width="@invWidth" />
	</vstack>
</inventory>

```

**Final C# Tree:**

* `Stackpanel (Orientation: "Vertical")`
* `Label` (Text: "Inventory")
* `Grid` (Binding: Width -> Model.invWidth)


---

## ⚠️ Note: Yoga gotchas

Always set explicit root size (Width, Height)

Use FlexGrow = 1 instead of relying on percentages for fill

Avoid % height deep in the tree unless all ancestors have sizes

If you animate sizes, call CalculateLayout() after changes


Wood Image from:
Kai photographer
https://www.vecteezy.com/photo/3498715-seamless-texture-wood-old-oak-or-modern-wood-texture

Button Image from:
Gamanbit – Survival Dark Style
https://gamanbit.itch.io/survival-dark-style-free-starter-asset-pack
