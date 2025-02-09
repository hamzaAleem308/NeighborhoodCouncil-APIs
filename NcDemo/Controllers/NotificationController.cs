using NcDemo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace NcDemo.Controllers
{
    public class NotificationController : ApiController
    {
        NeighborhoodCouncilEntities db = new NeighborhoodCouncilEntities();

        [HttpGet]
        public HttpResponseMessage GetNotifications(int councilId, int? memberId)
        {
            try
            {
                // Validate the input councilId
                if (councilId <= 0)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid councilId provided.");
                }

                // Fetch notifications filtered by councilId
                var notificationsQuery = db.Notifications.Where(n => n.council_id == councilId);

                // Include notifications specific to the memberId or with memberId as NULL
                if (memberId.HasValue)
                {
                    notificationsQuery = notificationsQuery.Where(n => n.member_id == memberId || n.member_id == null);
                }

                // Fetch and prepare the result
                var notifications = notificationsQuery
                    .OrderByDescending(n => n.created_at)
                    .Select(n => new
                    {
                        n.id,
                        n.council_id,
                        n.member_id,
                        n.module,
                        n.title,
                        n.message,
                        n.action_url,
                        n.data,
                        n.is_read,
                        CreatedAt = n.created_at,
                        UpdatedAt = n.updated_at
                    })
                    .ToList();

                // Check if notifications exist
                if (notifications.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No notifications found.");
                }

                // Return the notifications
                return Request.CreateResponse(HttpStatusCode.OK, notifications);
            }
            catch (Exception ex)
            {
                // Handle any unexpected errors
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred: " + ex.Message);
            }
        }


    }
}