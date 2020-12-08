using Crowd_knowledge_contribution.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Crowd_knowledge_contribution.Controllers
{
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Comments
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult New(Comment comm)
        {
            //comm.VersionId = 1;
            comm.Date = DateTime.Now;
            try
            {
                db.Comments.Add(comm);
                db.SaveChanges();
                return Redirect("/Articles/Show/" + comm.ArticleId);
            }

            catch (Exception e)
            {
                return Redirect("/Articles/Show/" + comm.ArticleId);
            }

        }

        [HttpDelete]
        public ActionResult Delete(int id)
        {
            Comment comm = db.Comments.Find(id);
            db.Comments.Remove(comm);
            db.SaveChanges();
            return Redirect("/Articles/Show/" + comm.ArticleId);
        }

        public ActionResult Edit(int id)
        {
            Comment comm = db.Comments.Find(id);
            ViewBag.Comment = comm;
            return View();
        }

        [HttpPut]
        public ActionResult Edit(int id, Comment requestComment)
        {
            try
            {
                Comment comm = db.Comments.Find(id);
                if (TryUpdateModel(comm))
                {
                    comm.Content = requestComment.Content;
                    db.SaveChanges();
                }
                return Redirect("/Articles/Show/" + comm.ArticleId);
            }
            catch (Exception e)
            {
                return View();
            }

        }
    }
}