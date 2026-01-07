-- SQL Script to Add AssignedPackageId Column to Members Table
-- Run this script against your Neon PostgreSQL database

-- Add the AssignedPackageId column
ALTER TABLE "Members" 
ADD COLUMN IF NOT EXISTS "AssignedPackageId" INTEGER NULL;

-- Add foreign key constraint
ALTER TABLE "Members"
ADD CONSTRAINT "FK_Members_MembershipPackages_AssignedPackageId"
FOREIGN KEY ("AssignedPackageId") 
REFERENCES "MembershipPackages"("PackageId")
ON DELETE SET NULL;

-- Create index for better query performance
CREATE INDEX IF NOT EXISTS "IX_Members_AssignedPackageId" 
ON "Members"("AssignedPackageId");

-- Verify the column was added
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'Members' 
AND column_name = 'AssignedPackageId';
