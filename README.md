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

// ASP.NET Core DI - uses the app's HttpClient, tracing and file request services if registered
services.AddPaperwork();

// ...and pass the configuration to the engine, for the "Paperwork" (or "Scryber") appsettings section
services.AddPaperwork(builder.Configuration);

// Without DI - once at startup
PaperworkConfiguration.Init(configuration);
```

The engine options (Fonts, Imaging, Parsing, Output, Tracing...) can be set in a `"Paperwork"` or `"Scryber"` section of the configuration. Both are read, and a value in `"Paperwork"` replaces the same value in `"Scryber"`.

### Layout

```csharp
// Inline HTML string
builder.WithLayout("<html><body>...</body></html>");

// From a file - relative images and stylesheets are found next to the file
builder.WithLayoutFile("invoice.html");

// From a URL (fetched at render time)
builder.WithLayoutUrl("https://cdn.example.com/template.html");

// Pre-built Document - configure it first (Info, RenderOptions, events...), it is generated as it is
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

A pre-built `Document`, `StyleGroup` or data object is passed to the factory alongside the request, so every document is generated the same way - with the factory's auth, remote file handling and logging - whatever the inputs.

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

// Write to a stream, e.g. a response body
await builder.SaveAsync(stream);

// Full result with metadata
PaperworkResult result = await builder.BuildAsync();
```

### Logging

```csharp
// Append the trace log to the end of the document
builder.WithLogLevel(PaperworkRequestLogOption.Messages);
```

The default, `PaperworkRequestLogOption.Template`, uses the template's own `<?paperwork append-log='true' ?>` setting. `Off` turns the log off, whatever the template says.

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
