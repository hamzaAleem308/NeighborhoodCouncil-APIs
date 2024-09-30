using NcDemo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using static System.Collections.Specialized.BitVector32;

namespace NcDemo.Controllers
{
    public class ElectionController : ApiController
    {
        NeighborhoodCouncilEntities db = new NeighborhoodCouncilEntities();

        [HttpPost]
        public HttpResponseMessage PostElection(Elections election)
        {
            try
            {
                db.Elections.Add(election);
                db.SaveChanges();
                return  Request.CreateResponse(HttpStatusCode.OK);
            }
            catch(Exception ee)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ee);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetElection(int councilId)
        {
            try {
                var election = db.Elections.Where(c => c.Council_id == councilId);
                if (election == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Election found for this Council!");
                }
                return Request.CreateResponse(HttpStatusCode.OK, election);
            }
            catch(Exception ee)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ee);
            }
        }

        [HttpPost]
        public HttpResponseMessage InitiateElection(Elections elect)
        {
            try
            {
                var election = new Elections
                {
                    Name = elect.Name,
                    StartDate = elect.StartDate,
                    EndDate = elect.EndDate,
                    status = "Ongoing",
                    Council_id = elect.Council_id,
                };
                db.Elections.Add(election);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Election initiated successfully.");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

    }
}