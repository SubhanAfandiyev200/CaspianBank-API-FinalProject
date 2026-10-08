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
        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null!;
        public CardTier Tier { get; set; }
        public int CardDesignId { get; set; }
        public CardDesign CardDesign { get; set; } = null!;
        public string CardNumber { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public decimal Balance { get; set; }
        public bool IsBlocked { get; set; }
        public byte[] RowVersion { get; set; } = null!;
    }
}
