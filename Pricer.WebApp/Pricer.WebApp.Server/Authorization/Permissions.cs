namespace Pricer.WebApp.Server.Authorization;

/// <summary>
/// All permission constants used for policy-based authorization.
/// Each permission maps to a named ASP.NET Core authorization policy.
/// Roles accumulate permissions as IdentityRoleClaims (type = "permission").
///
/// Three levels per feature:
///   *.view   — read-only access
///   *.manage — create / edit
///   *.delete — destructive removal (kept separate because it is usually irreversible)
/// </summary>
public static class Permissions
{
	public const string PrintersView   = "printers.view";
	public const string PrintersManage = "printers.manage";
	public const string PrintersDelete = "printers.delete";

	public const string MaterialsView   = "materials.view";
	public const string MaterialsManage = "materials.manage";
	public const string MaterialsDelete = "materials.delete";

	public const string CurrenciesView   = "currencies.view";
	public const string CurrenciesManage = "currencies.manage";
	public const string CurrenciesDelete = "currencies.delete";

	public const string SettingsView   = "settings.view";
	public const string SettingsManage = "settings.manage";

	public const string TransactionsView   = "transactions.view";
	public const string TransactionsManage = "transactions.manage";
	public const string TransactionsDelete = "transactions.delete";

	public const string UsersManage = "users.manage";
	public const string UsersDelete = "users.delete";

	public const string RolesManage = "roles.manage";
	public const string RolesDelete = "roles.delete";

	/// <summary>All defined permissions — used to seed the Administrator role and validate input.</summary>
	public static readonly IReadOnlyList<string> All =
	[
		PrintersView,   PrintersManage,   PrintersDelete,
		MaterialsView,  MaterialsManage,  MaterialsDelete,
		CurrenciesView, CurrenciesManage, CurrenciesDelete,
		SettingsView,   SettingsManage,
		TransactionsView, TransactionsManage, TransactionsDelete,
		UsersManage,  UsersDelete,
		RolesManage,  RolesDelete,
	];
}
