using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Crowd_knowledge_contribution.Models;
using Microsoft.AspNet.Identity;

namespace Crowd_knowledge_contribution.Controllers
{
    [Authorize]
    public class ArticlesController : Controller
    {
        private readonly ApplicationDbContext _database = new ApplicationDbContext();

        // GET: Articles, cea default
        //[HttpGet]
        [Authorize(Roles = "User,Editor,Admin")]
        public ActionResult Index(string searchName)
        {
            //pagina cu toate
            var articles = _database.Articles.Include("Domain").Include("User");
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
            }
            if (!string.IsNullOrEmpty(searchName))
            {
                var articles1 = articles.Where(c => c.Title.Contains(searchName));
                ViewBag.Articles = articles1;
                return View();
            }
            ViewBag.Articles = articles;
            return View();
        }

        /*public ActionResult Index(string searchName)
        {
            var articles = _database.Articles.Include("Domain").Include("User");
            if (!string.IsNullOrEmpty(searchName))
            {
                //articles = articles.Where(c => c.Title.Contains(searchName));
            }
            return View(articles);
        }*/

        [Authorize(Roles = "User,Editor,Admin")]
        public ActionResult Show(int id)
        {
            Article article = _database.Articles.Where(i => i.ArticleId == id).FirstOrDefault();
            ICollection<Comment> comments = _database.Comments.Where(x => x.ArticleId == article.ArticleId).ToArray();
            article.Comments = comments;
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
                    Article a = _database.Articles.Where(i => i.ArticleId == comm.ArticleId).FirstOrDefault();
                    return View(a);
                }

            }

            catch (Exception e)
            {
                Article a = _database.Articles.Where(i => i.ArticleId == comm.ArticleId).FirstOrDefault();
                return View(a);
            }

        }

        //[HttpGet]
        [Authorize(Roles = "Editor,Admin")]
        public ActionResult New()
        {
            var article = new Article {Dom = GetAllDomains(), UserId = User.Identity.GetUserId()};

            return View(article);
        }


        [HttpPost]
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
                    _database.Articles.Add(article);
                    _database.SaveChanges();
                    TempData["message"] = "Operatiune adaugare articol: succes";
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
            Article article = _database.Articles.Where(i => i.ArticleId == id).FirstOrDefault();
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
        [Authorize(Roles = "Editor,Admin")]
        public ActionResult Edit(int id, Article requestArticle)
        {
            requestArticle.Dom = GetAllDomains();

            try
            {
                if (ModelState.IsValid)
                {
                    Article article = _database.Articles.Where(i => i.ArticleId == id).FirstOrDefault();
                    if (article.UserId == User.Identity.GetUserId() || User.IsInRole("Admin"))
                    {
                        if (TryUpdateModel(article))
                        {
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
            var article = _database.Articles.Where(i => i.ArticleId == id).FirstOrDefault();
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