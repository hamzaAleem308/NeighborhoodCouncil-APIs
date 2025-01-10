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
        public HttpResponseMessage GetNotifications(int? councilId, int? memberId = null)
        {
            try
            {
                // Fetch notifications filtered by councilId and optionally by memberId
                var notificationsQuery = db.Notifications.Where(n => n.council_id == councilId);

                if (memberId.HasValue)
                {
                    notificationsQuery = notificationsQuery.Where(n => n.member_id == memberId.Value || n.member_id == null);
                }

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

                return Request.CreateResponse(HttpStatusCode.OK, notifications);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred: " + ex.Message);
            }
        }

    }
}