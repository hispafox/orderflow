# Bonus Track — ASP.NET Core Identity en Profundidad

## Microservicios con .NET 10

Bonus Track — Fuera del camino principal
Complemento de M5.1 — JWT y Seguridad

Duración estimada: 60 minutos
Curso de 25 horas · Formación técnica · Opcional

---

## Por qué este Bonus existe

En M5.1 usamos ASP.NET Core Identity como herramienta: lo configuramos, generamos tokens, protegemos rutas. Funciona.

Este Bonus explica **por qué funciona**. Qué hay dentro. Qué hace cada pieza. Para el alumno que quiere entender Identity de verdad — no solo copiarlo.

Si en tu proyecto necesitas:
- Registro de usuarios con confirmación de email
- Password reset personalizado
- Roles dinámicos creados en runtime
- Extender el usuario con propiedades del dominio
- Testear autenticación con usuarios reales en la BD

...este Bonus es para ti.

---

## La arquitectura de ASP.NET Core Identity

Identity no es una librería monolítica. Son **tres managers** con responsabilidades separadas más un conjunto de stores:

```
ASP.NET Core Identity

  UserManager<TUser>
  └── Gestiona usuarios: crear, buscar, validar, claims, roles

  SignInManager<TUser>
  └── Gestiona sesiones: login, logout, lockout, 2FA

  RoleManager<TRole>
  └── Gestiona roles: crear, listar, asignar permisos

  IUserStore<TUser>  /  IRoleStore<TRole>
  └── Abstracción de persistencia → EF Core lo implementa

  IdentityDbContext<TUser>
  └── El DbContext con las 5 tablas de Identity
```

En OrderFlow usamos los tres managers en Gateway.API. Los stores los implementa EF Core con LocalDB.

---

## IdentityUser: el usuario base

`IdentityUser` es la clase base que representa un usuario en Identity. Incluye todo lo necesario para autenticación:

```csharp
// Lo que IdentityUser ya tiene:
public class IdentityUser
{
    public string Id { get; set; }           // GUID como string
    public string UserName { get; set; }     // nombre de usuario único
    public string NormalizedUserName { get; set; }
    public string Email { get; set; }
    public string NormalizedEmail { get; set; }
    public bool EmailConfirmed { get; set; }
    public string PasswordHash { get; set; } // PBKDF2 — nunca la contraseña plana
    public string SecurityStamp { get; set; }// se regenera al cambiar contraseña
    public bool TwoFactorEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool LockoutEnabled { get; set; }
    public int AccessFailedCount { get; set; }
}
```

Nunca almacena la contraseña en plano. `PasswordHash` usa PBKDF2 con sal aleatoria — aunque dos usuarios tengan la misma contraseña, los hashes son distintos.

---

## ApplicationUser: extender el usuario con datos del dominio

En OrderFlow, un usuario de Identity corresponde a un cliente de TechShop. Necesitamos conectar ambos:

```csharp
// Gateway.API/Identity/ApplicationUser.cs

public class ApplicationUser : IdentityUser
{
    // Datos del dominio de TechShop
    public Guid CustomerId { get; set; }          // enlace con Orders/Payments
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Propiedad calculada útil en los claims
    public string FullName => $"{FirstName} {LastName}".Trim();
}
```

```csharp
// Registrar ApplicationUser en Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Configuración de contraseñas, lockout, etc.
})
.AddEntityFrameworkStores<GatewayIdentityDbContext>()
.AddDefaultTokenProviders();
```

Ahora `UserManager<ApplicationUser>` trabaja con nuestra clase extendida. Podemos leer y escribir `CustomerId` igual que cualquier propiedad.

---

## Las 5 tablas que crea IdentityDbContext

Al aplicar la migration de Identity, EF Core crea estas tablas en la BD:

