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
    
    public partial class Meetings
    {
        public int id { get; set; }
        public int council_id { get; set; }
        public string title { get; set; }
        public Nullable<int> problem_id { get; set; }
        public Nullable<int> project_id { get; set; }
        public string description { get; set; }
        public string address { get; set; }
        public System.DateTime scheduled_date { get; set; }
        public Nullable<System.DateTime> created_at { get; set; }
        public string meeting_type { get; set; }
    }
}
