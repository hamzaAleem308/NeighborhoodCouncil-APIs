using NcDemo.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
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
        public HttpResponseMessage GetCouncils(int memberId)
        {
            try
            {  // Fetch all council memberships for the given memberId
                var councilMemberships = db.CouncilMembers.Where(cm => cm.Member_Id == memberId).ToList();

                if (!councilMemberships.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No councils found for this member.");
                }

                // Extract council ids from the member's council memberships
                var councilIds = councilMemberships.Select(cm => cm.Council_id).ToList();

                // Fetch councils that match the council ids
                var councils = db.Council.Where(c => councilIds.Contains(c.id)).ToList();

                if (!councils.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No matching councils found.");
                }

                return Request.CreateResponse(HttpStatusCode.OK, councils);
            }
            catch(Exception ee) {
                return Request.CreateResponse(HttpStatusCode.InternalServerError ,"An errord Occured"+ ee);
            }
            }



        [HttpPost]
        public HttpResponseMessage PostCouncils(Council council, int memberId)
        {
            try
            { 
                db.Council.Add(council);
                db.SaveChanges(); 

               
                CouncilMembers councilMember = new CouncilMembers
                {
                    Council_id = council.id,  
                    Member_Id = memberId,     
                    Role_Id = 1             
                };

              
                db.CouncilMembers.Add(councilMember);
                db.SaveChanges(); 

                return Request.CreateResponse(HttpStatusCode.OK, council.Name + " Added Successfully!");
            }
            catch (Exception ex)
            {
               
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "An error occurred: " + ex.Message);
            }
        }
    }
    }