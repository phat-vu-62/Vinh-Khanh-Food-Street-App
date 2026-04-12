-- Run this on your Render PostgreSQL database console to manually ensure the admin user exists.
-- Password is '123456'

INSERT INTO "Users" ("Id", "Username", "PasswordHash", "Role")
SELECT gen_random_uuid(), 'admin', '$2a$11$KAzvVl.eEqXlyvU2X9V0i.XhG8OqXj3Z0uK9S0oZ1.V/U.p5wY0pG', 'Admin'
WHERE NOT EXISTS (
    SELECT 1 FROM "Users" WHERE "Username" = 'admin'
);

-- Ensure the role is capitalized if the user already exists
UPDATE "Users" SET "Role" = 'Admin' WHERE "Username" = 'admin';
