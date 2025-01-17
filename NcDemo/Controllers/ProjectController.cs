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
                    start_date = project.start_date,
                    end_date = project.end_date,
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

        [HttpPost]
        public HttpResponseMessage AddProjectLogsById(int projectId, Project_Logs Logs)
        {
            try
            {
                // Retrieve the project using projectId
                var project = db.Projects.FirstOrDefault(x => x.id == projectId);

                // If no project is found, return NoContent
                if (project == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Project Found");
                }
                else
                {
                    var log = new Project_Logs
                    {
                        project_id = projectId,
                        action_taken = Logs.action_taken,
                        action_date = Logs.action_date,
                        status = Logs.status,
                        comments = Logs.comments,
                        amount_spent = Logs.amount_spent,
                        feedback = Logs.feedback,
                        logged_by = Logs.logged_by,
                    };

                    db.Project_Logs.Add(log);
                    project.status = Logs.status;
                    db.SaveChanges();
                }
                return Request.CreateResponse(HttpStatusCode.OK, "Project Logs Added Successfully!");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetProjectWithLogs(int projectId)
        {
            try
            {
                // Fetch the project
                var project = db.Projects.FirstOrDefault(p => p.id == projectId);
                if (project == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "Project not found.");
                }

                // Fetch related logs
                var logs = db.Project_Logs.Where(log => log.project_id == projectId).ToList();

                // Combine project and logs
                var response = new
                {
                    Project = project,
                    Logs = logs
                };

                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

    }
}