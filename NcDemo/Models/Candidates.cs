//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace NcDemo.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Candidates
    {
        public int candidate_id { get; set; }
        public Nullable<int> member_id { get; set; }
        public Nullable<int> election_id { get; set; }
        public Nullable<int> panel_id { get; set; }
        public Nullable<System.DateTime> created_at { get; set; }
    }
}
