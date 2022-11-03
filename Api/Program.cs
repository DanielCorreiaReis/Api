using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MinimalApiAuth;
using MinimalApiAuth.Models;
using MinimalApiAuth.Repositories;
using System.Security.Claims;
using System.Text;

var key = Encoding.ASCII.GetBytes(Settings.Secret);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(x =>
    {
        x.RequireHttpsMetadata = false;
        x.SaveToken = true;
        x.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("manager"));
    options.AddPolicy("Employee", policy => policy.RequireRole("employee"));
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/login", (User model) =>
{
    var user = UserRepository.Get(model.Username, model.Password);

    if (user == null)
        return Results.NotFound(new { message = "Usuário ou senha inválida" });

    var token = TokenService.GenerateToken(user);

    user.Password = "";

    return Results.Ok(new
    {
        message = "Usuário logado com sucesso",
        user = user,
        token = token
    });
});


app.MapGet("/anonymous", () =>
{
    //Results.Ok("anonymous"); 
    return Results.Ok(new
    {
        message = "usuário anônimo autorizado"
    });
});

app.MapGet("/authenticated", (ClaimsPrincipal user) =>
{
    Results.Ok(new { message = $"Authenticated as {user.Identity.Name}" });
    return Results.Ok(new
    {
        message = $"usuário - {user.Identity.Name} - autenticado"
    });
}).RequireAuthorization();

app.MapGet("/employee", (ClaimsPrincipal user) =>
{
    Results.Ok(new { message = $"Authenticated as {user.Identity.Name}" });
    return Results.Ok(new
    {
        message = $"Funcinário - {user.Identity.Name} - autenticado"
    });
}).RequireAuthorization("Employee");

app.MapGet("/manager", (ClaimsPrincipal user) =>
{
    Results.Ok(new { message = $"Authenticated as {user.Identity.Name}" });
    return Results.Ok(new
    {
        message = $"Gerente - {user.Identity.Name} - autenticado"
    });
}).RequireAuthorization("Admin");

app.Run();
