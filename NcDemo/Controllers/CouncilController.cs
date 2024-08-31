using NcDemo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace NcDemo.Controllers
{
    public class CouncilController : ApiController
    {
        NeighborhoodCouncilEntities db = new NeighborhoodCouncilEntities();

        [HttpGet]
        public HttpResponseMessage GetCouncils() {
            var chk = db.Council.ToList();
            if (chk == null) { 
                return Request.CreateResponse(HttpStatusCode.NoContent);
            }
            return Request.CreateResponse(HttpStatusCode.OK, chk);
        }
    }
}