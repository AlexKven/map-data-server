using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace MapDataServer.Controllers
{
    public abstract class BaseController : ControllerBase
    {
        protected IConfiguration Configuration { get; }

        public BaseController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        protected bool IsAuthorized()
        {
            var headers = HttpContext.Request.Headers;
            if (!headers.ContainsKey("Authorization"))
                return false;
            var token = headers["Authorization"].ToString();
            if (!token.StartsWith("Bearer "))
                return false;
            token = token.Substring(7);
            if (token != Configuration["apikey"])
                return false;
            return true;
        }
    }
}
