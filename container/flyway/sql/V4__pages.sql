-- V4__pages.sql

-- 1) Schema and search_path
CREATE SCHEMA IF NOT EXISTS cms;
SET search_path = cms, public;

-- 2) Dependencies
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- 3) Tables

-- Pages: hierarchy & settings
CREATE TABLE IF NOT EXISTS cms.pages (
                                         id         uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    parent_id  uuid NULL REFERENCES cms.pages(id) ON DELETE CASCADE,
    slug       text NOT NULL CHECK (slug ~ '^[a-z0-9-]+$'),
    full_path  text NOT NULL,
    in_menu    boolean NOT NULL DEFAULT true,
    in_sitemap boolean NOT NULL DEFAULT true,
    title      text NOT NULL DEFAULT '',
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_parent_slug UNIQUE (parent_id, slug),
    CONSTRAINT uq_full_path UNIQUE (full_path)
    );

-- Page versions: content & publish state
CREATE TABLE IF NOT EXISTS cms.page_versions (
                                                 id           uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    page_id      uuid NOT NULL REFERENCES cms.pages(id) ON DELETE CASCADE,
    version_no   integer NOT NULL,
    content      jsonb NOT NULL DEFAULT '{}'::jsonb,
    template_id  uuid NULL, -- add FK to cms.templates(id) in a later migration if needed
    is_published boolean NOT NULL DEFAULT false,
    published_at timestamptz NULL,
    created_at   timestamptz NOT NULL DEFAULT now(),
    updated_at   timestamptz NOT NULL DEFAULT now()
    );

CREATE UNIQUE INDEX IF NOT EXISTS uq_page_versions_one_published
    ON cms.page_versions(page_id) WHERE is_published;

CREATE UNIQUE INDEX IF NOT EXISTS uq_page_versions_ver
    ON cms.page_versions(page_id, version_no);

-- 4) Functions & triggers

-- Prevent cycles in the page tree
CREATE OR REPLACE FUNCTION cms.pages_no_cycles() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
  IF NEW.parent_id IS NULL THEN
    RETURN NEW;
END IF;

  IF NEW.parent_id = NEW.id THEN
    RAISE EXCEPTION 'A page cannot be its own parent';
END IF;

  -- Check ancestors of NEW.parent_id for NEW.id (cycle)
  IF EXISTS (
    WITH RECURSIVE anc(id) AS (
      SELECT NEW.parent_id
      UNION ALL
      SELECT p.parent_id
      FROM cms.pages p
      JOIN anc ON p.id = anc.id
      WHERE p.parent_id IS NOT NULL
    )
    SELECT 1 FROM anc WHERE id = NEW.id
  ) THEN
    RAISE EXCEPTION 'Cycle detected: page % cannot have parent %', NEW.id, NEW.parent_id;
END IF;

RETURN NEW;
END$$;

-- Compute/maintain full_path and updated_at
CREATE OR REPLACE FUNCTION cms.pages_set_full_path() RETURNS trigger
LANGUAGE plpgsql AS $$
DECLARE parent_path text;
BEGIN
  IF TG_OP = 'INSERT'
     OR (NEW.slug IS DISTINCT FROM OLD.slug)
     OR (NEW.parent_id IS DISTINCT FROM OLD.parent_id) THEN

    IF NEW.parent_id IS NULL THEN
      NEW.full_path := '/';
ELSE
SELECT p.full_path INTO parent_path FROM cms.pages p WHERE p.id = NEW.parent_id;
IF parent_path IS NULL THEN
        RAISE EXCEPTION 'Parent % does not exist', NEW.parent_id;
END IF;

      IF parent_path = '/' THEN
        NEW.full_path := '/' || NEW.slug;
ELSE
        NEW.full_path := parent_path || '/' || NEW.slug;
END IF;
END IF;
END IF;

  NEW.updated_at := now();
RETURN NEW;
END$$;

-- Recompute descendant full_paths when a node moves/renames
CREATE OR REPLACE FUNCTION cms.pages_cascade_full_path(old_id uuid, old_path text, new_path text) RETURNS void
LANGUAGE plpgsql AS $$
DECLARE r record;
BEGIN
FOR r IN
    WITH RECURSIVE cte AS (
      SELECT p.id, p.parent_id, p.slug, p.full_path
      FROM cms.pages p
      WHERE p.parent_id = old_id
      UNION ALL
      SELECT p2.id, p2.parent_id, p2.slug, p2.full_path
      FROM cms.pages p2
      JOIN cte ON p2.parent_id = cte.id
    )
