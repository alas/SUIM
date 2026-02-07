# SUIM 1.0 Technical Specification

## 1. Overview

SUIM is a performance-first markup language architected for absolute layout predictability. It aims to reuse existing tags and concepts found in HTML and CSS that do not break determinism or degrade performance.
SUIM also incorporates foundational concepts from WinForms, such as explicit anchoring and docking logic, for desktop-grade application development.
SUIM aims to be familiar to web developers while also being easy to implement on top of existing layout engines.
The engine utilizes a top-down single-pass architecture to ensure $O(N)$ performance, providing high-speed rendering and zero layout jitter by eliminating expensive reflow cycles.
SUIM maps markup tags to native widgets.


---

## 1. Layout Models & Philosophy

### 1.1 The Box Model

Every element exists within a rectangular box. Spacing is governed by **margin** (external) and **padding** (internal). **Children never expand parents**; parent dimensions are absolute and calculated before children are processed.

### 1.2 Layout Alignment

All layout containers support the following for child positioning:

* **horizontalalignment** (synonym: **halign**): left, center, right, stretch.
* **verticalalignment** (synonym: **valign**): top, center, bottom, stretch.

### 1.3 Anchoring

Inspired by WinForms, **anchor** allows an element to define its relationship to parent bounds. Anchored elements are removed from standard flow and do not occupy space in stacks or grids.

### 1.4 Layout Sizing Logic

Sizing uses **integers** (fixed pixels), **star-ratios** (proportional space), **em**, **rem**, or **auto**.

* **em:** A multiplier of the parent font size.
* **rem:** A multiplier of the global root font size.
* **auto:** Resolves to a pre-defined metric constant (see Section 5.3).

**Default Sizing Rule:** Content tags default to **auto**, Structural & Layout Tags (except <row> and <column>) default to **1***.

---

## 2. Structural & Layout Tags

### 2.1 The <div> Tag

A coordinate-based container where children can overlap and define explicit positions.

* **Usage:** Grouping or absolute layouts.

### 2.2 The <stack> Tag

Arranges children sequentially along a single axis.

* **Attributes:** orientation (horizontal, vertical).
* **Synonyms:**
* **<vstack>** and **<vbox>**: Equivalent to <stack orientation="vertical">.
* **<hstack>** and **<hbox>**: Equivalent to <stack orientation="horizontal">.

### 2.3 The <grid> Tag

Divides space into a matrix.

* **Attributes:** columns, rows.
* **Child Logic:** grid.row, grid.column, grid.rowspan, grid.columnspan.

**Example 1: Explicit Attributes**
<grid columns="100, *" rows="50, *">

<div grid.row="0" grid.column="0" bg="gray" />
<div grid.row="0" grid.column="1" bg="silver" />
<div grid.row="1" grid.column="0" grid.columnspan="2" bg="white" />
</grid>

**Example 2: Using <row>**
<grid>
	<row height="2rem">
		<div width="100" bg="blue" />
		<div width="*" bg="green" />
		<div width="2*" bg="red" />
	</row>
</grid>

**Example 3: Using <column>**
<grid columns="200, *">
	<column>
		<div height="100" bg="blue" />
		<div height="*" bg="green" />
		<div height="3*" bg="red" />
	</column>
</grid>

### 2.4 The <dock> Tag

Pins children to edges. Mirrors WinForms **DockPanel** behavior.

* **Attributes:** lastchildfill (default true).
* **Child Logic:** dock.edge (left, right, top, bottom).

### 2.5 The <overlay> Tag

Forces itself to parent size and intercepts all input. **Overlays always render on the highest global layer**, regardless of the z-index of other elements.

---

## 3. Content Tags

### 3.1 The <button> Tag

Interactive element for triggering actions.

* `text`: `string` - Optional label inside.
* `sprite`: `string` - The 9-slice sprite for the "Idle" state.
* `hoverSprite`: `string` - The sprite for "Hover" state.
* `pressedSprite`: `string` - The sprite for "Pressed" state.
* `onClick`: `string` - Method name in the model to call.

### 3.2 The <input> Tag

Data entry field.

* `type`: `string` - text, password, number, range, date, time, datetime, datetime-local, checkbox, radio, button.
* `mask`: `string` - C# regex.
* `placeholder`: `string` - placeholder.
* `step`: `integer` - step for number slider.
* `min`: `integer` - min for number slider.
* `max`: `integer` - max for number slider.