```sql
-- Las 5 tablas de ASP.NET Core Identity

AspNetUsers          -- Los usuarios (ApplicationUser)
  Id, UserName, Email, PasswordHash, CustomerId, FirstName...

AspNetRoles          -- Los roles definidos
  Id, Name, NormalizedName, ConcurrencyStamp

AspNetUserRoles      -- Asignación usuario ↔ rol (muchos a muchos)
  UserId, RoleId

AspNetUserClaims     -- Claims adicionales por usuario
  Id, UserId, ClaimType, ClaimValue

AspNetRoleClaims     -- Claims adicionales por rol
  Id, RoleId, ClaimType, ClaimValue

-- También crea (si usas tokens de Email/SMS):
AspNetUserTokens     -- Tokens de confirmación, password reset
  UserId, LoginProvider, Name, Value
```

En OrderFlow con LocalDB, estas tablas viven en `IdentityDb`. En producción migran a Azure SQL igual que el resto de BDs.

---

## GatewayIdentityDbContext

El DbContext de Identity se configura en Gateway.API:

```csharp
// Gateway.API/Identity/GatewayIdentityDbContext.cs

public class GatewayIdentityDbContext
    : IdentityDbContext<ApplicationUser>
{
    public GatewayIdentityDbContext(
        DbContextOptions<GatewayIdentityDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Cambiar el schema a "identity" para separarlo del resto
        builder.HasDefaultSchema("identity");

        // Configurar las propiedades custom de ApplicationUser
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.CustomerId)
                .IsRequired();

            entity.Property(e => e.FirstName)
                .HasMaxLength(100);

            entity.Property(e => e.LastName)
                .HasMaxLength(100);
        });
    }
}
```

```bash
# Crear y aplicar la migration de Identity
dotnet ef migrations add InitialIdentitySchema \
    --project Gateway.API \
    --context GatewayIdentityDbContext

dotnet ef database update \
    --project Gateway.API \
    --context GatewayIdentityDbContext
```

---

## UserManager: el CRUD completo de usuarios

`UserManager<ApplicationUser>` es el servicio central. Se inyecta con DI:

```csharp
public class UserAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserAdminService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    // ── CREAR ─────────────────────────────────────────────────────
    public async Task<IdentityResult> CreateUserAsync(
        string email, string password, Guid customerId)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            CustomerId = customerId,
            EmailConfirmed = true   // en dev, confirmar automáticamente
        };

        return await _userManager.CreateAsync(user, password);
        // IdentityResult indica si tuvo éxito o los errores concretos
    }

    // ── BUSCAR ────────────────────────────────────────────────────
    public async Task<ApplicationUser?> FindByEmailAsync(string email)
        => await _userManager.FindByEmailAsync(email);

    public async Task<ApplicationUser?> FindByIdAsync(string userId)
        => await _userManager.FindByIdAsync(userId);

    // ── VALIDAR CONTRASEÑA ────────────────────────────────────────
    public async Task<bool> CheckPasswordAsync(
        ApplicationUser user, string password)
        => await _userManager.CheckPasswordAsync(user, password);

    // ── CAMBIAR CONTRASEÑA ────────────────────────────────────────
    public async Task<IdentityResult> ChangePasswordAsync(
        ApplicationUser user, string currentPassword, string newPassword)
        => await _userManager.ChangePasswordAsync(
            user, currentPassword, newPassword);
}
```

---

## RoleManager: crear y asignar roles

Los roles en OrderFlow son `customer` y `admin`. Se crean una vez en el seed:

```csharp
public class RoleSeeder
{
    private readonly RoleManager<IdentityRole> _roleManager;

    public RoleSeeder(RoleManager<IdentityRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task SeedRolesAsync()
    {
        string[] roles = ["customer", "admin", "support"];

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}

// Asignar un rol a un usuario
await _userManager.AddToRoleAsync(user, "customer");

// Verificar si un usuario tiene un rol
bool isAdmin = await _userManager.IsInRoleAsync(user, "admin");

// Obtener todos los roles de un usuario
IList<string> roles = await _userManager.GetRolesAsync(user);

// Quitar un rol
await _userManager.RemoveFromRoleAsync(user, "customer");
```

---

## Claims personalizados: enriquecer el JWT

Los claims son los datos que viajan dentro del JWT. Identity añade los básicos (`sub`, `email`). Nosotros añadimos los del dominio:

