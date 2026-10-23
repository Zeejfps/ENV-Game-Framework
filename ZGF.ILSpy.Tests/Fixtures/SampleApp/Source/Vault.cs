using System.Security;
using System.Security.Permissions;

namespace SampleApp;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class GuardAttribute(SecurityAction action) : CodeAccessSecurityAttribute(action)
{
    public override IPermission? CreatePermission() => null;
}

[Guard(SecurityAction.Demand)]
public class Vault
{
    [Guard(SecurityAction.Assert)]
    public void Open()
    {
    }
}
