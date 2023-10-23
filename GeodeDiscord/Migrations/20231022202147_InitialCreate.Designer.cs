﻿// <auto-generated />
using System;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace GeodeDiscord.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20231022202147_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.12");

            modelBuilder.Entity("GeodeDiscord.Database.Entities.Quote", b =>
                {
                    b.Property<string>("name")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("authorId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("channelId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("content")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("createdAt")
                        .HasColumnType("TEXT");

                    b.Property<int>("extraAttachments")
                        .HasColumnType("INTEGER");

                    b.Property<string>("images")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("jumpUrl")
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("lastEditedAt")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("messageId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("replyAuthorId")
                        .HasColumnType("INTEGER");

                    b.HasKey("name");

                    b.ToTable("quotes");
                });
#pragma warning restore 612, 618
        }
    }
}