```csharp
// Gateway.API/Identity/JwtTokenService.cs

public class JwtTokenService
{
    private readonly IConfiguration _config;

    public string GenerateToken(ApplicationUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            // Claims estándar
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.UserName!),

            // Claims del dominio de TechShop
            new("customer_id", user.CustomerId.ToString()),
            new("full_name", user.FullName),

            // Roles como claims
            // (cada rol = un claim separado)
        };

        // Añadir cada rol como claim
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                _config.GetValue<int>("Jwt:ExpiryMinutes")),
            signingCredentials: new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

---

## SignInManager: login, logout y lockout

`SignInManager<TUser>` gestiona el flujo de autenticación completo:

```csharp
// Gateway.API/Identity/LoginEndpoint.cs

public class LoginEndpoint
{
    public static async Task<IResult> Handle(
        LoginRequest request,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtTokenService tokenService)
    {
        // 1. Buscar el usuario
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Results.Unauthorized();

        // 2. Intentar login — SignInManager gestiona lockout automáticamente
        var result = await signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true); // ← tras N intentos fallidos, lockout

        if (result.IsLockedOut)
            return Results.Problem(
                title: "Account locked",
                detail: "Too many failed attempts. Try again later.",
                statusCode: 429);

        if (!result.Succeeded)
            return Results.Unauthorized();

        // 3. Generar JWT
        var roles = await userManager.GetRolesAsync(user);
        var token = tokenService.GenerateToken(user, roles);

        return Results.Ok(new LoginResponse(token, user.FullName));
    }
}
```

El `lockoutOnFailure: true` activa el lockout automáticamente. Tras `MaxFailedAccessAttempts` intentos fallidos, `LockoutEnd` se establece a `now + LockoutDefaultTimeSpan`.

---

## Configurar políticas de seguridad

Las políticas de contraseñas y lockout se configuran en `AddIdentity`:

```csharp
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // ── CONTRASEÑAS ────────────────────────────────────────────
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;   // menos fricción
    options.Password.RequireNonAlphanumeric = false;

    // ── LOCKOUT ───────────────────────────────────────────────
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.AllowedForNewUsers = true;

    // ── USUARIOS ──────────────────────────────────────────────
    options.User.RequireUniqueEmail = true;

    // ── EMAIL CONFIRMATION ────────────────────────────────────
    options.SignIn.RequireConfirmedEmail = false; // true en producción
})
.AddEntityFrameworkStores<GatewayIdentityDbContext>()
.AddDefaultTokenProviders();
```

Para OrderFlow en desarrollo `RequireConfirmedEmail = false` — confirmamos el email automáticamente en el seed. En producción se activa junto con el servicio de email.

---

## Password hashing: cómo funciona internamente

Nunca almacenes contraseñas en plano. Identity usa PBKDF2 por defecto:

```
PBKDF2 (Password-Based Key Derivation Function 2):

1. Salt aleatorio de 128 bits generado para CADA usuario
2. Hash = PBKDF2(password + salt, iterations=100.000, SHA-256)
3. Se almacena: version + iteraciones + salt + hash

Resultado en AspNetUsers.PasswordHash:
  AQAAAAIAAYagAAAAE... (base64 ~88 caracteres)

¿Por qué salt aleatorio?
  → Dos usuarios con "password123" tienen hashes completamente distintos
  → Imposible usar rainbow tables

¿Por qué 100.000 iteraciones?
  → Fuerza bruta de 1 contraseña = 100.000 operaciones de hash
  → Con GPU moderna: ~1 millón de intentos/segundo
  → Tiempo para crackear password débil: meses
