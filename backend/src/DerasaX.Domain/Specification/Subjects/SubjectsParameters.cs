using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DerasaX.Domain.Specification.Subjects
{
    /// <summary>
    /// Paging contract for the subjects list endpoint. Page size is clamped to a safe maximum so a
    /// caller can never request an unbounded result set (Phase 22 Step 8 / PERF-02), and page number is
    /// floored at 1 — matching <see cref="DerasaX.Application.Common.PaginationParameters"/>.
    /// </summary>
    public class SubjectsParameters
    {
        public const int MaxPageSize = 100;
        private const int DefaultPageSize = 10;

        private int _pageNumber = 1;
        private int _pageSize = DefaultPageSize;

        public string? Search { get; set; }

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
    }
}
