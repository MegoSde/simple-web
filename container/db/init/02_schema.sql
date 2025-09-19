-- 02_schema.sql
-- Create an application schema and roles defaults.
-- NOTE: Tables are created by EF Core migrations in later sprints.
BEGIN;

-- Ensure cmd schema exists; in init phase, CURRENT_USER is the bootstrap user.
CREATE SCHEMA IF NOT EXISTS cms AUTHORIZATION CURRENT_USER;

-- Set default search_path for the app user inside this database.
-- On first boot, ${POSTGRES_USER} is the superuser provided by environment.
-- We guard with DO-block in case ALTER ROLE variant differs.
DO $$
DECLARE
  r text := current_setting('server_version', true);
BEGIN
  EXECUTE format('ALTER ROLE %I IN DATABASE %I SET search_path = cms, public;', current_user, current_database());
END$$;

COMMIT;
