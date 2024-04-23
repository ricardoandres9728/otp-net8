using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace otel.Extensions;

public static class Extensions
{
    public static IHostBuilder AddServiceDefaults(this IHostBuilder builder, IConfiguration configuration)
    {
        builder.ConfigureOpenTelemetry(configuration);


        builder.ConfigureServices(services => {
            services.AddDefaultHealthChecks();
        });

        return builder;
    }

    public static IHostBuilder ConfigureOpenTelemetry(this IHostBuilder builder, IConfiguration configuration)
    {
        builder.ConfigureLogging(host =>
        {
            host.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddOpenTelemetry().WithMetrics(metrics =>
            {
                metrics.AddRuntimeInstrumentation()
                       .AddBuiltInMeters();
            })
           .WithTracing(tracing =>
           {
               tracing.SetSampler(new AlwaysOnSampler());
               tracing.AddAspNetCoreInstrumentation()
                      .AddGrpcClientInstrumentation()
                      .AddHttpClientInstrumentation();
           });

            services.AddOpenTelemetryExporters(configuration);
        });
       

        return builder;
    }

    private static IServiceCollection AddOpenTelemetryExporters(this IServiceCollection builder, IConfiguration configuration)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Configure<OpenTelemetryLoggerOptions>(logging => logging.AddOtlpExporter());
            builder.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddOtlpExporter());
            builder.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddOtlpExporter());
        }

        // Uncomment the following lines to enable the Prometheus exporter (requires the OpenTelemetry.Exporter.Prometheus.AspNetCore package)
        builder.AddOpenTelemetry()
           .WithMetrics(metrics => metrics.AddPrometheusExporter());

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.Exporter package)
        // builder.Services.AddOpenTelemetry()
        //    .UseAzureMonitor();

        return builder;
    }

    public static IServiceCollection AddDefaultHealthChecks(this IServiceCollection builder)
    {
        builder.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Uncomment the following line to enable the Prometheus endpoint (requires the OpenTelemetry.Exporter.Prometheus.AspNetCore package)
        app.MapPrometheusScrapingEndpoint();

        // All health checks must pass for app to be considered ready to accept traffic after starting
        app.MapHealthChecks("/health");

        // Only health checks tagged with the "live" tag must pass for app to be considered alive
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        return app;
    }

    private static MeterProviderBuilder AddBuiltInMeters(this MeterProviderBuilder meterProviderBuilder) =>
        meterProviderBuilder.AddMeter(
            "Microsoft.AspNetCore.Hosting",
            "Microsoft.AspNetCore.Server.Kestrel",
            "System.Net.Http");
}
