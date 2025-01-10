using NcDemo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Web.Http;

namespace NcDemo.Controllers
{
    public class ProjectController : ApiController
    {
        NeighborhoodCouncilEntities db = new NeighborhoodCouncilEntities();

        [HttpPost]
       public HttpResponseMessage AddProject (Projects project, int councilId, int memberId)
        {
            try
            {
                if(project == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Data Found for payload");
                }

                var newProject = new Projects
                {
                    title = project.title,
                    description = project.description,
                    status = "Pending",
                    Priority = project.Priority,
                    problem_id = project.problem_id,
                    start_date = DateTime.Now,
                    end_date = DateTime.Now,
                    budget = project.budget,
                    council_id = councilId,
                    created_by = memberId,
                    created_at = DateTime.Now
                };
                db.Projects.Add(newProject);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ee)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ee);
            }
        }

        [HttpPost]
        public HttpResponseMessage UpdateProjectPriority(int projectId, string newPriority)
        {
            try
            {
                var project =db.Projects.Find(projectId);
                if (project == null)
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No problem Found");
                project.Priority = newPriority;

                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch(Exception ee)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ee);
            }
        }

        [HttpGet]
        public HttpResponseMessage ShowProjects(int councilId)
        {
            try
            {
                var projects = db.Projects.Where(x => x.council_id == councilId);
                if (projects == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Projects Found");
                }
                return Request.CreateResponse(HttpStatusCode.OK, projects);
            }
            catch(Exception ee)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ee);
            }
        }
    }
}