using Customer.WebApi.Config;
using Customer.WebApi.Filter;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SunShine.Filter;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

// Create a WebApplication builder with default configurations and command-line arguments
var builder = WebApplication.CreateBuilder(args);

// Configure controllers with custom filters and formatters
// ... 现有代码 ...

// Configure controllers with custom filters and formatters
builder.Services.AddControllers(options => {
    // Add custom authentication filter to all controllers (global filter)
    options.Filters.Add<AuthFilter>();
    // Add global exception handling filter for API requests
    options.Filters.Add<ApiExceptionFilterAttribute>();
})
    // Add XML serializer support for request/response formatting
    .AddXmlSerializerFormatters()
    // Configure JSON serialization options
    .AddJsonOptions(options =>
    {
        // Custom datetime converter (commented out) - format: "yyyy-MM-dd HH:mm:ss"
        // options.JsonSerializerOptions.Converters.Add(new DatetimeJsonConverter("yyyy-MM-dd HH:mm:ss"));

        // Set JSON property naming policy to camelCase (first letter lowercase)
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        // Disable custom naming policy (commented out) - keep original property names
        //options.JsonSerializerOptions.PropertyNamingPolicy = null;

        // Disable Unicode encoding (support full character set including Chinese)
        options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);

        // Ignore null values in JSON serialization (commented out)
        //options.JsonSerializerOptions.IgnoreNullValues = true;

        // Allow trailing commas in JSON payloads (improves compatibility)
        options.JsonSerializerOptions.AllowTrailingCommas = true;

        // Enable case-insensitive property name matching during deserialization
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// 注册 JsonSerializerOptions 为单例服务，以便 AuthFilter 注入
builder.Services.AddSingleton(provider =>
{
    var options = new JsonSerializerOptions();
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
    options.AllowTrailingCommas = true;
    options.PropertyNameCaseInsensitive = true;
    return options;
});

// ... 现有代码 ...


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            ClockSkew = TimeSpan.Zero // Disable token expiration tolerance
        };
    });
// Batch register application services from configuration
builder.Services.BatchRegisterServices(builder.Configuration);

// Add core controller services (base MVC controller support)
builder.Services.AddControllers();

// Register in-memory caching service
builder.Services.AddMemoryCache();

// Add Swagger/OpenAPI documentation support
builder.Services.AddSwaggerGen();

// Build the WebApplication instance with configured services
var app = builder.Build();

// Configure middleware pipeline for development environment
if (app.Environment.IsDevelopment())
{
    // Enable Swagger JSON endpoint
    app.UseSwagger();
    // Enable Swagger UI for API testing/documentation
    app.UseSwaggerUI();
}

// Redirect HTTP requests to HTTPS
app.UseHttpsRedirection();

// Enable ASP.NET Core authorization middleware
app.UseAuthorization();

// Map controller endpoints to the request pipeline
app.MapControllers();

// Start the web application
app.Run();