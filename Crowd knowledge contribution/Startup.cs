using System;
using Crowd_knowledge_contribution.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Crowd_knowledge_contribution.Startup))]
namespace Crowd_knowledge_contribution
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);

            CreateApplicationRoles();
        }

        private void CreateApplicationRoles()
        {
            var context = new ApplicationDbContext();

            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));

            if (!roleManager.RoleExists("Admin"))
            {
                var role = new IdentityRole {Name = "Admin"};
                roleManager.Create(role);

                var user = new ApplicationUser {UserName = "admin@gmail.com", Email = "admin@gmail.com"};

                var adminCreated = userManager.Create(user, "!1Admin");
                if (adminCreated.Succeeded)
                    userManager.AddToRole(user.Id, "Admin");
            }

            if (!roleManager.RoleExists("Editor"))
            {
                var role = new IdentityRole {Name = "Editor"};
                roleManager.Create(role);
            }

            if (!roleManager.RoleExists("User"))
            {
                var role = new IdentityRole {Name = "User"};
                roleManager.Create(role);
            }

        }
    }
}
