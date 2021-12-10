//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace IhaleProject.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Offer
    {
        public int OfferId { get; set; }
        public int SupplierId { get; set; }
        public int IhaleId { get; set; }
        public string OfferDescription { get; set; }
        public string OfferImage { get; set; }
        public Nullable<double> OfferPrice { get; set; }
        public Nullable<bool> IsActiveOffer { get; set; }
        public string OfferCurrency { get; set; }
        public Nullable<double> OfferDailyCurrency { get; set; }
        public Nullable<System.DateTime> OfferTime { get; set; }
    
        public virtual Ihale Ihale { get; set; }
        public virtual Supplier Supplier { get; set; }
    }
}