using Serilog.Core;
using Serilog.Events;

namespace EkofyApp.Domain.Utils
{
    public class CustomDateFormatter : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("customTimeStamp", HelperMethod.GetUtcPlus7Time()));
        }
    }
}
