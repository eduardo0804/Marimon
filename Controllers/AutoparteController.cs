using Marimon.Data;
using Marimon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

public class AutoparteController : Controller
{
    private readonly ApplicationDbContext _context;

    public AutoparteController(ApplicationDbContext context)
    {
        _context = context;
    }

 
}