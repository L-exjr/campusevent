START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810170759_AddPaidEventPayments') THEN
    ALTER TABLE "Events" ADD "Currency" character varying(3) NOT NULL DEFAULT 'GHS';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810170759_AddPaidEventPayments') THEN
    ALTER TABLE "Events" ADD "PriceMinor" bigint NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810170759_AddPaidEventPayments') THEN
    ALTER TABLE "EventRegistrations" ADD "PaymentOrderId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810170759_AddPaidEventPayments') THEN
    CREATE TABLE "PaymentOrders" (
        "Id" uuid NOT NULL,
        "EventId" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "AmountMinor" bigint NOT NULL,
        "Currency" character varying(3) NOT NULL,
        "Provider" character varying(30) NOT NULL,
        "ProviderReference" character varying(100) NOT NULL,
        "AuthorizationUrl" character varying(2048),
        "Status" character varying(30) NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "VerifiedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PaymentOrders" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PaymentOrders_Events_EventId" FOREIGN KEY ("EventId") REFERENCES "Events" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_PaymentOrders_Users_StudentId" FOREIGN KEY ("StudentId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810170759_AddPaidEventPayments') THEN
    CREATE TABLE "PaymentWebhookReceipts" (
        "Id" character varying(64) NOT NULL,
        "Provider" character varying(30) NOT NULL,
        "EventType" character varying(100) NOT NULL,
        "ProviderReference" character varying(100),
        "Outcome" character varying(100) NOT NULL,
        "ProcessedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PaymentWebhookReceipts" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810170759_AddPaidEventPayments') THEN
    CREATE UNIQUE INDEX "IX_EventRegistrations_PaymentOrderId" ON "EventRegistrations" ("PaymentOrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810170759_AddPaidEventPayments') THEN
    CREATE INDEX "IX_PaymentOrders_EventId_StudentId_Status" ON "PaymentOrders" ("EventId", "StudentId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810170759_AddPaidEventPayments') THEN
    CREATE UNIQUE INDEX "IX_PaymentOrders_ProviderReference" ON "PaymentOrders" ("ProviderReference");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810170759_AddPaidEventPayments') THEN
    CREATE INDEX "IX_PaymentOrders_Status_ExpiresAt" ON "PaymentOrders" ("Status", "ExpiresAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810170759_AddPaidEventPayments') THEN
    CREATE INDEX "IX_PaymentOrders_StudentId" ON "PaymentOrders" ("StudentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810170759_AddPaidEventPayments') THEN
    CREATE INDEX "IX_PaymentWebhookReceipts_ProcessedAt" ON "PaymentWebhookReceipts" ("ProcessedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810170759_AddPaidEventPayments') THEN
    ALTER TABLE "EventRegistrations" ADD CONSTRAINT "FK_EventRegistrations_PaymentOrders_PaymentOrderId" FOREIGN KEY ("PaymentOrderId") REFERENCES "PaymentOrders" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810170759_AddPaidEventPayments') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260810170759_AddPaidEventPayments', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810175742_AddCertificates') THEN
    ALTER TABLE "EventRegistrations" ADD "CertificateGeneratedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810175742_AddCertificates') THEN
    ALTER TABLE "EventRegistrations" ADD "CertificateObjectKey" character varying(1024);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810175742_AddCertificates') THEN
    ALTER TABLE "EventRegistrations" ADD "CertificateTemplateVersion" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810175742_AddCertificates') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260810175742_AddCertificates', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810182317_AddEventVoting') THEN
    CREATE TABLE "VotingCampaigns" (
        "Id" uuid NOT NULL,
        "EventId" uuid NOT NULL,
        "OpensAt" timestamp with time zone NOT NULL,
        "ClosesAt" timestamp with time zone NOT NULL,
        "IsPublished" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_VotingCampaigns" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_VotingCampaigns_Events_EventId" FOREIGN KEY ("EventId") REFERENCES "Events" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810182317_AddEventVoting') THEN
    CREATE TABLE "VotingWebhookReceipts" (
        "Id" character varying(64) NOT NULL,
        "Provider" character varying(30) NOT NULL,
        "EventType" character varying(100) NOT NULL,
        "ProviderReference" character varying(100),
        "Outcome" character varying(100) NOT NULL,
        "ProcessedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_VotingWebhookReceipts" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810182317_AddEventVoting') THEN
    CREATE TABLE "VotingCategories" (
        "Id" uuid NOT NULL,
        "CampaignId" uuid NOT NULL,
        "Name" character varying(150) NOT NULL,
        "Description" character varying(1000),
        "Mode" character varying(20) NOT NULL,
        "PricePerVoteMinor" bigint NOT NULL,
        "Currency" character varying(3) NOT NULL DEFAULT 'GHS',
        "Position" integer NOT NULL,
        CONSTRAINT "PK_VotingCategories" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_VotingCategories_VotingCampaigns_CampaignId" FOREIGN KEY ("CampaignId") REFERENCES "VotingCampaigns" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810182317_AddEventVoting') THEN
    CREATE TABLE "VotingNominees" (
        "Id" uuid NOT NULL,
        "CategoryId" uuid NOT NULL,
        "Name" character varying(150) NOT NULL,
        "Description" character varying(1000),
        "Position" integer NOT NULL,
        CONSTRAINT "PK_VotingNominees" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_VotingNominees_VotingCategories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "VotingCategories" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810182317_AddEventVoting') THEN
    CREATE TABLE "VotingPaymentOrders" (
        "Id" uuid NOT NULL,
        "CategoryId" uuid NOT NULL,
        "NomineeId" uuid NOT NULL,
        "VoterId" uuid NOT NULL,
        "Quantity" integer NOT NULL,
        "UnitPriceMinor" bigint NOT NULL,
        "AmountMinor" bigint NOT NULL,
        "Currency" character varying(3) NOT NULL,
        "Provider" character varying(30) NOT NULL,
        "ProviderReference" character varying(100) NOT NULL,
        "AuthorizationUrl" character varying(2048),
        "Status" character varying(30) NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "VerifiedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_VotingPaymentOrders" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_VotingPaymentOrders_Users_VoterId" FOREIGN KEY ("VoterId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_VotingPaymentOrders_VotingCategories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "VotingCategories" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_VotingPaymentOrders_VotingNominees_NomineeId" FOREIGN KEY ("NomineeId") REFERENCES "VotingNominees" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810182317_AddEventVoting') THEN
    CREATE TABLE "VoteRecords" (
        "Id" uuid NOT NULL,
        "CategoryId" uuid NOT NULL,
        "NomineeId" uuid NOT NULL,
        "VoterId" uuid NOT NULL,
        "Quantity" integer NOT NULL,
        "VotingPaymentOrderId" uuid,
        "CastAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_VoteRecords" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_VoteRecords_Users_VoterId" FOREIGN KEY ("VoterId") REFERENCES "Users" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_VoteRecords_VotingCategories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "VotingCategories" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_VoteRecords_VotingNominees_NomineeId" FOREIGN KEY ("NomineeId") REFERENCES "VotingNominees" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_VoteRecords_VotingPaymentOrders_VotingPaymentOrderId" FOREIGN KEY ("VotingPaymentOrderId") REFERENCES "VotingPaymentOrders" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810182317_AddEventVoting') THEN
    CREATE INDEX "IX_VoteRecords_CategoryId" ON "VoteRecords" ("CategoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810182317_AddEventVoting') THEN
    CREATE UNIQUE INDEX "IX_VoteRecords_CategoryId_VoterId" ON "VoteRecords" ("CategoryId", "VoterId") WHERE "VotingPaymentOrderId" IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810182317_AddEventVoting') THEN
    CREATE INDEX "IX_VoteRecords_NomineeId_CastAt" ON "VoteRecords" ("NomineeId", "CastAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810182317_AddEventVoting') THEN
    CREATE INDEX "IX_VoteRecords_VoterId" ON "VoteRecords" ("VoterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810182317_AddEventVoting') THEN
    CREATE UNIQUE INDEX "IX_VoteRecords_VotingPaymentOrderId" ON "VoteRecords" ("VotingPaymentOrderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810182317_AddEventVoting') THEN
    CREATE UNIQUE INDEX "IX_VotingCampaigns_EventId" ON "VotingCampaigns" ("EventId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810182317_AddEventVoting') THEN
    CREATE INDEX "IX_VotingCampaigns_IsPublished_OpensAt_ClosesAt" ON "VotingCampaigns" ("IsPublished", "OpensAt", "ClosesAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810182317_AddEventVoting') THEN
    CREATE UNIQUE INDEX "IX_VotingCategories_CampaignId_Position" ON "VotingCategories" ("CampaignId", "Position");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810182317_AddEventVoting') THEN
    CREATE UNIQUE INDEX "IX_VotingNominees_CategoryId_Position" ON "VotingNominees" ("CategoryId", "Position");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810182317_AddEventVoting') THEN
    CREATE INDEX "IX_VotingPaymentOrders_CategoryId_VoterId_Status" ON "VotingPaymentOrders" ("CategoryId", "VoterId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810182317_AddEventVoting') THEN
    CREATE INDEX "IX_VotingPaymentOrders_NomineeId" ON "VotingPaymentOrders" ("NomineeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810182317_AddEventVoting') THEN
    CREATE UNIQUE INDEX "IX_VotingPaymentOrders_ProviderReference" ON "VotingPaymentOrders" ("ProviderReference");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810182317_AddEventVoting') THEN
    CREATE INDEX "IX_VotingPaymentOrders_VoterId" ON "VotingPaymentOrders" ("VoterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810182317_AddEventVoting') THEN
    CREATE INDEX "IX_VotingWebhookReceipts_ProcessedAt" ON "VotingWebhookReceipts" ("ProcessedAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260810182317_AddEventVoting') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260810182317_AddEventVoting', '10.0.7');
    END IF;
END $EF$;
COMMIT;

