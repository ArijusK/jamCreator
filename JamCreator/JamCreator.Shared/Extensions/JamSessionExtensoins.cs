using JamCreator.Shared.Models;

namespace JamCreator.Shared.Extensions
{
    public static class JamSessionExtensions
    {
        public static bool IsJoinable(this JamSessionModel session)
        {
            return !session.IsPrivate || !string.IsNullOrWhiteSpace(session.Password);
        }
    }
}
