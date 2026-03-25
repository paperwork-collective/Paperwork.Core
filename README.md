# Paperwork.Core

Core PDF generation library for Paperwork. Provides a fluent builder API over the [Scryber](https://github.com/richard-scryber/scryber.core) rendering engine, to generate PDFs from HTML templates, CSS, and JSON data. to generate PDFs from HTML templates, CSS, and JSON data.

## Installation

```bash
dotnet add package Paperwork.Core
```

## Quick Start

```csharp
using Paperwork;

var factory = PaperworkFactory.Create(httpClient).Build();

var bytes = await factory.NewDocument()
    .WithLayout("<html><body><p data-content='{{fields[\"title\"]}}'></p></body></html>")
    .WithParameter("title", "Hello World")
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

// Pre-built Scryber Document
builder.WithLayout(document);
```

### Styles

```csharp
builder.WithStyle("body { font-family: sans-serif; }");
builder.WithStyleFile("styles.css");
builder.WithStyleUrl("https://cdn.example.com/styles.css");
builder.WithStyle(styleGroup);  // Scryber StyleGroup
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

### Parameters

Scalar values accessible in templates as `fields["key"]`:

```csharp
builder.WithParameter("date", "2026-03-25");
builder.WithParameter("title", "Invoice #1001");
```

```html
<p data-content='{{fields["title"]}}'></p>
```

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
<p data-content='{{fields["date"]}}'></p>

<!-- Data object field -->
<p data-content='{{order.customerName}}'></p>

<!-- Loop over array -->
{{#each items}}
<p data-content='{{this.label}}'></p>
{{/each}}
```

## License

MIT
