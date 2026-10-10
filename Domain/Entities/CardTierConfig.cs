using Domain.Common;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class CardTierConfig : BaseEntity
    {
        public CardTier Tier { get; set; }
        public decimal IssueFee { get; set; }
        public decimal CashbackPercent { get; set; }
        public decimal TransferLimit { get; set; }
        public decimal CommissionPercent { get; set; }
        public int CardDesignId { get; set; }
        public CardDesign CardDesign { get; set; } = null!;
    }
}
