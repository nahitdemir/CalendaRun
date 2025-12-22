using Calendarun.Contracts.Planning;
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

// Add EmailService
var smtpHost = builder.Configuration["Smtp:Host"] ?? "localhost";
var smtpPort = int.Parse(builder.Configuration["Smtp:Port"] ?? "1025");
var smtpFrom = builder.Configuration["Smtp:From"] ?? "noreply@calendarun.local";
builder.Services.AddSingleton(new EmailService(smtpHost, smtpPort, smtpFrom));

// Add MassTransit
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<UserPlannedEventConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        // Set entity name for message routing
        cfg.Message<PlanningUserPlannedV1>(m => m.SetEntityName("planning.userplanned.v1"));

        cfg.ReceiveEndpoint("planning.userplanned.v1", e =>
        {
            e.ConfigureConsumer<UserPlannedEventConsumer>(context);
        });
    });
});

var host = builder.Build();

// Startup test mail
host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStarted.Register(() =>
{
    _ = Task.Run(async () =>
    {
        try
        {
            using var scope = host.Services.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<EmailService>();
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("BootMail");

            logger.LogInformation("📧 Sending boot test email...");
            
            await sender.SendEmailAsync(
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
