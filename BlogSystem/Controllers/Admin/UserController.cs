using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using BlogSystem.Extensions;
using BlogSystem.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;

namespace BlogSystem.Controllers.Admin
{
    [Authorize(Roles ="Admin")]
    public class UserController : Controller
    {
        // GET: User
        public ActionResult Index()
        {
            return RedirectToAction("List");
        }

        //
        //GET: User/List
        public ActionResult List()
        {
            using (var database = new BlogDbContext())
            {
                var users = database.Users.ToList();

                var admins = this.GetAdminUserNames(users, database);
                this.ViewBag.Admins = admins;

                return View(users);
            }
        }

        //
        //GET: User/Edit
        public ActionResult Edit(string id)
        {
            //Validate id
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            using (var databse = new BlogDbContext())
            {
                //Get user from database
                var user = databse.Users
                    .Where(u => u.Id == id)
                    .First();

                //Check if user exists
                if (user == null)
                {
                    return HttpNotFound();
                }

                //Create a view model
                var viewModel = new EditUserViewModel();
                viewModel.User = user;
                viewModel.Roles = this.GetUserRoles(user, databse);

                //Pass the model to the view
                return View(viewModel);
            }
        }

        //
        //POST: User/Edit
        [HttpPost]
        public ActionResult Edit(string id, EditUserViewModel viewModel)
        {
            //check if model is valid
            if (this.ModelState.IsValid)
            {
                using (var database = new BlogDbContext())
                {
                    //Get user from database
                    var user = database.Users.FirstOrDefault(u => u.Id == id);

                    //Check if user exists
                    if (user == null)
                    {
                        return HttpNotFound();
                    }

                    //If password field is not empty, change password
                    if (!string.IsNullOrEmpty(viewModel.Password))
                    {
                        var hasher = new PasswordHasher();
                        var passwordHash = hasher.HashPassword(viewModel.Password);
                        user.PasswordHash = passwordHash;
                    }

                    //Set user properties
                    user.Email = viewModel.User.Email;
                    user.FullName = viewModel.User.FullName;
                    user.UserName = viewModel.User.Email;
                    this.SetUserRoles(viewModel, user, database);

                    //Save changes
                    database.Entry(user).State = EntityState.Modified;
                    database.SaveChanges();
                    this.AddNotification("User edited!", NotificationType.SUCCESS);

                    return RedirectToAction("List");
                }
            }

            return View(viewModel);
        }

        //
        //GET: User/Delete
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            using (var database = new BlogDbContext())
            {
                //Get user from database
                var user = database.Users
                    .Where(u => u.Id == id)
                    .First();

                //Check if user exists
                if (user == null)
                {
                    return HttpNotFound();
                }

                this.AddNotification("User data will be lost!", NotificationType.WARNING);

                return View(user);
            }
        }

        //
        //POST: User/Delete
        [HttpPost]
        [ActionName("Delete")]
        public ActionResult DeleteConfirmed(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            using (var database = new BlogDbContext())
            {
                //Get user from database
                var user = database.Users
                    .Where(u => u.Id == id)
                    .First();

                //Get user articles from database
                var userArticles = database.Articles
                    .Where(a => a.Author.Id == user.Id);

                //Delete user articles
                foreach (var article in userArticles)
                {
                    database.Articles.Remove(article);
                }

                //Delete user and save changes
                database.Users.Remove(user);
                database.SaveChanges();
                this.AddNotification("User deleted!", NotificationType.SUCCESS);

                return RedirectToAction("List");
            }
        }

        private void SetUserRoles(EditUserViewModel viewModel, ApplicationUser user, BlogDbContext database)
        {
            var userManager = Request
                .GetOwinContext()
                .GetUserManager<ApplicationUserManager>();

            foreach (var role in viewModel.Roles)
            {
                if (role.IsSelected)
                {
                    userManager.AddToRole(user.Id, role.Name);
                }
                else if (!role.IsSelected)
                {
                    userManager.RemoveFromRole(user.Id, role.Name);
                }
            }
        }

        private List<Role> GetUserRoles(ApplicationUser user, BlogDbContext databse)
        {
            //Create user manager
            var userManager = Request
                .GetOwinContext()
                .GetUserManager<ApplicationUserManager>();

            //Get all application roles
            var roles = databse.Roles
                .Select(r => r.Name)
                .OrderBy(r => r)
                .ToList();

            //For each applicatio role, check if the user has it
            var userRoles = new List<Role>();

            foreach (var roleName in roles)
            {
                var role = new Role { Name = roleName };

                if (userManager.IsInRole(user.Id, roleName))
                {
                    role.IsSelected = true;
                }

                userRoles.Add(role);
            }

            //Return a list with all roles
            return userRoles;
        }

        private HashSet<string> GetAdminUserNames(List<ApplicationUser> users, BlogDbContext context)
        {
            var userManager = new UserManager<ApplicationUser>(
                new UserStore<ApplicationUser>(context));

            var admins = new HashSet<string>();

            foreach (var user in users)
            {
                if (userManager.IsInRole(user.Id, "Admin"))
                {
                    admins.Add(user.UserName);
                }
            }

            return admins;
        }
    }
}