
using api.cernahomecare.com.Data;
using Microsoft.AspNetCore.Mvc;

namespace api.safepatch.com.Controllers;

[Route("[controller]")]
[ApiController]

// (NO AUTH)
// 
// - DESIGNED FOR: Pulse check the API after fres deployment
// 
public class ValuesController : ControllerBase
{
    #region Properties
    private readonly CernaHomeCareDbContext _context;
    #endregion

    #region Constructor
    public ValuesController(CernaHomeCareDbContext context)
    {
        _context = context;
    }
    #endregion

    #region Test Get (Pulse Check)
    [HttpGet(Name = "GetList")]
    public IDictionary<int, string> Get()
    {
        IDictionary<int, string> list = new Dictionary<int, string>();
        list.Add(1, "API");
        list.Add(2, "API.Cerna");
        list.Add(3, "isLIVE");
        return list;
    }
    #endregion
}
