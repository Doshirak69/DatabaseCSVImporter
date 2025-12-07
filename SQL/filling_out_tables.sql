BEGIN;

ALTER TABLE public."dvtable_{7473f07f-11ed-4762-9f1e-7ff10808ddd1}"
ADD CONSTRAINT uq_subdivision_name_parent 
UNIQUE ("Name", "ParentTreeRowID", "Type");

ALTER TABLE public."dvtable_{cfdfe60a-21a8-4010-84e9-9d2df348508c}"
ADD CONSTRAINT uq_position_name 
UNIQUE ("Name");

ALTER TABLE public."dvtable_{dbc8ae9d-c1d2-4d5e-978b-339d22b32482}"
ADD CONSTRAINT uq_employee_full_name_position_parent 
UNIQUE ("FirstName", "MiddleName", "LastName", "Position", "ParentRowID");

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE public.city_statuses (
    city_id UUID REFERENCES public."dvtable_{1b1a44fb-1fb1-4876-83aa-95ad38907e24}"("RowID"),  
    employee_id UUID REFERENCES public."dvtable_{dbc8ae9d-c1d2-4d5e-978b-339d22b32482}"("RowID"),
    status BOOLEAN,
    PRIMARY KEY (city_id, employee_id) 
);

CREATE OR REPLACE FUNCTION public.ddfn_get_or_create_subdivision(
    sub_name   text,
    parentid     uuid DEFAULT 'efd2fd74-b9c0-4ab4-bd7e-6e95d82729ee'::uuid, -- подразделение "Внештатный персонал"
    in_sdid    uuid DEFAULT '10aa0726-9580-41a9-9fb3-0aef79d88b91'::uuid,
	type_number INTEGER DEFAULT 1
)
RETURNS uuid
LANGUAGE plpgsql
AS $$
DECLARE
    rid uuid;
BEGIN
    SELECT "RowID" INTO rid
    FROM public."dvtable_{7473f07f-11ed-4762-9f1e-7ff10808ddd1}"
    WHERE "Name" = trim(sub_name)
      AND "ParentTreeRowID" = parentid
	  AND "Type" = type_number
    LIMIT 1;

    IF rid IS NULL THEN
        rid := gen_random_uuid();
        INSERT INTO public."dvtable_{7473f07f-11ed-4762-9f1e-7ff10808ddd1}"(
			"RowID",
			"Name", 
			"SDID",
			"ParentTreeRowID",
			"Type"
		)
        VALUES (
			rid,
			trim(sub_name),
			in_sdid,
			parentid,
			type_number
		)
		ON CONFLICT ("Name", "ParentTreeRowID", "Type") 
		DO UPDATE SET
			"SDID" = EXCLUDED."SDID";
    END IF;
	
	UPDATE dvsys_instances_date
    SET "ChangeDateTime" = now()
    WHERE "InstanceID" = '6710B92A-E148-4363-8A6F-1AA0EB18936C';
	
RETURN rid;
END $$;


CREATE OR REPLACE FUNCTION public.ddfn_get_or_create_position(
    pos_name text,
    in_sdid  uuid DEFAULT '10aa0726-9580-41a9-9fb3-0aef79d88b91'::uuid
)
RETURNS uuid
LANGUAGE plpgsql
AS $$
DECLARE
    rid uuid;
BEGIN
    SELECT "RowID" INTO rid
    FROM public."dvtable_{cfdfe60a-21a8-4010-84e9-9d2df348508c}"
    WHERE "Name" = trim(pos_name) 
    LIMIT 1;
	
	IF rid IS NULL THEN
		rid := gen_random_uuid();
		INSERT INTO public."dvtable_{cfdfe60a-21a8-4010-84e9-9d2df348508c}"(
			"RowID",
			"Name",
			"SDID"
		)
		VALUES(
			rid,
			trim(pos_name),
			in_sdid
		)
		ON CONFLICT ("Name") 
		DO UPDATE SET
			"SDID" = EXCLUDED."SDID";
	END IF;
	
	UPDATE dvsys_instances_date
    SET "ChangeDateTime" = now()
    WHERE "InstanceID" = '6710B92A-E148-4363-8A6F-1AA0EB18936C';
	
