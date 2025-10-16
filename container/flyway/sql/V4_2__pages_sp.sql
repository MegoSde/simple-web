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
FROM pages p
         JOIN page_versions pv ON pv.page_id = p.id AND pv.is_published
WHERE p.full_path = CASE
                        WHEN p_path = '' THEN '/'
                        WHEN left(p_path,1) <> '/' THEN '/'||p_path
    ELSE p_path
END
  LIMIT 1;
$$;

        
-- ADD PAGE: opret struktur + første (kladde)version
CREATE OR REPLACE FUNCTION cms.add_page(
  p_parent_path text,
  p_slug text,
  p_title text,
  p_template_id uuid,
  p_content jsonb
) RETURNS TABLE(new_page_id uuid, new_version_no int)
LANGUAGE plpgsql AS $$
DECLARE
parent_id uuid;
BEGIN
  -- Normaliser sti
  IF p_parent_path IS NULL OR p_parent_path = '' THEN
    p_parent_path := '/';
END IF;
  IF left(p_parent_path,1) <> '/' THEN
    p_parent_path := '/'||p_parent_path;
END IF;

  -- Find parent via full_path (også root)
SELECT id INTO parent_id FROM cms.pages WHERE full_path = p_parent_path;

IF parent_id IS NULL THEN
    -- Parent findes ikke. Det er kun OK hvis vi er ved at oprette selve root.
    IF p_parent_path = '/' THEN
      -- Opret root KUN hvis der ikke findes en i forvejen
      IF EXISTS (SELECT 1 FROM cms.pages WHERE parent_id IS NULL) THEN
        RAISE EXCEPTION 'Root page already exists; cannot create another';
END IF;

INSERT INTO cms.pages(parent_id, slug, title)
VALUES (NULL, COALESCE(NULLIF(p_slug,''),'root'), COALESCE(p_title,''))
    RETURNING id INTO new_page_id;

INSERT INTO cms.page_versions(page_id, version_no, content, template_id, is_published)
VALUES (new_page_id, 1, COALESCE(p_content,'{}'::jsonb), p_template_id, false);

new_version_no := 1;
RETURN QUERY
SELECT new_page_id, new_version_no;
ELSE
      RAISE EXCEPTION 'Parent path % not found', p_parent_path;
END IF;
END IF;

  -- Her er parent fundet (inkl. root). Opret som barn.
INSERT INTO cms.pages(parent_id, slug, title)
VALUES (parent_id, p_slug, p_title)
    RETURNING id INTO new_page_id;

INSERT INTO cms.page_versions(page_id, version_no, content, template_id, is_published)
VALUES (new_page_id, 1, COALESCE(p_content,'{}'::jsonb), p_template_id, false);

new_version_no := 1;
RETURN QUERY
SELECT new_page_id, new_version_no;
END$$;

-- SAVE PAGE: opret ny kladdeversion (auto-increment version_no)
CREATE OR REPLACE FUNCTION cms.save_page(
  p_page_id uuid,
  p_content jsonb,
  p_template_id uuid
) RETURNS TABLE(version_id uuid, version_no int)
LANGUAGE plpgsql AS $$
DECLARE
next_no int;
  _ver_id uuid;
  _ver_no int;
BEGIN
SELECT COALESCE(MAX(pv.version_no),0)+1
INTO next_no
FROM cms.page_versions pv
WHERE pv.page_id = p_page_id;

INSERT INTO cms.page_versions AS pv (page_id, version_no, content, template_id, is_published)
VALUES (p_page_id, next_no, COALESCE(p_content,'{}'::jsonb), p_template_id, false)
    RETURNING pv.id, pv.version_no INTO _ver_id, _ver_no;

RETURN QUERY
SELECT _ver_id, _ver_no;
END$$;

-- PUBLISH PAGE: sæt valgt version som publiceret (eneste)
CREATE OR REPLACE FUNCTION cms.publish_page(
  p_page_id uuid,
  p_version_no int
) RETURNS void LANGUAGE plpgsql AS $$
BEGIN
UPDATE page_versions
SET is_published = false, published_at = NULL, updated_at = now()
WHERE page_id = p_page_id AND is_published;

UPDATE page_versions
SET is_published = true, published_at = now(), updated_at = now()
WHERE page_id = p_page_id AND version_no = p_version_no;

IF NOT FOUND THEN
    RAISE EXCEPTION 'Version % not found for page %', p_version_no, p_page_id;
END IF;
END$$;

