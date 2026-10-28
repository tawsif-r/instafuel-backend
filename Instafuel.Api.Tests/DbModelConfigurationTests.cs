using Instafuel.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Instafuel.Api.Tests;

public class DbModelConfigurationTests
{
    [Fact]
    public void UserAndRoleMappingsUseSnakeCaseNames()
    {
        using var context = CreateContext();

        var user = context.Model.FindEntityType(typeof(User))!;
        var userTable = StoreObjectIdentifier.Table("users", null);
        Assert.Equal("users", user.GetTableName());
        Assert.Equal("id", user.FindProperty(nameof(User.Id))!.GetColumnName(userTable));
        Assert.Equal("full_name", user.FindProperty(nameof(User.FullName))!.GetColumnName(userTable));

        var role = context.Model.FindEntityType(typeof(Role))!;
        var roleTable = StoreObjectIdentifier.Table("roles", null);
        Assert.Equal("name", role.FindProperty(nameof(Role.Name))!.GetColumnName(roleTable));
    }

    [Fact]
    public void UserRoleUsesCompositeKeyAndSnakeCaseForeignKeys()
    {
        using var context = CreateContext();

        var userRole = context.Model.FindEntityType(typeof(UserRole))!;
        var key = userRole.FindPrimaryKey()!;
        Assert.Equal(new[] { nameof(UserRole.UserId), nameof(UserRole.RoleId) },
            key.Properties.Select(property => property.Name));

        var table = StoreObjectIdentifier.Table("user_roles", null);
        Assert.Equal("user_id", userRole.FindProperty(nameof(UserRole.UserId))!.GetColumnName(table));
        Assert.Equal("role_id", userRole.FindProperty(nameof(UserRole.RoleId))!.GetColumnName(table));
    }

    private static PgDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PgDbContext>()
            .UseNpgsql("Host=localhost;Database=instafuel_model_only;Username=test;Password=test")
            .Options;

        return new PgDbContext(options);
    }
}
