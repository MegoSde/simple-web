-- 01_extensions.sql
-- Runs automatically on first init of the ${POSTGRES_DB} database.
-- Enable useful extensions for UUIDs and crypto helpers.
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS pgcrypto;