SELECT * FROM cte
                  LOOP
UPDATE cms.pages
SET full_path = new_path || substring(r.full_path FROM length(old_path)+1),
    updated_at = now()
WHERE id = r.id;
END LOOP;
END$$;

CREATE OR REPLACE FUNCTION cms.pages_after_update_cascade() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
  IF (NEW.full_path IS DISTINCT FROM OLD.full_path) THEN
    PERFORM cms.pages_cascade_full_path(NEW.id, OLD.full_path, NEW.full_path);
END IF;
RETURN NULL;
END$$;

DROP TRIGGER IF EXISTS trg_pages_no_cycles ON cms.pages;
CREATE TRIGGER trg_pages_no_cycles
    BEFORE INSERT OR UPDATE ON cms.pages
                         FOR EACH ROW EXECUTE FUNCTION cms.pages_no_cycles();

DROP TRIGGER IF EXISTS trg_pages_full_path ON cms.pages;
CREATE TRIGGER trg_pages_full_path
    BEFORE INSERT OR UPDATE ON cms.pages
                         FOR EACH ROW EXECUTE FUNCTION cms.pages_set_full_path();

DROP TRIGGER IF EXISTS trg_pages_after_update_cascade ON cms.pages;
CREATE TRIGGER trg_pages_after_update_cascade
    AFTER UPDATE ON cms.pages
    FOR EACH ROW WHEN (OLD.full_path IS DISTINCT FROM NEW.full_path)
EXECUTE FUNCTION cms.pages_after_update_cascade();

-- 5) Views

-- Latest/published status per page
CREATE OR REPLACE VIEW cms.page_version_status AS
WITH latest AS (
  SELECT pv.page_id, MAX(pv.version_no) AS latest_version_no
  FROM cms.page_versions pv
  GROUP BY pv.page_id
),
published AS (
  SELECT pv.page_id, MAX(pv.version_no) AS published_version_no
  FROM cms.page_versions pv
  WHERE pv.is_published
  GROUP BY pv.page_id
)
SELECT
    p.id,
    p.full_path,
    p.slug,
    p.parent_id,
    p.in_menu,
    p.in_sitemap,
    p.title,
    COALESCE(l.latest_version_no, 0)      AS latest_version_no,
    COALESCE(pub.published_version_no, 0) AS published_version_no,
    (pub.published_version_no IS NOT NULL) AS has_published,
    (COALESCE(l.latest_version_no,0) > COALESCE(pub.published_version_no,0)) AS has_newer_draft
FROM cms.pages p
         LEFT JOIN latest l   ON l.page_id  = p.id
         LEFT JOIN published pub ON pub.page_id = p.id;

-- Node list (with depth)
CREATE OR REPLACE VIEW cms.site_nodes AS
SELECT
    s.*,
    (CASE WHEN parent_id IS NULL THEN 0 ELSE length(full_path) - length(replace(full_path,'/','')) END) AS depth
FROM cms.page_version_status s
ORDER BY full_path;

-- Menu (only published + in_menu)
CREATE OR REPLACE VIEW cms.menu_view AS
SELECT full_path, title
FROM cms.page_version_status
WHERE in_menu = true AND has_published = true
ORDER BY full_path;

-- 6) Functions (SP-style)

-- Lookup published by slug/path
CREATE OR REPLACE FUNCTION cms.get_published_page_by_slug(p_path text)
RETURNS TABLE (
  page_id uuid,
  version_id uuid,
  full_path text,
  title text,
  version_no integer,
  template_id uuid,
  content jsonb,
  published_at timestamptz
) LANGUAGE sql AS $$
SELECT
    p.id,
    pv.id,
    p.full_path,
    p.title,
    pv.version_no,
    pv.template_id,
    pv.content,
    pv.published_at
FROM cms.pages p
         JOIN cms.page_versions pv ON pv.page_id = p.id AND pv.is_published
WHERE p.full_path = CASE
                        WHEN p_path IS NULL OR p_path = '' THEN '/'
                        WHEN left(p_path,1) <> '/' THEN '/'||p_path
    ELSE p_path
END
  LIMIT 1;
