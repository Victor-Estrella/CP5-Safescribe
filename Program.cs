using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SafeScribe.Services;

var builder = WebApplication.CreateBuilder(args);

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Chave de assinatura JWT não configurada.");

// CORS: política de desenvolvimento (permite tudo). Em produção, restrinja origens/métodos/headers.
const string CorsPolicyDev = "CorsPolicyDev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyDev, policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, // rejeita tokens emitidos por terceiros
            ValidateAudience = true, // confirma que o token se destina a esta API
            ValidateLifetime = true, // bloqueia tokens expirados
            ValidateIssuerSigningKey = true, // garante que o token foi assinado pela SafeScribe
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        // Logs para diagnosticar 401 com detalhes
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"[JWT] AuthenticationFailed: {context.Exception.GetType().Name} - {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                // Executa depois da falha para enriquecer logs
                Console.WriteLine($"[JWT] Challenge: Error={context.Error}, Desc={context.ErrorDescription}");
                return Task.CompletedTask;
            },
            OnForbidden = context =>
            {
                var payload = JsonSerializer.Serialize(new { mensagem = "Permissão insuficiente para realizar esta ação." });
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync(payload);
            },
            OnMessageReceived = context =>
            {
                // Confirma se o header Authorization chegou e como
                var hasAuth = context.Request.Headers.ContainsKey("Authorization");
                if (hasAuth)
                {
                    var preview = context.Request.Headers["Authorization"].ToString();
                    Console.WriteLine($"[JWT] Authorization header: {preview}");
                }
                else
                {
                    Console.WriteLine("[JWT] Sem header Authorization");
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<ITokenBlacklistService, InMemoryTokenBlacklistService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<INoteService, NoteService>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    // Permite enviar/receber enums como strings (ex.: "Admin", "Editor", "Leitor")
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SafeScribe API",
        Version = "v1",
        Description = "API para gestão de notas com autenticação JWT"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT desta forma: {seu Token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configuração do pipeline HTTP da aplicação.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SafeScribe API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
// CORS deve vir antes de autenticação/autorização para aplicar aos endpoints
app.UseCors(CorsPolicyDev);
app.UseAuthentication();
app.UseMiddleware<SafeScribe.Middleware.JwtBlacklistMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();
