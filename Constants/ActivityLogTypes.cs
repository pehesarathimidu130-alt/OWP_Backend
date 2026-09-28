using System;

namespace Backend.Constants
{
    public static class ActivityLogTypes
    {
        // Admin Accounts & Permissions
        public const string AdminAccountCreated = "AdminAccountCreated";
        public const string AdminAccountDeleted = "AdminAccountDeleted";
        public const string AdminPinChanged = "AdminPinChanged";
        public const string AdminPasswordChanged = "AdminPasswordChanged";
        public const string AdminRoleChanged = "AdminRoleChanged";

        // Admin Auth
        public const string AdminLogin = "AdminLogin";
        public const string AdminLogout = "AdminLogout";

        // Vendor Governance
        public const string VendorStatusChanged = "VendorStatusChanged";
        public const string VendorFieldEdited = "VendorFieldEdited";

        // Customer Governance
        public const string CustomerStatusToggled = "CustomerStatusToggled";
    }
}
