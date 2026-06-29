using ICONDENIM.Web.Data;
using ICONDENIM.Web.Filters;
using ICONDENIM.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ICONDENIM.Web.Controllers;
[AdminAuthorize]
public class AdminPromotionsController : Controller
{
    private readonly AppDbContext _db;
    public AdminPromotionsController(AppDbContext db) { _db = db; }
    public async Task<IActionResult> Index() => View(await _db.KhuyenMais.OrderByDescending(x => x.ngayBatDau).ToListAsync());
    public IActionResult Create() => View(new KhuyenMai { ngayBatDau = DateTime.Today, ngayKetThuc = DateTime.Today.AddMonths(1) });
    [HttpPost] public async Task<IActionResult> Create(KhuyenMai km) { if (!ModelState.IsValid) return View(km); _db.KhuyenMais.Add(km); await _db.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
}
