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

        public static Int32 EditDistance(string a, string b)
        {
            if (String.IsNullOrEmpty(a) && String.IsNullOrEmpty(b))
                return 0;
            if (String.IsNullOrEmpty(a))
                return b.Length;
            if (String.IsNullOrEmpty(b))
                return a.Length;
        
            int la = a.Length;
            int lb = b.Length;
            var dp = new int[la + 1, lb + 1];

            for (int i = 0; i <= la; i++)
                for (int j = 0; j <= lb; j++)
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

        private static int subString(string str, string cautat)
        {
            for (int i = 0; i < str.Length; i++)
            {
                for (int j = i; j < str.Length; j++)
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
            var articles = _database.Articles.Include("Domain").Include("User").OrderBy(article => article.LastModified);
            string search = "";
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

                List<int> articleIds = new List<int>();
                foreach (var article in _database.Articles)
                {
                    if (article.Title.ToLower().Contains(search) || article.Content.ToLower().Contains(search) || subString(article.Title.ToLower(), search) == 1)
                        articleIds.Add(article.ArticleId);
                    System.Diagnostics.Debug.WriteLine(subString(article.Title, search));

                }

                var commentIds = _database.Comments.Where(c => c.Content.Contains(search)).Select(com => com.ArticleId)
                    .ToList();

                var mergedIds = articleIds.Union(commentIds).ToList();
                articles = _database.Articles.Where(article => mergedIds.Contains(article.ArticleId)).Include("Domain").Include("User").OrderBy(article => article.LastModified);
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
            var article = _database.Articles.FirstOrDefault(i => i.ArticleId == id);
            ICollection<Comment> comments = _database.Comments.Where(x => x.ArticleId == article.ArticleId).ToArray();
            article.Comments = comments;
            var commentUsernames = comments.Select(comment => _database.Users.FirstOrDefault(i => i.Id == comment.UserId).Email).ToList();
            ViewBag.CommentUsernames = commentUsernames.ToArray();
            //Article article = _database.Articles.Find(id,1);
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
                    return Redirect("/Articles/Show/" + comm.ArticleId);
                }

                else
                {
                    var a = _database.Articles.FirstOrDefault(i => i.ArticleId == comm.ArticleId);
                    return View(a);
                }

            }

            catch (Exception e)
            {
                var a = _database.Articles.FirstOrDefault(i => i.ArticleId == comm.ArticleId);
                return View(a);
            }
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
            var article = _database.Articles.FirstOrDefault(i => i.ArticleId == id);
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
                    var article = _database.Articles.FirstOrDefault(i => i.ArticleId == id);
                    if (article.UserId == User.Identity.GetUserId() || User.IsInRole("Admin"))
                    {
                        if (TryUpdateModel(article))
                        {
                            article.Content = Sanitizer.GetSafeHtmlFragment(article.Content);
                            article.Title = requestArticle.Title;
                            article.Content = requestArticle.Content;
                            article.DomainId = requestArticle.DomainId;
                            //article.VersionId ++ ;
                            _database.SaveChanges();
                            TempData["message"] = "Operatiune adaugare articol: succes";
                        }
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
            var article = _database.Articles.FirstOrDefault(i => i.ArticleId == id);
            ICollection<Comment> comments = _database.Comments.Where(x => x.ArticleId == article.ArticleId).ToArray();

            if (article.UserId == User.Identity.GetUserId() || User.IsInRole("Admin"))
            {
                foreach (var comment in comments)
                    _database.Comments.Remove(comment);

                _database.Articles.Remove(article);
                _database.SaveChanges();
                TempData["message"] = "Articolul a fost sters";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["message"] = "Nu aveti dreptul sa stergeti un articol care nu va apartine";
                return RedirectToAction("Index");
            }
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
            ViewBag.utilizatorCurent = User.Identity.GetUserId();
        }
    }
}