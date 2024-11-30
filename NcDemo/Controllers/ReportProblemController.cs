using NcDemo.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace NcDemo.Controllers
{
    public class ReportProblemController : ApiController
    {
        NeighborhoodCouncilEntities db = new NeighborhoodCouncilEntities();
        [HttpPost]
        public HttpResponseMessage ReportProblem()
        {
            try
            {
                var request = HttpContext.Current.Request;

                // Set the server path for storing uploaded files
                var path = HttpContext.Current.Server.MapPath("~/Evidence");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                // Initialize variables for file path and request form data
                string evidencePath = null;

                // Process uploaded files
                for (int i = 0; i < request.Files.Count; i++)
                {
                    var file = request.Files[i];
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    var fullPath = Path.Combine(path, fileName);
                    file.SaveAs(fullPath);
                    evidencePath = "/Evidence/" + fileName;
                }

                // Parse the form data
                var form = request.Form;
                string title = form["title"];
                string description = form["description"];
                string problemType = form["problemType"];
                string category = form["category"];
                int memberId = int.Parse(form["memberId"]);
                int councilId = int.Parse(form["councilId"]);

                // Insert into Report_Problem table
                var problem = new Report_Problem
                {
                    title = title,
                    Description = description,
                    VisualEvidence = evidencePath,
                    Status = "Pending",
                    ProblemType = problemType,
                    Category = category,
                    CreatedAt = DateTime.UtcNow
                };
                db.Report_Problem.Add(problem);
                db.SaveChanges();

                // Insert into Report_Problem_info table
                var problemInfo = new Report_Problem_info
                {
                    Report_Problem_id = problem.id,
                    Member_id = memberId,
                    Council_id = councilId
                };
                db.Report_Problem_info.Add(problemInfo);
                db.SaveChanges();

                // Return success response
                return Request.CreateResponse(HttpStatusCode.OK, new { Message = "Problem reported successfully.", ProblemId = problem.id });
            }
            catch (Exception ex)
            {
                // Return error response
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { Message = "An error occurred while reporting the problem.", Error = ex.Message });
            }
        }

        [HttpGet]
        public HttpResponseMessage getProblemStatusSummary(int memberId, int councilId)
        {

            try
            {
                var problemSummary = (from rp in db.Report_Problem
                                      join rpi in db.Report_Problem_info on rp.id equals rpi.Report_Problem_id
                                      where rpi.Member_id == memberId && rpi.Council_id == councilId
                                      group rp by rp.Status into statusGroup
                                      select new
                                      {
                                          Status = statusGroup.Key,
                                          Count = statusGroup.Count()
                                      })
                                      .OrderBy(x => x.Status)
                                      .ToList();

                if (problemSummary == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Problem Status Found");
                }

                var totalProblems = problemSummary.Sum(x => x.Count);

                var response = new
                {
                    StatusCounts = problemSummary,
                    Total = totalProblems
                };

                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception ee)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError , ee);
            }
        }
    }
}