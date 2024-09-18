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

        public HttpResponseMessage GetCouncilJoinCode(int councilId)
        {
            try
            {
                var council = db.Council.Where(p => p.id == councilId);

                if (!council.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "Invalid Code");
                }

                var getCode = council.Select(gc => gc.JoinCode).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, getCode);
            }
            catch (Exception ee)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ee);
            }
        }


        [HttpGet]
        public HttpResponseMessage GetCouncil(int councilId)
        {
            try
            {
                var council = db.Council.Where(p => p.id == councilId);

                if (!council.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Council Found");
                }
                return Request.CreateResponse(HttpStatusCode.OK, council);
            }
            catch (Exception ee)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ee);
            }
        }

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

                // Extract council ids 
                var councilIds = councilMemberships.Select(cm => cm.Council_id).ToList();

                // Fetch councils that match the council ids
                var councils = db.Council.Where(c => councilIds.Contains(c.id)).ToList();

                if (!councils.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No matching councils found.");
                }

                return Request.CreateResponse(HttpStatusCode.OK, councils);
            }
            catch (Exception ee) {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An errord Occured" + ee);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetUserType(int memberId, int councilId)
        {
            // Get the member's role for the given council
            var memberRole = (from cm in db.CouncilMembers
                              join r in db.Role on cm.Role_Id equals r.id
                              where cm.Member_Id == memberId && cm.Council_id == councilId
                              select r.Role_Name).FirstOrDefault();

            if (memberRole == null)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, "No role found for the user in this council.");
            }

            return Request.CreateResponse(HttpStatusCode.OK, memberRole);
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
                    Role_Id = 1    // ID: 1 == 'Admin'       
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

        [HttpPost]
        public HttpResponseMessage JoinCouncilUsingCode(int memberId, string joinCode)
        {
            try
            {
                var getJoinCode = db.Council.Where(p => p.JoinCode.Equals(joinCode));

                if (!getJoinCode.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Council Found with this Join Code");
                }

                var getCouncil = getJoinCode.Select(gc => gc.id).FirstOrDefault();

                var existingMember = db.CouncilMembers
                    .Where(cm => cm.Council_id == getCouncil && cm.Member_Id == memberId)
                    .FirstOrDefault();

                if (existingMember != null)
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict, "You are already a part of this council");
                }

                CouncilMembers councilMember = new CouncilMembers
                {
                    Council_id = getCouncil,
                    Member_Id = memberId,
                    Role_Id = 2    // ID: 2 == 'Member'       
                };

                db.CouncilMembers.Add(councilMember);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Council Joined Successfully");
            }
            catch (Exception ee)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ee);
            }
        }
    }
    }