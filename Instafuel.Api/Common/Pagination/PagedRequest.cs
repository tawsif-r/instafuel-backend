namespace Instafuel.Api.Common.Pagination;

public record PagedRequest
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;
    private const int MaxPage = int.MaxValue / MaxPageSize + 1;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;

    public int Page
    {
        get => _page;
        init => _page = value < 1 ? 1 : (value > MaxPage ? MaxPage : value);
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value < 1 ? DefaultPageSize : (value > MaxPageSize ? MaxPageSize : value);
    }
}

