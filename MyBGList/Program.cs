using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
builder.Services.AddControllers(options =>
{
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
app.UseAuthorization();
app.MapGet("/error/test", () => { throw new InvalidDataException(); });
app.MapGet("/cod/test", [EnableCors("AnyOrigin")][ResponseCache(NoStore = true)] () =>

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
