# How-to: Add a New Authorization Policy or Role

This guide explains how to define new roles and policies in the Menlo application.

## Adding a New Role

Roles map to Entra ID App Roles. To add a new role:

1. Open `src/api/Menlo.Api/Auth/Policies/MenloPolicies.cs`.
2. Add a new constant to the `Roles` nested class.

```csharp
public static class Roles
{
    // ... existing roles
    public const string Auditor = "Menlo.Auditor";
}
```

> **Important:** You must also define this App Role in the Entra ID App Registration manifest for it to be assigned to users.

## Adding a New Policy

Policies define the requirements for access, usually based on roles.

1. Open `src/api/Menlo.Api/Auth/Policies/MenloPolicies.cs`.
2. Add a new policy name constant.

   ```csharp
   public static class MenloPolicies
   {
       // ... existing policies
       public const string CanAuditLog = "CanAuditLog";
   }
   ```

3. Open `src/api/Menlo.Api/Auth/Policies/AuthPoliciesExtensions.cs`.
4. Register the policy in the `AddMenloPolicies` method.

```csharp
public static AuthorizationBuilder AddMenloPolicies(this AuthorizationBuilder builder)
{
    // ... existing policies

    builder.AddPolicy(MenloPolicies.CanAuditLog, policy =>
        policy.RequireRole(MenloPolicies.Roles.Admin, MenloPolicies.Roles.Auditor));

    return builder;
}
```

## Using the New Policy

You can now use the new policy on endpoints:

```csharp
app.MapGet("/api/audit-logs", GetLogs)
   .RequireAuthorization(MenloPolicies.CanAuditLog);
```
