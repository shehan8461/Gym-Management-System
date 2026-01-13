-- Migration: Add fingerprintenrollmenthistory table
-- This script is for the Neon PostgreSQL database used by GymDbContext.
-- Run once (e.g. from pgAdmin, psql, or your Neon SQL console) before using
-- the new fingerprint history features.

BEGIN;

-- 1) Create table if it does not already exist
CREATE TABLE IF NOT EXISTS fingerprintenrollmenthistory
(
    id                   SERIAL PRIMARY KEY,
    memberid             INTEGER      NOT NULL,
    deviceid             INTEGER      NOT NULL,
    enrollmenttimeutc    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    status               VARCHAR(100) NOT NULL,
    issuccess            BOOLEAN      NOT NULL DEFAULT FALSE,
    message              VARCHAR(1000)
);

-- 2) Add indexes (if they don't already exist)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE c.relname = 'ix_fingerprinthistory_memberid'
          AND n.nspname = 'public'
    ) THEN
        CREATE INDEX ix_fingerprinthistory_memberid
            ON fingerprintenrollmenthistory (memberid);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        WHERE c.relname = 'ix_fingerprinthistory_enrollmenttimeutc'
          AND n.nspname = 'public'
    ) THEN
        CREATE INDEX ix_fingerprinthistory_enrollmenttimeutc
            ON fingerprintenrollmenthistory (enrollmenttimeutc);
    END IF;
END $$;

-- 3) Optional: add foreign keys to members and biometricdevices for data integrity
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'fk_fingerprinthistory_members'
          AND table_name = 'fingerprintenrollmenthistory'
    ) THEN
        ALTER TABLE fingerprintenrollmenthistory
            ADD CONSTRAINT fk_fingerprinthistory_members
            FOREIGN KEY (memberid) REFERENCES members(memberid)
            ON DELETE RESTRICT;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'fk_fingerprinthistory_devices'
          AND table_name = 'fingerprintenrollmenthistory'
    ) THEN
        ALTER TABLE fingerprintenrollmenthistory
            ADD CONSTRAINT fk_fingerprinthistory_devices
            FOREIGN KEY (deviceid) REFERENCES biometricdevices(deviceid)
            ON DELETE RESTRICT;
    END IF;
END $$;

COMMIT;

