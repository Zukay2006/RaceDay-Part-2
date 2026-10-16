
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;

namespace RaceDay.API.Data
{
    public class RaceDayContext : DbContext
    {
        public RaceDayContext(DbContextOptions<RaceDayContext> options)
            : base(options)
        {
        }

        public DbSet<Organiser> Organisers => Set<Organiser>();
        public DbSet<Participant> Participants => Set<Participant>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<RaceDay.API.Models.Route> Routes => Set<RaceDay.API.Models.Route>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Enrolment> Enrolments => Set<Enrolment>();
        public DbSet<Result> Results => Set<Result>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Use the same table names as Part 1
            modelBuilder.Entity<Organiser>().ToTable("Organiser");
            modelBuilder.Entity<Participant>().ToTable("Participant");
            modelBuilder.Entity<Event>().ToTable("Event");
            modelBuilder.Entity<RaceDay.API.Models.Route>().ToTable("Route");
            modelBuilder.Entity<Category>().ToTable("Category");
            modelBuilder.Entity<Enrolment>().ToTable("Enrolment");
            modelBuilder.Entity<Result>().ToTable("Result");

            // Each email address must be unique
            modelBuilder.Entity<Organiser>()
                .HasIndex(o => o.Email).IsUnique();

            modelBuilder.Entity<Participant>()
                .HasIndex(p => p.Email).IsUnique();

            // One organiser can create many events
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Organiser)
                .WithMany()
                .HasForeignKey(e => e.OrganiserID);

            // One event can have many routes
            modelBuilder.Entity<RaceDay.API.Models.Route>()
                .HasOne(r => r.Event)
                .WithMany()
                .HasForeignKey(r => r.EventID);

            // One event can have many categories
            modelBuilder.Entity<Category>()
                .HasOne(c => c.Event)
                .WithMany()
                .HasForeignKey(c => c.EventID);

            // One participant can have many enrolments
            modelBuilder.Entity<Enrolment>()
                .HasOne(e => e.Participant)
                .WithMany()
                .HasForeignKey(e => e.ParticipantID);


            // Each enrolment selects a category
            modelBuilder.Entity<Enrolment>()
                .HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryID);

            // An enrolment can have zero or one result
            modelBuilder.Entity<Enrolment>()
                .HasOne(e => e.Result)
                .WithOne(r => r.Enrolment)
                .HasForeignKey<Result>(r => r.EnrolmentID);


            // Each enrolment selects a category
            modelBuilder.Entity<Enrolment>()
                .HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryID);

            // An enrolment can have zero or one result
            modelBuilder.Entity<Enrolment>()
                .HasOne(e => e.Result)
                .WithOne(r => r.Enrolment)
                .HasForeignKey<Result>(r => r.EnrolmentID);


            // Set precision for distances and entry fees
            modelBuilder.Entity<Category>()
                .Property(c => c.DistanceKM)
                .HasPrecision(6, 2);

            modelBuilder.Entity<Category>()
                .Property(c => c.EntryFee)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Event>()
                .Property(e => e.DistanceKM)
                .HasPrecision(6, 2);

            modelBuilder.Entity<RaceDay.API.Models.Route>()
                .Property(r => r.DistanceKM)
                .HasPrecision(6, 2);
        }
    }
}

