CREATE SCHEMA IF NOT EXISTS web;

CREATE OR REPLACE VIEW web.v_menu_pages AS
SELECT
    p.id,
    p.full_path,
    p.title,
    pv.published_at
FROM cms.pages            AS p
         JOIN cms.page_versions    AS pv
              ON pv.page_id = p.id
                  AND pv.is_published = true
WHERE
    p.in_menu = true
ORDER BY
    p.full_path;

CREATE OR REPLACE VIEW web.v_sitemap_pages AS
SELECT
    p.id,
    p.full_path,
    p.title,
    pv.published_at
FROM cms.pages            AS p
         JOIN cms.page_versions    AS pv
              ON pv.page_id = p.id
                  AND pv.is_published = true
WHERE
    p.in_sitemap = true
ORDER BY
    p.full_path;


CREATE OR REPLACE FUNCTION web.get_published_page_by_slug(p_path text)
RETURNS TABLE (
  full_path    text,
  title        text,
  content      jsonb,
  published_at timestamptz
) LANGUAGE sql AS $$
SELECT
    p.full_path,
    p.title,
    pv.content,
    pv.published_at
FROM cms.pages         AS p
         JOIN cms.page_versions AS pv
              ON pv.page_id = p.id
                  AND pv.is_published = true
WHERE p.full_path =
      CASE
          WHEN p_path = '' THEN '/'
          WHEN left(p_path,1) <> '/' THEN '/' || p_path
    ELSE p_path
END
LIMIT 1;
$$;
