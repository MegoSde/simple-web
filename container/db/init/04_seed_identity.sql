-- Seed af rolle "admin" og bruger admin@example.com (password: ChangeThis!123)

BEGIN;

-- 1) Sørg for at rollen "admin" findes
DO $$
DECLARE
role_id text;
BEGIN
SELECT "Id" INTO role_id FROM public."AspNetRoles" WHERE "NormalizedName" = 'ADMIN';

IF role_id IS NULL THEN
    role_id := gen_random_uuid()::text;
INSERT INTO public."AspNetRoles" ("Id","Name","NormalizedName","ConcurrencyStamp")
VALUES (role_id, 'admin', 'ADMIN', gen_random_uuid()::text);
END IF;
END
$$;

-- 2) Sørg for at brugeren admin@example.com findes med password ChangeThis!123
-- PasswordHash er en gyldig ASP.NET Identity v3 PBKDF2-SHA256-hash for "ChangeThis!123"
-- (Iterations: 310000, Salt 16 bytes, Subkey 32 bytes)
DO $$
DECLARE
usr_id   text;
  email    text := 'admin@example.com';
  nemail   text := 'ADMIN@EXAMPLE.COM';
  nuname   text := 'ADMIN@EXAMPLE.COM';
  pw_hash  text := 'AQEAAADwugQAEAAAAH86Kxxdbn+AkaKzxNXm9wggAAAACNzDh5OG0kd0cFHATv8cqNe9X6GqeOLNVjI6+61PRpE=';
BEGIN
SELECT "Id" INTO usr_id FROM public."AspNetUsers" WHERE "NormalizedEmail" = nemail;

IF usr_id IS NULL THEN
    usr_id := gen_random_uuid()::text;
INSERT INTO public."AspNetUsers" (
    "Id","UserName","NormalizedUserName","Email","NormalizedEmail",
    "EmailConfirmed","PasswordHash","SecurityStamp","ConcurrencyStamp",
    "PhoneNumber","PhoneNumberConfirmed","TwoFactorEnabled",
    "LockoutEnd","LockoutEnabled","AccessFailedCount"
) VALUES (
             usr_id, email, nuname, email, nemail,
             TRUE, pw_hash, gen_random_uuid()::text, gen_random_uuid()::text,
             NULL, FALSE, FALSE,
             NULL, FALSE, 0
         );
END IF;
END
$$;

-- 3) Tilføj admin-brugeren til rollen "admin"
DO $$
DECLARE
role_id text;
  usr_id  text;
BEGIN
SELECT "Id" INTO role_id FROM public."AspNetRoles" WHERE "NormalizedName" = 'ADMIN';
SELECT "Id" INTO usr_id  FROM public."AspNetUsers" WHERE "NormalizedEmail" = 'ADMIN@EXAMPLE.COM';

IF role_id IS NOT NULL AND usr_id IS NOT NULL THEN
    -- indsæt kun hvis relationen ikke allerede findes
    IF NOT EXISTS (
      SELECT 1 FROM public."AspNetUserRoles"
      WHERE "UserId" = usr_id AND "RoleId" = role_id
    ) THEN
      INSERT INTO public."AspNetUserRoles" ("UserId","RoleId") VALUES (usr_id, role_id);
END IF;
END IF;
END
$$;

COMMIT;
