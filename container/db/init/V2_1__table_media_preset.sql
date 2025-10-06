-- 04_table_media_preset.sql
-- Opretter tabel til image presets i CMS'et
-- Kræver PostgreSQL

BEGIN;

CREATE TABLE IF NOT EXISTS media_presets (
                                             id          uuid            PRIMARY KEY,
                                             name        varchar(64)     NOT NULL UNIQUE,
    width       integer         NOT NULL DEFAULT 0,
    height      integer         NOT NULL DEFAULT 0,
    types       varchar(128)    NOT NULL DEFAULT 'webp', -- CSV: fx "webp,jpg,png"
    created_at  timestamptz     NOT NULL DEFAULT now(),
    updated_at  timestamptz     NOT NULL DEFAULT now(),

    -- Constraints
    CONSTRAINT chk_media_preset_size_width
    CHECK (width  BETWEEN 0 AND 10000),
    CONSTRAINT chk_media_preset_size_height
    CHECK (height BETWEEN 0 AND 10000),

    -- slug-format: lowercase a-z, 0-9, '-' og '_' (2-64 tegn; skal starte med bogstav/tal)
    CONSTRAINT chk_media_preset_name_slug
    CHECK (
              name ~ '^[a-z0-9][a-z0-9_-]{1,63}$'
          ),

    -- 'new' er reserveret til opret-URL
    CONSTRAINT chk_media_preset_name_not_new
    CHECK (name <> 'new'),

    -- tilladte tegn i types (CSV af lowercase alfanumerisk + komma)
    CONSTRAINT chk_media_preset_types_chars
    CHECK (types <> '' AND types !~ '[^a-z0-9,]')
    );

COMMENT ON TABLE  media_presets IS 'Billed-presets (navn/slug, width/height, tilladte output-typer)';
COMMENT ON COLUMN media_presets.types IS 'CSV af tilladte mime-extensions i lowercase (fx "webp,jpg")';

-- Trigger til automatisk at sætte updated_at ved UPDATE
CREATE OR REPLACE FUNCTION trg_set_updated_at()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  NEW.updated_at := now();
RETURN NEW;
END $$;

DROP TRIGGER IF EXISTS set_updated_at_on_media_presets ON media_presets;
CREATE TRIGGER set_updated_at_on_media_presets
    BEFORE UPDATE ON media_presets
    FOR EACH ROW EXECUTE FUNCTION trg_set_updated_at();

COMMIT;

BEGIN;

CREATE OR REPLACE FUNCTION gcd_int(a integer, b integer)
    RETURNS integer
    LANGUAGE sql
    IMMUTABLE
    RETURNS NULL ON NULL INPUT
AS $$
WITH RECURSIVE t(x,y) AS (
    SELECT abs(a), abs(b)
    UNION ALL
    SELECT y, x % y FROM t WHERE y <> 0
)
SELECT COALESCE(NULLIF(x,0), 1) FROM t WHERE y = 0 LIMIT 1
$$;

-- Tilføj normaliserede ratiofelter
ALTER TABLE media_presets
    ADD COLUMN IF NOT EXISTS ratio_w integer
        GENERATED ALWAYS AS (
            CASE WHEN width  > 0 AND height > 0 THEN width  / gcd_int(width, height) ELSE 0 END
            ) STORED,
    ADD COLUMN IF NOT EXISTS ratio_h integer
        GENERATED ALWAYS AS (
            CASE WHEN width  > 0 AND height > 0 THEN height / gcd_int(width, height) ELSE 0 END
            ) STORED,
    ADD COLUMN IF NOT EXISTS ratio_key text
        GENERATED ALWAYS AS (
            CASE WHEN width > 0 AND height > 0
                     THEN (width  / gcd_int(width, height))::text
                              || ':' ||
                          (height / gcd_int(width, height))::text
                 ELSE 'free' END
            ) STORED;

-- Ny indeks for hurtig gruppering/søgning på ratio
CREATE INDEX IF NOT EXISTS idx_media_presets_ratio_key ON media_presets(ratio_key);

COMMIT;