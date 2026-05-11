namespace INF4027W_BPTTIN002_MiniPrj_2026.Helpers
{
    /// <summary>
    /// Thin wrapper around ISession for typed get/set of user session values.
    /// </summary>
    public static class SessionHelper
    {
        private const string KeyUserId = "UserId";
        private const string KeyEmail = "Email";
        private const string KeyDisplayName = "DisplayName";
        private const string KeyRole = "Role";

        public static void SetUserSession(
            ISession session,
            string userId,
            string email,
            string displayName,
            string role)
        {
            session.SetString(KeyUserId, userId);
            session.SetString(KeyEmail, email);
            session.SetString(KeyDisplayName, displayName);
            session.SetString(KeyRole, role);
        }

        public static string? GetUserId(ISession session)
            => session.GetString(KeyUserId);

        public static string? GetEmail(ISession session)
            => session.GetString(KeyEmail);

        public static string? GetDisplayName(ISession session)
            => session.GetString(KeyDisplayName);

        public static string? GetRole(ISession session)
            => session.GetString(KeyRole);

        public static void ClearUserSession(ISession session)
            => session.Clear();
    }
}