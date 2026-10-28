using CMS.Application.Common.Abstractions;
using CMS.Application.Identity;
using CMS.Domain.Identity.Roles;
using CMS.Domain.Identity.Users;
using NSubstitute;

namespace CMS.Application.UnitTests;

// Role creation and assignment cannot grant capabilities outside the explicit catalogue.
public sealed class RoleHandlerTests
{
    // Missing roles or users fail without mutating an account.
    [Fact]
    public async Task Roles_require_valid_permissions_and_tenant_visible_owners()
    {
        var tenantId = Guid.NewGuid();
        var tenant = Substitute.For<ICurrentTenant>();
        tenant.TenantId.Returns(tenantId);
        var ids = Substitute.For<IIdGenerator>();
        ids.NewId().Returns(Guid.NewGuid());
        var roles = Substitute.For<IRepository<Role>>();
        var create = new CreateRoleHandler(roles, tenant, ids);
        Assert.True((await create.Handle(new("Editor", ["content.edit"], "key"), default)).IsSuccess);
        Assert.True((await create.Handle(new("Editor", ["invented.permission"], "key"), default)).IsFailure);
        roles.Received(1).Add(Arg.Any<Role>());
        var users = Substitute.For<IRepository<User>>();
        var assign = new AssignRoleHandler(users, roles);
        var user = User.Create(Guid.NewGuid(), tenantId, "user@example.test", "hash").Value;
        var role = Role.Create(Guid.NewGuid(), tenantId, "Editor", ["content.edit"]).Value;
        Assert.True((await assign.Handle(new(user.Id, role.Id, "key"), default)).IsFailure);
        users.GetByIdAsync(user.Id, default).Returns(user);
        roles.GetByIdAsync(role.Id, default).Returns(role);
        Assert.True((await assign.Handle(new(user.Id, role.Id, "key"), default)).IsSuccess);
        Assert.Contains(role.Id.ToString(), user.RoleIdsJson);
    }
}
