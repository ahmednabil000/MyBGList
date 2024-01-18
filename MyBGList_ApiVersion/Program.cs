using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	options.SwaggerDoc(
	"v1",
	new OpenApiInfo { Title = "MyBGList", Version = "v1.0" });
	options.SwaggerDoc(
	"v2",
	new OpenApiInfo { Title = "MyBGList", Version = "v2.0" });
});

builder.Services.AddCors(config =>
{
	config.AddDefaultPolicy(options =>
	{
		options.WithOrigins(builder.Configuration["AllowedOrigins"]);
		options.AllowAnyHeader();
		options.AllowAnyMethod();
	});
	config.AddPolicy(name: "AnyOrigin", options =>
	{
		options.AllowAnyOrigin();
		options.AllowAnyHeader();
		options.AllowAnyMethod();
	});
});

builder.Services.AddApiVersioning(options =>
{
	options.ApiVersionReader = new UrlSegmentApiVersionReader();
	options.AssumeDefaultVersionWhenUnspecified = true;
	options.DefaultApiVersion = new ApiVersion(1, 0);
});
builder.Services.AddVersionedApiExplorer(options =>
{
	options.GroupNameFormat = "'v'VVV";
	options.SubstituteApiVersionInUrl = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI(options =>
	{
		options.SwaggerEndpoint(
		$"/swagger/v1/swagger.json",
		$"MyBGList v1");
		options.SwaggerEndpoint(
		$"/swagger/v2/swagger.json",
		$"MyBGList v2");
	});
}
if (app.Configuration.GetValue<bool>("UseDeveloperExceptionPage"))
{
	app.UseDeveloperExceptionPage();
}
else
{
	app.UseExceptionHandler("/error");
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapGet("/v{version:ApiVersion}/error", [ApiVersion("1.0")][ApiVersion("2.0")] () => Results.Problem());
app.MapGet("/v{version:ApiVersion}/error/test", [ApiVersion("1.0")][ApiVersion("2.0")] () => { throw new InvalidDataException(); });
app.MapGet("/v{version:ApiVersion}/cod/test", [EnableCors("AnyOrigin")][ResponseCache(NoStore = true)][ApiVersion("1.0")][ApiVersion("2.0")] () =>

	Results.Text($@"
        <html>
            <head>
                <title>JavaScript Test</title>
            </head>
            <body>
                <script>
						window.alert(`this is`)
                  </script>
                <noscript>Your client does not support JavaScript</noscript>
            </body>
        </html>",
		"text/html")

);

app.MapControllers();
app.MapDefaultControllerRoute();

app.Run();
