﻿using NcDemo.Models;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;



namespace NcDemo.Controllers
{
    public class CouncilController : ApiController
    {
        NeighborhoodCouncilEntities db = new NeighborhoodCouncilEntities();

        [HttpGet]
        public HttpResponseMessage GetCouncilJoinCode(int councilId)
        {
            try
            {
                var council = db.Council.Where(p => p.id == councilId);

                if (!council.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "Invalid Code");
                }

                var getCode = council.Select(gc => gc.JoinCode).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, getCode);
            }
            catch (Exception ee)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ee);
            }
        }


        [HttpGet]
        public HttpResponseMessage GetCouncil(int councilId)
        {
            try { 
                var council = db.Council.Find(councilId);
            if (council == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }
            return Request.CreateResponse(HttpStatusCode.OK, council);
        }
            catch(Exception ee)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ee);
            }
        }

        [HttpGet]
        public HttpResponseMessage CountCouncilMembers(int councilId)
        {
            try
            {
                var memberCount = db.CouncilMembers
                                    .Where(cm => cm.Council_Id == councilId)
                                    .Count();

                if (memberCount == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No members found for this Council!");
                }

                return Request.CreateResponse(HttpStatusCode.OK, new { MemberCount = memberCount });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred: " + ex.Message);
            }
        }


        /*
                [HttpGet]
                public HttpResponseMessage GetCouncils(int memberId)
                {
                    try
                    {  // Fetch all council memberships for the given memberId
                        var councilMemberships = db.CouncilMembers.Where(cm => cm.Member_Id == memberId).ToList();

                        if (!councilMemberships.Any())
                        {
                            return Request.CreateResponse(HttpStatusCode.NoContent, "No councils found for this member.");
                        }

                        // Extract council ids 
                        var councilIds = councilMemberships.Select(cm => cm.Council_Id).ToList();

                        // Fetch councils that match the council ids
                        var councils = db.Council.Where(c => councilIds.Contains(c.id)).ToList();

                        if (!councils.Any())
                        {
                            return Request.CreateResponse(HttpStatusCode.NoContent, "No matching councils found.");
                        }

                        return Request.CreateResponse(HttpStatusCode.OK, councils);
                    }
                    catch (Exception ee) {
                        return Request.CreateResponse(HttpStatusCode.InternalServerError, "An errord Occured" + ee);
                    }
                }*/

        [HttpGet]
        public HttpResponseMessage GetCouncils(int memberId)
        {
            try
            {
                var councilMemberships = db.CouncilMembers
                    .Where(cm => cm.Member_Id == memberId)
                    .ToList();

                if (!councilMemberships.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No councils found for this member.");
                }

                var councilIds = councilMemberships.Select(cm => cm.Council_Id).ToList();

                var councils = db.Council
                    .Where(c => councilIds.Contains(c.id))
                    .ToList();

                var baseUrl = Request.RequestUri.GetLeftPart(UriPartial.Authority);

                var result = councils.Select(c => new
                {
                    c.id,
                    c.Name,
                    c.Description,
                    c.Date,
                    DisplayPictureUrl = c.DisplayPicture
                }).ToList();

                if (!result.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No matching councils found.");
                }

                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ee)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred: " + ee.Message);
            }
        }


        [HttpGet]
        public HttpResponseMessage GetUserType(int memberId, int councilId)
        {
            // Get the member's role
            var memberRole = (from cm in db.CouncilMembers
                              join r in db.Role on cm.Role_Id equals r.id
                              where cm.Member_Id == memberId && cm.Council_Id == councilId
                              select r.Role_Name).FirstOrDefault();

            if (memberRole == null)
            {
                return Request.CreateResponse(HttpStatusCode.NoContent, "No role found for the user in this council.");
            }

            return Request.CreateResponse(HttpStatusCode.OK, memberRole);
        }


        /*[HttpPost]
        public HttpResponseMessage PostCouncils(Council council, int memberId)
        {
            try
            {
                Council council1 = new Council()
                {
                    Name = council.Name,
                    Description = council.Description,
                    Date = DateTime.Now,
                };
                db.Council.Add(council1);
                db.SaveChanges();

                var setCode = db.Council.FirstOrDefault(cm => cm.id == council1.id);

                if (setCode != null)
                {
                    setCode.JoinCode = CodeGenerator.GenerateJoinCode(council1.id);
                }

                CouncilMembers councilMember = new CouncilMembers
                {
                    Member_Id = memberId,
                    Council_Id = council1.id,
                    Role_Id = 1,   // Id: 1 == 'Admin'
                    Panel_Id = 0,
                };


                db.CouncilMembers.Add(councilMember);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, council.Name + " Added Successfully!");
            }
            catch (Exception ex)
            {

                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "An error occurred: " + ex.Message);
            }
        }*/

        [HttpPost]
    public HttpResponseMessage PostCouncils()
    {
        try
        {
            // Check if the request contains multipart/form-data.
            if (!Request.Content.IsMimeMultipartContent())
            {
                return Request.CreateResponse(HttpStatusCode.UnsupportedMediaType, "Unsupported media type.");
            }

            // Get the memberId from the query parameters.
            var memberId = HttpContext.Current.Request.Params["memberId"];
            if (string.IsNullOrEmpty(memberId))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, "memberId is required.");
            }

            var checkIfJoinedTwo = db.CouncilMembers.Where(c => c.Member_Id == int.Parse(memberId)).ToList();
                if(checkIfJoinedTwo.Count >= 2)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "You cannot Join More Than 2 Councils!");
                }

            // Get other form data (e.g., Name and Description).
            var name = HttpContext.Current.Request.Params["Name"];
            var description = HttpContext.Current.Request.Params["Description"];

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(description))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Name and Description are required.");
            }

            // Process uploaded image if provided.
            string imagePath = null;
            var httpRequest = HttpContext.Current.Request;
            if (httpRequest.Files.Count > 0)
            {
                var postedFile = httpRequest.Files[0];
                if (postedFile != null && postedFile.ContentLength > 0)
                {
                    // Ensure the folder for storing images exists.
                    string folderPath = HttpContext.Current.Server.MapPath("~/displayPictures");
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    // Generate a unique filename and save the file.
                    string fileName = Path.GetFileNameWithoutExtension(postedFile.FileName);
                    string extension = Path.GetExtension(postedFile.FileName);
                    string uniqueFileName = $"{fileName}_{Guid.NewGuid()}{extension}";
                    imagePath = Path.Combine(folderPath, uniqueFileName);
                    postedFile.SaveAs(imagePath);

                    // Store relative path to serve it later.
                    imagePath = $"/displayPictures/{uniqueFileName}";
                }
            }
            else
            {
                // Set default image if no file is uploaded.
                imagePath = "/displayPictures/default.png";
            }

            // Save the council to the database.
            Council council = new Council
            {
                Name = name,
                Description = description,
                Date = DateTime.Now,
                DisplayPicture = imagePath // Save the image path.
            };
            db.Council.Add(council);
            db.SaveChanges();

            // Generate and update the join code.
            var setCode = db.Council.FirstOrDefault(cm => cm.id == council.id);
            if (setCode != null)
            {
                setCode.JoinCode = CodeGenerator.GenerateJoinCode(council.id);
            }

            // Save the admin as a council member.
            CouncilMembers councilMember = new CouncilMembers
            {
                Member_Id = int.Parse(memberId),
                Council_Id = council.id,
                Role_Id = 1,   // Id: 1 == 'Admin'
                Panel_Id = 0,
            };
            db.CouncilMembers.Add(councilMember);
            db.SaveChanges();

            return Request.CreateResponse(HttpStatusCode.OK, $"{name} Added Successfully!");
        }
        catch (Exception ex)
        {
            return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "An error occurred: " + ex.Message);
        }
    }

        [HttpPost]
        public HttpResponseMessage JoinCouncilUsingCode(int memberId, string joinCode)
        {
            try
            {
                var getCouncil = db.Council
                    .Where(c => c.JoinCode.Equals(joinCode))
                    .Select(c => c.id)
                    .FirstOrDefault();

                if (getCouncil == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, "No Council Found with this Join Code");
                }

                var existingMember = db.CouncilMembers
                    .Where(cm => cm.Council_Id == getCouncil && cm.Member_Id == memberId)
                    .FirstOrDefault();

                if (existingMember != null)
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict, "You are already a part of this council");
                }

                var checkIfJoinedTwo = db.CouncilMembers.Where(c => c.Member_Id == memberId).ToList();
                if (checkIfJoinedTwo.Count >= 2)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "You cannot Join More Than 2 Councils!");
                }

                CouncilMembers councilMember = new CouncilMembers
                {
                    Member_Id = memberId,
                    Council_Id = getCouncil,
                    Role_Id = 2 ,   // Id: 2 == 'Member'
                    Panel_Id = 0,
                };

                db.CouncilMembers.Add(councilMember);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Council Joined Successfully");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetCouncilMembers(int councilId)
        {
            try
            {
                var members = (from cm in db.CouncilMembers
                               join m in db.Member on cm.Member_Id equals m.id
                               where cm.Council_Id == councilId && cm.Panel_Id == 0 && cm.Role_Id != 1
                               select new
                               {
                                   MembersId = m.id,
                                   MembersName = m.Full_Name,
                               }).ToList();

                if (members == null )
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

        [HttpGet]
        public HttpResponseMessage GetCouncilMembersForManaging(int councilId)
        {
            try
            {
                var members = (from cm in db.CouncilMembers
                               join m in db.Member on cm.Member_Id equals m.id
                               join r in db.Role on cm.Role_Id equals r.id
                               where cm.Council_Id == councilId 
                               select new
                               {
                                   MembersId = m.id,
                                   MembersName = m.Full_Name,
                                   Role = r.Role_Name
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

        [HttpPut]
        public HttpResponseMessage UpdateRole (int memberId, int councilId, int roleId)
        {
            try
            {
                var member = db.CouncilMembers.FirstOrDefault(cm => cm.Member_Id == memberId && cm.Council_Id == councilId);
                if(member == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Such Member Found");
                }

                member.Role_Id = roleId;
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Successfull");

            }
            catch(Exception ee)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ee);
            }
        }

        [HttpDelete]
        public HttpResponseMessage RemoveResidentFromCouncil(int memberId, int councilId)
        {
            try
            {
                var member = db.CouncilMembers.FirstOrDefault(cm => cm.Member_Id == memberId && cm.Council_Id == councilId);
                if (member == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Such Member Found");
                }
                db.CouncilMembers.Remove(member);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "Resident Removed Sucessfully!");
            }
            catch(Exception ee)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ee);
            }
        }

        [HttpGet]
        public HttpResponseMessage getCouncilPanel(int councilId)
        {
            try
            {
                var getCouncil = db.CouncilMembers.Where(c => c.Council_Id == councilId);
                return Request.CreateResponse(HttpStatusCode.OK, getCouncil);
            }
            catch(Exception ee)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ee);
            }
        }

        public HttpResponseMessage GetCouncilPanelData(int councilId)
        {
            try
            {
                // Fetch council members for the given council and relevant roles
                var councilMembers = db.CouncilMembers
                    .Where(c => c.Council_Id == councilId &&
                                (c.Role_Id == 3 || c.Role_Id == 4 || c.Role_Id == 5 || c.Role_Id == 6))
                    .ToList();

                // Ensure members are found
                if (councilMembers == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Panel Found for the given Council ID.");
                }

                // Fetch corresponding member details
                var memberIds = councilMembers.Select(c => c.Member_Id).Distinct().ToList();
                var members = db.Member.Where(m => memberIds.Contains(m.id)).ToList();

                // Assign roles based on Role_Id
                var chairmanName = members.FirstOrDefault(m =>
                    councilMembers.Any(c => c.Member_Id == m.id && c.Role_Id == 5))?.Full_Name;
                var treasurerName = members.FirstOrDefault(m =>
                    councilMembers.Any(c => c.Member_Id == m.id && c.Role_Id == 4))?.Full_Name;
                var councillorName = members.FirstOrDefault(m =>
                    councilMembers.Any(c => c.Member_Id == m.id && c.Role_Id == 3))?.Full_Name;
                var secretaryName = members.FirstOrDefault(m =>
                    councilMembers.Any(c => c.Member_Id == m.id && c.Role_Id == 6))?.Full_Name;

                // Check if any role is unassigned
               /* if (string.IsNullOrEmpty(chairmanName) ||
                    string.IsNullOrEmpty(treasurerName) ||
                    string.IsNullOrEmpty(councillorName) ||
                    string.IsNullOrEmpty(secretaryName))
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "Some or all panel roles are unassigned.");
                }*/

                // Construct panel data object
                var councilPanel = new
                {
                    Chairman = chairmanName,
                    Treasurer = treasurerName,
                    Councillor = councillorName,
                    Secretary = secretaryName
                };

                return Request.CreateResponse(HttpStatusCode.OK, councilPanel);
            }
            catch (Exception ex)
            {
                // Log the exception and return a friendly error message
                Console.WriteLine($"Error: {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred while fetching council panel data.");
            }
        }

        [HttpDelete]
        public HttpResponseMessage LeaveCouncil(int councilId, int memberId)
        {
            try {
                var getCouncil = db.CouncilMembers.FirstOrDefault(c => c.Council_Id == councilId && c.Member_Id == memberId);
                if(getCouncil == null)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "NO Member Found For this Council");
                }
                string currentMonthYear = DateTime.Now.ToString("MM-yyyy");

                var checkContribution = db.Monthly_Contributions.FirstOrDefault(m =>
                    m.council_id == councilId &&
                    m.member_id == memberId &&
                    m.month_year == currentMonthYear
                );

                if (checkContribution != null && checkContribution.status == "Unpaid")
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, "You have uncleared dues. Kindly pay them first!");
                }

                db.CouncilMembers.Remove(getCouncil);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Council left Successfully!");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later." +  ex);
            }

        }

        [HttpPut]
        public HttpResponseMessage UpdateCouncil(int councilId, [FromBody] CouncilUpdateDto updatedCouncil)
        {
            try
            {
                if (updatedCouncil == null || string.IsNullOrWhiteSpace(updatedCouncil.Name))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new { message = "Invalid data. Name is required." });
                }

                var council = db.Council.FirstOrDefault(c => c.id == councilId);
                if (council == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { message = "Council not found." });
                }

                // Update only Name and Description
                council.Name = updatedCouncil.Name;
                council.Description = updatedCouncil.Description;

                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, new { message = "Council updated successfully." });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { message = "An error occurred.", error = ex.Message });
            }
        }

        // DTO to restrict updating other fields
        public class CouncilUpdateDto
        {
            public string Name { get; set; }
            public string Description { get; set; }
        }


        [HttpPut]
        public HttpResponseMessage SwitchCouncil(int OldCouncilId, int memberId, int NewCouncilId)
        {
            try
            {
                var getCouncil = db.CouncilMembers.FirstOrDefault(c => c.Council_Id == OldCouncilId && c.Member_Id == memberId);
                if (getCouncil == null)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "NO Member Found For this Council");
                }
                string currentMonthYear = DateTime.Now.ToString("MM-yyyy");

                var checkContribution = db.Monthly_Contributions.FirstOrDefault(m =>
                    m.council_id == OldCouncilId &&
                    m.member_id == memberId &&
                    m.month_year == currentMonthYear
                );

                if (checkContribution.status == "Unpaid")
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, "You have Uncleared Dues, Kindly Pay them First!");
                }

                db.CouncilMembers.Remove(getCouncil);

                var newMember = new CouncilMembers {
                    Council_Id = NewCouncilId,
                    Member_Id = memberId,
                    Role_Id = 2,
                    Panel_Id = 0
                };
                db.CouncilMembers.Add(newMember);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Council Switched SuccessFully!");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetAllCouncils(int memberId)
        {
            try
            {
                // Fetch all councils from the database
                var allCouncils = db.Council.ToList();

                // Fetch the member's current council
                /*var currentCouncil = db.CouncilMembers
                    .Where(cm => cm.Member_Id == memberId)
                    .Select(cm => cm.Council_Id)
                    .FirstOrDefault();
*/
                if (!allCouncils.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No councils found.");
                }

                var result = allCouncils.Select(c => new
                {
                    c.id,
                    c.Name,
                    c.Description,
                    c.Date,
                    DisplayPictureUrl = c.DisplayPicture,
                    //IsCurrent = c.id == currentCouncil
                }).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred: " + ex.Message);
            }
        }

        [HttpPost]
        public HttpResponseMessage SwitchCouncilUsingCode(int councilId, int memberId, string joinCode)
        {
            try
            {
                //Check If Code is Valid
                var getCouncil = db.Council
                    .Where(c => c.JoinCode.Equals(joinCode))
                    .Select(c => c.id)
                    .FirstOrDefault();

                if (getCouncil == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.Forbidden, "No Council Found with this Join Code");
                }

                //Check If Already part of Council
                var existingMember = db.CouncilMembers
                    .Where(cm => cm.Council_Id == getCouncil && cm.Member_Id == memberId)
                    .FirstOrDefault();

                if (existingMember != null)
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict, "You are already a part of this council");
                }

                var getExistingCouncil = db.CouncilMembers.FirstOrDefault(c => c.Council_Id == councilId && c.Member_Id == memberId);
                
                // Delete Old Council
                if (getExistingCouncil == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "No Member Found For this Council");
                }
                db.CouncilMembers.Remove(getExistingCouncil);

                //Check if part of 2 councils already
                /*var checkIfJoinedTwo = db.CouncilMembers.Where(c => c.Member_Id == memberId).ToList();
                if (checkIfJoinedTwo.Count > 2)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "You cannot Join More Than 2 Councils!");
                }*/

                CouncilMembers councilMember = new CouncilMembers
                {
                    Member_Id = memberId,
                    Council_Id = getCouncil,
                    Role_Id = 2,   // Id: 2 == 'Member'
                    Panel_Id = 0,
                };

                db.CouncilMembers.Add(councilMember);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Council Joined Successfully");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
    }
}