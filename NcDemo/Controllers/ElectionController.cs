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

        [HttpPost]
        public HttpResponseMessage InitiateElection(Elections elect)
        {
            try
            {
                var election = new Elections
                {
                    Name = elect.Name,
                    StartDate = elect.StartDate,
                    EndDate = elect.EndDate,
                    status = "Initiated",
                    Council_id = elect.Council_id,
                };
                db.Elections.Add(election);
                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, "Election initiated successfully.");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        public HttpResponseMessage NominateCandidateForPanel(
            int councilMemberId, 
            int electionId, 
            string panelName, 
            int councilId
            ) {
            try 
            {
                // Check if the council member is already a candidate for this election
                var existingCandidate = db.Candidates.FirstOrDefault(c => c.member_id == councilMemberId && c.election_id == electionId);
                if (existingCandidate != null)
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict, "This member is already nominated as a candidate for this election.");
                }

                // Create a new Panel for the candidate
                Panel newPanel = new Panel
                {
                    Candidate_Id = councilMemberId,  // Candidate leads the panel
                    Name = panelName,
                    Election_Id = electionId
                };

                db.Panel.Add(newPanel);
                db.SaveChanges();

                // Nominate the candidate and assign them to the panel
                Candidates newCandidate = new Candidates
                {
                    member_id = councilMemberId,  // Link to the council member
                    election_id = electionId,     // Link to the election
                    panel_id = newPanel.id,       // Link to the newly created panel
                    created_at = DateTime.Now
                };

                db.Candidates.Add(newCandidate);
                db.SaveChanges();

                // Update the CouncilMembers table to assign the panel ID
                var councilMember = db.CouncilMembers.FirstOrDefault(cm => cm.Member_Id == councilMemberId && cm.Council_Id == councilId);
                if (councilMember != null)
                {
                    councilMember.Panel_Id = newPanel.id; // Assign the new panel ID to the council member
                    db.SaveChanges();
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Council member not found.");
                }

                return Request.CreateResponse(HttpStatusCode.OK, "Candidate successfully nominated, panel created, and member added to panel.");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred: " + ex.Message);
            }
        }

        [HttpDelete]
        public HttpResponseMessage RemoveCandidateFromPanel(
            int councilMemberId, 
            int electionId, 
            int councilId
            ) {
            try
            {
                // Find the candidate in the Candidates table
                var candidate = db.Candidates.FirstOrDefault(c => c.member_id == councilMemberId && c.election_id == electionId);
                if (candidate == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Candidate not found for this election.");
                }

                // Find the corresponding panel associated with the candidate
                var panel = db.Panel.FirstOrDefault(p => p.id == candidate.panel_id);
                if (panel == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Panel not found.");
                }

                // Remove the candidate from the Candidates table
                db.Candidates.Remove(candidate);

                // Remove the panel associated with the candidate
                db.Panel.Remove(panel);
                db.SaveChanges();

                // Revert the CouncilMembers table: Reset the Panel_Id to 0
                var councilMember = db.CouncilMembers.FirstOrDefault(cm => cm.Member_Id == councilMemberId && cm.Council_Id == councilId);
                if (councilMember != null)
                {
                    councilMember.Panel_Id = 0;  // Reset the panel ID to its original state (no panel)
                    db.SaveChanges();
                }

                return Request.CreateResponse(HttpStatusCode.OK, "Candidate successfully removed from nomination and panel.");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred: " + ex.Message);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetSelectedCandidates(int electionId)
        {
            try
            {
                var result = (from e in db.Elections
                              join p in db.Panel on e.id equals p.Election_Id
                              join c in db.Candidates on p.id equals c.panel_id
                              join m in db.Member on c.member_id equals m.id
                              where e.id == electionId
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
                /* var result = (from e in db.Elections
                               join p in db.Panel on e.id equals p.Election_Id
                               where e.Council_id == councilId
                                  select new
                                  {
                                      ElectionId = e.id,
                                      ElectionName = e.Name,
                                      Status = e.status,
                                      StartDate = e.StartDate,
                                      EndDate = e.EndDate,
                                      PanelId = p.id,
                                      PanelName = p.Name,
                                  }).ToList();*/

                var result = (from e in db.Elections
                              join p in db.Panel on e.id equals p.Election_Id
                              join c in db.Candidates on p.id equals c.panel_id
                              join m in db.Member on c.member_id equals m.id
                              where e.Council_id == councilId
                              select new
                              {
                                  ElectionId = e.id,
                                  ElectionName = e.Name,
                                  Status = e.status,
                                  StartDate = e.StartDate,
                                  EndDate = e.EndDate,
                                  CouncilId = e.Council_id,
                                  PanelId = p.id,
                                  PanelName = p.Name,
                                  CandidateId = p.Candidate_Id,
                                  MemberId = m.id,
                                  MemberName = m.Full_Name
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
            try
            {
                var election = db.Elections.Find(electionId);
                if (election == null)
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Election not found");

                if (election.status == "Closed")
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Election is already closed");

                // Retrieve candidates and votes for the election
                var candidates = db.Votes
                    .Where(v => v.Election_id == electionId)
                    .GroupBy(v => v.Candidate_id)
                    .Select(g => new
                    {
                        CandidateId = g.Key,
                        VoteCount = g.Count()
                    })
                    .OrderByDescending(c => c.VoteCount)
                    .ToList();

                if (candidates.Count == 0)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "No votes found for this election");

                // Determine the winner (candidate with the most votes)
                var winningCandidateId = candidates.First().CandidateId;

                // Find winning member in member table
                var memberData = db.Candidates.Find(winningCandidateId);
                var member = memberData.member_id;

                // Fetch winner's member details from Members table
                var winnerMember = db.Member.Find(member);
                if (winnerMember == null)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Winning candidate not found in Members table");
                
                // Fetch ref of member in CM table to update role of winner
                var getWinnerIdFromCM = db.CouncilMembers.FirstOrDefault(cm => cm.Member_Id == member && cm.Council_Id == councilId);
                if(getWinnerIdFromCM == null)
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, "Member not found in council members table");

                // Update the winning candidate's role to "Chairman"
                var chairmanRole = db.Role.FirstOrDefault(r => r.Role_Name == "Chairperson");
                if (chairmanRole == null)
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, "Chairman role not found in roles table");

                getWinnerIdFromCM.Role_Id = chairmanRole.id;

                // Close the election
                election.status = "Closed";
                db.SaveChanges();

                // Return winner details in the response
                var result = new
                {
                    WinnerId = winnerMember.id,
                    WinnerName = winnerMember.Full_Name,
                    Role = chairmanRole.Role_Name,
                    TotalVotes = candidates.First().VoteCount
                };

                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }


        [HttpGet]
        public HttpResponseMessage GetElectionWithVotes(int electionId)
        {
            try
            {
                var election = db.Elections
                    .Where(e => e.id == electionId)
                    .Select(e => new
                    {
                        ElectionId = e.id,
                        ElectionName = e.Name,
                        Status = e.status,
                        Candidates = db.Candidates
                            .Where(c => c.election_id == electionId)
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
                    .Where(e => e.Council_id == councilId)
                    .Select(e => new
                    {
                        ElectionId = e.id,
                        ElectionName = e.Name,
                        Status = e.status,
                        Candidates = db.Candidates
                            .Where(c => c.election_id == e.id)
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