```

Identity hace todo esto automáticamente. `CheckPasswordAsync` verifica el hash sin que el código toque la contraseña en ningún momento.

---

## Password reset: el flujo completo

El flujo de password reset con Identity:

```csharp
// PASO 1: Solicitar el reset (envía email con token)
public async Task<IResult> RequestPasswordReset(
    string email, UserManager<ApplicationUser> userManager,
    IEmailService emailService)
{
    var user = await userManager.FindByEmailAsync(email);
    if (user is null) return Results.Ok(); // No revelar si existe el email

    // Generar token de reset (válido por defecto 1 día)
    var token = await userManager.GeneratePasswordResetTokenAsync(user);
    var resetLink = $"https://techshop.es/reset-password?token={Uri.EscapeDataString(token)}&email={email}";

    await emailService.SendPasswordResetAsync(user.Email!, resetLink);
    return Results.Ok();
}

// PASO 2: Aplicar el nuevo password con el token
public async Task<IResult> ResetPassword(
    ResetPasswordRequest request,
    UserManager<ApplicationUser> userManager)
{
    var user = await userManager.FindByEmailAsync(request.Email);
    if (user is null) return Results.BadRequest();

    var result = await userManager.ResetPasswordAsync(
        user, request.Token, request.NewPassword);

    return result.Succeeded
        ? Results.Ok()
        : Results.BadRequest(result.Errors);
}
```

El token se invalida automáticamente después de usarse o de expirar. `SecurityStamp` se regenera al cambiar contraseña, invalidando todos los tokens anteriores.

---

## Email confirmation: verificar que el email es real

En producción, los usuarios deben confirmar su email antes de poder hacer login:

```csharp
// Al registrar: generar token de confirmación
var user = new ApplicationUser { Email = email, UserName = email };
await userManager.CreateAsync(user, password);

var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
var confirmLink = $"https://techshop.es/confirm-email?token={Uri.EscapeDataString(token)}&userId={user.Id}";
await emailService.SendConfirmationAsync(email, confirmLink);

// Al hacer clic en el link: confirmar
var user = await userManager.FindByIdAsync(userId);
var result = await userManager.ConfirmEmailAsync(user, token);
// result.Succeeded = true → user.EmailConfirmed = true en la BD
```

Con `RequireConfirmedEmail = true` en las opciones, `SignInManager` rechaza el login hasta que `EmailConfirmed = true`.

---

## Claims-based Authorization: policies reales

En M5.1 vimos `[Authorize(Roles = "admin")]`. Las policies son más potentes:

```csharp
// Gateway.API/Program.cs — definir policies

builder.Services.AddAuthorization(options =>
{
    // Policy simple basada en rol
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireRole("admin"));

    // Policy basada en claim del dominio
    options.AddPolicy("RequireVerifiedCustomer", policy =>
        policy.RequireClaim("customer_id")
              .RequireRole("customer"));

    // Policy con lógica custom
    options.AddPolicy("CanManageOrders", policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole("admin") ||
            (context.User.IsInRole("support") &&
             context.User.HasClaim("department", "orders"))));
});
```

```csharp
// En el Controller o Minimal API
[Authorize(Policy = "RequireVerifiedCustomer")]
public async Task<IActionResult> CreateOrder(...)

// En YARP Gateway — proteger rutas enteras
.RequireAuthorization("RequireAdmin") // en MapReverseProxy
```

---

## IClaimsTransformation: enriquecer claims dinámicamente

Si necesitas añadir claims que no están en el JWT (por ejemplo, permisos cargados desde la BD):

```csharp
// Gateway.API/Identity/OrderFlowClaimsTransformation.cs

public class OrderFlowClaimsTransformation : IClaimsTransformation
{
    private readonly IPermissionService _permissions;

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Solo transformar si es un usuario autenticado
        if (!principal.Identity?.IsAuthenticated ?? true)
            return principal;

        var customerId = principal.FindFirstValue("customer_id");
        if (customerId is null) return principal;

        // Cargar permisos adicionales desde el servicio
        var permissions = await _permissions.GetPermissionsAsync(
            Guid.Parse(customerId));

        var identity = new ClaimsIdentity();
        foreach (var permission in permissions)
        {
            identity.AddClaim(new Claim("permission", permission));
        }

        principal.AddIdentity(identity);
        return principal;
    }
}

// Registrar
builder.Services.AddScoped<IClaimsTransformation,
    OrderFlowClaimsTransformation>();
