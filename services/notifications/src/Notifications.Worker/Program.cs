using Calendarun.Contracts.Planning;
using Calendarun.Contracts.Settings;
using Calendarun.Settings.Client;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Notifications.Infrastructure;
using Notifications.Infrastructure.Channels;
using Notifications.Infrastructure.Templates;
using Notifications.Worker.Consumers;
using Notifications.Worker.Dispatcher;
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
        "notifications.email.body_template",
        "notifications.reminder.offsets_minutes",
        "notifications.dispatcher.max_attempts",
        "notifications.dispatcher.batch_size"
    };
});

// Add Channel senders
builder.Services.AddScoped<EmailSender>();
builder.Services.AddScoped<SmsSender>();
builder.Services.AddScoped<PushSender>();
builder.Services.AddScoped<IChannelSenderFactory, ChannelSenderFactory>();

// Add Template renderer
builder.Services.AddScoped<ITemplateRenderer, TemplateRenderer>();

// Add MassTransit (Consumer)
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

        // Set entity names for message routing
        cfg.Message<PlanningUserPlannedV1>(m => m.SetEntityName("planning.userplanned.v1"));
        cfg.Message<SettingsChangedV1>(m => m.SetEntityName("settings.changed.v1"));

        cfg.ReceiveEndpoint("planning.userplanned.v1", e =>
        {
            e.ConfigureConsumer<UserPlannedEventConsumer>(context);
            e.UseMessageRetry(r => r.Intervals(
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(30)));
        });

        cfg.ConfigureSettingsChangedEndpoint(context, "notifications-worker");
    });
});

// Add Dispatcher (Background Worker)
builder.Services.AddHostedService<NotificationDispatcher>();

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString!, name: "postgres-notifications-db")
    .AddRabbitMQ("amqp://guest:guest@localhost:5672", name: "rabbitmq");

var host = builder.Build();

// Startup test
host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStarted.Register(() =>
{
    _ = Task.Run(async () =>
    {
        try
        {
            // Give settings warmup time to complete
            await Task.Delay(2000);
            
            using var scope = host.Services.CreateScope();
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("Boot");

            logger.LogInformation("✅ Notifications.Worker started successfully");
            logger.LogInformation("📥 Consumer: Listening for planning.userplanned.v1");
            logger.LogInformation("📮 Dispatcher: Processing due notification jobs");
        }
        catch (Exception ex)
        {
            var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Boot");
            logger.LogError(ex, "❌ Boot check failed");
        }
    });
});

host.Run();
