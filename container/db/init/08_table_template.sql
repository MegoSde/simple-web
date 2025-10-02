create table IF NOT EXISTS templates (
                           id          uuid primary key default gen_random_uuid(),
                           name        text not null unique check (length(name) <= 120),
                           version     int  not null default 1,
                           root        jsonb not null default '{}', -- { type, v, props, children|slots 
                           created_at  timestamptz not null default now(),
                           updated_at  timestamptz not null default now(),

    -- slug-format: lowercase a-z, 0-9, '-' og '_' (2-64 tegn; skal starte med bogstav/tal)
                           CONSTRAINT chk_templates_name_slug
                               CHECK (
                                   name ~ '^[a-z0-9][a-z0-9_-]{1,63}$'
                                   ),

    -- 'new' er reserveret til opret-URL
                           CONSTRAINT chk_templates_name_not_new
                               CHECK (name <> 'new')
);
CREATE INDEX IF NOT EXISTS idx_templates_slug ON templates(name);