```

Se ejecuta en cada request, después de validar el JWT. Útil para permisos dinámicos que no quieres hardcodear en el token.

---

## Cookie auth vs JWT: la decisión correcta para cada caso

Identity puede autenticar con cookies O con JWT. ¿Cuándo usar cada uno?

```
COOKIE AUTH (SignInManager.SignInAsync):
  ✅ Aplicaciones web con sesión de servidor
  ✅ Protección CSRF automática
  ✅ Revocación inmediata (eliminar la sesión en el servidor)
  ❌ No funciona bien para APIs consumidas por móviles/SPAs
  ❌ Difícil de propagar entre microservicios

JWT:
  ✅ APIs REST consumidas por cualquier cliente
  ✅ Stateless — el servidor no almacena nada
  ✅ Fácil de propagar entre microservicios (header Authorization)
  ❌ No hay revocación inmediata (hasta que expire)
  ❌ El token crece con cada claim que añades

OrderFlow usa JWT porque:
  - Gateway.API es una API, no una app web
  - Los servicios internos (Orders, Products...) reciben el token propagado
  - El frontend de TechShop es una SPA (React/Next.js en el futuro)
```

---

## Refresh Token con Identity

Los JWT de corta duración (15-60 min) requieren un mecanismo de renovación:

```csharp
// Gateway.API/Identity/RefreshTokenService.cs

public class RefreshTokenService
{
    // Los refresh tokens se almacenan en AspNetUserTokens
    private const string Provider = "OrderFlow";
    private const string TokenName = "RefreshToken";

    public async Task<string> GenerateAsync(
        ApplicationUser user, UserManager<ApplicationUser> userManager)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        await userManager.SetAuthenticationTokenAsync(
            user, Provider, TokenName, token);

        return token;
    }

    public async Task<ApplicationUser?> ValidateAndRotateAsync(
        string refreshToken, UserManager<ApplicationUser> userManager)
    {
        // Buscar el usuario por el refresh token
        // (en producción: índice en la tabla AspNetUserTokens)
        var users = userManager.Users.ToList();
        foreach (var user in users)
        {
            var stored = await userManager.GetAuthenticationTokenAsync(
                user, Provider, TokenName);

            if (stored == refreshToken)
            {
                // Rotar el token — invalidar el anterior
                await userManager.RemoveAuthenticationTokenAsync(
                    user, Provider, TokenName);
                return user;
            }
        }
        return null;
    }
}
```

---

## Two-Factor Authentication (2FA): el concepto

Identity incluye soporte completo para 2FA. En OrderFlow no lo activamos por defecto, pero el alumno debe conocerlo:

```csharp
// Verificar si el usuario tiene 2FA activado
bool twoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user);

// En el login, SignInManager devuelve RequiresTwoFactor:
var result = await signInManager.PasswordSignInAsync(email, password,
    isPersistent: false, lockoutOnFailure: true);

if (result.RequiresTwoFactor)
{
    // Redirigir al paso 2: pedir el código TOTP (Google Authenticator, etc.)
}

// Verificar el código TOTP
var verifyResult = await signInManager.TwoFactorAuthenticatorSignInAsync(
    code: "123456",
    isPersistent: false,
    rememberClient: false);
```

Para el curso: `TwoFactorEnabled = false` en todos los usuarios del seed. En producción, los administradores de TechShop deberían tener 2FA obligatorio.

---

## Seed de usuarios y roles: el completo

El seed de M5.1 simplificado. La versión completa con todos los escenarios:

```csharp
// Gateway.API/Identity/IdentitySeeder.cs

