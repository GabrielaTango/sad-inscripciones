using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SAD.Inscripciones.API.Data;
using SAD.Inscripciones.API.Middleware;
using SAD.Inscripciones.API.Repositories;
using SAD.Inscripciones.API.Repositories.Interfaces;
using SAD.Inscripciones.API.Services;
using SAD.Inscripciones.API.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Data access
builder.Services.AddSingleton<DbConnectionFactory>();

// Repositories
builder.Services.AddScoped<IInscripcionRepository, InscripcionRepository>();
builder.Services.AddScoped<IContactoRepository, ContactoRepository>();
builder.Services.AddScoped<IEventoRepository, EventoRepository>();
builder.Services.AddScoped<ITipoEventoRepository, TipoEventoRepository>();
builder.Services.AddScoped<ITipoAlumnoRepository, TipoAlumnoRepository>();
builder.Services.AddScoped<IEventoPrecioRepository, EventoPrecioRepository>();
builder.Services.AddScoped<IEventoProvinciaBeneficioRepository, EventoProvinciaBeneficioRepository>();
builder.Services.AddScoped<IEventoArticuloRegaloRepository, EventoArticuloRegaloRepository>();
builder.Services.AddScoped<IPagoRepository, PagoRepository>();
builder.Services.AddScoped<IBecaEventoRepository, BecaEventoRepository>();
builder.Services.AddScoped<IBecaCodigoRepository, BecaCodigoRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IPromocionRepository, PromocionRepository>();
builder.Services.AddScoped<IPromocionCuponRepository, PromocionCuponRepository>();
builder.Services.AddScoped<IConfiguracionEmailRepository, ConfiguracionEmailRepository>();
builder.Services.AddScoped<IConfiguracionMercadoPagoRepository, ConfiguracionMercadoPagoRepository>();
builder.Services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();

// Services
builder.Services.AddScoped<ITipoEventoService, TipoEventoService>();
builder.Services.AddScoped<ITipoAlumnoService, TipoAlumnoService>();
builder.Services.AddScoped<IEventoService, EventoService>();
builder.Services.AddScoped<IEventoPrecioService, EventoPrecioService>();
builder.Services.AddScoped<IEventoProvinciaBeneficioService, EventoProvinciaBeneficioService>();
builder.Services.AddScoped<IEventoArticuloRegaloService, EventoArticuloRegaloService>();
builder.Services.AddScoped<IPagoService, PagoService>();
builder.Services.AddScoped<IInscripcionService, InscripcionService>();
builder.Services.AddScoped<IBecaEventoService, BecaEventoService>();
builder.Services.AddScoped<IBecaCodigoService, BecaCodigoService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IPromocionService, PromocionService>();
builder.Services.AddScoped<IPromocionCuponService, PromocionCuponService>();
builder.Services.AddSingleton<IMercadoPagoService, MercadoPagoService>();
builder.Services.AddScoped<IInscripcionPagoValidationService, InscripcionPagoValidationService>();
builder.Services.AddSingleton<ICryptoService, CryptoService>();
builder.Services.AddSingleton<IEmailService, EmailService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                  "http://localhost:5173",
                  "https://www.diabetes2.org.ar")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(secretKey)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy =>
        policy.RequireRole("Admin"));
    options.AddPolicy("Capitulo", policy =>
        policy.RequireRole("Capitulo"));
});

// Controllers
builder.Services.AddControllers();

// Swagger with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SAD Inscripciones API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Ejemplo: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// Exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed admin user
using (var scope = app.Services.CreateScope())
{
    var usuarioService = scope.ServiceProvider.GetRequiredService<IUsuarioService>();
    await usuarioService.SeedAdminAsync();
}

app.Run();
