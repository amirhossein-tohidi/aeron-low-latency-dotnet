namespace Aeron.LowLatency.Core;

public static class AeronClientFactory
{
    public static Adaptive.Aeron.Aeron Connect(AeronSettings settings, string clientName)
    {
        var context = new Adaptive.Aeron.Aeron.Context().ClientName(clientName);
        if (!string.IsNullOrWhiteSpace(settings.AeronDirectory))
        {
            context.AeronDirectoryName(settings.AeronDirectory);
        }

        return Adaptive.Aeron.Aeron.Connect(context);
    }
}
