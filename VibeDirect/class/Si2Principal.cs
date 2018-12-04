using System;
using System.Security.Principal;

namespace VibeDirect
{
    /// <summary>
    /// Summary description for Si2Principal.
    /// </summary>
    public class Si2Principal : IPrincipal
    {
        private IIdentity _identity;
        private string[] _roles;
        private string _fullName = "";
        private string _refCode = "";

        public bool IsInRole(string role)
        {
            return Array.BinarySearch(_roles, role) >= 0 ? true : false;
        }

        public IIdentity Identity
        {
            get
            {
                return _identity;
            }
        }

        public Si2Principal(IIdentity identity, string[] roles)
        {
            _identity = identity;
            _roles = new string[roles.Length];
            roles.CopyTo(_roles, 0);
            Array.Sort(_roles);
        }

        public Si2Principal(IIdentity identity, string[] roles, string FullName, string RefCode)
        {
            _fullName = FullName;
            _refCode = RefCode;
            _identity = identity;
            _roles = new string[roles.Length];
            roles.CopyTo(_roles, 0);
            Array.Sort(_roles);
        }

        public string FullName
        {
            get
            {
                return (_fullName);
            }
        }

        public string RefCode
        {
            get
            {
                return (_refCode);
            }
        }
    }
}
