// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Amqp.Net.Broker.Host;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
            formatProvider: CultureInfo.InvariantCulture);
});

// Add AMQP broker services
builder.Services.AddAmqpBroker(builder.Configuration);

// Build and run
var app = builder.Build();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

// Metrics endpoint (if OpenTelemetry is configured)
app.MapGet("/", () => Results.Ok(new
{
    Name = "Amqp.Net Broker",
    Version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0",
    Status = "Running"
}));
await app.RunAsync().ConfigureAwait(false);