### 3.3 The <textarea> Tag

Multi-line text input for long content.

### 3.4 The <select> & <option> Tags

Dropdown menu. Supports **multiple** selection attribute.

### 3.5 The <label> Tag

Text Display.

* `text`: `string` - The string to display.
* `font`: `string` - Asset name of the SpriteFont.
* `fontSize`: `integer` - Base size for measurement.
* `color`: `Color` - Text color.
* `wrap`: `bool` - Enable word wrapping.

### 3.5 The <image> Tag

Graphic Display.

* `source`: `string` - Sprite or Texture name.
* `stretch`: `enum` - `None`, `Fill`, `Uniform`, `UniformToFill`.


## 5. The scroll Attribute & Constraints

The **scroll** attribute triggers a structural transformation. The tag is wrapped in an outer scroll-viewport (the scroll component), which inherits **all of the tag's styling** (including size, background, borders, and padding). The original tag remains as the direct child of the scroll-viewport, containing all nested children. Example:

Original.suim
```xml
<style>
.myclass {
	width: 500,
	height: 400,
	scroll.width: 10000,
	scroll.height: 800
}
<style>
<dock class="myclass" scroll="vertical">
	<label text="Inventory" />
</dock>

```

**Final C# Tree:**

* `Stackpanel (Orientation: "Vertical", width: 500, height: 400)`
* `Dockpanel (width: 10000, height: 800)`
* `Label` (Text: "Inventory")

### 5.2 Allowed Variations

* **scroll="vertical"**: Outer wrapper is a vertical scroll-viewport.
* **scroll="horizontal"**: Outer wrapper is a horizontal scroll-viewport.
* **scroll="both"**: Outer wrapper is a horizontal and vertical scroll-viewport.

### 5.3 The "auto" Rule (Experimental)

`auto` resolutions are determined by the engine metric table (using the font information to calculate text sizes) rather than content measurement, ensuring efficiency.

## 5. The border Attribute

The **border** attribute triggers a structural transformation. The tag is wrapped in an outer border-element (the border component). The original tag remains as the direct child of the border, containing all nested children. Example:

Original.suim
```xml
<style>
.myclass {
	width: 500,
	height: 400,
	border: 10 White,
}
<style>
<dock class="myclass">
	<label text="Inventory" />
</dock>

```

**Final C# Tree:**

* `Border (Thicknes: 10, Color: "White")`
* `Dockpanel (width: 10000, height: 800)`
* `Label` (Text: "Inventory")

---

## 6. Language Grammar & Markup Syntax

### A. Primitive Elements & Attributes

Every element in SUIM inherits a set of **Common Attributes** for layout and styling.

#### 1. Common Attributes (Supported by ALL tags)

| Attribute | Type | Description |
| --- | --- | --- |
| `id` | `string` | Unique identifier for the element. |
| `width` | `integer/string` | Fixed pixels (e.g., `100`) or `@variable`. |
| `height` | `integer/string` | Fixed pixels or `@variable`. |
| `padding` | `integer/string` | Shorthand for all sides (e.g., `10`). |
| `margin` | `integer/string` | External spacing (e.g., `5`). |
| `horizontalalignment` | `enum` | `Left`, `Center`, `Right`, `Stretch`. |
| `verticalalignment` | `enum` | `Top`, `Center`, `Bottom`, `Stretch`. |
| `visibility` | `enum/bool` | `Visible`, `Collapsed`, `Hidden`. |
| `opacity` | `integer` | Transparency `0.0` to `1.0`. |
| `background` | `string/Color` | Hex code, color name. Alias: bg |
| `class` | `string` | Space-separated styles. |
| `x, y` | `integer` | Pixel offset (required for <div> or anchor). |
| `z-index` | `integer` |  **Global Layering.** Higher values render on top of lower values across the entire application. |
| `anchor` | `string` | Pin to edges: top, bottom, left, right. |
| `scroll` | `bool` | Wraps the tag in an outer scroll-container. |

#### 1.2 Common Container Attributes (Supported by ALL Container and Layout tags)

* `spacing`: `integer|integer integer` - Spacing between children, one value for both orientations, 2 values for each separately.
* `clip`: `bool` - If true, children outside bounds are not drawn.
* `slicewidth`: `integer|integer integer integer integer` - Thickness of the 9-slice border or borders.


---

#### B. Special Formatting Rules for Parser

