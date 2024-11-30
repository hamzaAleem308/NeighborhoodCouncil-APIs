using NcDemo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace NcDemo.Controllers
{
    public class MeetingController : ApiController
    {
        NeighborhoodCouncilEntities db = new NeighborhoodCouncilEntities();

        [HttpPost]
        public HttpResponseMessage PostMeeting(Meetings meeting)
        {
            try
            {
                Meetings newMeeting = new Meetings
                {
                    title = meeting.title,
                    description = meeting.description,
                    address = meeting.address,
                    council_id = meeting.council_id,
                    scheduled_date = meeting.scheduled_date,
                    created_at = DateTime.Now,
                    meeting_type = meeting.meeting_type
                };
                db.Meetings.Add(newMeeting);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch(Exception ee)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ee);
            }
        }

        [HttpGet]
        public HttpResponseMessage getMeetings(int councilId)
        {
            try
            {
                var meetings = db.Meetings.Where(m => m.council_id == councilId);
                if(meetings == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Meeting Found");
                }

                return Request.CreateResponse(HttpStatusCode.OK, meetings);
            }
            catch(Exception ee)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ee);
            }
        }

        [HttpPost]
        public HttpResponseMessage AddMinutesOfMeeting(Meeting_Minutes obj)
        {
            try
            {
                if(obj == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Data Found");
                }
                db.Meeting_Minutes.Add(obj);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }

        [HttpGet]
        public HttpResponseMessage getMeetingAlongMinutes (int councilId)
        {
            try
            {
                var result = db.Meetings.Where(m => m.council_id == councilId)
                    .Select(m => new
                    {
                        MeetingId = m.id,
                        MeetingTitle = m.title,
                        Minutes = db.Meeting_Minutes.Where(mm => mm.meeting_id == m.id)
                        .Select(mm => new
                        {
                            MinutesId = mm.id,
                            MeetingMinutes = mm.minutes,
                            MinutesDate = mm.created_at,
                            CreatedBy = db.Member.Where(me => me.id == mm.recorded_by)
                            .Select(me => me.Full_Name)
                        }).ToList(),
                    }).FirstOrDefault();

                if(result == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Meeting Along with Minutes Found");
                }

                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch(Exception ee)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ee);
            }
        }
    }
}