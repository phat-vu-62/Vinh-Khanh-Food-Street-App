-- Fix OwnerId column type in Pois table
ALTER TABLE "Pois" DROP COLUMN IF EXISTS "OwnerId";
ALTER TABLE "Pois" ADD COLUMN "OwnerId" uuid;

-- Optional: Create index for performance
CREATE INDEX IF NOT EXISTS "IX_Pois_OwnerId" ON "Pois" ("OwnerId");
