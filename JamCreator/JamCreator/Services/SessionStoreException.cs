public class SessionStoreException : Exception
{
    public SessionStoreException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
