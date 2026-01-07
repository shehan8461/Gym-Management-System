-- Add CustomPackageAmount column to Members table
-- This allows setting custom amounts for package assignments

-- Add the column
ALTER TABLE "Members" 
ADD COLUMN "CustomPackageAmount" NUMERIC(18,2) NULL;

-- Verify the column was added
SELECT column_name, data_type, is_nullable 
FROM information_schema.columns 
WHERE table_name = 'Members' 
AND column_name = 'CustomPackageAmount';
