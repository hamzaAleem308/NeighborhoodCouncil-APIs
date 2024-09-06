using NcDemo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace NcDemo.Controllers
{
    public class AccountController : ApiController
    {
        NeighborhoodCouncilEntities db = new NeighborhoodCouncilEntities();

        /*[HttpGet]
        public HttpResponseMessage Login(string phoneNo) {
            var check = db.Member.SingleOrDefault(p => p.PhoneNo == phoneNo);

            if (check == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, phoneNo + " Ye bnda ni mila");
            }
            return Request.CreateResponse(HttpStatusCode.OK, "Bnda Milm gya " + phoneNo);
        }*/

        [HttpGet]
        public HttpResponseMessage Login(string phoneNo, string password)
        {
            var member = db.Member.SingleOrDefault(p => p.PhoneNo == phoneNo);

            if (member == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, phoneNo + " is Not Registered!");
            }
            if (member.Password != password)
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized, "Incorrect password.");
            }
            return Request.CreateResponse(HttpStatusCode.OK, member);
        }


        [HttpPost]
        public HttpResponseMessage SignUp(Member member)
        {
            try
            {
                bool memberExists = db.Member.Any(p => p.PhoneNo == member.PhoneNo);

                if (memberExists)
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict, "Member Already Exists!");
                }
                db.Member.Add(member);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Member Added Successfully!");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred while processing your request." + ex);
            }
        }

    }
}