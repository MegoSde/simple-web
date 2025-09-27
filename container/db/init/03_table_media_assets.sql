-- 03_table_media_assets.sql
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE IF NOT EXISTS media_assets (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    hash TEXT NOT NULL,                               -- lowercase hex (sha256)
    original_url TEXT NOT NULL,                       -- fx https://cdn.example.com/originals/ab/cd/<hash>.jpg
    mime TEXT NOT NULL,
    width INT NOT NULL CHECK (width > 0),
    height INT NOT NULL CHECK (height > 0),
    bytes BIGINT NOT NULL CHECK (bytes > 0),
    alt_text TEXT NULL,
    uploaded_by TEXT NOT NULL,                        -- brugernavn/id (string)
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    meta JSONB NOT NULL DEFAULT '{}'::jsonb
    );

CREATE UNIQUE INDEX IF NOT EXISTS ux_media_assets_hash ON media_assets(hash);
CREATE INDEX IF NOT EXISTS ix_media_assets_created_at ON media_assets(created_at DESC);
