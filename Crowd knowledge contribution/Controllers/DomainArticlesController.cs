using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Crowd_knowledge_contribution.Models;

namespace Crowd_knowledge_contribution.Controllers
{
    public class DomainArticlesController : Controller
    {
        private readonly ApplicationDbContext _database = new ApplicationDbContext();
        // GET: DomainArticles
        public ActionResult Index(string id)
        {
            ViewBag.DomainName = "";
            if (id is null)
                return View();
            id = id.ToLower().Trim();
            var domainObject = _database.Domains.FirstOrDefault(domain => domain.DomainName.ToLower().Trim() == id);
            ViewBag.DomainName = id;
            if (domainObject != null)
            {
                var articles = _database.Articles
                    .Include("Domain")
                    .Include("User")
                    .Where(article => article.DomainId == domainObject.DomainId)
                    .Where(article => article.VersionId == _database.Articles.Where(a => a.ArticleId == article.ArticleId).Select(a => a.VersionId).Max());
                ViewBag.Articles = articles.ToList();
            }

            return View();
        }
    }
}