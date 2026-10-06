using Domain.Common;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Card : BaseEntity
    {
        public string UserId { get; set; }
        public AppUser User { get; set; }
        public CardTier Tier { get; set; }
        public int CardDesignId { get; set; }
        public CardDesign CardDesign { get; set; }
        public string CardNumber { get; set; }
        public DateTime ExpiryDate { get; set; }
        public decimal Balance { get; set; }
        public bool IsBlocked { get; set; }
        public byte[] RowVersion { get; set; }
    }
}