RETURN rid;
END $$;

CREATE OR REPLACE FUNCTION public.ddfn_get_or_create_employee(
    p_first_name   text,
    p_middle_name  text,
    p_last_name    text,
    p_position_id  uuid,  
    p_parent_rowid uuid,
    p_sdid         uuid DEFAULT '10aa0726-9580-41a9-9fb3-0aef79d88b91'::uuid
)
RETURNS uuid
LANGUAGE plpgsql
AS $$
DECLARE
    v_employee_id uuid;
BEGIN
    IF NOT EXISTS (
        SELECT 1
		FROM public."dvtable_{cfdfe60a-21a8-4010-84e9-9d2df348508c}" 
		WHERE "RowID" = p_position_id
    ) THEN
        RAISE EXCEPTION 'Должность с RowID=% не найдена', p_position_id;
    END IF;

    IF NOT EXISTS (
        SELECT 1 
		FROM public."dvtable_{7473f07f-11ed-4762-9f1e-7ff10808ddd1}" 
		WHERE "RowID" = p_parent_rowid
    ) THEN
        RAISE EXCEPTION 'Подразделение с RowID=% не найдено', p_parent_rowid;
    END IF;

    SELECT "RowID" INTO v_employee_id
    FROM public."dvtable_{dbc8ae9d-c1d2-4d5e-978b-339d22b32482}"
    WHERE "FirstName"   = trim(p_first_name)
      AND "MiddleName"  = trim(p_middle_name)
      AND "LastName"    = trim(p_last_name)
      AND "Position"    = p_position_id
      AND "ParentRowID" = p_parent_rowid
    LIMIT 1;

    IF v_employee_id IS NULL THEN
        v_employee_id := gen_random_uuid();

        INSERT INTO public."dvtable_{dbc8ae9d-c1d2-4d5e-978b-339d22b32482}"(
			"RowID",
			"FirstName",
			"MiddleName",
			"LastName",
			"Position",
			"SDID",
			"ParentRowID")
        VALUES(
		     v_employee_id,
             trim(p_first_name), 
			 trim(p_middle_name),
			 trim(p_last_name),
             p_position_id,
			 p_sdid,
			 p_parent_rowid)
	    ON CONFLICT (
			"FirstName",
			"MiddleName",
			"LastName",
			"Position",
			"ParentRowID"
		) 
        DO UPDATE SET
			"SDID" = EXCLUDED."SDID";
    END IF;
	
	UPDATE dvsys_instances_date
    SET "ChangeDateTime" = now()
    WHERE "InstanceID" = '6710B92A-E148-4363-8A6F-1AA0EB18936C';
	
RETURN v_employee_id;
	
END $$;

CREATE OR REPLACE FUNCTION public.ddfn_upsert_city_status_by_city_name(
    p_city_name    text,
    p_employee_id  uuid,
    p_status       boolean
)
RETURNS uuid
LANGUAGE plpgsql
AS $$
DECLARE
    v_city_id uuid;
BEGIN
    SELECT "RowID" INTO v_city_id
    FROM public."dvtable_{1b1a44fb-1fb1-4876-83aa-95ad38907e24}"
	WHERE
    UPPER(trim("Name")) = UPPER(trim(p_city_name))
    LIMIT 1;

    IF v_city_id IS NULL THEN
        RAISE EXCEPTION 'Город "%" не найден в справочнике', p_city_name;
    END IF;

	IF NOT EXISTS (
        SELECT 1 
        FROM public."dvtable_{dbc8ae9d-c1d2-4d5e-978b-339d22b32482}" 
        WHERE "RowID" = p_employee_id
    ) THEN
        RAISE EXCEPTION 'Сотрудник с RowID=% не найден', p_employee_id;
    END IF;
	
    INSERT INTO public.city_statuses (
		city_id,
		employee_id,
		status
	)
    VALUES (
		v_city_id,
		p_employee_id,
		p_status
	)
    ON CONFLICT (city_id, employee_id) 
	DO UPDATE
        SET status = EXCLUDED.status;
		
RETURN v_city_id;
	
END $$;

COMMIT;