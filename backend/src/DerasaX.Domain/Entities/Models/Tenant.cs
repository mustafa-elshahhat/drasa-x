using DerasaX.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DerasaX.Domain.Entities.Models
{
    public class Tenant
    {
        public string Id { get; set; } 
        public string Name { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public SubscriptionPlan SubscriptionPlan { get; set; }
        public string? LogoUrl { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public CurriculumType Type { get; set; }

        /// <summary>
        /// Tenant lifecycle state. Only <see cref="TenantStatus.Active"/> tenants may
        /// authenticate or have members access the application (Phase 3 tenant gate).
        /// </summary>
        public TenantStatus Status { get; set; } = TenantStatus.Active;
    }
}
