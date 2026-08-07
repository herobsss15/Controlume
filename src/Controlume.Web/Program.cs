using Controlume.Web.Components;
using Controlume.Web.Data;
using Controlume.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("ControlumeDb")
    ?? throw new InvalidOperationException("ConnectionStrings:ControlumeDb não configurada.");

builder.Services.AddDbContext<ControlumeDbContext>(options =>
    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

builder.Services.AddScoped<TipoProdutoService>();
builder.Services.AddScoped<ProdutoService>();
builder.Services.AddScoped<CaixaService>();
builder.Services.AddScoped<VendaService>();
builder.Services.AddScoped<VendaEmAndamentoState>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ControlumeDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// Em produção o TLS é terminado no Cloudflare Tunnel e o Kestrel só escuta HTTP
// (sem porta HTTPS configurada), então UseHttpsRedirection() ficaria só logando
// warning a cada request. Em Development ela continua valendo para o profile "https".
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