$$;

-- Add page (+ first draft)
CREATE OR REPLACE FUNCTION cms.add_page(
  p_parent_path text,
  p_slug text,
  p_title text,
  p_template_id uuid,
  p_content jsonb
) RETURNS TABLE(new_page_id uuid, new_version_no int)
LANGUAGE plpgsql AS $$
DECLARE parent_id uuid;
BEGIN
  IF p_parent_path IS NULL OR p_parent_path = '' THEN
    p_parent_path := '/';
END IF;
  IF left(p_parent_path,1) <> '/' THEN
    p_parent_path := '/'||p_parent_path;
END IF;

SELECT id INTO parent_id FROM cms.pages WHERE full_path = p_parent_path;
IF p_parent_path = '/' THEN
    parent_id := NULL;
  ELSIF parent_id IS NULL THEN
    RAISE EXCEPTION 'Parent path % not found', p_parent_path;
END IF;

INSERT INTO cms.pages(parent_id, slug, title)
VALUES (parent_id, p_slug, p_title)
    RETURNING id INTO new_page_id;

INSERT INTO cms.page_versions(page_id, version_no, content, template_id, is_published)
VALUES (new_page_id, 1, COALESCE(p_content,'{}'::jsonb), p_template_id, false);

new_version_no := 1;
  RETURN;
END$$;

-- Save new draft (auto-increment version)
CREATE OR REPLACE FUNCTION cms.save_page(
  p_page_id uuid,
  p_content jsonb,
  p_template_id uuid
) RETURNS TABLE(version_id uuid, version_no int)
LANGUAGE plpgsql AS $$
DECLARE next_no int;
BEGIN
SELECT COALESCE(MAX(version_no),0)+1 INTO next_no
FROM cms.page_versions WHERE page_id = p_page_id;

INSERT INTO cms.page_versions(page_id, version_no, content, template_id, is_published)
VALUES (p_page_id, next_no, COALESCE(p_content,'{}'::jsonb), p_template_id, false)
    RETURNING id, version_no INTO version_id, version_no;

RETURN;
END$$;

-- Publish a specific version
CREATE OR REPLACE FUNCTION cms.publish_page(
  p_page_id uuid,
  p_version_no int
) RETURNS void
LANGUAGE plpgsql AS $$
BEGIN
UPDATE cms.page_versions
SET is_published = false, published_at = NULL, updated_at = now()
WHERE page_id = p_page_id AND is_published;

UPDATE cms.page_versions
SET is_published = true, published_at = now(), updated_at = now()
WHERE page_id = p_page_id AND version_no = p_version_no;

IF NOT FOUND THEN
    RAISE EXCEPTION 'Version % not found for page %', p_version_no, p_page_id;
END IF;
END$$;

-- Change settings (slug, template, in_menu, in_sitemap)
CREATE OR REPLACE FUNCTION cms.change_settings(
  p_page_id uuid,
  p_new_slug text DEFAULT NULL,
  p_new_template_id uuid DEFAULT NULL,
  p_in_menu boolean DEFAULT NULL,
  p_in_sitemap boolean DEFAULT NULL
) RETURNS void
LANGUAGE plpgsql AS $$
DECLARE has_draft boolean;
BEGIN
UPDATE cms.pages
SET slug      = COALESCE(p_new_slug, slug),
    in_menu   = COALESCE(p_in_menu, in_menu),
    in_sitemap= COALESCE(p_in_sitemap, in_sitemap),
    updated_at= now()
WHERE id = p_page_id;

IF p_new_template_id IS NOT NULL THEN
SELECT EXISTS(
    SELECT 1 FROM cms.page_versions
    WHERE page_id = p_page_id AND is_published = false
    ORDER BY version_no DESC LIMIT 1
) INTO has_draft;

IF has_draft THEN
UPDATE cms.page_versions pv
SET template_id = p_new_template_id, updated_at = now()
WHERE pv.id = (
    SELECT id FROM cms.page_versions
    WHERE page_id = p_page_id AND is_published = false
    ORDER BY version_no DESC LIMIT 1
    );
ELSE
      PERFORM cms.save_page(
        p_page_id,
        (SELECT content FROM cms.page_versions WHERE page_id = p_page_id AND is_published LIMIT 1),
        p_new_template_id
      );
END IF;
END IF;
END$$;
