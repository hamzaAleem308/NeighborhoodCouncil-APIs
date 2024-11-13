using NcDemo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace NcDemo.Controllers
{
    public class AnnouncementController : ApiController
    {
        NeighborhoodCouncilEntities db = new NeighborhoodCouncilEntities();

        [HttpPost]
        public HttpResponseMessage PostAnnouncement(Announcements announcement, int memberId, int councilId)
        {
            try
            {
                Announcements ann = new Announcements()
                {
                    title = announcement.title,
                    Description = announcement.Description,
                    Date = DateTime.Now,
                };
                db.Announcements.Add(ann);
                db.SaveChanges();


                Announcement_Info ann_info = new Announcement_Info
                {
                    Member_id = memberId,
                    Council_id = councilId,
                    Announcement_id = ann.id
                };

                db.Announcement_Info.Add(ann_info);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Announcement Added Successfully!");
            }
            catch (Exception ex)
            {

                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "An error occurred: " + ex.Message);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetAnnouncements(int memberId, int councilId)
        {
            try
            {
                var announcements = (from a in db.Announcements
                                     join ai in db.Announcement_Info on a.id equals ai.Announcement_id
                                     where ai.Council_id == councilId && ai.Member_id == memberId
                                     select new
                                     {
                                         AnnouncementId = a.id,
                                         Title = a.title,
                                         Description = a.Description,
                                         Date = a.Date,
                                        /* CouncilId = ai.Council_id,
                                         MemberId = ai.Member_id*/
                                     }).ToList();

                if (announcements == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No announcements found for this member and council.");
                }

                return Request.CreateResponse(HttpStatusCode.OK, announcements);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

    }
}