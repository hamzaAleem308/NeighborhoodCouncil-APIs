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
    
    public partial class Project_log
    {
        public int id { get; set; }
        public string Action_Taken { get; set; }
        public Nullable<System.DateTime> Action_Date { get; set; }
        public string Status { get; set; }
        public string Comments { get; set; }
        public Nullable<int> AmountCollected { get; set; }
        public string Feedback { get; set; }
        public Nullable<int> Project_id { get; set; }
    }
}