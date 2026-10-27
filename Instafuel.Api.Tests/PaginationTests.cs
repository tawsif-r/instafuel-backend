using System.Text.Json;
using Instafuel.Api.Common.Pagination;
using Xunit;

namespace Instafuel.Api.Tests;

public class PaginationTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(3, 3)]
    public void PagedRequest_ClampsPageCorrectly(int inputPage, int expectedPage)
    {
        var request = new PagedRequest { Page = inputPage };
        Assert.Equal(expectedPage, request.Page);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(-10, 20)]
    [InlineData(50, 50)]
    [InlineData(150, 100)]
    public void PagedRequest_ClampsPageSizeCorrectly(int inputSize, int expectedSize)
    {
        var request = new PagedRequest { PageSize = inputSize };
        Assert.Equal(expectedSize, request.PageSize);
    }

    [Fact]
    public void PagedResult_CalculatesPaginationMetadataCorrectly()
    {
        var items = new List<string> { "a", "b", "c" };
        var result = new PagedResult<string>(items, total: 25, page: 2, pageSize: 10);

        Assert.Equal(25, result.Total);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
        Assert.Equal(3, result.Items.Count);
    }

    [Fact]
    public void PagedResult_EmptyResult_HasZeroTotalPages()
    {
        var result = PagedResult<string>.Empty(page: 1, pageSize: 20);

        Assert.Equal(0, result.Total);
        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
        Assert.Empty(result.Items);
    }

    [Fact]
    public void PagedResult_SerializesExpectedJsonStructure()
    {
        var items = new[] { "item1", "item2" };
        var result = new PagedResult<string>(items, total: 2, page: 1, pageSize: 20);

        var json = JsonSerializer.Serialize(result);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("items", out var itemsProp));
        Assert.Equal(2, itemsProp.GetArrayLength());
        Assert.True(root.TryGetProperty("page", out var pageProp));
        Assert.Equal(1, pageProp.GetInt32());
        Assert.True(root.TryGetProperty("pageSize", out var sizeProp));
        Assert.Equal(20, sizeProp.GetInt32());
        Assert.True(root.TryGetProperty("total", out var totalProp));
        Assert.Equal(2, totalProp.GetInt32());
    }
}

