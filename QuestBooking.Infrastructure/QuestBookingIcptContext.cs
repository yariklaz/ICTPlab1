using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using QuestBooking.Domain.Model;

namespace QuestBooking.Infrastructure;
public partial class QuestBookingIcptContext : DbContext
{
    public QuestBookingIcptContext()
    {
    }

    public QuestBookingIcptContext(DbContextOptions<QuestBookingIcptContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<Extraservice> Extraservices { get; set; }

    public virtual DbSet<Promocode> Promocodes { get; set; }

    public virtual DbSet<Questroom> Questrooms { get; set; }

    public virtual DbSet<Timeslot> Timeslots { get; set; }

    public DbSet<AppUser> AppUsers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=QuestBookingICPT;Username=postgres;Password=+L4679013y");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("bookings_pkey");

            entity.ToTable("bookings");

            entity.HasIndex(e => e.PromocodeId, "bookings_promocode_id_key").IsUnique();

            entity.HasIndex(e => e.SlotId, "bookings_slot_id_key").IsUnique();

            entity.HasIndex(e => e.PromocodeId, "unique_promocode").IsUnique();

            entity.HasIndex(e => e.SlotId, "unique_slot").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.PromocodeId).HasColumnName("promocode_id");
            entity.Property(e => e.SlotId).HasColumnName("slot_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Active'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.TotalPrice)
                .HasPrecision(10, 2)
                .HasColumnName("total_price");

            entity.HasOne(d => d.Client).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("bookings_client_id_fkey");

            entity.HasOne(d => d.Promocode).WithOne(p => p.Booking)
                .HasForeignKey<Booking>(d => d.PromocodeId)
                .HasConstraintName("bookings_promocode_id_fkey");

            entity.HasOne(d => d.Slot).WithOne(p => p.Booking)
                .HasForeignKey<Booking>(d => d.SlotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("bookings_slot_id_fkey");

            entity.HasMany(d => d.Services).WithMany(p => p.Bookings)
                .UsingEntity<Dictionary<string, object>>(
                    "Bookingservice",
                    r => r.HasOne<Extraservice>().WithMany()
                        .HasForeignKey("ServiceId")
                        .HasConstraintName("bookingservices_service_id_fkey"),
                    l => l.HasOne<Booking>().WithMany()
                        .HasForeignKey("BookingId")
                        .HasConstraintName("bookingservices_booking_id_fkey"),
                    j =>
                    {
                        j.HasKey("BookingId", "ServiceId").HasName("bookingservices_pkey");
                        j.ToTable("bookingservices");
                        j.IndexerProperty<int>("BookingId").HasColumnName("booking_id");
                        j.IndexerProperty<int>("ServiceId").HasColumnName("service_id");
                    });
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("clients_pkey");

            entity.ToTable("clients");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("full_name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
        });

        modelBuilder.Entity<Extraservice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("extraservices_pkey");

            entity.ToTable("extraservices");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
            entity.Property(e => e.ServiceName)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("service_name");
        });

        modelBuilder.Entity<Promocode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("promocodes_pkey");

            entity.ToTable("promocodes");

            entity.HasIndex(e => e.Code, "promocodes_code_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("code");
            entity.Property(e => e.DiscountPercent).HasColumnName("discount_percent");
            entity.Property(e => e.ValidFrom)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("valid_from");
            entity.Property(e => e.ValidTo)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("valid_to");
        });

        modelBuilder.Entity<Questroom>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("questrooms_pkey");

            entity.ToTable("questrooms");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BasePrice)
                .HasPrecision(10, 2)
                .HasColumnName("base_price");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DurationMinutes)
                .HasDefaultValue(60)
                .HasColumnName("duration_minutes");
            entity.Property(e => e.MaxPlayers).HasColumnName("max_players");
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnName("title");
        });

        modelBuilder.Entity<Timeslot>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("timeslots_pkey");

            entity.ToTable("timeslots");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IsAvailable)
                .HasDefaultValue(true)
                .HasColumnName("is_available");
            entity.Property(e => e.RoomId).HasColumnName("room_id");
            entity.Property(e => e.StartTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("start_time");

            entity.HasOne(d => d.Room).WithMany(p => p.Timeslots)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("timeslots_room_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