-- CHANGE SETTINGS: slug, template, in_menu, in_sitemap
CREATE OR REPLACE FUNCTION cms.change_settings(
  p_page_id uuid,
  p_new_slug text DEFAULT NULL,
  p_new_template_id uuid DEFAULT NULL,
  p_in_menu boolean DEFAULT NULL,
  p_in_sitemap boolean DEFAULT NULL
) RETURNS void LANGUAGE plpgsql AS $$
DECLARE curr_path text;
BEGIN
  -- 1) opdater page settings (slug/in_menu/in_sitemap)
UPDATE pages
SET slug = COALESCE(p_new_slug, slug),
    updated_at = now(),
    in_menu = COALESCE(p_in_menu, in_menu),
    in_sitemap = COALESCE(p_in_sitemap, in_sitemap)
WHERE id = p_page_id;

-- full_path opdateres automatisk af BEFORE trigger; subtree kaskade håndteres i AFTER trigger

-- 2) hvis template ønskes ændret, så ret på NYESTE kladde (ellers opret en ny kladde)
IF p_new_template_id IS NOT NULL THEN
    WITH latest AS (
      SELECT id, version_no FROM page_versions
      WHERE page_id = p_page_id
      ORDER BY version_no DESC
      LIMIT 1
    )
UPDATE page_versions pv
SET template_id = p_new_template_id,
    updated_at = now()
    FROM latest l
WHERE pv.id = l.id AND pv.is_published = false;

IF NOT FOUND THEN
      -- ingen kladde → opret en ny fra publiceret som udgangspunkt
      PERFORM save_page(
        p_page_id,
        (SELECT content FROM page_versions WHERE page_id = p_page_id AND is_published LIMIT 1),
        p_new_template_id
      );
END IF;
END IF;
END$$;


CREATE OR REPLACE FUNCTION cms.delete_page(
  p_page_id uuid,
  p_confirm_path text
) RETURNS TABLE (
  deleted_page_id uuid,
  deleted_path text,
  deleted_count int
)
LANGUAGE plpgsql
AS $$
DECLARE
curr_path text;
  subcnt int;
BEGIN
  -- Find og tjek path
SELECT full_path INTO curr_path
FROM cms.pages
WHERE id = p_page_id;

IF curr_path IS NULL THEN
    RAISE EXCEPTION 'Page % not found', p_page_id
      USING ERRCODE = 'no_data_found';
END IF;

  IF p_confirm_path IS NULL OR btrim(p_confirm_path) <> curr_path THEN
    RAISE EXCEPTION 'Confirm path mismatch. Expected "%", got "%".', curr_path, p_confirm_path
      USING ERRCODE = '22023'; -- invalid_parameter_value
END IF;

  -- Tæl subtree (denne + alle børn)
WITH RECURSIVE sub AS (
    SELECT id, full_path
    FROM cms.pages
    WHERE id = p_page_id
    UNION ALL
    SELECT p.id, p.full_path
    FROM cms.pages p
             JOIN sub s ON p.parent_id = s.id
)
SELECT count(*) INTO subcnt FROM sub;

-- Slet roden (CASCADE tager børn)
DELETE FROM cms.pages WHERE id = p_page_id;
IF NOT FOUND THEN
    RAISE EXCEPTION 'Failed to delete page %', p_page_id;
END IF;

  deleted_page_id := p_page_id;
  deleted_path    := curr_path;
  deleted_count   := subcnt;
  RETURN NEXT;
END;
$$;

create or replace function cms.cms_get_page_for_edit(
  p_page_id uuid,
  p_version_text text default null
)
returns jsonb
language plpgsql
as $$
declare
v_version int;
  v_content jsonb;
begin
  -- find ønsket eller seneste version
  if nullif(trim(p_version_text), '') is null then
select pv.version_no
into v_version
from page_versions pv
where pv.page_id = p_page_id
order by pv.version_no desc
    limit 1;
else
begin
      v_version := trim(p_version_text)::int;
exception when invalid_text_representation then
      raise exception 'INVALID_VERSION_NUMBER' using errcode = 'P0001';
end;
end if;

  if v_version is null then
    raise exception 'VERSION_NOT_FOUND' using errcode = 'P0001';
end if;

  -- hent content for versionen
select pv.content
into v_content
from page_versions pv
where pv.page_id   = p_page_id
  and pv.version_no = v_version;

if v_content is null then
    raise exception 'PAGE_CONTENT_NOT_FOUND' using errcode = 'P0001';
end if;

return jsonb_build_object(
        'pageId',  p_page_id,
        'version', v_version,
        'content', v_content  -- brug evt. lille c for konsistens
       );
end
$$;