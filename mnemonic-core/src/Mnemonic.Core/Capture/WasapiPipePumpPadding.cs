namespace Mnemonic.Capture;

internal static class WasapiPipePumpPadding
{
    internal static bool ShouldWriteSilence(
        long nowMs,
        long pipeConnectedAtMs,
        long lastRealPcmAtMs,
        int silencePadDelayMs)
    {
        if (pipeConnectedAtMs <= 0)
        {
            return false;
        }

        var anchorMs = lastRealPcmAtMs > pipeConnectedAtMs ? lastRealPcmAtMs : pipeConnectedAtMs;
        return nowMs - anchorMs >= silencePadDelayMs;
    }
}
