using System;
using System.Collections.Generic;
using System.Linq;

namespace ArchiveSystem.Application.Authorization;

public static class Permissions
{
    public const string ClaimType = "permission";

    public static class DynamicFields
    {
        public const string Manage = "dynamicfields.manage";
    }

    public static class Categories
    {
        public const string Create = "categories.create";
        public const string Edit = "categories.edit";
        public const string Read = "categories.read";
        public const string Manage = "categories.manage";
    }

    public static class SoftwareTabs
    {
        public const string Dashboard = "tabs.dashboard.view";
        public const string Archive = "tabs.archive.view";
        public const string Roles = "tabs.roles.view";
        public const string Settings = "tabs.settings.view";
        public const string Reports = "tabs.reports.view";
        public const string AccessControl = "tabs.accesscontrol.view";
        public const string CaseLoans = "tabs.caseloans.view";
        public const string Taxpayers = "tabs.taxpayers.view";
        public const string Users = "tabs.users.view";
    }

    public static class Reports
    {
        public const string ExportExcel = "reports.exportexcel";
    }

    public static class Tags
    {
        public const string Manage = "tags.manage";
    }

    public static class Dossiers
    {
        public const string Manage = "dossiers.manage";
    }

    public static class Files
    {
        public const string Read = "files.read";
        public const string Manage = "files.manage";
        public const string Delete = "files.delete";
    }

    public static class Roles
    {
        public const string Manage = "roles.manage";
    }

    public static class CaseLoans
    {
        public const string Manage = "caseloans.manage";
    }

    public static class Taxpayers
    {
        public const string Manage = "taxpayers.manage";
    }

    public static class Users
    {
        public const string Manage = "users.manage";
    }

    public static class Behsaz
    {
        public const string Search = "behsaz.search";
    }

    private static readonly string[] AllPermissions =
    {
        DynamicFields.Manage,
        Categories.Create,
        Categories.Edit,
        Categories.Read,
        Categories.Manage,
        SoftwareTabs.Dashboard,
        SoftwareTabs.Archive,
        SoftwareTabs.Roles,
        SoftwareTabs.Settings,
        SoftwareTabs.Reports,
        SoftwareTabs.AccessControl,
        SoftwareTabs.CaseLoans,
        SoftwareTabs.Taxpayers,
        SoftwareTabs.Users,
        Reports.ExportExcel,
        Tags.Manage,
        Dossiers.Manage,
        Files.Read,
        Files.Manage,
        Files.Delete,
        Roles.Manage,
        CaseLoans.Manage,
        Taxpayers.Manage,
        Users.Manage,
        Behsaz.Search
    };

    public static IReadOnlyCollection<string> GetAll() => AllPermissions;

    public static bool IsDefined(string permission)
    {
        return !string.IsNullOrWhiteSpace(permission)
               && AllPermissions.Contains(permission.Trim(), StringComparer.OrdinalIgnoreCase);
    }
}
