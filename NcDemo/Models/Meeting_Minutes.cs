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
    
    public partial class Meeting_Minutes
    {
        public int id { get; set; }
        public int meeting_id { get; set; }
        public string minutes { get; set; }
        public int recorded_by { get; set; }
        public Nullable<System.DateTime> created_at { get; set; }
    }
}