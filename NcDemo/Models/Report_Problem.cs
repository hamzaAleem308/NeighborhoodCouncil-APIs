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
    
    public partial class Report_Problem
    {
        public int id { get; set; }
        public string title { get; set; }
        public string Description { get; set; }
        public string VisualEvidence { get; set; }
        public string Status { get; set; }
        public string ProblemType { get; set; }
        public Nullable<int> Member_id { get; set; }
        public System.DateTime Date { get; set; }
    }
}
