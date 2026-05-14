# FlareAPI

FlareAPI is a lightweight, easy-to-use C# web server framework built on top of `HttpListener`. It provides a simple routing system, support for parameters, automatic response compression, and simplified handling of various content types.

## Features

* **Simple Routing:** Define routes with specific HTTP methods and handle parameters easily.
* **Automatic Compression:** Supports Gzip, Brotli, and Deflate encoding for responses.
* **Fluent Response API:** Specialized methods for sending JSON, HTML, Text, CSS, CSV, and JavaScript.
* **File Streaming:** Efficiently stream files to clients with support for HTTP Range requests (Partial Content).
* **Global Configuration:** Set global headers, cookies, and compression settings for all responses.
* **Async Support:** Asynchronous request handling and file operations.

## Quick Start

### 1. Initialize the Server
```csharp
using FlareAPI;

// Create a server listening on port 8080
var server = new FlareServer("http://localhost:8080/");

// Optional: Set global headers
server.globalHeaders.Add("X-Powered-By", "FlareAPI");

```

### 2. Define Routes

```csharp
// Simple text route
server.AddRoute(HttpMethod.Get, "/hello", (req, res) => {
    res.Text("Hello, World!");
});

// Route with parameters (e.g., /user/123)
server.AddRoute(HttpMethod.Get, "/user/:id", (req, res) => {
    var parameters = req.GetParameters();
    res.Json(new { userId = parameters["id"], status = "active" });
});

// Handling file downloads
server.AddRoute(HttpMethod.Get, "/download", (req, res) => {
    res.File("path/to/your/file.zip", "application/zip");
});

```

### 3. Start the Server

```csharp
server.Start();
Console.WriteLine("Server started on http://localhost:8080/");

```

## Content Encoding

You can enable compression globally for text responses or per-response:

```csharp
// Per-response encoding
res.SetEncoding(new[] { ContentEncoding.Brotli, ContentEncoding.Gzip });
res.Html("<h1>Compressed Content</h1>");

```

## Error Handling

The library includes custom exceptions for common API errors:

* `InvalidResponseBodyException`: Thrown if you try to set multiple bodies for one response.
* `UnmodifiableResponseException`: Thrown if you try to modify headers after the body has been sent.
* `FileNotFoundException`: Thrown when a requested file does not exist on disk.

## Requirements

* .NET 8.0 or higher
