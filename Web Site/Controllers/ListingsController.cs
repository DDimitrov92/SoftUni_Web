﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Web_Site.Models;

namespace Web_Site.Controllers
{
    public class ListingsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Listings
        public ActionResult Index(string searchBy, string search)
        {
            if (searchBy != null && searchBy.Equals("Title"))
            {
                return View("Search", 
                    db.Listings.Include(p => p.Author)
                    .Where(l => l.Title.ToLower().Contains(search.ToLower()) || search == null)
                    .ToList());
            }
            else if (searchBy != null && searchBy.Equals("Author"))
            {
                return View("Search",
                    db.Listings.Include(p => p.Author)
                        .Where(l => l.Author.UserName.ToLower().Contains(search.ToLower()) || search == null)
                        .ToList());
               // return View(db.Listings.Include(p => p.Author).Where(l => l.Author.UserName.ToLower().Contains(search.ToLower()) || search == null).ToList());
            }
            else
            {
                return View(db.Listings.Include(p => p.Author).ToList());
            }
        }

        // GET: Listings/Details/5
        public ActionResult Details(int? id)
        {
            
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Listings listings = db.Listings.Find(id);
            listings = db.Listings.Include(l => l.Files).SingleOrDefault(l => l.Id == id);
            if (listings == null)
            {
                return HttpNotFound();
            }
            return View(listings);
        }

        // GET: Listings/Create
        [Authorize]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Listings/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Title,Body")] Listings listings, IEnumerable<HttpPostedFileBase> files)
        {
            if (ModelState.IsValid)
            {
                UserManager<ApplicationUser> UserManager =
                    new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
                ApplicationUser user = UserManager.FindById(this.User.Identity.GetUserId());
                listings.Author = user;

                var allowedExtensions = new[] { ".jpg", ".png", ".gif" };
                foreach (var file in files)
                {
                    if (file != null)
                    {
                        string extension = file.FileName.Substring(file.FileName.LastIndexOf(".")).ToLower();
                        if (!allowedExtensions.Contains(extension))
                        {
                            TempData["notice"] = "Select .jpg, .png or .gif files format";
                            return View(listings);
                        }
                        else if (file.ContentLength > 1024 * 1024)
                        {
                            TempData["notice"] = "Select files less than 2MB";
                            return View(listings);
                        }
                    }
                    if (file != null && file.ContentLength > 0)
                    {
                        var image = new File
                        {
                            FileName = System.IO.Path.GetFileName(file.FileName),
                            ContentType = file.ContentType
                        };
                        using (var reader = new System.IO.BinaryReader(file.InputStream))
                        {
                            image.Content = reader.ReadBytes(file.ContentLength);
                        }
                        listings.Files.Add(image);
                    }
                }

                db.Listings.Add(listings);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(listings);
        }

        // GET: Listings/Edit/5
        [Authorize]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Listings listings = db.Listings.Find(id);
            listings = db.Listings.Include(l => l.Files).SingleOrDefault(l => l.Id == id);
            if (listings == null)
            {
                return HttpNotFound();
            }
            return View(listings);
        }

        // POST: Listings/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Title,Body,Date")] Listings listings, IEnumerable<HttpPostedFileBase> files)
        {
            if (User.IsInRole("Admin") || User.Identity.GetUserId() == listings.Author_Id)
            {


                string request = Request.Form["check"];

                var tempListing = listings;
                listings = db.Listings.Include(l => l.Files).SingleOrDefault(l => l.Id == listings.Id);
                listings.Title = tempListing.Title;
                listings.Body = tempListing.Body;
                listings.Date = tempListing.Date;
                listings.Comments = tempListing.Comments;
                if (ModelState.IsValid)
                {
                    var allowedExtensions = new[] { ".jpg", ".png", ".gif" };
                    foreach (var file in files)
                    {
                        if (file != null)
                        {
                            string extension = file.FileName.Substring(file.FileName.LastIndexOf(".")).ToLower();
                            if (!allowedExtensions.Contains(extension))
                            {
                                TempData["notice"] = "Select .jpg, .png or .gif files format";
                                return View(listings);
                            }
                            else if (file.ContentLength > 1024 * 1024)
                            {
                                TempData["notice"] = "Select files less than 2MB";
                                return View(listings);
                            }
                        }
                        if (file != null && file.ContentLength > 0)
                        {
                            var image = new File
                            {
                                FileName = System.IO.Path.GetFileName(file.FileName),
                                ContentType = file.ContentType
                            };
                            using (var reader = new System.IO.BinaryReader(file.InputStream))
                            {
                                image.Content = reader.ReadBytes(file.ContentLength);
                            }
                            listings.Files.Add(image);
                        }
                    }

                    if (request != null)
                    {
                        int[] actionArgs = request.Split(',').Select(int.Parse).ToArray(); ;
                        foreach (int fileIdToRemove in actionArgs)
                        {
                            db.Files.Remove(listings.Files.First(f => f.FileId == fileIdToRemove));
                        }
                    }

                    db.Entry(listings).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            return View(listings);
        }

        // GET: Listings/Delete/5
        [Authorize]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Listings listings = db.Listings.Find(id);
            if (listings == null)
            {
                return HttpNotFound();
            }
            return View(listings);
        }

        // POST: Listings/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Listings listings = db.Listings.Find(id);
            db.Listings.Remove(listings);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
