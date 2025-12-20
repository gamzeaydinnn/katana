using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<JsonOptions>(opts => {
    opts.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    opts.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

app.MapGet("/", () => Results.Text("Mock Luca Koza Server - root"));

// Health endpoint
app.MapGet("/Yetki/Health", () => Results.Json(new { status = "OK" }));

// Giris.do - authentication (cookie-based session)
app.MapPost("/Yetki/Giris.do", async (HttpRequest req, HttpResponse res) =>
{
    var body = await req.ReadFromJsonAsync<LoginRequest>();
    if (body is null || string.IsNullOrEmpty(body.orgCode) || string.IsNullOrEmpty(body.userName) || string.IsNullOrEmpty(body.userPassword))
    {
        return Results.Json(new { code = 99, message = "Invalid credentials" });
    }

    // For test, accept the provided test credentials
    if (body.orgCode == "1422649" && body.userName == "Admin" && body.userPassword == "WebServis")
    {
        // Set a cookie to emulate session
        var sessionId = Guid.NewGuid().ToString();
        res.Cookies.Append("LucaSession", sessionId, new CookieOptions { HttpOnly = true, Path = "/", SameSite = SameSiteMode.Lax });
        return Results.Json(new { code = 0, message = "Giris basarili" });
    }

    return Results.Json(new { code = 99, message = "Kullanici/Parola Yanlis" });
});

// Return branches
app.MapPost("/Yetki/YdlUserResponsibilityOrgSs.do", (HttpRequest req) =>
{
    var branches = new[] { new { ack = "MainBranch", id = 1 }, new { ack = "Secondary", id = 2 } };
    return Results.Json(branches);
});

// Change branch
app.MapPost("/Yetki/GuncelleYtkSirketSubeDegistir.do", async (HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<ChangeBranchRequest>();
    if (body is null)
        return Results.Json(new { message = "Missing body" });

    return Results.Json(new { message = "Oturumda Calistiginiz Sirket Sube Basariyla Degistirildi.", sirketSubeAdi = $"Branch-{body.orgSirketSubeId}" });
});

// Product creation endpoint (EkleStkWsSkart.do) - emulate create
app.MapPost("/Yetki/EkleStkWsSkart.do", async (HttpRequest req) =>
{
    var body = await req.ReadFromJsonAsync<JsonElement>();
    // Return success with dummy id
    return Results.Json(new { code = 0, message = "Stok karti eklendi", createdId = 123456 });
});

// Also expose a generic products path that Katana may call (api/products)
app.MapPost("/Yetki/api/products", async (HttpRequest req) =>
{
    var payload = await req.ReadFromJsonAsync<object>();
    // Log payload to console for developer inspection
    Console.WriteLine("[MockLuca] Received /api/products payload: " + System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    return Results.Ok(new { code = 0, message = "Products received", received = true });
});

app.Run("http://localhost:6001");

// DTOs
public record LoginRequest(string orgCode, string userName, string userPassword);
public record ChangeBranchRequest(long orgSirketSubeId);
