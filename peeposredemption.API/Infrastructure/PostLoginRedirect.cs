namespace peeposredemption.API.Infrastructure
{
    public static class PostLoginRedirect
    {
        // community.torvex.app users land straight in the chat; everyone else on the business dashboard
        public static string Page(HttpRequest request) =>
            request.Host.Host.Equals("community.torvex.app", StringComparison.OrdinalIgnoreCase)
                ? "/App/Index"
                : "/Dashboard";
    }
}
