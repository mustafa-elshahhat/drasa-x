namespace DerasaX.Application.Common
{
    /// <summary>
    /// Common bounded paging contract for list endpoints (Phase 5 §8.5). Page size is
    /// clamped to a safe maximum so a caller can never request an unbounded result set,
    /// and page number is floored at 1. Bind from the query string via <c>[FromQuery]</c>.
    /// </summary>
    public class PaginationParameters
    {
        public const int MaxPageSize = 100;
        private const int DefaultPageSize = 20;

        private int _pageNumber = 1;
        private int _pageSize = DefaultPageSize;

        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value < 1 ? DefaultPageSize : (value > MaxPageSize ? MaxPageSize : value);
        }

        /// <summary>Optional free-text search term (whitelisted per endpoint; never used to build raw SQL).</summary>
        public string? Search { get; set; }
    }
}
