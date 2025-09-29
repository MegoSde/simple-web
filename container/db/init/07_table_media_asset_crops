BEGIN;

CREATE TABLE IF NOT EXISTS media_asset_crops (
  id           uuid            PRIMARY KEY,
  asset_hash   varchar(64)     NOT NULL,       -- FK logisk til media_assets.hash
  preset_name  varchar(64)     NOT NULL,       -- FK logisk til media_presets.name
  x            double precision NOT NULL,      -- normalized [0..1]
  y            double precision NOT NULL,
  w            double precision NOT NULL,
  h            double precision NOT NULL,
  updated_by   varchar(128)    NOT NULL,
  updated_at   timestamptz     NOT NULL DEFAULT now(),
  CONSTRAINT uq_asset_preset UNIQUE (asset_hash, preset_name),
  CONSTRAINT chk_norm_coords CHECK (
    x >= 0 AND y >= 0 AND w > 0 AND h > 0 AND
    x <= 1 AND y <= 1 AND x + w <= 1 AND y + h <= 1
  )
);

COMMENT ON TABLE media_asset_crops IS 'Crop/clip pr. asset hash pr. preset (normalized rect)';

COMMIT;
