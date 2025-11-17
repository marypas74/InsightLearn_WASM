var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => "Hello World from Minimal API");
app.MapGet("/test", () => new { message = "Test OK", timestamp = DateTime.UtcNow });
app.MapGet("/raw-test", () => Results.Text("RAW TEST OK - Minimal Program.cs"));
app.MapGet("/api/info", () => new { version = "1.6.8-dev-minimal", status = "OK" });

app.Run();
