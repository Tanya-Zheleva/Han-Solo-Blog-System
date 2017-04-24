using System.Data.Entity;
using Microsoft.Owin;
using Owin;
using BlogSystem.Migrations;
using BlogSystem.Models;

[assembly: OwinStartupAttribute(typeof(BlogSystem.Startup))]
namespace BlogSystem
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            Database.SetInitializer(
                new MigrateDatabaseToLatestVersion<BlogDbContext, Configuration>());

            ConfigureAuth(app);
        }
    }
}
