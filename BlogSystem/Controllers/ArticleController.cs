using System.Web;

namespace BlogSystem.Controllers
{
    using System;
    using System.Data.Entity;
    using System.Linq;
    using System.Net;
    using System.Web.Mvc;
    using Models;
    using Extensions;

    public class ArticleController : Controller
    {
        //
        // GET: Article
        public ActionResult Index(string searchString)
        {
            var db = new BlogDbContext();

            var articles = from a in db.Articles
                           select a;

            if (!string.IsNullOrEmpty(searchString))
            {
                articles = articles.Where(s => s.Title.ToLower().StartsWith(searchString.ToLower()));
            }

            return View(articles);

        }

        [HttpPost]
        [Authorize]
        public string Index(FormCollection fc, string searchString)
        {
            return "<h3> From [HttpPost]Index: " + searchString + "</h3>";
        }

        //
        //GET: Article/List
        public ActionResult List()
        {
            using (var database = new BlogDbContext())
            {
                var articles = database.Articles
                    .Include(a => a.Author)
                    .Include(a => a.Tags)
                    .ToList();

                return View(articles);
            }
        }

        //
        //GET: Article/Details
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            using (var database = new BlogDbContext())
            {
                var article = database.Articles
                    .Where(a => a.Id == id)
                    .Include(a => a.Author)
                    .Include(a => a.Tags)
                    .First();

                if (article == null)
                {
                    return HttpNotFound();
                }

                return View(article);
            }
        }

        //
        //GET: Article/Create
        [Authorize]        
        public ActionResult Create()
        {
            using (var database = new BlogDbContext())
            {
                var model = new ArticleViewModel
                {
                    Categories = database.Categories
                        .OrderBy(c => c.Name)
                        .ToList()
                };

                return View(model);
            }
        }

        //
        //POST: Article/Create
        [HttpPost]
        [Authorize]
        public ActionResult Create(ArticleViewModel model, HttpPostedFileBase image)
        {
            if (this.ModelState.IsValid)
            {
                using (var database = new BlogDbContext())
                {
                    var authorId = database.Users
                        .First(u => u.UserName == this.User.Identity.Name)
                        .Id;

                    var article = new Article(authorId, model.Title, model.Content, model.CategoryId);
                    this.SetArticleTags(article, model, database);

                    if (image != null)
                    {
                        var allowedContentTypes = new[] {"image/jpeg", "image/jpg", "image/png"};

                        if (allowedContentTypes.Contains(image.ContentType))
                        {
                            var imagesPath = "/Content/Images";
                            var fileName = image.FileName;
                            var uploadPath = imagesPath + fileName;

                            var physicalPath = this.Server.MapPath(uploadPath);
                            image.SaveAs(physicalPath);

                            article.ImagePath = uploadPath;
                        }
                    }

                    database.Articles.Add(article);
                    database.SaveChanges();
                    this.AddNotification("Article created!", NotificationType.INFO);

                    return RedirectToAction("List");
                }
            }

            return View(model);
        }

        //
        //GET: Articles/Delete
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadGateway);
            }

            using (var database = new BlogDbContext())
            {
                var article = database.Articles
                    .Where(a => a.Id == id)
                    .Include(a => a.Author)
                    .Include(a => a.Category)
                    .First();

                if (!IsUserAuthorizedToEdit(article))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }

                this.ViewBag.TagsString = string.Join(", ", article.Tags.Select(t => t.Name));

                if (article == null)
                {
                    return HttpNotFound();
                }

                this.AddNotification("Article data will be lost!", NotificationType.WARNING);

                //Pass article to view
                return View(article);
            }
        }

        //
        //POST: Articles/Delete
        [HttpPost]
        [ActionName("Delete")]
        public ActionResult DeleteConfirmed(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadGateway);
            }

            using (var database = new BlogDbContext())
            {
                var article = database.Articles
                    .Where(a => a.Id == id)
                    .Include(a => a.Author)
                    .First();

                if (article == null)
                {
                    return HttpNotFound();
                }

                database.Articles.Remove(article);
                database.SaveChanges();
                this.AddNotification("Article deleted!", NotificationType.SUCCESS);

                return RedirectToAction("List");
            }
        }

        //
        //GET: Article/Edit
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadGateway);
            }

            using (var database = new BlogDbContext())
            {
                var article = database.Articles
                    .FirstOrDefault(a => a.Id == id);

                if (!IsUserAuthorizedToEdit(article))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }

                if (article == null)
                {
                    return HttpNotFound();
                }

                var model = new ArticleViewModel
                {
                    Id = article.Id,
                    Title = article.Title,
                    Content = article.Content,
                    CategoryId = article.CategoryId,
                    Categories = database.Categories
                        .OrderBy(c => c.Name)
                        .ToList(),
                    Tags = string.Join(", ", article.Tags.Select(t => t.Name))
                };

                return View(model);
            }
        }

        //
        //POST: Article/Edit
        [HttpPost]
        public ActionResult Edit(ArticleViewModel model, HttpPostedFileBase image)
        {
            if (this.ModelState.IsValid)
            {
                using (var database = new BlogDbContext())
                {
                    var article = database.Articles
                        .FirstOrDefault(a => a.Id == model.Id);

                    article.Title = model.Title;
                    article.Content = model.Content;
                    article.CategoryId = model.CategoryId;
                    this.SetArticleTags(article, model, database);

                    if (image != null)
                    {
                        var allowedContentTypes = new[] { "image/jpeg", "image/jpg", "image/png" };

                        if (allowedContentTypes.Contains(image.ContentType))
                        {
                            var imagesPath = "/Content/Images";
                            var fileName = image.FileName;
                            var uploadPath = imagesPath + fileName;

                            var physicalPath = this.Server.MapPath(uploadPath);
                            image.SaveAs(physicalPath);

                            article.ImagePath = uploadPath;
                        }
                    }

                    database.Entry(article).State = EntityState.Modified;
                    database.SaveChanges();
                    this.AddNotification("Article edited!", NotificationType.SUCCESS);

                    return RedirectToAction("List");
                }
            }

            return View(model);
        }

        private void SetArticleTags(Article article, ArticleViewModel model, BlogDbContext database)
        {
            var tagsString = model.Tags
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.ToLower())
                .Distinct();

            article.Tags.Clear();

            foreach (var tagString in tagsString)
            {
                Tag tag = database.Tags
                    .FirstOrDefault(t => t.Name.Equals(tagString));

                if (tag == null)
                {
                    tag = new Tag() { Name = tagString };
                    database.Tags.Add(tag);
                }

                article.Tags.Add(tag);
            }
        }

        private bool IsUserAuthorizedToEdit(Article article)
        {
            bool isAdmin = this.User.IsInRole("Admin");
            bool isAuthor = article.IsAuthor(this.User.Identity.Name);

            return isAdmin || isAuthor;
        }
    }
}