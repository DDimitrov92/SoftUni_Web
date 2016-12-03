using System;
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
using System.Web.UI.WebControls;
using System.Collections;

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
            }
            else if (searchBy != null && searchBy.Equals("Town"))
            {
                return View("Search",
                    db.Listings.Include(p => p.Author)
                        .Where(l => l.Town.Town.ToLower().Contains(search.ToLower()) || search == null)
                        .ToList());
            }
            else
            {
                return View(db.Listings.Include(p => p.Author).ToList());
            }
        }
        public ActionResult SortingByTown(int? id)
        {
            return View(db.Listings.Include(c => c.Author).Where(t => t.TownId == id).ToList());
        }


        public ActionResult SelectedCategorie(int? id)
        {
            var cat = db.Listings.Include(c => c.Author)
                .Where(c => c.CategoryId == id)
                .ToList();
            return View(cat);
        }

        // GET: Listings/Details/5
        public ActionResult Details(int? id)
        {

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Listings listings = db.Listings.Include(l => l.Author).Single(l => l.Id == id);
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
        public ActionResult Create(CreateListingViewModel listings, IEnumerable<HttpPostedFileBase> files)
        {
            if (ModelState.IsValid)
            {
                UserManager<ApplicationUser> UserManager =
                    new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
                ApplicationUser user = UserManager.FindById(this.User.Identity.GetUserId());
                var listing = new Listings { Title = listings.Title, Body = listings.Body, Author = user, Price = listings.Price, ContactNumber = listings.ContactNumber, TownId = listings.TownId, CategoryId = listings.CategoryId };
                var allowedExtensions = new[] { ".jpg", ".png", ".gif" };
                foreach (var file in files)
                {
                    if (file != null)
                    {
                        string extension = file.FileName.Substring(file.FileName.LastIndexOf(".")).ToLower();
                        if (!allowedExtensions.Contains(extension))
                        {
                            TempData["notice"] = "Select .jpg, .png or .gif files format";
                            return View(listing);
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
                        listing.Files.Add(image);
                    }
                }
                db.Listings.Add(listing);
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
            // Check If User is Admin or it's the author of the Listing
            if (User.IsInRole("Admin"))
            {
                return View(listings);
            }
            if (User.Identity.GetUserId() != listings.Author_Id)
            {
                return View("Error");
            }
            return View(listings);
        }

        // POST: Listings/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Title,Body,Date,CategoryId,TownId,Price,ContactNumber")] Listings listings, IEnumerable<HttpPostedFileBase> files)
        {

            string request = Request.Form["check"];

            var tempListing = listings;

            listings = db.Listings.Include(l => l.Files).SingleOrDefault(l => l.Id == listings.Id);
            listings.Title = tempListing.Title;
            listings.Body = tempListing.Body;
            listings.Date = tempListing.Date;
            listings.Comments = tempListing.Comments;
            listings.TownId = tempListing.TownId;
            listings.Price = tempListing.Price;
            listings.CategoryId = tempListing.CategoryId;
            listings.ContactNumber = tempListing.ContactNumber;

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
                return RedirectToAction($"Details/{listings.Id}");
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
            // Check If User is Admin or it's the author of the Listing
            if (User.IsInRole("Admin"))
            {
                return View(listings);
            }
            if (User.Identity.GetUserId() != listings.Author_Id)
            {
                return View("Error");
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