using System;
using Dobropay.Payments.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dobropay.Payments.Persistence.Migrations
{
    [DbContext(typeof(PaymentsDbContext))]
    [Migration("20261107210000_Stage2ProvidersAndPaymentMethods")]
    partial class Stage2ProvidersAndPaymentMethods
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Dobropay.Payments.Persistence.CheckoutLink", b =>
                {
                    b.Property<string>("TokenHash")
                        .HasColumnType("text")
                        .HasColumnName("token_hash");

                    b.Property<DateTimeOffset>("ExpiresAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("expires_at");

                    b.Property<Guid>("PaymentId")
                        .HasColumnType("uuid")
                        .HasColumnName("payment_id");

                    b.Property<Guid>("ShopId")
                        .HasColumnType("uuid")
                        .HasColumnName("shop_id");

                    b.HasKey("TokenHash");

                    b.HasIndex("ShopId", "PaymentId")
                        .IsUnique();

                    b.ToTable("checkout_links", (string)null);
                });

            modelBuilder.Entity("Dobropay.Payments.Persistence.IdempotencyRecord", b =>
                {
                    b.Property<Guid>("ShopId")
                        .HasColumnType("uuid")
                        .HasColumnName("shop_id");

                    b.Property<string>("Operation")
                        .HasColumnType("text")
                        .HasColumnName("operation");

                    b.Property<string>("IdempotencyKey")
                        .HasColumnType("text")
                        .HasColumnName("idempotency_key");

                    b.Property<Guid>("PaymentId")
                        .HasColumnType("uuid")
                        .HasColumnName("payment_id");

                    b.Property<string>("RequestHash")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("request_hash");

                    b.Property<Guid?>("ResourceId")
                        .HasColumnType("uuid")
                        .HasColumnName("resource_id");

                    b.HasKey("ShopId", "Operation", "IdempotencyKey");

                    b.HasIndex("ShopId", "PaymentId");

                    b.ToTable("idempotency_records", (string)null);
                });

            modelBuilder.Entity("Dobropay.Payments.Persistence.OutboxEvent", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<string>("Envelope")
                        .IsRequired()
                        .HasColumnType("jsonb")
                        .HasColumnName("envelope");

                    b.Property<DateTimeOffset>("OccurredAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("occurred_at");

                    b.Property<Guid>("PaymentId")
                        .HasColumnType("uuid")
                        .HasColumnName("payment_id");

                    b.Property<bool>("Published")
                        .HasColumnType("boolean")
                        .HasColumnName("published");

                    b.Property<Guid>("ShopId")
                        .HasColumnType("uuid")
                        .HasColumnName("shop_id");

                    b.Property<string>("Topic")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("topic");

                    b.HasKey("Id");

                    b.HasIndex("Published", "OccurredAt");

                    b.ToTable("outbox_events", (string)null);
                });

            modelBuilder.Entity("Dobropay.Payments.Persistence.Payment", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<long>("Amount")
                        .HasColumnType("bigint")
                        .HasColumnName("amount");

                    b.Property<string>("CancellationParty")
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)")
                        .HasColumnName("cancellation_party");

                    b.Property<string>("CancellationReason")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)")
                        .HasColumnName("cancellation_reason");

                    b.Property<bool>("Capture")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true)
                        .HasColumnName("capture");

                    b.Property<long?>("CapturedAmount")
                        .HasColumnType("bigint")
                        .HasColumnName("captured_amount");

                    b.Property<DateTimeOffset?>("CapturedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("captured_at");

                    b.Property<string>("ConfirmationData")
                        .HasColumnType("text")
                        .HasColumnName("confirmation_data");

                    b.Property<string>("ConfirmationType")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)")
                        .HasDefaultValue("redirect")
                        .HasColumnName("confirmation_type");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("now()");

                    b.Property<string>("Currency")
                        .IsRequired()
                        .HasMaxLength(3)
                        .HasColumnType("character varying(3)")
                        .HasColumnName("currency");

                    b.Property<DateTimeOffset>("ExpiresAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("expires_at")
                        .HasDefaultValueSql("now()");

                    b.Property<string>("ExternalOrderId")
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("external_order_id");

                    b.Property<string>("PaymentMethodType")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)")
                        .HasDefaultValue("sandbox")
                        .HasColumnName("payment_method_type");

                    b.Property<long>("RefundedAmount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasDefaultValue(0L)
                        .HasColumnName("refunded_amount");

                    b.Property<bool>("SavePaymentMethod")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(false)
                        .HasColumnName("save_payment_method");

                    b.Property<Guid?>("SavedPaymentMethodId")
                        .HasColumnType("uuid")
                        .HasColumnName("saved_payment_method_id");

                    b.Property<Guid>("ShopId")
                        .HasColumnType("uuid")
                        .HasColumnName("shop_id");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)")
                        .HasColumnName("status");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at")
                        .HasDefaultValueSql("now()");

                    b.HasKey("Id");

                    b.HasIndex("ShopId", "CreatedAt");

                    b.HasIndex("Status", "ExpiresAt");

                    b.ToTable("payments", (string)null);
                });

            modelBuilder.Entity("Dobropay.Payments.Persistence.PaymentAttempt", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<string>("ConfirmationPayload")
                        .HasColumnType("text")
                        .HasColumnName("confirmation_payload");

                    b.Property<int?>("Confirmations")
                        .HasColumnType("integer")
                        .HasColumnName("confirmations");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("now()");

                    b.Property<string>("CryptoAmount")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)")
                        .HasColumnName("crypto_amount");

                    b.Property<string>("CryptoCurrency")
                        .HasMaxLength(16)
                        .HasColumnType("character varying(16)")
                        .HasColumnName("crypto_currency");

                    b.Property<string>("CryptoNetwork")
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)")
                        .HasColumnName("crypto_network");

                    b.Property<string>("DepositAddress")
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)")
                        .HasColumnName("deposit_address");

                    b.Property<decimal?>("ExchangeRate")
                        .HasColumnType("numeric")
                        .HasColumnName("exchange_rate");

                    b.Property<string>("ExternalId")
                        .HasColumnType("text")
                        .HasColumnName("external_id");

                    b.Property<Guid>("OperationKey")
                        .HasColumnType("uuid")
                        .HasColumnName("operation_key");

                    b.Property<string>("PayerBankId")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)")
                        .HasColumnName("payer_bank_id");

                    b.Property<Guid>("PaymentId")
                        .HasColumnType("uuid")
                        .HasColumnName("payment_id");

                    b.Property<string>("PaymentMethodType")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)")
                        .HasDefaultValue("sandbox")
                        .HasColumnName("payment_method_type");

                    b.Property<DateTimeOffset?>("QuoteExpiresAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("quote_expires_at");

                    b.Property<int?>("RequiredConfirmations")
                        .HasColumnType("integer")
                        .HasColumnName("required_confirmations");

                    b.Property<string>("SbpOperationId")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)")
                        .HasColumnName("sbp_operation_id");

                    b.Property<string>("SbpQrcId")
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)")
                        .HasColumnName("sbp_qrc_id");

                    b.Property<Guid>("ShopId")
                        .HasColumnType("uuid")
                        .HasColumnName("shop_id");

                    b.Property<string>("State")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("state");

                    b.Property<string>("TxHash")
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)")
                        .HasColumnName("tx_hash");

                    b.HasKey("Id");

                    b.HasIndex("ExternalId")
                        .IsUnique();

                    b.HasIndex("OperationKey")
                        .IsUnique();

                    b.HasIndex("ShopId", "PaymentId")
                        .IsUnique();

                    b.ToTable("payment_attempts", (string)null);
                });

            modelBuilder.Entity("Dobropay.Payments.Persistence.ProviderInbox", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<int>("Attempts")
                        .HasColumnType("integer")
                        .HasColumnName("attempts");

                    b.Property<string>("Error")
                        .HasColumnType("text")
                        .HasColumnName("error");

                    b.Property<DateTimeOffset>("NextAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("next_at");

                    b.Property<string>("Payload")
                        .IsRequired()
                        .HasColumnType("jsonb")
                        .HasColumnName("payload");

                    b.Property<Guid>("PaymentId")
                        .HasColumnType("uuid")
                        .HasColumnName("payment_id");

                    b.Property<string>("ProviderEventId")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("provider_event_id");

                    b.Property<Guid>("ShopId")
                        .HasColumnType("uuid")
                        .HasColumnName("shop_id");

                    b.Property<string>("State")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("state");

                    b.HasKey("Id");

                    b.HasIndex("ProviderEventId")
                        .IsUnique();

                    b.HasIndex("State", "NextAt");

                    b.ToTable("provider_inbox", (string)null);
                });

            modelBuilder.Entity("Dobropay.Payments.Persistence.Refund", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<long>("Amount")
                        .HasColumnType("bigint")
                        .HasColumnName("amount");

                    b.Property<DateTimeOffset?>("CompletedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("completed_at");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("now()");

                    b.Property<string>("Currency")
                        .IsRequired()
                        .HasMaxLength(3)
                        .HasColumnType("character varying(3)")
                        .HasColumnName("currency");

                    b.Property<string>("ExternalRefundId")
                        .HasColumnType("text")
                        .HasColumnName("external_refund_id");

                    b.Property<Guid>("PaymentId")
                        .HasColumnType("uuid")
                        .HasColumnName("payment_id");

                    b.Property<string>("Reason")
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)")
                        .HasColumnName("reason");

                    b.Property<Guid>("ShopId")
                        .HasColumnType("uuid")
                        .HasColumnName("shop_id");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)")
                        .HasColumnName("status");

                    b.HasKey("Id");

                    b.HasIndex("ShopId", "CreatedAt");

                    b.HasIndex("ShopId", "PaymentId");

                    b.ToTable("refunds", (string)null);
                });

            modelBuilder.Entity("Dobropay.Payments.Persistence.SavedPaymentMethod", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id");

                    b.Property<string>("CardBrand")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)")
                        .HasColumnName("card_brand");

                    b.Property<string>("CardFirst6")
                        .IsRequired()
                        .HasMaxLength(6)
                        .HasColumnType("character varying(6)")
                        .HasColumnName("card_first6");

                    b.Property<string>("CardLast4")
                        .IsRequired()
                        .HasMaxLength(4)
                        .HasColumnType("character varying(4)")
                        .HasColumnName("card_last4");

                    b.Property<int>("ExpiryMonth")
                        .HasColumnType("integer")
                        .HasColumnName("expiry_month");

                    b.Property<int>("ExpiryYear")
                        .HasColumnType("integer")
                        .HasColumnName("expiry_year");

                    b.Property<string>("ProviderToken")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)")
                        .HasColumnName("provider_token");

                    b.Property<DateTimeOffset>("SavedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("saved_at")
                        .HasDefaultValueSql("now()");

                    b.Property<Guid>("ShopId")
                        .HasColumnType("uuid")
                        .HasColumnName("shop_id");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)")
                        .HasColumnName("type");

                    b.HasKey("Id");

                    b.HasAlternateKey("ShopId", "Id");

                    b.HasIndex("ProviderToken")
                        .IsUnique();

                    b.HasIndex("ShopId", "SavedAt");

                    b.ToTable("saved_payment_methods", (string)null);
                });

            modelBuilder.Entity("Dobropay.Payments.Persistence.CheckoutLink", b =>
                {
                    b.HasOne("Dobropay.Payments.Persistence.Payment", null)
                        .WithMany()
                        .HasForeignKey("ShopId", "PaymentId")
                        .HasPrincipalKey("ShopId", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Dobropay.Payments.Persistence.IdempotencyRecord", b =>
                {
                    b.HasOne("Dobropay.Payments.Persistence.Payment", null)
                        .WithMany()
                        .HasForeignKey("ShopId", "PaymentId")
                        .HasPrincipalKey("ShopId", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Dobropay.Payments.Persistence.PaymentAttempt", b =>
                {
                    b.HasOne("Dobropay.Payments.Persistence.Payment", null)
                        .WithMany()
                        .HasForeignKey("ShopId", "PaymentId")
                        .HasPrincipalKey("ShopId", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Dobropay.Payments.Persistence.Refund", b =>
                {
                    b.HasOne("Dobropay.Payments.Persistence.Payment", null)
                        .WithMany()
                        .HasForeignKey("ShopId", "PaymentId")
                        .HasPrincipalKey("ShopId", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
