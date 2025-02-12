using Microsoft.Ajax.Utilities;
using NcDemo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using static System.Collections.Specialized.BitVector32;

namespace NcDemo.Controllers
{
    public class ElectionController : ApiController
    {
        NeighborhoodCouncilEntities db = new NeighborhoodCouncilEntities();

        [HttpPost]
        public HttpResponseMessage PostElection(Elections election)
        {
            try
            {
                db.Elections.Add(election);
                db.SaveChanges();
                return  Request.CreateResponse(HttpStatusCode.OK);
            }
            catch(Exception ee)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ee);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetElection(int councilId)
        {
            try {
                var election = db.Elections.Where(c => c.Council_id == councilId);
                if (election == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Election found for this Council!");
                }
                return Request.CreateResponse(HttpStatusCode.OK, election);
            }
            catch(Exception ee)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ee);
            }
        }

        [HttpGet]
        public HttpResponseMessage SetPanelMembersToMember(int councilId)
        {
            try
            {
                // Get the last closed election
                var lastClosedElection = db.Elections
                    .Where(e => e.Council_id == councilId && e.status == "Closed")
                    .OrderByDescending(e => e.EndDate)
                    .FirstOrDefault();

                if (lastClosedElection != null)
                {
                    // Add 2 months to the last closed election's EndDate
                    DateTime nextEligibleDate = lastClosedElection.EndDate.Value.AddMonths(2);
                    if (DateTime.Now < nextEligibleDate)
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, $"New election can only be initiated after {nextEligibleDate:yyyy-MM-dd}");
                    }
                }
                else
                {
                    // Change PanelMembers To Simple Members
                    var councilMembers = db.CouncilMembers.Where(cm => cm.Council_Id == councilId).ToList();
                    if (councilMembers == null)
                        return Request.CreateResponse(HttpStatusCode.NoContent, "No member Found for this Council");
                    foreach(var c in councilMembers)
                    {
                        // Set to Member
                        if (c.Role_Id != 1)
                        {
                            c.Role_Id = 2;
                        }
                    }
                    db.SaveChanges();
                }
                return Request.CreateResponse(HttpStatusCode.OK, "Panel Member Successfully Changed to Member!");
            }
            catch (Exception ee)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ee);
            }
        }

        [HttpPost]
        public HttpResponseMessage InitiateElection(Elections elect)
        {
            try
            {
                // Check if there's an active election
                var activeElection = db.Elections.FirstOrDefault(e => e.Council_id == elect.Council_id && e.status != "Closed");
                if (activeElection != null)
                {
                    return Request.CreateResponse(HttpStatusCode.Found, "Election already exists, close the previous election first.");
                }

                // Get the last closed election
                var lastClosedElection = db.Elections
                    .Where(e => e.Council_id == elect.Council_id && e.status == "Closed")
                    .OrderByDescending(e => e.EndDate)
                    .FirstOrDefault();

                if (lastClosedElection != null)
                {
                    // Add 2 months to the last closed election's EndDate
                    DateTime nextEligibleDate = lastClosedElection.EndDate.Value.AddMonths(2);
                    if (DateTime.Now < nextEligibleDate)
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, $"New election can only be initiated after {nextEligibleDate:yyyy-MM-dd}");
                    }
                }

                // Create and initiate the new election
                var election = new Elections
                {
                    Name = elect.Name,
                    StartDate = elect.StartDate,
                    EndDate = elect.EndDate,
                    status = "Initiated",
                    Council_id = elect.Council_id,
                };
                db.Elections.Add(election);

                // Add a notification
                var notify = new Notifications
                {
                    council_id = (int)elect.Council_id,
                    title = "An Election has been Initiated in the Council",
                    message = $"{elect.StartDate} to {elect.EndDate}",
                    module = "Election",
                    created_at = DateTime.Now,
                };
                db.Notifications.Add(notify);

                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "Election initiated successfully.");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }


        [HttpPost]
        public HttpResponseMessage NominateCandidateForPanel([FromBody] NominateCandidateRequest request)
        {
            try
            {
                // Extract values from the request object
                int candidateId = request.CandidateId;
                List<PanelMemberDto> panelMembersDto = request.PanelMembers;
                string panelName = request.PanelName;
                int councilId = request.CouncilId;
                int MemberId = request.MemberId;

                // Check if the member is already a candidate for this election
                var existingCandidate = db.Candidates.FirstOrDefault(c => c.member_id == candidateId && c.council_id == councilId);
                if (existingCandidate != null)
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict, "This member is already nominated as a candidate for this election.");
                }

                // Create a new panel
                Panel newPanel = new Panel
                {
                    Candidate_Id = candidateId,
                    Name = panelName,
                    council_Id = councilId,
                    created_by = MemberId
                };
                db.Panel.Add(newPanel);
                db.SaveChanges(); // Save to generate Panel ID

                // Assign panel ID to council members and prepare panel members
                var panelMembers = new List<PanelMembers>();

                // Fetch council members manually without using `Contains`
                var councilMembers = new List<CouncilMembers>();
                foreach (var memberDto in panelMembersDto)
                {
                    var memberId = memberDto.MemberId;
                    var member = db.CouncilMembers
                        .FirstOrDefault(cm => cm.Member_Id == memberId && cm.Council_Id == councilId);
                    if (member != null)
                    {
                        councilMembers.Add(member);
                    }

                    // Prepare PanelMember object
                    var PM = new PanelMembers
                    {
                        Member_Id = member.Member_Id,
                        Panel_Id = newPanel.id,
                        role_id = memberDto.Role
                    };
                    panelMembers.Add(PM);
                }

                // Add all PanelMembers to the database at once
                db.PanelMembers.AddRange(panelMembers);


                if (councilMembers == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Members Found for Panel Selection.");
                }

                foreach (var member in councilMembers)
                {
                    member.Panel_Id = newPanel.id; // Assign Panel ID to CouncilMember
                }

                // Assign the candidate's panel ID to the council member
                var councilMember = db.CouncilMembers.FirstOrDefault(cm => cm.Member_Id == candidateId && cm.Council_Id == councilId);
                if (councilMember != null)
                {
                    councilMember.Panel_Id = newPanel.id; // Assign the new panel ID to the council member
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Council member not found.");
                }

                // Create notifications for panel members
                var notifications = new List<Notifications>();
                foreach (var member in councilMembers)
                {
                    notifications.Add(new Notifications
                    {
                        //council_id = councilId,
                        member_id = member.Member_Id,
                        title = "You've been Nominated as a Panel Member",
                        message = "Nominations are being made for Committee Selection, and you've been Nominated.",
                        module = "Election",
                        is_read = false,
                        created_at = DateTime.Now,
                    });
                }
                db.Notifications.AddRange(notifications);

                // Nominate the candidate and assign to panel

                Candidates newCandidate = new Candidates
                {
                    member_id = candidateId,
                    council_id = councilId,
                    panel_id = newPanel.id,
                    created_at = DateTime.Now
                };
                db.Candidates.Add(newCandidate);

                var notificationForCandidate = new Notifications 
                {
                    //council_id = councilId,
                    member_id = candidateId,
                    title = "You've been Nominated as a Panel Chairman",
                    message = "Nominations are being made for Committee Selection, and you've been Nominated.",
                    module = "Election",
                    is_read = false,
                    created_at = DateTime.Now,
                };
                db.Notifications.Add(notificationForCandidate);
                // Save all changes at once
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Candidate successfully nominated, panel created, and members added to panel.");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An internal server error occurred: " + ex.Message);
            }
        }
        public class NominateCandidateRequest
        {
            public int CandidateId { get; set; }
            public List<PanelMemberDto> PanelMembers { get; set; }
            public string PanelName { get; set; }
            public int CouncilId { get; set; }
            public int MemberId { get; set; }
        }
        public class PanelMemberDto
        {
            public int MemberId { get; set; }
            public int Role { get; set; }
        }



        [HttpPost]
        public HttpResponseMessage RemoveCandidateForPanel([FromBody] RemoveCandidateRequest request)
        {
            try
            {
                // Extract values from the request object
                int candidateId = request.CandidateId;
                int councilId = request.CouncilId;

                // Find the candidate
                var candidate = db.Candidates.FirstOrDefault(c => c.member_id == candidateId && c.council_id == councilId);
                if (candidate == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Candidate not found for the specified council.");
                }

                // Find the panel associated with the candidate
                var panel = db.Panel.FirstOrDefault(p => p.Candidate_Id == candidateId && p.council_Id == councilId);
                if (panel == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Panel not found for the candidate.");
                }

                // Remove all panel members associated with the panel
                var panelMembers = db.PanelMembers.Where(pm => pm.Panel_Id == panel.id).ToList();
                db.PanelMembers.RemoveRange(panelMembers);

                // Reset Panel_Id for all council members
                var councilMembers = db.CouncilMembers.Where(cm => cm.Panel_Id == panel.id).ToList();
                foreach (var member in councilMembers)
                {
                    member.Panel_Id = 0;
                }

                // Remove the panel itself
                db.Panel.Remove(panel);

                // Remove the candidate entry
                db.Candidates.Remove(candidate);

                // Save changes
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Candidate and associated panel successfully removed.");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An internal server error occurred: " + ex.Message);
            }
        }
        public class RemoveCandidateRequest
        {
            public int CandidateId { get; set; }
            public int CouncilId { get; set; }
        }

        [HttpGet]
        public HttpResponseMessage ShowNominationByMember(int memberId, int councilId)
        {
            try
            {
                var nomination = db.Panel.Where(p => p.created_by == memberId && p.council_Id == councilId).
                    Select(c => new
                    {
                        CandidateId = c.Candidate_Id,
                        CandidateName = db.Member.FirstOrDefault(m => m.id == c.Candidate_Id).Full_Name.ToString(),
                        PanelName = db.Panel.FirstOrDefault(p => p.id == c.id).Name,
                        PanelMembers = db.PanelMembers
                            .Where(pm => pm.Panel_Id == c.id)
                            .Select(pm => new
                            {
                                MemberId = pm.Member_Id,
                                MemberName = db.Member.FirstOrDefault(cm => cm.id == pm.Member_Id).Full_Name,
                                MemberRole = db.Role.FirstOrDefault(r => r.id == pm.role_id).Role_Name
                            }).ToList()
                      
                    }).ToList();

                if(nomination == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Nomination Found");
                }
               return Request.CreateResponse(HttpStatusCode.OK, nomination);
            }
            catch(Exception ee)
            {
               return Request.CreateResponse(HttpStatusCode.InternalServerError, ee);
            }
        }

        [HttpGet]
        public HttpResponseMessage ShowCandidates(int councilId)
        {
            try
            {
                // Fetch all candidates for the specified council
                var candidates = db.Candidates
                    .Where(c => c.council_id == councilId)
                    .Select(c => new
                    {
                        CandidateId = c.member_id,
                        CandidateName = db.Member.FirstOrDefault(m => m.id == c.member_id).Full_Name.ToString(),
                        PanelName = db.Panel.FirstOrDefault(p => p.id == c.panel_id).Name,
                        PanelMembers = db.PanelMembers
                            .Where(pm => pm.Panel_Id == c.panel_id)
                            .Select(pm => new
                            {
                                MemberId = pm.Member_Id,
                                MemberName = db.Member.FirstOrDefault(cm => cm.id == pm.Member_Id).Full_Name,
                                MemberRole = db.Role.FirstOrDefault(r => r.id == pm.role_id).Role_Name
                            }).ToList()
                    }).ToList();

                if (candidates == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No candidates found for the specified council.");
                }

                return Request.CreateResponse(HttpStatusCode.OK, candidates);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An internal server error occurred: " + ex.Message);
            }
        }



        [HttpGet]
        public HttpResponseMessage GetSelectedCandidates(int councilId)
        {
            try
            {
                var result = (from co in db.Council
                              join p in db.Panel on co.id equals p.council_Id
                              join c in db.Candidates on p.id equals c.panel_id
                              join m in db.Member on c.member_id equals m.id
                              where co.id == councilId
                              select new
                              {
                                  MemberId = m.id,
                                  MemberName = m.Full_Name,
                                  PanelName = p.Name
                              }).ToList();
                
                if (result == null )
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Panel Found");
                }


                return Request.CreateResponse(HttpStatusCode.OK, result);

            }
            catch (Exception ee)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ee);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetElectionWithNomination(int councilId)
        {
            try
            {
                
                var result = (from co in db.Council
                              join e in db.Elections on co.id equals e.Council_id
                              join p in db.Panel on co.id equals p.council_Id
                              join c in db.Candidates on p.id equals c.panel_id
                              join m in db.Member on c.member_id equals m.id
                              where co.id == councilId && e.status == "Initiated" || e.status == "Active"
                              select new
                              {
                                  ElectionId = e.id,
                                  ElectionName = e.Name,
                                  Status = e.status,
                                  StartDate = e.StartDate,
                                  EndDate = e.EndDate,
                                  /*CouncilId = co.Council_id,*/
                                  PanelId = p.id,
                                  PanelName = p.Name,
                                  CandidateId = p.Candidate_Id,
                                  MemberId = m.id,
                                  MemberName = m.Full_Name,
                                  PanelMembers = db.PanelMembers
                                    .Where(pm => pm.Panel_Id == c.panel_id)
                                    .Select(pm => new
                                    {
                                        MemberId = pm.Member_Id,
                                        MemberName = db.Member.FirstOrDefault(cm => cm.id == pm.Member_Id).Full_Name,
                                        MemberRole = db.Role.FirstOrDefault(r => r.id == pm.role_id).Role_Name
                                    }).ToList()
                              }).ToList();

                if (result == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Election Data Found");
                }

                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ee)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ee.Message);
            }
        }

        [HttpPost]
        public HttpResponseMessage StartElection(int electionId)
        {
            try
            {
                var election = db.Elections.Find(electionId);
                if (election == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Election not found");

                election.status = "Active";
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Election started successfully");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        /// <summary>
        /// Closing Election API to close election and assign desired role
        /// </summary>
        /// <param name="electionId" name ='councilId'></param>
        /// <returns></returns>
        [HttpPost]
        public HttpResponseMessage CloseElection(int electionId, int councilId)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // Step 1: Validate the election
                    var election = db.Elections.Find(electionId);
                    if (election == null)
                        return Request.CreateResponse(HttpStatusCode.NotFound, "Election not found");

                    if (election.status == "Closed")
                        return Request.CreateResponse(HttpStatusCode.BadRequest, "Election is already closed");

                    // Step 2: Retrieve and validate votes with candidates
                    var topCandidatePanel = db.Votes
                          .Where(v => v.Election_id == electionId) // Filter by Election ID
                          .GroupBy(v => v.Candidate_id) // Group votes by Candidate ID
                          .Select(g => new
                          {
                              CandidateId = g.Key,
                              VoteCount = g.Count() // Count the votes
                          })
                          .OrderByDescending(vc => vc.VoteCount) // Sort by VoteCount in descending order
                          .Take(1) // Ensure only the top candidate is selected
                          .Join(db.Candidates,
                                vc => vc.CandidateId,
                                c => c.candidate_id,
                                (vc, c) => new
                                {
                                    CandidateId = vc.CandidateId,
                                    PanelId = c.panel_id,
                                    VoteCount = vc.VoteCount
                                })
                          .FirstOrDefault(); // Fetch the top candidate

                    if (topCandidatePanel == null)
                        return Request.CreateResponse(HttpStatusCode.BadRequest, "No valid candidates or votes found for this election");

                    
                    // Step 3: Get the panel of the winning candidate
                    var panel = db.Panel.FirstOrDefault(t => t.id == topCandidatePanel.PanelId);
                    if (panel == null)
                        return Request.CreateResponse(HttpStatusCode.BadRequest, "Winning candidate's panel not found");

                    var panelId = panel.id;

                    // Step 4: Update roles for panel members and notify them
                    var panelMembers = db.PanelMembers.Where(pm => pm.Panel_Id == panelId).ToList();
                    var councilMembers = db.CouncilMembers.Where(cm => cm.Panel_Id == panelId).ToList();

                    if (!panelMembers.Any())
                        return Request.CreateResponse(HttpStatusCode.BadRequest, "No panel members found for the winning panel");

                    if (!councilMembers.Any())
                        return Request.CreateResponse(HttpStatusCode.BadRequest, "No council members found for the winning panel");

                    var notifications = new List<Notifications>();
                    foreach (var cm in councilMembers)
                    {
                        var matchingPanelMember = panelMembers.FirstOrDefault(pm => pm.Member_Id == cm.Member_Id);
                        if (matchingPanelMember != null)
                        {
                            cm.Role_Id = (int)matchingPanelMember.role_id;

                            // Create a notification for the council member
                            notifications.Add(new Notifications
                            {
                                title = "Congratulations! Your panel has won the election",
                                module = "Election",
                                //council_id = councilId,
                                member_id = cm.Member_Id,
                                message = "You have been assigned a new role in the council.",
                                created_at = DateTime.Now,
                                updated_at = DateTime.Now
                            });
                        }
                    }
                    db.Notifications.AddRange(notifications);

                    // Step 5: Update the winner's role to "Chairman"
                    var winner = db.Candidates.FirstOrDefault(c => c.candidate_id == topCandidatePanel.CandidateId);
                    if (winner == null)
                        return Request.CreateResponse(HttpStatusCode.BadRequest, "Winning candidate not found in Candidates table");

                    var winnerMemberId = winner.member_id;

                    var notifyForChairman = new Notifications
                    {
                        title = "You are now the Chairman of the Council",
                        message = "You are now responsible for major tasks in the council",
                        module = "Election",
                        member_id = winnerMemberId,
                        //council_id = councilId,
                        created_at = DateTime.Now,
                        updated_at = DateTime.Now
                    };
                    db.Notifications.Add(notifyForChairman);


                    // Step 6: Find and assign the Chairman role
                    var chairmanRole = db.Role.FirstOrDefault(r => r.Role_Name == "Chairperson");
                    if (chairmanRole == null)
                        return Request.CreateResponse(HttpStatusCode.InternalServerError, "Chairman role not found in roles table");

                    var winnerCouncilMember = db.CouncilMembers
                        .FirstOrDefault(cm => cm.Member_Id == winnerMemberId && cm.Council_Id == councilId);
                    if (winnerCouncilMember == null)
                        return Request.CreateResponse(HttpStatusCode.InternalServerError, "Winning candidate not found in CouncilMembers table");

                    winnerCouncilMember.Role_Id = chairmanRole.id;

                    // Step 7: Revert all Panel_Id values in CouncilMembers to 0
                    var allCouncilMembers = db.CouncilMembers.Where(c => c.Council_Id == councilId).ToList();
                    foreach (var cm in allCouncilMembers)
                    {
                        cm.Panel_Id = 0;
                    }

                    // Step 8: Close the election
                    election.status = "Closed";

                    //Step 9: Delete current Candidates.
                    var electionCandidate = db.Candidates.Where(c => c.council_id == councilId).ToList();
                    db.Candidates.RemoveRange(electionCandidate);
                    //Step 10: Delete Panel ref to CouncilId
                    var electionPanel = db.Panel.Where(p => p.council_Id == councilId).ToList();
                    db.Panel.RemoveRange(electionPanel);
                    //Step 11: Delete Panel Member ref to Panel
                    foreach (var panelMember in electionPanel)
                    {
                        var panelMember1 = db.PanelMembers.Where(pm => pm.Panel_Id == panelMember.id);
                        db.PanelMembers.RemoveRange(panelMember1);
                    }

                    db.SaveChanges();

                    transaction.Commit();
                    // Step 12: Return winner details
                    var result = new
                    {
                        WinnerId = winnerMemberId,
                        WinnerName = db.Member.Find(winnerMemberId)?.Full_Name ?? "Unknown",
                        Role = chairmanRole.Role_Name,
                        TotalVotes = topCandidatePanel.VoteCount
                    };
                    return Request.CreateResponse(HttpStatusCode.OK, result);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, $"An error occurred: {ex.Message}");
                }
            }
        }

        [HttpGet]
        public HttpResponseMessage GetElectionWithVotes(int councilId)
        {
            try
            {
                var election = db.Elections
                    .Where(e => e.Council_id == councilId && e.status == "Active" || e.status == "Initiated")
                    .Select(e => new
                    {
                        ElectionId = e.id,
                        ElectionName = e.Name,
                        Status = e.status,
                        StartDate = e.StartDate,
                        EndDate = e.EndDate,
                        Candidates = db.Candidates
                            .Where(c => c.council_id == councilId)
                            .Select(c => new
                            {
                                CandidateId = c.candidate_id,
                                CandidateName = db.Member
                                                .Where(m => m.id == c.member_id)
                                                .Select(m => m.Full_Name)
                                                .FirstOrDefault(),
                                VoteCount = db.Votes
                                                .Where(v => v.Candidate_id == c.candidate_id)
                                                .Count()
                            }).ToList()
                    }).FirstOrDefault();

                if (election == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Election not found");
                }

                return Request.CreateResponse(HttpStatusCode.OK, election);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetElectionForVotes(int councilId)
        {
            try
            {
                var election = db.Elections
                    .Where(e => e.Council_id == councilId && e.status == "Active" || e.status == "Initiated")
                    .Select(e => new
                    {
                        ElectionId = e.id,
                        ElectionName = e.Name,
                        Status = e.status,
                        Candidates = db.Candidates
                            .Where(c => c.council_id == councilId)
                            .Select(c => new
                            {
                                CandidateId = c.candidate_id,
                                CandidateName = db.Member
                                                .Where(m => m.id == c.member_id)
                                                .Select(m => m.Full_Name)
                                                .FirstOrDefault()
                            }).ToList()
                    }).FirstOrDefault();

                if (election == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Election not found");
                }

                return Request.CreateResponse(HttpStatusCode.OK, election);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }


        [HttpPost]
        public HttpResponseMessage CastVote(Votes model)
        {
            try
            {
                // Check if the election is active
                var election = db.Elections.FirstOrDefault(e => e.id == model.Election_id && e.status == "Active");
                if (election == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Election is not active or doesn't exist.");
                }

                // Check if voter has already voted in this election
                var existingVote = db.Votes.FirstOrDefault(v => v.Voter_id == model.Voter_id && v.Election_id == model.Election_id);
                if (existingVote != null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "You have already voted in this election.");
                }

                // Record the vote
                var newVote = new Votes
                {
                    Voter_id = model.Voter_id,
                    Candidate_id = model.Candidate_id,
                    Election_id = model.Election_id,
                    Vote_date = DateTime.Now
                };

                db.Votes.Add(newVote);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.Created, "Vote cast successfully.");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred while casting the vote: " + ex.Message);
            }
        }

    }
}