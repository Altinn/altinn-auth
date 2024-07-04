using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace Altinn.ResourceRegistry.Configuration
{
    /// <summary>
    /// Set up custom telemetry for Application Insights
    /// </summary>
    public class CustomTelemetryInitializer : ITelemetryInitializer
    {
        private static int _logged = 0;

        /// <summary>
        /// Custom TelemetryInitializer that sets some specific values for the component
        /// </summary>
        public void Initialize(ITelemetry telemetry)
        {
            if (string.IsNullOrEmpty(telemetry.Context.Cloud.RoleName))
            {
                telemetry.Context.Cloud.RoleName = "resource-registry";
            }

            if (Interlocked.CompareExchange(ref _logged, 1, 0) == 0)
            {
                Console.WriteLine($"Cloud.RoleName: {telemetry.Context.Cloud.RoleName}");
            }
        }
    }
}
