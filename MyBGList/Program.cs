using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using MyBGList.Constants;
using MyBGList.Models;
using MyBGList.Swagger;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;


var builder = WebApplication.CreateBuilder(args);
// built-in logging provider configuration
builder.Logging
	.ClearProviders()
	.AddConsole()
	.AddDebug();


//Serilog configuration

builder.Host.UseSerilog((hostBuilderContext, loggerConfg) =>
{
	loggerConfg.ReadFrom.Configuration(hostBuilderContext.Configuration);
	loggerConfg.WriteTo.File("Logs/EventLogs.txt", rollingInterval: RollingInterval.Minute);
	loggerConfg.WriteTo.Console();
	loggerConfg.WriteTo.File("Logs/Errors.txt"
		, outputTemplate:
			"{Timestamp:HH:mm:ss} [{Level:u3}] " +
			"[{MachineName} #{ThreadId}] " +
			"{Message:lj}{NewLine}{Exception}"
		, restrictedToMinimumLevel: LogEventLevel.Error
		, rollingInterval: RollingInterval.Day);

	loggerConfg.Enrich.WithMachineName();
	loggerConfg.Enrich.WithThreadId();
	loggerConfg.Enrich.WithThreadName();
	loggerConfg.WriteTo.MSSqlServer(connectionString: hostBuilderContext.Configuration.GetConnectionString("DefaultConnection"),
		sinkOptions: new MSSqlServerSinkOptions()
		{
			AutoCreateSqlTable = true,
			TableName = "LogEvents"
		},
		columnOptions: new ColumnOptions()
		{
			AdditionalColumns = new SqlColumn[]
			{
				new SqlColumn()
				{
					ColumnName = "SourceContext",
					PropertyName="SourceContext",
					DataType= System.Data.SqlDbType.NVarChar
				}
			}
		});
}, writeToProviders: true);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddResponseCaching(options =>
{
	options.SizeLimit = 50 * 1024 * 1024;       //50MB
	options.MaximumBodySize = 32 * 1024 * 1024; //32MB
});

builder.Services.AddMemoryCache();

builder.Services.AddControllers(options =>
{
	options.CacheProfiles.Add("NoCache", new CacheProfile() { NoStore = true });
	options.CacheProfiles.Add("Any-60", new CacheProfile() { Location = ResponseCacheLocation.Any, Duration = 60 });
	options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(x => $"The value {x} is invalid");
	options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(x => $"The value {x} must be a number");
	options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((x, y) => $"The value {x} is not valid for {y}");
	options.ModelBindingMessageProvider.SetMissingKeyOrValueAccessor(() => "The value is required");
}
);
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
	options.SuppressModelStateInvalidFilter = true;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	options.ParameterFilter<SortOrderFilter>();
	options.ParameterFilter<SortColumnFilter>();

	options.ParameterFilter<SortColumnFilter>();
	options.ParameterFilter<SortOrderFilter>();

	options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		In = ParameterLocation.Header,
		Description = "Please enter token",
		Name = "Authorization",
		Type = SecuritySchemeType.Http,
		BearerFormat = "JWT",
		Scheme = "bearer"
	});
	options.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type=ReferenceType.SecurityScheme,
					Id="Bearer"
				}
			},
			Array.Empty<string>()
		}
	});

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

builder.Services.AddIdentity<ApiUser, IdentityRole>(options =>
{
	options.Password.RequiredLength = 8;
	options.Password.RequireDigit = true;
	options.Password.RequireLowercase = true;
	options.Password.RequireUppercase = true;


}).AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthentication(options =>
{
	options.DefaultScheme =
	options.DefaultForbidScheme =
	options.DefaultAuthenticateScheme =
	options.DefaultSignOutScheme =
	options.DefaultSignInScheme =
	JwtBearerDefaults.AuthenticationScheme;
})
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters()
		{
			ValidateIssuer = true,
			ValidIssuer = builder.Configuration["JWT:Issuer"],
			ValidateAudience = true,
			ValidAudience = builder.Configuration["JWT:Audience"],
			ValidateIssuerSigningKey = true,
			IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration["JWT:SigningKey"]))

		};
	});



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}
if (app.Configuration.GetValue<bool>("UseDeveloperExceptionPage"))
{
	app.UseDeveloperExceptionPage();
}
else
{
	app.UseExceptionHandler(action =>
	{
		action.Run(async context =>
		{
			var exceptionHandler =
			context.Features.Get<IExceptionHandlerPathFeature>();
			var details = new ProblemDetails();
			details.Detail = exceptionHandler?.Error.Message;
			details.Extensions["traceId"] =
			System.Diagnostics.Activity.Current?.Id
			?? context.TraceIdentifier;
			details.Type =
			"https://tools.ietf.org/html/rfc7231#section-6.6.1";
			details.Status = StatusCodes.Status500InternalServerError;
			await context.Response.WriteAsync(
			System.Text.Json.JsonSerializer.Serialize(details));
			app.Logger.LogError(CustomLogEvents.Error_Get, exceptionHandler?.Error, "An unhandeled exception ocurres");
		});
	}); ;
}



app.UseHttpsRedirection();
app.UseCors();
app.UseResponseCaching();
app.UseAuthentication();
app.UseAuthorization();
app.Use((context, next) =>
{
	context.Response.Headers["cache-control"] = "no-cahce, no-store";
	return next.Invoke();
});
app.MapGet("cache/test/1", (HttpContext httpContext) =>
{
	httpContext.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
	{
		NoCache = true,
		NoStore = true,
	};
	return Results.Ok();
});
app.MapGet("/auth/test/1", [Authorize, EnableCors("AnyOrigin")] () =>
{
	return Results.Ok("You are authorized");
});
app.MapGet("/auth/test/2", [Authorize(Roles = UserRoles.Moderator)][EnableCors("AnyOrigin")][ResponseCache(NoStore = true)] () =>
{
	return Results.Ok("You are authorized");
});
app.MapGet("/auth/test/3", [Authorize(Roles = UserRoles.Adminstrator)][EnableCors("AnyOrigin")][ResponseCache(NoStore = true)] () =>
{
	return Results.Ok("You are authorized");
});
app.MapGet("/error/test", () => { throw new InvalidDataException(); });


app.MapControllers();
app.MapDefaultControllerRoute();

app.Run();
