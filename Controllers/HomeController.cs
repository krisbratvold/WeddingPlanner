using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WeddingPlanner.Models;

namespace WeddingPlanner.Controllers
{
    public class HomeController : Controller
    {
        private WeddingPlannerContext db;
        public HomeController(WeddingPlannerContext context)
        {
            db = context;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Success");
            }
            return View("Index");
        }
        public IActionResult Success()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Index");
            }
            List<Wedding> AllWeddings = db.Weddings
            .Include(w => w.PeopleComing)
            .ThenInclude(l => l.User)
            .ToList();
            return View("Success", AllWeddings);
        }

        [HttpPost("/register")]
        public IActionResult Register(User newUser)
        {
            if (ModelState.IsValid)
            {
                if (db.Users.Any(u => u.Email == newUser.Email))
                {
                    ModelState.AddModelError("Email", "is taken");
                }
            }
            if (ModelState.IsValid == false)
            {
                return View("Index");
            }
            PasswordHasher<User> hasher = new PasswordHasher<User>();
            newUser.Password = hasher.HashPassword(newUser, newUser.Password);

            db.Users.Add(newUser);
            db.SaveChanges();

            HttpContext.Session.SetInt32("UserId", newUser.UserId);
            HttpContext.Session.SetString("Name", newUser.FirstName);
            return RedirectToAction("Success");
        }

        [HttpPost("/login")]
        public IActionResult Login(LoginUser newLogin)
        {
            if (ModelState.IsValid == false)
            {
                return View("Index");

            }
            User dbUser = db.Users.FirstOrDefault(user => user.Email == newLogin.LoginEmail);

            if (dbUser == null)
            {
                ModelState.AddModelError("LoginEmail", "incorrect credntials");
                return View("Index");
            }
            PasswordHasher<LoginUser> hasher = new PasswordHasher<LoginUser>();
            PasswordVerificationResult pwCompareResult = hasher.VerifyHashedPassword(newLogin, dbUser.Password, newLogin.LoginPassword);

            if (pwCompareResult == 0)
            {
                ModelState.AddModelError("LoginEmail", "incorrect credntials");
                return View("Index");
            }
            HttpContext.Session.SetInt32("UserId", dbUser.UserId);
            HttpContext.Session.SetString("Name", dbUser.FirstName);
            return RedirectToAction("Success");
        }

        [HttpPost("/logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        [HttpGet("/add/wedding")]
        public IActionResult AddWedding()
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Index");
            }
            return View("AddWedding");
        }
        [HttpPost("/add/wedding/create")]
        public IActionResult CreateWedding(Wedding newWedding)
        {
            if (ModelState.IsValid)
            {
                newWedding.UserId = (int)HttpContext.Session.GetInt32("UserId");
                db.Weddings.Add(newWedding);
                db.SaveChanges();
                return RedirectToAction("Success");
            }
            return View("AddWedding");
        }

        [HttpGet("/view/wedding/{weddingId}")]
        public IActionResult ViewWedding(int weddingId)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Index");
            }
            Wedding wedding = db.Weddings
            .FirstOrDefault(w => w.WeddingId == weddingId);

            var Guests = db.Weddings
            .Include(w=>w.PeopleComing)
            .ThenInclude(u=>u.User)
            .FirstOrDefault(w => w.WeddingId==weddingId);

            ViewBag.AllGuests = Guests.PeopleComing;
            return View("ViewWedding", wedding);
        }

        [HttpPost("/add/RSVP")]
        public IActionResult RSVP(int weddingId)
        {
            RSVP found = db.RSVPs
            .FirstOrDefault(r => r.WeddingId == weddingId && r.UserId == (int)HttpContext.Session.GetInt32("UserId"));

            if (found == null)
            {
                RSVP newRSVP = new RSVP()
                {
                    WeddingId = weddingId,
                    UserId = (int)HttpContext.Session.GetInt32("UserId")
                };
                db.RSVPs.Add(newRSVP);
            }
            else
            {
                db.RSVPs.Remove(found);
            }
            db.SaveChanges();
            return RedirectToAction("Success");
        }

        [HttpPost("/delete")]
        public IActionResult Delete(int weddingId)
        {
            Wedding wedding = db.Weddings.FirstOrDefault(w=>w.WeddingId == weddingId);
            db.Weddings.Remove(wedding);
            db.SaveChanges();
            return RedirectToAction("Success");
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
