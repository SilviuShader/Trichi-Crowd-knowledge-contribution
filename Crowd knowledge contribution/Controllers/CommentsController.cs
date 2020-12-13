using Crowd_knowledge_contribution.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;

namespace Crowd_knowledge_contribution.Controllers
{
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        // GET: Comments
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public ActionResult New(Comment comm)
        {
            //comm.VersionId = 1;
            comm.Date = DateTime.Now;
            try
            {
                _db.Comments.Add(comm);
                _db.SaveChanges();

                return Redirect("/Articles/Show/" + comm.ArticleId);
            }
            catch (Exception)
            {
                // ignored
            }

            return Redirect("/Articles/Show/" + comm.ArticleId);

        }

        [HttpDelete]
        [Authorize(Roles = "User,Editor,Admin")]
        public ActionResult Delete(int id)
        {
            var comm = _db.Comments.Find(id);
            if (comm is null)
            {
                TempData["message"] = "Comentariul nu există.";
                return Redirect("/Articles/Index/");
            }

            if (comm.UserId != User.Identity.GetUserId() && !User.IsInRole("Admin"))
                return Redirect("/Articles/Show/" + comm.ArticleId);

            _db.Comments.Remove(comm);
            _db.SaveChanges();
            return Redirect("/Articles/Show/" + comm.ArticleId);
        }

        [Authorize(Roles = "User,Editor,Admin")]
        public ActionResult Edit(int id)
        {
            var comm = _db.Comments.Find(id);

            if (comm is null)
            {
                TempData["message"] = "Comentariul nu există.";
                return Redirect("/Articles/Index/");
            }

            if (comm.UserId != User.Identity.GetUserId())
                return Redirect("/Articles/Show/" + comm.ArticleId);

            ViewBag.Comment = comm;
            return View();
        }

        [HttpPut]
        [Authorize(Roles = "User,Editor,Admin")]
        public ActionResult Edit(int id, Comment requestComment)
        {
            try
            {
                var comm = _db.Comments.Find(id);

                if (comm is null)
                {
                    TempData["message"] = "Comentariul nu există.";
                    return Redirect("/Articles/Index/");
                }

                if (comm.UserId != User.Identity.GetUserId())
                    return Redirect("/Articles/Show/" + comm.ArticleId);

                if (TryUpdateModel(comm))
                {
                    comm.Content = requestComment.Content;
                    _db.SaveChanges();
                }
                return Redirect("/Articles/Show/" + comm.ArticleId);
            }
            catch (Exception)
            {
                return View();
            }
        }
    }
}