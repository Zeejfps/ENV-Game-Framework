#pragma warning disable SYSLIB0003

using System.Security;
using System.Security.Permissions;

namespace ZGF.ILSpy.App.Tests.Fixtures;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class GuardAttribute(SecurityAction action) : CodeAccessSecurityAttribute(action)
{
    public override IPermission? CreatePermission() => null;
}

public class Guarded
{
    [Guard(SecurityAction.Demand)]
    public void Enter()
    {
    }
}
