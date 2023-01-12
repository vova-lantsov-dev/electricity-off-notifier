﻿// <auto-generated />
using System;
using ElectricityOffNotifier.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ElectricityOffNotifier.Data.Migrations
{
    [DbContext(typeof(ElectricityDbContext))]
    [Migration("20230111205002_WebhookModeSupport")]
    partial class WebhookModeSupport
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.12")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("ElectricityOffNotifier.Data.Models.Address", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("BuildingNo")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("building_no");

                    b.Property<int>("CityId")
                        .HasColumnType("integer")
                        .HasColumnName("city_id");

                    b.Property<string>("Street")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("street");

                    b.HasKey("Id");

                    b.HasIndex("CityId");

                    b.ToTable("addresses", "public");
                });

            modelBuilder.Entity("ElectricityOffNotifier.Data.Models.Checker", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("AddressId")
                        .HasColumnType("integer")
                        .HasColumnName("address_id");

                    b.HasKey("Id");

                    b.HasIndex("AddressId")
                        .IsUnique();

                    b.ToTable("checkers", "public");
                });

            modelBuilder.Entity("ElectricityOffNotifier.Data.Models.CheckerEntry", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<int>("CheckerId")
                        .HasColumnType("integer")
                        .HasColumnName("checker_id");

                    b.Property<DateTime>("DateTime")
                        .HasColumnType("timestamp")
                        .HasColumnName("date_time");

                    b.HasKey("Id");

                    b.HasIndex("CheckerId");

                    b.ToTable("checker_entries", "public");
                });

            modelBuilder.Entity("ElectricityOffNotifier.Data.Models.City", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<string>("Region")
                        .HasColumnType("text")
                        .HasColumnName("region");

                    b.HasKey("Id");

                    b.ToTable("cities", "public");
                });

            modelBuilder.Entity("ElectricityOffNotifier.Data.Models.Producer", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<byte[]>("AccessTokenHash")
                        .IsRequired()
                        .HasColumnType("bytea")
                        .HasColumnName("access_token_hash");

                    b.Property<int>("CheckerId")
                        .HasColumnType("integer")
                        .HasColumnName("checker_id");

                    b.Property<bool>("IsEnabled")
                        .HasColumnType("boolean")
                        .HasColumnName("is_enabled");

                    b.Property<int>("Mode")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasDefaultValue(0)
                        .HasColumnName("mode");

                    b.Property<string>("WebhookUrl")
                        .HasColumnType("text")
                        .HasColumnName("webhook_url");

                    b.HasKey("Id");

                    b.HasIndex("CheckerId");

                    b.ToTable("producers", "public");
                });

            modelBuilder.Entity("ElectricityOffNotifier.Data.Models.SentNotification", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("CheckerId")
                        .HasColumnType("integer")
                        .HasColumnName("checker_id");

                    b.Property<DateTime>("DateTime")
                        .HasColumnType("timestamp")
                        .HasColumnName("date_time");

                    b.Property<bool>("IsUpNotification")
                        .HasColumnType("boolean")
                        .HasColumnName("is_up_notification");

                    b.HasKey("Id");

                    b.HasIndex("CheckerId");

                    b.ToTable("sent_notifications", "public");
                });

            modelBuilder.Entity("ElectricityOffNotifier.Data.Models.Subscriber", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("CheckerId")
                        .HasColumnType("integer")
                        .HasColumnName("checker_id");

                    b.Property<string>("Culture")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("culture");

                    b.Property<int>("ProducerId")
                        .HasColumnType("integer")
                        .HasColumnName("producer_id");

                    b.Property<long>("TelegramId")
                        .HasColumnType("bigint")
                        .HasColumnName("telegram_id");

                    b.Property<int?>("TelegramThreadId")
                        .HasColumnType("integer")
                        .HasColumnName("telegram_thread_id");

                    b.Property<string>("TimeZone")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("time_zone");

                    b.HasKey("Id");

                    b.HasIndex("CheckerId");

                    b.HasIndex("ProducerId");

                    b.ToTable("subscribers", "public");
                });

            modelBuilder.Entity("ElectricityOffNotifier.Data.Models.Address", b =>
                {
                    b.HasOne("ElectricityOffNotifier.Data.Models.City", "City")
                        .WithMany("Addresses")
                        .HasForeignKey("CityId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("City");
                });

            modelBuilder.Entity("ElectricityOffNotifier.Data.Models.Checker", b =>
                {
                    b.HasOne("ElectricityOffNotifier.Data.Models.Address", "Address")
                        .WithOne("Checker")
                        .HasForeignKey("ElectricityOffNotifier.Data.Models.Checker", "AddressId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Address");
                });

            modelBuilder.Entity("ElectricityOffNotifier.Data.Models.CheckerEntry", b =>
                {
                    b.HasOne("ElectricityOffNotifier.Data.Models.Checker", "Checker")
                        .WithMany("Entries")
                        .HasForeignKey("CheckerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Checker");
                });

            modelBuilder.Entity("ElectricityOffNotifier.Data.Models.Producer", b =>
                {
                    b.HasOne("ElectricityOffNotifier.Data.Models.Checker", "Checker")
                        .WithMany("Producers")
                        .HasForeignKey("CheckerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Checker");
                });

            modelBuilder.Entity("ElectricityOffNotifier.Data.Models.SentNotification", b =>
                {
                    b.HasOne("ElectricityOffNotifier.Data.Models.Checker", "Checker")
                        .WithMany("SentNotifications")
                        .HasForeignKey("CheckerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Checker");
                });

            modelBuilder.Entity("ElectricityOffNotifier.Data.Models.Subscriber", b =>
                {
                    b.HasOne("ElectricityOffNotifier.Data.Models.Checker", "Checker")
                        .WithMany("Subscribers")
                        .HasForeignKey("CheckerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ElectricityOffNotifier.Data.Models.Producer", "Producer")
                        .WithMany("Subscribers")
                        .HasForeignKey("ProducerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Checker");

                    b.Navigation("Producer");
                });

            modelBuilder.Entity("ElectricityOffNotifier.Data.Models.Address", b =>
                {
                    b.Navigation("Checker")
                        .IsRequired();
                });

            modelBuilder.Entity("ElectricityOffNotifier.Data.Models.Checker", b =>
                {
                    b.Navigation("Entries");

                    b.Navigation("Producers");

                    b.Navigation("SentNotifications");

                    b.Navigation("Subscribers");
                });

            modelBuilder.Entity("ElectricityOffNotifier.Data.Models.City", b =>
                {
                    b.Navigation("Addresses");
                });

            modelBuilder.Entity("ElectricityOffNotifier.Data.Models.Producer", b =>
                {
                    b.Navigation("Subscribers");
                });
#pragma warning restore 612, 618
        }
    }
}