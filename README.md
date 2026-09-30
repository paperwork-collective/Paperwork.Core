# Paperwork.Core

Core PDF generation library for Paperwork. Provides a fluent builder API over the [Scryber](https://github.com/richard-scryber/scryber.core) rendering engine, to generate PDFs from HTML templates, CSS, and JSON data.
For documentation see [Paperwork.help](https://paperwork.help)

## Installation

```bash
dotnet add package Paperwork.Core
```

## Quick Start

```csharp
using Paperwork;

using var factory = PaperworkFactory.Create(httpClient).Build();
//using var factory = PaperworkFactory.Create(); - own internal httpClient

var bytes = await factory.NewDocument()
    .WithLayout("<html><body><p data-content='{{$fields[\"title\"]}}'></p></body></html>")
    .WithField("title", "Hello World")
    .BuildBytesAsync();

await File.WriteAllBytesAsync("output.pdf", bytes);
```

## Usage

### Setup

```csharp
// Minimal — no auth
var factory = PaperworkFactory.Create(httpClient).Build();

// With custom auth
var factory = PaperworkFactory.Create(httpClient)
    .WithAuth(new MyAuthService())
    .Build();

// ASP.NET Core DI
services.AddPaperwork();
```

### Layout

```csharp
// Inline HTML string
builder.WithLayout("<html><body>...</body></html>");

// From a file
builder.WithLayoutFile("invoice.html");

// From a URL (fetched at render time)
builder.WithLayoutUrl("https://cdn.example.com/template.html");

// Pre-built Document
builder.WithLayout(document);

// Add custom partials that can be accessed via $layouts["name"]
builder.WithLayout("<p> Injected by {{model.name}}</p>", "innerPartial");
```

### Styles

```csharp
builder.WithStyle("body { font-family: sans-serif; }");
builder.WithStyleFile("styles.css");
builder.WithStyleUrl("https://cdn.example.com/styles.css");
builder.WithStyle(styleGroup);  // Custom StyleGroup
```

### Data

```csharp
// JSON string — bound as {{name.field}} in templates
builder.WithData("order", "{\"total\": 1200}");

// From a file
builder.WithDataFile("order", "order.json");

// Any object — set directly on doc.Params
builder.WithData("order", new { total = 1200 });
```

### Fields

Values accessible in templates as `$fields["key"]`:

```csharp
builder.WithField("date", "2026-03-25");
builder.WithField("title", "Invoice #1001");
```

```html
<p data-content='{{$fields["title"]}}'></p>
```

`WithField(id, value, type)` only accepts a **scalar** `string` value. A field whose value is a nested object or array (e.g. a Designer-authored `single`/`list`-type field) can't be built up this way — load it via `FromConfig`/`FromDefinition` instead (below), which preserves nested values as `JsonElement`.

### Loading a full template config

```csharp
// From a stream/file — deserializes straight into TemplateDefinitionV1,
// preserving any nested field values as JsonElement rather than flattening them
using var stream = File.OpenRead("template-config.json");
var builder = factory.FromConfig(stream);

var bytes = await builder.BuildBytesAsync();
```

The JSON is the same `{"schemaVers": "1.1", "template": {...}}` shape a published Paperwork template is stored as — see [Paperwork.CLI's config-format docs](https://github.com/paperwork-collective/Paperwork.CLI/blob/main/docs/config-format.md) for the full schema, including nested field values.

### Generate

```csharp
// As bytes
byte[] pdf = await builder.BuildBytesAsync();

// Save to file
await builder.SaveAsync("output.pdf");

// Full result with metadata
PaperworkResult result = await builder.BuildAsync();
```

## Template Binding

Inside HTML templates, use handlebars-style expressions:

```html
<!-- Parameter -->
<p data-content='{{$fields["date"]}}'></p>

<!-- Data object field -->
<p data-content='{{order.customerName}}'></p>

<!-- Loop over array -->
{{#each items}}
<p data-content='{{.label}}'></p>
{{/each}}
```

## Related packages

- [`Paperwork.Core.Extensions`](https://github.com/paperwork-collective/Paperwork.Core.Extensions) — shared portal config, `$assets`/`$maps` remote-file resolution, and cross-platform auth handlers (Atlassian, Firebase Asset) for consumers that need them. Not a dependency of this package — install it separately if you need that functionality.
- [`Paperwork.CLI`](https://github.com/paperwork-collective/Paperwork.CLI) — command-line PDF generation built on this package.

## License

MIT
