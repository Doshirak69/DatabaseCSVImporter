CREATE OR REPLACE VIEW public.dvview_author_head_department AS
WITH employee_departments AS (
    SELECT
	    mi."InstanceID" AS card_id,
        d."SectionTreeKey" AS dept_key
    FROM   public."dvtable_{30eb9b87-822b-4753-9a50-a1825dca1b74}"  mi   -- секция "Основная информация"
    JOIN   public."dvtable_{dbc8ae9d-c1d2-4d5e-978b-339d22b32482}"  e    -- справочник сотрудников
           ON mi."Author" = e."RowID"
    JOIN   public."dvtable_{7473f07f-11ed-4762-9f1e-7ff10808ddd1}"  d    -- подразделения
           ON e."ParentRowID" = d."RowID"
)
SELECT
    emp_dep.card_id,
    head."Name" AS head_dept_name
FROM   employee_departments AS emp_dep
JOIN   public."dvtable_{7473f07f-11ed-4762-9f1e-7ff10808ddd1}" head
       ON head."SectionTreeKey" = subpath(emp_dep.dept_key, 0, 2)
	   
--SELECT * FROM public.dvview_author_head_department LIMIT 20;