public class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        // ── ROLES ────────────────────────────────────────────────
        foreach (var role in new[] { "customer", "admin", "support" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // ── USUARIOS DE PRUEBA ────────────────────────────────────
        var testUsers = new[]
        {
            new { Email = "john@techshop.es",  Password = "Test1234!",
                  Role = "customer", CustomerId = Guid.NewGuid(),
                  FirstName = "John", LastName = "Smith" },
            new { Email = "jane@techshop.es",  Password = "Test1234!",
                  Role = "customer", CustomerId = Guid.NewGuid(),
                  FirstName = "Jane", LastName = "Doe" },
            new { Email = "admin@techshop.es", Password = "Admin1234!",
                  Role = "admin",    CustomerId = Guid.NewGuid(),
                  FirstName = "Admin", LastName = "TechShop" },
        };

        foreach (var u in testUsers)
        {
            if (await userManager.FindByEmailAsync(u.Email) is null)
            {
                var user = new ApplicationUser
                {
                    UserName = u.Email,
                    Email = u.Email,
                    EmailConfirmed = true,
                    CustomerId = u.CustomerId,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                };
                var result = await userManager.CreateAsync(user, u.Password);
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(user, u.Role);
            }
        }
    }
}

// En Program.cs — ejecutar el seed al arrancar
await IdentitySeeder.SeedAsync(app.Services);
```

---

## Testing con Identity: usuarios reales vs tokens fake

En los tests de integración tenemos dos opciones:

**Opción A — JwtTestHelper (rápido, sin BD)**
```csharp
// Tests/Helpers/JwtTestHelper.cs
public static class JwtTestHelper
{
    public static string GenerateToken(
        string userId = "test-user-id",
        string email = "john@techshop.es",
        string customerId = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        string role = "customer",
        string jwtKey = "test-secret-key-for-tests-only-32chars")
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim("customer_id", customerId),
            new Claim(ClaimTypes.Role, role),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

**Opción B — Usuario real en LocalDB (más lento, más realista)**
```csharp
// Crear usuario real en el test de integración
var userManager = scope.ServiceProvider
    .GetRequiredService<UserManager<ApplicationUser>>();

var user = new ApplicationUser
{
    UserName = "john@techshop.es",
    Email = "john@techshop.es",
    EmailConfirmed = true,
    CustomerId = Guid.NewGuid()
};
await userManager.CreateAsync(user, "Test1234!");
await userManager.AddToRoleAsync(user, "customer");

// Ahora hacer login real a través del endpoint
var loginResponse = await client.PostAsJsonAsync("/account/login",
    new { Email = "john@techshop.es", Password = "Test1234!" });
var token = (await loginResponse.Content.ReadFromJsonAsync<LoginResponse>())!.Token;
```

Para la mayoría de tests usar **Opción A** — es más rápido. Para los tests de login, registro y ownership usar **Opción B**.

---

## Ownership check: john no puede ver pedidos de jane

El test más importante de seguridad en OrderFlow:

```csharp
[Fact]
public async Task GetOrder_ShouldReturn403_WhenOrderBelongsToAnotherCustomer()
{
    // Arrange
    var johnCustomerId  = Guid.NewGuid();
    var janeCustomerId  = Guid.NewGuid();

    // Token de John
    var johnToken = JwtTestHelper.GenerateToken(
        customerId: johnCustomerId.ToString(), role: "customer");

    // Pedido que pertenece a Jane
    var janeOrderId = await CreateOrder(janeCustomerId);

    // Act — John intenta ver el pedido de Jane
    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", johnToken);

    var response = await _client.GetAsync($"/api/orders/{janeOrderId}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}
```

Este test verifica que el handler comprueba el `CustomerId` del JWT contra el `CustomerId` del pedido. Si falla, cualquier usuario autenticado puede ver los pedidos de cualquier otro.

---

## Identity en el dashboard de Aspire

Identity con LocalDB se integra perfectamente en el dashboard de Aspire. En el AppHost:

```csharp
// OrderFlow.AppHost/Program.cs

var sqlServer = builder.AddConnectionString("sqlserver");

// BD dedicada para Identity (separada de OrdersDb, ProductsDb...)
var identityDb = sqlServer.AddDatabase("IdentityDb");

var gateway = builder.AddProject<Projects.Gateway_API>("gateway-api")
    .WithReference(identityDb)    // Identity
    .WithReference(ordersDb)      // Para los health checks del gateway
    .WithReference(messaging);    // Para correlationId

// El gateway expone:
//   /account/login    → genera JWT
//   /account/logout   → invalida refresh token
//   /api/*            → YARP → servicios internos
```

En el dashboard de Aspire, las llamadas a `/account/login` aparecen como trazas. Puedes ver el tiempo de hashing de contraseña, la consulta a AspNetUsers, la generación del token.

---

## Migración a Azure AD en producción: lo que cambia

En M7.2 migramos de Identity local a Azure AD. El código de validación **no cambia**. Solo cambia la configuración:

```csharp
// Program.cs del Gateway — ANTES (Identity local)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = "https://gateway.local",
            ValidAudience = "orderflow-api",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["Jwt:Key"]!))
        };
    });

// Program.cs del Gateway — DESPUÉS (Azure AD)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration);
// En appsettings.Production.json:
// "AzureAd": { "TenantId": "...", "ClientId": "..." }
```

Los servicios internos (Orders.API, Products.API...) no cambian en absoluto — solo validan el token, no les importa quién lo emitió.

---

## External Login Providers: Google y Microsoft

Para el futuro de TechShop — login con Google o cuenta Microsoft:

```csharp
// Gateway.API/Program.cs — añadir providers externos

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = config["Authentication:Google:ClientId"]!;
        options.ClientSecret = config["Authentication:Google:ClientSecret"]!;
        // Se configura desde User Secrets — nunca en appsettings.json
    })
    .AddMicrosoftAccount(options =>
    {
        options.ClientId = config["Authentication:Microsoft:ClientId"]!;
        options.ClientSecret = config["Authentication:Microsoft:ClientSecret"]!;
    });

// El flujo:
// 1. Usuario hace clic en "Login con Google"
// 2. Redirect a Google OAuth
// 3. Google callback → Gateway.API/signin-google
// 4. Identity crea/encuentra el ApplicationUser
// 5. Gateway genera JWT propio y lo devuelve al cliente
// Los servicios internos nunca ven el token de Google — solo el JWT de OrderFlow
```

---

## Checklist del Bonus — ASP.NET Core Identity

**Arquitectura:**
- ✅ Entiendes los tres managers: UserManager, SignInManager, RoleManager
- ✅ Sabes la diferencia entre IdentityUser y ApplicationUser
- ✅ Conoces las 5 tablas que crea IdentityDbContext
- ✅ Entiendes PBKDF2 y por qué nunca se almacenan contraseñas en plano

**Implementación:**
- ✅ ApplicationUser extendido con CustomerId y datos del dominio
- ✅ UserManager: CreateAsync, FindByEmailAsync, CheckPasswordAsync, AddToRoleAsync
- ✅ SignInManager: CheckPasswordSignInAsync con lockout activado
- ✅ RoleManager: seed de roles customer/admin/support
- ✅ JwtTokenService: claims del dominio en el token
- ✅ Password reset con GeneratePasswordResetTokenAsync
- ✅ Refresh tokens almacenados en AspNetUserTokens

**Seguridad:**
- ✅ Password policies configuradas (longitud, complejidad, lockout)
- ✅ Ownership check: john no puede ver pedidos de jane
- ✅ Claims-based policies más allá de roles simples
- ✅ Cookie vs JWT: sabes cuándo usar cada uno

**Testing:**
- ✅ JwtTestHelper para tests rápidos sin BD
- ✅ Usuarios reales en LocalDB para tests de integración
- ✅ Test de ownership implementado y pasando

**Producción:**
- ✅ Entiendes la migración de Identity local a Azure AD
- ✅ La línea de cambio en Program.cs al migrar

---

## Lo que este Bonus te da

Con M5.1 tienes autenticación funcionando en OrderFlow. Con este Bonus tienes el conocimiento para:

- Construir un sistema de autenticación desde cero en cualquier proyecto .NET
- Depurar problemas de Identity cuando algo falla en producción
- Extender Identity con cualquier requisito del negocio
- Explicarle a tu equipo cómo funciona la seguridad del sistema

ASP.NET Core Identity no es magia. Es tres managers, cinco tablas y un algoritmo de hashing. Ahora lo sabes todo.
