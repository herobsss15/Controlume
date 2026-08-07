using Controlume.Web.Components;
using Controlume.Web.Data;
using Controlume.Web.Domain;
using Controlume.Web.Services;
using Controlume.Web.Services.Autorizacao;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("ControlumeDb")
    ?? throw new InvalidOperationException("ConnectionStrings:ControlumeDb não configurada.");

builder.Services.AddDbContext<ControlumeDbContext>(options =>
    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

// Regra 16: cookie authentication direto do ASP.NET Core — para 2 ou 3 usuários fixos,
// o ASP.NET Core Identity inteiro (tabelas, UI, tokens) não se paga.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "controlume_auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        // Em produção o TLS termina no Cloudflare: o navegador sempre fala HTTPS, mesmo o
        // Kestrel só ouvindo HTTP. Em Development o profile "http" precisa continuar funcionando.
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/acesso-negado";
        options.ReturnUrlParameter = "returnUrl";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        // Papel e status mudam pela tela de usuários, mas o cookie carrega os claims do login:
        // sem reconferir, um usuário desativado (ou rebaixado) seguiria dentro até o cookie expirar.
        options.Events.OnValidatePrincipal = async context =>
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<ControlumeDbContext>();
            if (context.Principal is null || !await RevalidacaoDeSessao.ContinuaValidaAsync(db, context.Principal))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<IUsuarioAtual, UsuarioAtualBlazor>();
builder.Services.AddScoped<AuthenticationStateProvider, RevalidacaoDeSessao>();

builder.Services.AddScoped<TipoProdutoService>();
builder.Services.AddScoped<ProdutoService>();
builder.Services.AddScoped<CaixaService>();
builder.Services.AddScoped<SangriaService>();
builder.Services.AddScoped<VendaService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<VendaEmAndamentoState>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ControlumeDbContext>();
    db.Database.Migrate();

    await UsuarioSeeder.SincronizarAsync(
        db,
        scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>(),
        app.Configuration,
        app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(UsuarioSeeder)));
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

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Sem AllowAnonymous o CSS/JS cairia no RequireAuthorization abaixo e a tela de login
// apareceria sem estilo — os assets estáticos não têm nada de sensível.
app.MapStaticAssets().AllowAnonymous();

// Regra 16: toda rota exige autenticação. A tela de login se marca com [AllowAnonymous],
// que vence esta convenção; o [Authorize] global vem do _Imports.razor dos componentes.
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .RequireAuthorization();

app.Run();
