using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Crowd_knowledge_contribution.Models;

namespace Crowd_knowledge_contribution.Controllers
{
    public class VersionsController : Controller
    {
        private readonly ApplicationDbContext _database = new ApplicationDbContext();
        // GET: Versions
        [Authorize(Roles = "Admin")]
        public ActionResult Index(int id)
        {
            var versions = _database.Articles.Where(article => article.ArticleId == id)
                .OrderByDescending(article => article.LastModified);

            var maxVersion = _database.Articles.Where(article => article.ArticleId == id)
                .Select(article => article.VersionId).Prepend(0).Max();
            var defaultVersions = versions.Select(version => version.VersionId == maxVersion).ToList();
            ViewBag.Versions = versions.ToArray();
            ViewBag.DefaultVersions = defaultVersions.ToArray();

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public ActionResult SetDefault(int id, int version)
        {
            var crtArticle =
                _database.Articles.FirstOrDefault(article => article.ArticleId == id && article.VersionId == version);

            var maxVersion = _database.Articles.Where(art => art.ArticleId == id)
                .Select(art => art.VersionId).Prepend(0).Max();

            var defaultArticle = _database.Articles
                .Where(article => article.ArticleId == id).FirstOrDefault(article => article.VersionId == maxVersion);

            if (crtArticle is null || defaultArticle is null)
                return RedirectToAction("Index", new {id = id});

            var temp = new Article()
            {
                Title = defaultArticle.Title,
                Content = defaultArticle.Content,
                DomainId = defaultArticle.DomainId,
                LastModified = defaultArticle.LastModified
            };

            defaultArticle.Title = crtArticle.Title;
            defaultArticle.Content = crtArticle.Content;
            defaultArticle.DomainId = crtArticle.DomainId;
            defaultArticle.LastModified = crtArticle.LastModified;

            crtArticle.Title = temp.Title;
            crtArticle.Content = temp.Content;
            crtArticle.DomainId = temp.DomainId;
            crtArticle.LastModified = temp.LastModified;

            TryUpdateModel(crtArticle);
            TryUpdateModel(defaultArticle);
            _database.SaveChanges();

            return RedirectToAction("Index", new { id = id });
        }
    }
}