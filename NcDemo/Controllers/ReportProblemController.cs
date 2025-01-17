using NcDemo.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
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
        public HttpResponseMessage GetReportedProblems(int councilId)
        {
            try
            {
                // Fetch raw data from the database
                var rawProblems = (from problem in db.Report_Problem
                                   join problemInfo in db.Report_Problem_info
                                   on problem.id equals problemInfo.Report_Problem_id
                                   where problemInfo.Council_id == councilId
                                   select new
                                   {
                                       ProblemId = problem.id,
                                       Title = problem.title,
                                       Description = problem.Description,
                                       Status = problem.Status,
                                       ProblemType = problem.ProblemType,
                                       Category = problem.Category,
                                       CreatedAt = problem.CreatedAt,
                                       VisualEvidence = problem.VisualEvidence,
                                       MemberId = problemInfo.Member_id,
                                       CouncilId = problemInfo.Council_id,
                                       SolverId = problem.solver_id
                                   }).ToList();

                // Perform transformations in memory
                var transformedProblems = rawProblems.Select(problem => new
                {
                    problem.ProblemId,
                    problem.Title,
                    problem.Description,
                    problem.Status,
                    problem.ProblemType,
                    problem.Category,
                    problem.CreatedAt,
                    VisualEvidence = !string.IsNullOrEmpty(problem.VisualEvidence)
                        ? (problem.VisualEvidence)
                        : null,
                    problem.MemberId,
                    problem.CouncilId,
                    problem.SolverId
                }).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, transformedProblems);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }


        [HttpGet]
        public HttpResponseMessage GetReportedProblemByMember(int councilId, int memberId)
        {
            try
            {
                // Fetch raw data from the database
                var rawProblems = (from problem in db.Report_Problem
                                   join problemInfo in db.Report_Problem_info
                                   on problem.id equals problemInfo.Report_Problem_id
                                   where problemInfo.Council_id == councilId && problemInfo.Member_id == memberId
                                   select new
                                   {
                                       ProblemId = problem.id,
                                       Title = problem.title,
                                       Description = problem.Description,
                                       Status = problem.Status,
                                       ProblemType = problem.ProblemType,
                                       Category = problem.Category,
                                       CreatedAt = problem.CreatedAt,
                                       VisualEvidence = problem.VisualEvidence,
                                       MemberId = problemInfo.Member_id,
                                       CouncilId = problemInfo.Council_id,
                                       SolverId = problem.solver_id
                                   }).ToList();

                // Perform transformations in memory
                var transformedProblems = rawProblems.Select(problem => new
                {
                    problem.ProblemId,
                    problem.Title,
                    problem.Description,
                    problem.Status,
                    problem.ProblemType,
                    problem.Category,
                    problem.CreatedAt,
                    VisualEvidence = !string.IsNullOrEmpty(problem.VisualEvidence)
                        ? (problem.VisualEvidence)
                        : null,
                    problem.MemberId,
                    problem.CouncilId,
                    problem.SolverId
                }).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, transformedProblems);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
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

        [HttpPost]
        public HttpResponseMessage SetStatusToOpen(int problemId)
        {
            try
            {
                var problem = db.Report_Problem.Find(problemId);
                if (problem == null)
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Problem Found");

                var getCouncil = db.Report_Problem_info.FirstOrDefault(c => c.Report_Problem_id == problemId).Council_id;

                var getMember = db.Report_Problem_info.FirstOrDefault(c => c.Report_Problem_id == problemId).Member_id;

                if (getMember == 0)
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Member Found For Problem");

                if (getCouncil == 0)
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Council Found For Problem");

                problem.Status = "Opened";

                var notify = new Notifications {
                    council_id = getCouncil,
                    member_id = getMember,
                    title = "You're Reported Problem is Now Opened!",
                    message = $"You're Problem {problem.title} is Now Open",
                    module = "ReportProblem",
                    created_at = DateTime.Now,
                    updated_at = DateTime.Now,
                };
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "Status Set to Opened");
            }
            catch(Exception ee)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ee);
            }
        }

        [HttpPost]
        public HttpResponseMessage SetStatusToClose(int problemId)
        {
            try
            {
                var problem = db.Report_Problem.Find(problemId);
                if (problem == null)
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Problem Found");

                problem.Status = "Closed";
                
                var getCouncil = db.Report_Problem_info.FirstOrDefault(c => c.Report_Problem_id == problemId).Council_id;

                var getMember = db.Report_Problem_info.FirstOrDefault(c => c.Report_Problem_id == problemId).Member_id;

                if (getMember == 0)
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Member Found For Problem");

                if (getCouncil == 0)
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Council Found For Problem");

                var notify = new Notifications
                {
                    council_id = getCouncil,
                    member_id = getMember,
                    title = "You're Reported Problem is Now Closed!",
                    message = $"You're Problem {problem.title} is Now Closed, update whether you Happy or Not!",
                    module = "ReportProblem",
                    created_at = DateTime.Now,
                    updated_at = DateTime.Now,
                };
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "Status Set to Closed");
            }
            catch (Exception ee)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ee);
            }
        }

        [HttpPost]
        public HttpResponseMessage SetProblemSatisfiedOrUnsatisfied(int problemId, string status)
        {
            try
            {
                var problem = db.Report_Problem.Find(problemId);
                if (problem == null)
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Problem Found");

                if (status == "Satisfied")
                {
                    problem.Status = "Completed";
                }
                else
                {
                    problem.Status = "Pending";
                }
                var getCouncil = db.Report_Problem_info.FirstOrDefault(c => c.Report_Problem_id == problemId).Council_id;

                var getMember = db.Report_Problem_info.FirstOrDefault(c => c.Report_Problem_id == problemId).Member_id;

                if (getMember == 0)
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Member Found For Problem");

                if (getCouncil == 0)
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Council Found For Problem");

                /*var notify = new Notifications
                {
                    council_id = getCouncil,
                    member_id = getMember,
                    title = "You're are Satisfied on th!",
                    message = $"You're Problem {problem.title} is , update whether you Happy or Not!",
                    module = "ReportProblem",
                    created_at = DateTime.Now,
                    updated_at = DateTime.Now,
                };*/
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "Status Set As per the Satisfaction Level!");

            }
           catch(Exception ee)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ee);
            }
        }


        [HttpGet]
        public HttpResponseMessage GetProblemHierarchy(int problemId)
        {
            try
            {
                // Fetch the raw problem details (simplified query)
                var rawProblem = db.Report_Problem
                    .Where(p => p.id == problemId)
                    .Select(p => new
                    {
                        ProblemId = p.id,
                        Title = p.title,
                        Description = p.Description,
                        Status = p.Status,
                        ProblemType = p.ProblemType,
                        Category = p.Category,
                        CreatedAt = p.CreatedAt,
                        VisualEvidence = p.VisualEvidence
                    })
                    .FirstOrDefault();

                if (rawProblem == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, new
                    {
                        Message = "Problem not found.",
                        StatusCode = 404
                    });
                }

                // Fetch related MemberId and CouncilId from Report_Problem_Info (separate query)
                var problemInfo = db.Report_Problem_info
                    .Where(dpi => dpi.Report_Problem_id == problemId)
                    .Select(dpi => new
                    {
                        dpi.Member_id,
                        dpi.Council_id
                    })
                    .FirstOrDefault();

                // Fetch meetings linked to the problem
                var rawMeetings = db.Meetings
                    .Where(m => m.problem_id == problemId)
                    .Select(m => new
                    {
                        MeetingId = m.id,
                        Title = m.title,
                        Description = m.description,
                        Address = m.address,
                        ScheduledDate = m.scheduled_date,
                        MeetingType = m.meeting_type
                    })
                    .ToList();

                // Fetch projects linked to the problem
                var rawProjects = db.Projects
                    .Where(pr => pr.problem_id == problemId)
                    .Select(pr => new
                    {
                        ProjectId = pr.id,
                        Title = pr.title,
                        Description = pr.description,
                        Status = pr.status,
                        Priority = pr.Priority,
                        StartDate = pr.start_date,
                        EndDate = pr.end_date,
                        Budget = pr.budget
                    })
                    .ToList();

                // Fetch project logs linked to the projects
                var projectIds = rawProjects.Select(pr => pr.ProjectId).ToList();
                var rawProjectLogs = db.Project_Logs
                    .Where(pl => projectIds.Contains(pl.project_id))
                    .Select(pl => new
                    {
                        LogId = pl.id,
                        ProjectId = pl.project_id,
                        ActionTaken = pl.action_taken,
                        ActionDate = pl.action_date,
                        Status = pl.status,
                        Comments = pl.comments,
                        AmountSpent = pl.amount_spent,
                        Feedback = pl.feedback
                    })
                    .ToList();

                // Transform the data in memory
                var transformedProblem = new
                {
                    rawProblem.ProblemId,
                    rawProblem.Title,
                    rawProblem.Description,
                    rawProblem.Status,
                    rawProblem.ProblemType,
                    rawProblem.Category,
                    rawProblem.CreatedAt,
                    VisualEvidence = !string.IsNullOrEmpty(rawProblem.VisualEvidence)
                        ? (rawProblem.VisualEvidence)
                        : null,
                    MemberId = problemInfo?.Member_id,
                    CouncilId = problemInfo?.Council_id,
                    Meetings = rawMeetings.Select(m => new
                    {
                        m.MeetingId,
                        m.Title,
                        m.Description,
                        m.Address,
                        m.ScheduledDate,
                        m.MeetingType
                    }).ToList(),
                    Projects = rawProjects.Select(p => new
                    {
                        p.ProjectId,
                        p.Title,
                        p.Description,
                        p.Status,
                        p.Priority,
                        p.StartDate,
                        p.EndDate,
                        p.Budget,
                        Logs = rawProjectLogs
                            .Where(pl => pl.ProjectId == p.ProjectId)
                            .Select(pl => new
                            {
                                pl.LogId,
                                pl.ActionTaken,
                                pl.ActionDate,
                                pl.Status,
                                pl.Comments,
                                pl.AmountSpent,
                                pl.Feedback
                            })
                            .ToList()
                    }).ToList()
                };

                // Return the transformed data
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    Message = "Hierarchy fetched successfully.",
                    StatusCode = 200,
                    Data = transformedProblem
                });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    Message = "An error occurred while fetching the hierarchy.",
                    ExceptionMessage = ex.Message,
                    StatusCode = 500
                });
            }
        }

        [HttpGet]
        public HttpResponseMessage GetCouncilMembersForAssigning(int councilId)
        {
            try
            {
                var members = (from cm in db.CouncilMembers
                               join m in db.Member on cm.Member_Id equals m.id
                               where cm.Council_Id == councilId && cm.Role_Id != 1 && cm.Role_Id != 2 && cm.Role_Id != 5
                               select new
                               {
                                   MembersId = m.id,
                                   MembersName = m.Full_Name,
                                   MemberRole = db.Role.FirstOrDefault(c => c.id == cm.Role_Id).Role_Name
                               }).ToList();

                if (members == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No members available for this Council Id.");
                }

                return Request.CreateResponse(HttpStatusCode.OK, members);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred: " + ex.Message);
            }
        }

        [HttpPost]
        public HttpResponseMessage AssignProblemToPanelMember(int councilId, int solverId, int problemId)
        {
            try
            {
                var getProblem = db.Report_Problem.Find(problemId);
                    getProblem.solver_id = solverId;

                var notify = new Notifications { 
                    member_id = solverId,
                    council_id = councilId,
                    title = $"You have been assigned to Solve a Problem",
                    message = $"{getProblem.title}",
                    created_at = DateTime.Now,
                    module = "ReportProblem",
                    updated_at = DateTime.Now,
                };
                db.Notifications.Add(notify);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "Problem Assigned to " + solverId);
            }
            catch(Exception ee)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ee);
            }
        }

        [HttpGet]
        public HttpResponseMessage FetchMemberById(int memberId, int councilId)
        {
            try
            {
                var getMember = db.Member.Find(memberId)?.Full_Name;

                var memberRoleId = db.CouncilMembers
                    .FirstOrDefault(c => c.Member_Id == memberId && c.Council_Id == councilId)?.Role_Id;

                var memberRole = memberRoleId != null
                    ? db.Role.FirstOrDefault(r => r.id == memberRoleId)?.Role_Name
                    : null;

                if (getMember == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "Member Not Found");
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { getMember, memberRole });
            }
            catch(Exception ee)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ee);
            }
        }
    }
}