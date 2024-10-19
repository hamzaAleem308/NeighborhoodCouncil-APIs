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
                    status = "Ongoing",
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
        public HttpResponseMessage NominateCandidateForPanel(int councilMemberId, int electionId, string panelName)
        {
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
                    panel_id = newPanel.id        // Link to the newly created panel
                };

                db.Candidates.Add(newCandidate);
                db.SaveChanges();

                // Update the CouncilMembers table to assign the panel ID
                var councilMember = db.CouncilMembers.FirstOrDefault(cm => cm.Member_Id == councilMemberId);
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
        public HttpResponseMessage RemoveCandidateFromPanel(int councilMemberId, int electionId)
        {
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
                var councilMember = db.CouncilMembers.FirstOrDefault(cm => cm.Member_Id == councilMemberId);
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

    }
}