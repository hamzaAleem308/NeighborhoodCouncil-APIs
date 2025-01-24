using NcDemo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace NcDemo.Controllers
{
    public class MonthlyContributionsController : ApiController
    {
        NeighborhoodCouncilEntities db = new NeighborhoodCouncilEntities();
        
        [HttpPost]
        public HttpResponseMessage NotifyResidents(int councilId)
        {
            try
            {
                var residents = db.CouncilMembers.Where(r => r.Council_Id == councilId).ToList();
                foreach (var resident in residents)
                {
                    var contribution = new Monthly_Contributions
                    {
                        council_id = councilId,
                        member_id = resident.Member_Id,
                        month_year = DateTime.Now.ToString("MM-yyyy"),
                        amount = 1000,
                        status = "Unpaid",
                        payment_date = DateTime.Now,
                        created_at = DateTime.Now,
                        updated_at = DateTime.Now
                    };
                    db.Monthly_Contributions.Add(contribution);

                    // Optionally add notification for each resident
                    var notification = new Notifications
                    {
                        member_id = resident.Member_Id,
                        council_id = councilId,
                        title = "Monthly Contribution Reminder",
                        message = "Please pay your monthly contribution of Rs. 1000.",
                        module = "Contributions",
                        created_at = DateTime.Now,
                        updated_at = DateTime.Now
                    };
                    db.Notifications.Add(notification);
                }
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "Residents notified successfully!");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpGet]
        //[Route("MonthlyContributions/GetContributions")]
        public HttpResponseMessage GetContributions(int councilId)
        {
            try
            {
                var contributions = db.Monthly_Contributions
                    .Where(mc => mc.council_id == councilId)
                    .Select(mc => new
                    {
                        ResidentId = mc.member_id,
                        ResidentName = db.Member.FirstOrDefault(c => c.id == mc.member_id).Full_Name,
                        Status = mc.status,
                        HouseNumber = db.Member.FirstOrDefault(o => o.id == mc.member_id).Address,
                        PaymentDate = mc.payment_date
                    }).ToList();

                var totalPaid = contributions.Where(c => c.Status == "Paid").Sum(c => 1000); // Example
                var totalPending = contributions.Where(c => c.Status == "Unpaid").Sum(c => 1000);

                return Request.CreateResponse(HttpStatusCode.OK, new { residents = contributions, totalPaid, totalPending });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        //[Route("MonthlyContributions/MarkAsPaid")]
        public HttpResponseMessage MarkAsPaid(int residentId, int councilId)
        {
            try
            {
                var contribution = db.Monthly_Contributions.FirstOrDefault(mc => mc.council_id == councilId && mc.member_id == residentId && mc.month_year == DateTime.Now.ToString("MM-yyyy"));
                if (contribution != null)
                {
                    contribution.status = "Paid";
                    contribution.payment_date = DateTime.Now;
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Payment marked as paid!");
                }
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Contribution not found.");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

    }
}