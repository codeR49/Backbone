using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNet.Identity.EntityFramework;

namespace forCrowd.Backbone.BusinessObjects.Entities
{
    public class UserRole : IdentityUserRole<int>, IEntity
    {
        public User User { get; set; }
        public Role Role { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime ModifiedOn { get; set; }

        public DateTime? DeletedOn { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }
    }
}
