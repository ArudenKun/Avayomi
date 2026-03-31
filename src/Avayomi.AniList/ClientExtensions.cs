namespace Avayomi.AniList;

public static class ClientExtensions
{
    extension(ISearchQuery query)
    {
        public Task<StrawberryShake.IOperationResult<ISearchResult>> ExecuteAsync(
            string search,
            int page = 1,
            int perPage = 20,
            CancellationToken cancellationToken = default
        )
        {
            return query.ExecuteAsync(search, page, perPage, cancellationToken);
        }
    }
}
