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
        public HttpResponseMessage GetAnnouncements(int councilId)
        {
            try
            {
                var announcements = (from a in db.Announcements
                                     join ai in db.Announcement_Info on a.id equals ai.Announcement_id
                                     where ai.Council_id == councilId
                                     select a).ToList();

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

        [HttpGet]
        public HttpResponseMessage GetAnnouncementsForCouncil(int councilId)
        {
            try
            {
                var announcements = (from a in db.Announcements
                                     join ai in db.Announcement_Info on a.id equals ai.Announcement_id
                                     where ai.Council_id == councilId
                                     select new AnnouncementDto
                                     {
                                         AnnouncementId = a.id,
                                         Title = a.title,
                                         Description = a.Description,
                                         Date = a.Date,
                                         AddedBy = db.Member
                                                    .Where(m => m.id == ai.Member_id)
                                                    .Select(x => x.Full_Name)
                                                    .FirstOrDefault(),
                                         RoleName = db.CouncilMembers
                                                      .Where(cm => cm.Member_Id == ai.Member_id && cm.Council_Id == councilId)
                                                      .Select(x => db.Role
                                                                     .Where(r => r.id == x.Role_Id)
                                                                     .Select(o => o.Role_Name)
                                                                     .FirstOrDefault())
                                                      .FirstOrDefault()
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
        public class AnnouncementDto
        {
            public int AnnouncementId { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public Nullable<DateTime> Date { get; set; }
            public string AddedBy { get; set; }
            public string RoleName { get; set; }
        }

    }
}