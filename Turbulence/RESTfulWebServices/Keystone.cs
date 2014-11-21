using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using net.openstack.Core.Domain;
using net.openstack.Providers.Rackspace;
using System.Configuration;

namespace Turbulence.REST
{
    public class Keystone
    {
        public static UserAccess Authenticate(string token)
        {
            var identityProvider = new CloudIdentityProvider(new Uri(ConfigurationManager.AppSettings["Keystone.Uri"]));
            var identity = new ExtendedCloudIdentity()
            {
                TenantName = ConfigurationManager.AppSettings["Keystone.AdminTenant"],
                Username = ConfigurationManager.AppSettings["Keystone.AdminUser"],
                Password = ConfigurationManager.AppSettings["Keystone.AdminPassword"],
            };
            UserAccess userAccess = identityProvider.ValidateToken(token, null, identity);
            bool isUser = userAccess.User.Roles.Any(item => (item.Name == "user" || item.Name == "admin"));
            if (!isUser)
            {
                throw new NotAuthorizedException("You do not have the permission to access this service");
            }
            return userAccess;
        }
    }
}