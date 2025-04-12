using Microsoft.AspNetCore.Mvc;

public class CarritoController : Controller
{
    private readonly ILogger<CarritoController> _logger;

    public CarritoController(ILogger<CarritoController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    
}