1. **Color Formatting:** Supported as Hex (`#FFFFFF`), RGBA (`255,255,255,255`), or named colors (`Red`).
2. **Size Units:** * Numbers (e.g., `100`) are treated as **Pixels**.
* Percentages (e.g., `50%`) are treated as **Relative to Parent**.
* `Auto` tells the layout engine to use the **MetricTable** to fit content.


3. **The `@` Prefix:**
* If any attribute starts with `@`, the **Hydrator** must create a `PropertyBinding` instead of a static assignment.


---


### C. Control Flow (The `@` Directive)

Control flow is resolved during the **Expansion Pass** before the layout engine runs.
Supported syntax is:

@if identifierbool
{
	<label text="true" />
}

Produces the label if identifierbool exist in the model with a value of true.


@if identifierbool
{
	<label text="true" />
}
else
{
	<label text="not!" />
}

Produces the correct label depending on the value of identifierbool.

@if identifierbool
{
	<label text="true" />
}
else if identifierbool2
{
	<label text="true2" />
}
else
{
	<label text="not!" />
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
	<label text="@i" />
}

The parser must clone the inner XML (label in this case) 100 times, i will go from 0 to 99 (100 different values).

@for i=0 count=100 step=-1
{
	<label text="@i" />
}

The parser must clone the inner XML (label in this case) 100 times, i will go from 0 to -99 (100 different values, each pass will add -1 to i).

@foreach myitem in Collection
{
	<label text="@myitem.Property" />
}

The parser must clone the inner XML for every item in the `Collection`.

@foreach i in 0..100
{
	<label text="@i" />
}

The parser must clone the inner XML (label in this case) 100 times, i will go from 0 to 99 (not inclusive end value).

Within the loop, `@i` or `@item` or `@item.Property` acts as a local binding, the engine will call .ToString() if the types dont match and the target type is string, otherwise it will fail.


### E. Custom Components (Tags)

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
			<label text="Inventory" />
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


### F. Data Binding Syntax

* **Static:** `width="100"` (Immediate value)
* **Dynamic Binding:** `width="@currentWidth"` (Links to a Model Property via Reflection/Getters)

---

## 3. The Processing Pipeline

### Phase 1: Expansion (Pre-Processing)

* **Input:** Raw `.suim` XML + Data Model.
* **Task:** Resolve all `@directives`. Expand custom tags into their internal XML.
* **Output:** A "Flat XML" string containing only primitive tags and static/binding attributes.

### Phase 2: Hydration (Object Creation)

* **Input:** Flat XML.
* **Task:** Instantiate C# objects.
* **Binding:** If an attribute starts with `@`, create a `PropertyBinding` object linking the Model to the UI Element property.

### Phase 3: Layout (Pass)

* **Measurement:** Use `MetricTable` (cached character widths) instead of `MeasureString` for speed.
* **Vertical Pass:** Calculate `LineHeight` based on font size or Stride's `LineSpacing`.

---

## 4. Performance Requirements

1. **Zero Garbage Collection:** Avoid string allocations during the Layout Pass.
2. **Deterministic Measurement:** Character widths must be pre-calculated into a `float[256]` array.
3. **One-Pass Layout:** Sizes must be calculated in a single walk of the tree.

---

## 5. Implementation Roadmap

### Core Classes:

1. **`SUIMProcessor`**: Handles `@if` chains, `@foreach`, and component file loading.
2. **`SUIMHydrator`**: Uses Reflection to turn XML attributes into object properties and bindings.
3. **`MetricTable`**: Pre-bakes font widths into a lookup table.
4. **`SUIMElement`**: Base class containing `Bounds`, `Children`, and `List<PropertyBinding>`.

---

## 6. Example Input/Output

**Input (`Inventory.suim`):**

```xml
<inventory>
	<model> { showTitle: true } </model>
	<vstack>
		@if (showTitle)
		{
			<label text="Inventory" />
		}
		<grid width="@invWidth" />
	</vstack>
</inventory>

```

**Intermediate Flat XML (after Processor):**

```xml
<inventory>
	<vstack>
		<label text="Inventory" />
		<grid width="@invWidth" />
	</vstack>
</inventory>

```

**Final C# Tree:**

* `Stackpanel (Orientation: "Vertical")`
* `Label` (Text: "Inventory")
* `Grid` (Binding: Width -> Model.invWidth)
