CREATE OR REPLACE VIEW cms.page_version_status AS
WITH latest AS (
  SELECT
    pv.page_id,
    MAX(pv.version_no) AS latest_version_no
  FROM page_versions pv
  GROUP BY pv.page_id
),
published AS (
  SELECT
    pv.page_id,
    MAX(pv.version_no) AS published_version_no
  FROM page_versions pv
  WHERE pv.is_published
  GROUP BY pv.page_id
),
pv_latest AS (
  SELECT pv.page_id, pv.template_id
  FROM cms.page_versions pv
  JOIN latest l
    ON pv.page_id = l.page_id
   AND pv.version_no = l.latest_version_no
)
SELECT
    p.id,
    p.full_path,
    p.slug,
    p.parent_id,
    p.in_menu,
    p.in_sitemap,
    p.title,
    COALESCE(l.latest_version_no, 0)     AS latest_version_no,
    COALESCE(pub.published_version_no, 0) AS published_version_no,
    (pub.published_version_no IS NOT NULL) AS has_published,
    (l.latest_version_no > COALESCE(pub.published_version_no,0)) AS has_newer_draft,
    pv_latest.template_id                  AS latest_template_id
FROM pages p
         LEFT JOIN latest l ON l.page_id = p.id
         LEFT JOIN pv_latest ON pv_latest.page_id = p.id
         LEFT JOIN published pub ON pub.page_id = p.id;


CREATE OR REPLACE VIEW cms.site_nodes AS
SELECT
    s.*,
    (CASE WHEN parent_id IS NULL THEN 0 ELSE length(full_path) - length(replace(full_path,'/','')) END) AS depth
FROM page_version_status s
ORDER BY full_path;


CREATE OR REPLACE VIEW menu_view AS
SELECT full_path, title
FROM page_version_status
WHERE in_menu = true AND has_published = true
ORDER BY full_path;
