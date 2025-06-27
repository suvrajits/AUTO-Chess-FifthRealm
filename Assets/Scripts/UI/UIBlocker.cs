public static class UIBlocker
{
    public static bool IsBlockingInput { get; private set; } = false;

    public static void SetBlocking(bool state)
    {
        IsBlockingInput = state;
    }
}
