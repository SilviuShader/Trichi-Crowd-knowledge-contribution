using System;
using System.Linq;
using System.Web.Mvc;
using Crowd_knowledge_contribution.Models;

namespace Crowd_knowledge_contribution.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DomainsController : Controller
    {
        private readonly ApplicationDbContext _database = new ApplicationDbContext();
        // GET: Categories

        [HttpGet]
        public ActionResult Index()
        {
            ViewBag.Domains = _database.Domains;
            ViewBag.Message = TempData["message"];
            return View();
        }

        [HttpGet]
        public ActionResult Show(int id)
        {
            var domain = _database.Domains.Find(id);

            if (domain is null)
            {
                TempData["message"] = "Nu există domeniul cerut"; 
                return RedirectToAction("Index");
            }

            return View(domain);
        }

        [HttpDelete]
        public ActionResult Delete(int id)
        {
            var domain = _database.Domains.Find(id);
            
            if (domain != null)
            {
                var relatedArticles = from article in _database.Articles
                    where article.DomainId == id
                    select article;

                if (relatedArticles.Any())
                {
                    TempData["message"] = "Domeniul a putut fi șters deoarece încă există articole din acest domeniu";
                }
                else
                {
                    _database.Domains.Remove(domain);
                    TempData["message"] = "Domeniul a fost șters!";
                    _database.SaveChanges();
                }
            }
            else
            {
                TempData["message"] = "Domeniul nu a fost șters deoarece nu a fost găsit!";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            var domain = _database.Domains.Find(id);

            if (domain != null)
                return View(domain);

            TempData["message"] = "Nu există domeniul pe care doriți să îl editați.";
            return RedirectToAction("Index");
        }

        [HttpPut]
        public ActionResult Edit(int id, Domain _)
        {
            try
            {
                var domain = _database.Domains.Find(id);

                if (domain != null)
                {
                    if (TryUpdateModel(domain))
                    {
                        TempData["message"] = "Domeniul a fost modificat!";
                        _database.SaveChanges();
                    }
                }
            }
            catch (Exception)
            {
                TempData["message"] = "Domeniul nu a putut fi modificat.";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult New()
        {
            var domain = new Domain();

            return View(domain);
        }

        
        [HttpPost]
        public ActionResult New(Domain newDomain)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _database.Domains.Add(newDomain);
                    _database.SaveChanges();
                    TempData["message"] = "Domeniul a fost adăugat!";
                    return RedirectToAction("Index");
                }
                    
                return View(newDomain);
            }
            catch (Exception)
            {

                return View(newDomain);
            }
        }
    }
}