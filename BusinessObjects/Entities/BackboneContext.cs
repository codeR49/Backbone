﻿using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity.EntityFramework;

namespace forCrowd.Backbone.BusinessObjects.Entities
{
    public class BackboneDbConfiguration : DbConfiguration
    {
        public BackboneDbConfiguration()
        {
            AddInterceptor(new UserCommandTreeInterceptor());
            AddInterceptor(new UserCommandInterceptor());
        }
    }

    public class BackboneContext : IdentityDbContext<User, Role, int, UserLogin, UserRole, UserClaim>
    {
        public BackboneContext() : this("BackboneContext")
        {
        }

        public BackboneContext(string nameOrConnectionString) : base(nameOrConnectionString)
        {
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
        }

        // TODO Is this correct to make DbContext accessible from Web application?
        public static BackboneContext Create()
        {
            return new BackboneContext();
        }

        // These definitions are being used in generating OData metadata / coni2k - 07 Nov '14
        public DbSet<Project> Project { get; set; }
        public DbSet<Element> Element { get; set; }
        public DbSet<ElementField> ElementField { get; set; }
        public DbSet<ElementItem> ElementItem { get; set; }
        public DbSet<ElementCell> ElementCell { get; set; }
        public DbSet<UserElementField> UserElementField { get; set; }
        public DbSet<UserElementCell> UserElementCell { get; set; }
        public DbSet<UserLogin> UserLogin { get; set; }
        public DbSet<UserRole> UserRole { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Conventions
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            var userAwareConvention = new AttributeToTableAnnotationConvention<UserAwareAttribute, string>(
                UserAwareAttribute.UserAnnotation, (type, attributes) => attributes.Single().ColumnName);
            modelBuilder.Conventions.Add(userAwareConvention);

            // Table names
            modelBuilder.Entity<User>().ToTable("User");
            modelBuilder.Entity<UserClaim>().ToTable("UserClaim");
            modelBuilder.Entity<UserLogin>().ToTable("UserLogin");
            modelBuilder.Entity<UserRole>().ToTable("UserRole");
            modelBuilder.Entity<Role>().ToTable("Role");

            // Cascade deletes
            modelBuilder.Entity<ElementCell>()
                .HasRequired(item => item.ElementField)
                .WithMany(item => item.ElementCellSet)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<UserElementField>()
                .HasRequired(item => item.User)
                .WithMany(item => item.UserElementFieldSet)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<UserElementCell>()
                .HasRequired(item => item.User)
                .WithMany(item => item.UserElementCellSet)
                .WillCascadeOnDelete(false);
        }

        public override int SaveChanges()
        {
            ProcessChangeTrackerEntries();

            try
            {
                return base.SaveChanges();
            }
            catch (DbEntityValidationException entityException)
            {
                throw new Exception(CatchDbEntityValidationException(entityException), entityException);
            }
        }

        public override Task<int> SaveChangesAsync()
        {
            ProcessChangeTrackerEntries();

            try
            {
                return base.SaveChangesAsync();
            }
            catch (DbEntityValidationException entityException)
            {
                throw new Exception(CatchDbEntityValidationException(entityException), entityException);
            }
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            ProcessChangeTrackerEntries();

            try
            {
                return base.SaveChangesAsync(cancellationToken);
            }
            catch (DbEntityValidationException entityException)
            {
                throw new Exception(CatchDbEntityValidationException(entityException), entityException);
            }
        }

        private void ProcessChangeTrackerEntries()
        {
            var entries = ChangeTracker.Entries().Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var item in entries)
            {
                var changedOrAddedItem = item.Entity as IEntity;
                if (changedOrAddedItem != null)
                {
                    if (item.State == EntityState.Added)
                    {
                        changedOrAddedItem.CreatedOn = DateTime.UtcNow;
                        changedOrAddedItem.DeletedOn = null;
                    }

                    changedOrAddedItem.ModifiedOn = DateTime.UtcNow;
                }
            }
        }

        private string CatchDbEntityValidationException(DbEntityValidationException entityException)
        {
            var errors = entityException.EntityValidationErrors;
            var result = new StringBuilder();
            foreach (var error in errors)
                foreach (var validationError in error.ValidationErrors)
                    result.AppendFormat("\r\n  Entity of type {0} has validation error \"{1}\" for property {2}.\r\n", error.Entry.Entity.GetType(), validationError.ErrorMessage, validationError.PropertyName);
            return result.ToString();
        }
    }
}
