using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Crowd_knowledge_contribution.Models;
using Microsoft.AspNet.Identity;
using Microsoft.Security.Application;

namespace Crowd_knowledge_contribution.Controllers
{
    public class ArticlesController : Controller
    {
        private const int ARTICLES_PER_PAGE = 3;
        private readonly ApplicationDbContext _database = new ApplicationDbContext();

        public static int EditDistance(string a, string b)
        {
            if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b))
                return 0;
            if (string.IsNullOrEmpty(a))
                return b.Length;
            if (string.IsNullOrEmpty(b))
                return a.Length;
        
            var la = a.Length;
            var lb = b.Length;
            var dp = new int[la + 1, lb + 1];

            for (var i = 0; i <= la; i++)
                for (var j = 0; j <= lb; j++)
                {
                    if (i == 0)
                        dp[i, j] = j;
                    else if (j == 0)
                        dp[i, j] = i;
                    else if (a[i - 1] == b[j - 1])
                        dp[i, j] = dp[i - 1, j - 1];
                    else
                        dp[i, j] = 1 + Math.Min(dp[i, j - 1], Math.Min(dp[i - 1, j], dp[i - 1, j - 1]));
                }
            return dp[la, lb];
        }

        private static int SubString(string str, string cautat)
        {
            for (var i = 0; i < str.Length; i++)
            {
                for (var j = i; j < str.Length; j++)
                {
                    var substring = str.Substring(i, j - i + 1);
                    //System.Diagnostics.Debug.WriteLine(substring);
                    if (EditDistance(substring, cautat) < 2)
                        return 1;
                }
            }
            return 0;
        }

        // GET: Articles, cea default
        //[HttpGet]
        public ActionResult Index()
        {
            //pagina cu toate
            var articles = _database.Articles
                .Include("Domain")
                .Include("User")
                .Where(article => article.VersionId == _database.Articles.Where(a => a.ArticleId == article.ArticleId).Select(a => a.VersionId).Max())
                .OrderBy(article => article.LastModified);
            var search = "";
            var carryArguments = "";
            var order = "0";

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
            }

            if (Request.Params.Get("search") != null)
            {
                search = Request.Params.Get("search").Trim();
                if (search != "")
                    carryArguments += "search=" + search;
                /*var articleIds = _database.Articles.Where(
                    at => at.Title.Contains(search)
                          || at.Content.Contains(search) ).Select(a => a.ArticleId).ToList();*/

                var articleIds = new List<int>();
                foreach (var article in _database.Articles)
                {
                    if (article.Title.ToLower().Contains(search) || article.Content.ToLower().Contains(search) || SubString(article.Title.ToLower(), search) == 1)
                        articleIds.Add(article.ArticleId);
                    System.Diagnostics.Debug.WriteLine(SubString(article.Title, search));

                }

                var commentIds = _database.Comments.Where(c => c.Content.Contains(search)).Select(com => com.ArticleId)
                    .ToList();

                var mergedIds = articleIds.Union(commentIds).ToList();
                articles = _database.Articles
                    .Where(article => mergedIds.Contains(article.ArticleId))
                    .Include("Domain")
                    .Include("User")
                    .Where(article => article.VersionId == _database.Articles.Where(a => a.ArticleId == article.ArticleId).Select(a => a.VersionId).Max())
                    .OrderBy(article => article.LastModified);
            }

            var validOrder = false;
            if (Request.Params.Get("order") != null)
            {
                order = Request.Params.Get("order").Trim();
                switch (order)
                {
                    case "0":
                    case "1":
                    case "2":
                    case "3":
                        validOrder = true;
                        break;
                }

                if (validOrder)
                {
                    if (carryArguments == "")
                        carryArguments += "order=" + order;
                    else
                        carryArguments += "&order=" + order;
                }
                else
                {
                    order = "0";
                }
            }
            else
            {
                order = "0";
            }

            switch (order)
            {
                case "0":
                    articles = articles.OrderByDescending(article => article.LastModified);
                    break;
                case "1":
                    articles = articles.OrderBy(article => article.LastModified);
                    break;
                case "2":
                    articles = articles.OrderBy(article => article.Title);
                    break;
                case "3":
                    articles = articles.OrderByDescending(article => article.Title);
                    break;
            }

            var totalItems = articles.Count();
            var currentPage = 0;
            try
            {
                currentPage = Convert.ToInt32(Request.Params.Get("page"));
            }
            catch (Exception)
            {
                // ignored
            }

            var offset = 0;
            if (!currentPage.Equals(0))
                offset = (currentPage - 1) * ARTICLES_PER_PAGE;

            var paginatedArticles = articles.Skip(offset).Take(ARTICLES_PER_PAGE);

            ViewBag.CarryArguments = carryArguments;
            ViewBag.Order = order;
            ViewBag.Articles = paginatedArticles;
            ViewBag.LastPage = Math.Ceiling((float) totalItems / ARTICLES_PER_PAGE);
            ViewBag.SearchString = search;
            return View();
        }

        public ActionResult Show(int id)
        {
            var version = 0;
            if (Request.Params.Get("version") != null && (User.IsInRole("Admin") || User.IsInRole("Editor")))
            {
                var versionString = Request.Params.Get("version").Trim();
                int.TryParse(versionString, out version);
            }

            var article = _database.Articles.FirstOrDefault(art => art.ArticleId == id && art.VersionId == version);
            if (article is null)
                article = _database.Articles.Where(art => art.VersionId == _database.Articles.Where(a => a.ArticleId == art.ArticleId).Select(a => a.VersionId).Max()).FirstOrDefault(i => i.ArticleId == id);

            if (article is null)
            {
                TempData["message"] = "Nu există acest articol";
                return RedirectToAction("Index");
            }

            ICollection<Comment> comments = _database.Comments.Where(x => x.ArticleId == article.ArticleId).ToArray();
            article.Comments = comments;
            var commentUsernames = comments.Select(comment => _database.Users.FirstOrDefault(i => i.Id == comment.UserId).Email).ToList();
            ViewBag.CommentUsernames = commentUsernames.ToArray();

            SetAccessRights();
            return View(article);
        }

        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public ActionResult Show(Comment comm)
        {
            //ca sa ia ora reala a postarii comentariului
            comm.Date = DateTime.Now;
            try
            {
                if (ModelState.IsValid)
                {
                    _database.Comments.Add(comm);
                    _database.SaveChanges();
                }

            }
            catch (Exception)
            {
                // ignored
            }

            return Redirect("/Articles/Show/" + comm.ArticleId);
        }

        [HttpGet]
        [Authorize(Roles = "Editor,Admin")]
        public ActionResult New()
        {
            var article = new Article {Dom = GetAllDomains(), UserId = User.Identity.GetUserId()};

            return View(article);
        }


        [HttpPost]
        [ValidateInput(false)]
        [Authorize(Roles = "Editor,Admin")]
        public ActionResult New(Article article)
        {
            var maxId = _database.Articles.Select(art => art.ArticleId).Prepend(0).Max();
            article.ArticleId = maxId + 1;
            article.UserId = User.Identity.GetUserId();
            article.Dom = GetAllDomains();
            article.LastModified = DateTime.Now;
            article.VersionId = 1;
            try
            {
                if (ModelState.IsValid)
                {
                    article.Content = Sanitizer.GetSafeHtmlFragment(article.Content);

                    _database.Articles.Add(article);
                    _database.SaveChanges();
                    TempData["message"] = "Articolul a fost adăugat cu succes.";
                    return RedirectToAction("Index");
                }
                else
                {
                    return View(article);
                }
            }
            catch (Exception )
            {
                return View(article);
            }   
        }

        [Authorize(Roles = "Editor,Admin")]
        public ActionResult Edit(int id)
        {
            var article = _database.Articles.OrderByDescending(i => i.VersionId).FirstOrDefault(i => i.ArticleId == id);
            article.Dom = GetAllDomains();
            if (article.UserId == User.Identity.GetUserId() || User.IsInRole("Admin"))
            {
                return View(article);
            }

            else
            {
                TempData["message"] = "Nu aveti dreptul sa faceti modificari asupra unui articol care nu va apartine";
                return RedirectToAction("Index");
            }
            return View(article);
        }

        [HttpPut]
        [ValidateInput(false)]
        [Authorize(Roles = "Editor,Admin")]
        public ActionResult Edit(int id, Article requestArticle)
        {
            requestArticle.Dom = GetAllDomains();
            try
            {
                if (ModelState.IsValid)
                {
                    requestArticle.VersionId = _database.Articles
                        .Where(article => article.ArticleId == requestArticle.ArticleId)
                        .Select(article => article.VersionId).Prepend(0).Max() + 1;

                    requestArticle.UserId = _database.Articles
                        .FirstOrDefault(article => article.ArticleId == requestArticle.ArticleId)
                        ?.UserId;

                    if (requestArticle.VersionId == 1)
                    {
                        TempData["message"] = "Nu puteți edita un articol care nu există.";
                        return RedirectToAction("Index");
                    }

                    if (requestArticle.Protected && !User.IsInRole("Admin"))
                    {
                        TempData["message"] = "Articolul a fost protejat, prin urmare nu aveți dreptul la editare";
                        return RedirectToAction("Index");
                    }

                    if (requestArticle.UserId == User.Identity.GetUserId() || User.IsInRole("Admin"))
                    {
                        requestArticle.Content = Sanitizer.GetSafeHtmlFragment(requestArticle.Content);
                        requestArticle.LastModified = DateTime.Now;

                        _database.Articles.Add(requestArticle);
                        _database.SaveChanges();
                        TempData["message"] = "Operatiune adaugare articol: succes";

                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["message"] = "Nu aveti dreptul sa faceti modificari asupra unui articol care nu va apartine";
                        return RedirectToAction("Index");
                    }

                }
                else
                {
                    requestArticle.Dom = GetAllDomains();
                    return View(requestArticle);
                }

            }
            catch (Exception )
            {
                requestArticle.Dom = GetAllDomains();
                return View(requestArticle);
            }
        }

        [HttpDelete]
        [Authorize(Roles = "Editor,Admin")]
        public ActionResult Delete(int id)
        {
            var articles = _database.Articles.Where(i => i.ArticleId == id).ToList();

            if (articles.Count == 0)
            {
                TempData["message"] = "Nu există articolul cerut.";
                return RedirectToAction("Index");
            }

            var firstArticle = articles[0];
            ICollection<Comment> comments = _database.Comments.Where(x => x.ArticleId == firstArticle.ArticleId).ToArray();

            if (firstArticle.UserId == User.Identity.GetUserId() || User.IsInRole("Admin"))
            {
                foreach (var comment in comments)
                    _database.Comments.Remove(comment);

                foreach (var article in articles)
                    _database.Articles.Remove(article);
                _database.SaveChanges();
                TempData["message"] = "Articolul a fost sters";
                return RedirectToAction("Index");
            }

            TempData["message"] = "Nu aveti dreptul sa stergeti un articol care nu va apartine";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "Editor,Admin")]
        public ActionResult Protect(int id, int version, bool setProtected)
        {
            var articles = _database.Articles.Where(article => article.ArticleId == id).ToArray();
            if (articles.Length <= 0)
                return Redirect("/Articles/Show/" + id + "?version=" + version);

            if (articles[0].UserId == User.Identity.GetUserId() && !User.IsInRole("Admin"))
                return Redirect("/Articles/Show/" + id + "?version=" + version);

            foreach (var article in articles)
                article.Protected = setProtected;

            _database.SaveChanges();

            return Redirect("/Articles/Show/" + id + "?version=" + version);
        }

        [NonAction]
        private IEnumerable<SelectListItem> GetAllDomains()
        {
            var selectList = new List<SelectListItem>();

            var domains = from domain in _database.Domains
                select domain;

            foreach (var domain in domains)
            {
                selectList.Add(new SelectListItem
                {
                    Value = domain.DomainId.ToString(),
                    Text = domain.DomainName
                });
            }
            
            return selectList;
        }

        private void SetAccessRights()
        {
            ViewBag.afisareButoane = false;
            if (User.IsInRole("Editor") || User.IsInRole("Admin"))
            {
                ViewBag.afisareButoane = true;
            }

            ViewBag.esteAdmin = User.IsInRole("Admin");
            ViewBag.IsEditor = User.IsInRole("Editor");
            ViewBag.utilizatorCurent = User.Identity.GetUserId();
        }
    }
}