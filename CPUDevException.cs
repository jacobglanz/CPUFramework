namespace CPUFramework;

public class CPUDevException : Exception
{
    public CPUDevException(string? message, Exception? innerException) : base(message, innerException)
    {

    }
}
