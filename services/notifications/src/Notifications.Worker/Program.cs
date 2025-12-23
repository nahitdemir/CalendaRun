using Calendarun.Contracts.Planning;
using Calendarun.Contracts.Settings;
using Calendarun.Settings.Client;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Notifications.Infrastructure;
using Notifications.Worker;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "Notifications.Worker")
    .WriteTo.Console(outputTemplate: 
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Service} {CorrelationId} {PlanItemId} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSerilog();

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("NotificationsDb");
builder.Services.AddDbContext<NotificationsDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add Settings Client
builder.Services.AddSettingsClient(options =>
{
    options.SettingsServiceUrl = builder.Configuration["SettingsService:Url"] ?? "http://localhost:5301";
    options.RedisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.Environment = builder.Configuration["Environment"] ?? "dev";
    options.WarmupKeys = new[]
    {
        "notifications.smtp.host",
        "notifications.smtp.port",
        "notifications.smtp.from",
        "notifications.email.subject_template",
        "notifications.email.body_template"
    };
});

// Add EmailService (now using ISettingsClient)
builder.Services.AddScoped<EmailService>();

// Add MassTransit
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<UserPlannedEventConsumer>();
    x.AddSettingsChangedConsumer("notifications-worker");

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // Set entity name for message routing
        cfg.Message<PlanningUserPlannedV1>(m => m.SetEntityName("planning.userplanned.v1"));
        cfg.Message<SettingsChangedV1>(m => m.SetEntityName("settings.changed.v1"));

        cfg.ReceiveEndpoint("planning.userplanned.v1", e =>
        {
            e.ConfigureConsumer<UserPlannedEventConsumer>(context);
        });

        cfg.ConfigureSettingsChangedEndpoint(context, "notifications-worker");
    });
});

var host = builder.Build();

// Startup test mail using settings
host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStarted.Register(() =>
{
    _ = Task.Run(async () =>
    {
        try
        {
            // Give settings warmup time to complete
            await Task.Delay(2000);
            
            using var scope = host.Services.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("BootMail");

            logger.LogInformation("📧 Sending boot test email...");
            
            await emailService.SendEmailAsync(
                "nahit@local", 
                "Worker boot test", 
                "Worker started and SMTP works", 
                CancellationToken.None);

            logger.LogInformation("✅ Boot test email sent");
        }
        catch (Exception ex)
        {
            var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("BootMail");
            logger.LogError(ex, "❌ Boot test email failed");
        }
    });
});

host.Run();
