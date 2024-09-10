-- Table: public.users

-- DROP TABLE IF EXISTS public.users;

CREATE TABLE IF NOT EXISTS public.users
(
    "UserId" integer NOT NULL DEFAULT nextval('users_user_id_seq'::regclass),
    "Username" character varying(50) COLLATE pg_catalog."default" NOT NULL,
    "Email" character varying(100) COLLATE pg_catalog."default" NOT NULL,
    "Password" character varying(255) COLLATE pg_catalog."default" NOT NULL,
    "CreatedAt" timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    "Authority" character varying(100) COLLATE pg_catalog."default",
    "PlanId" integer,
    CONSTRAINT users_pkey PRIMARY KEY ("UserId"),
    CONSTRAINT users_email_key UNIQUE ("Email"),
    CONSTRAINT users_username_key UNIQUE ("Username")
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.users
    OWNER to postgres;

-- Table: public.licenses

-- DROP TABLE IF EXISTS public.licenses;

CREATE TABLE IF NOT EXISTS public.licenses
(
    "LicenseId" integer NOT NULL DEFAULT nextval('licenses_license_id_seq'::regclass),
    "UserId" integer,
    "LicenseKey" character varying(100) COLLATE pg_catalog."default" NOT NULL,
    "PlanId" integer,
    "ExpirationDate" date,
    "CreatedAt" timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    "DeviceInfo" character varying(256) COLLATE pg_catalog."default",
    "LicenseIv" character varying(100) COLLATE pg_catalog."default",
    "LicensePublicKey" character varying(5000) COLLATE pg_catalog."default",
    CONSTRAINT licenses_pkey PRIMARY KEY ("LicenseId"),
    CONSTRAINT licenses_license_key_key UNIQUE ("LicenseKey"),
    CONSTRAINT licenses_plan_id_fkey FOREIGN KEY ("PlanId")
        REFERENCES public.licenseplans ("LicensePlansId") MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT licenses_user_id_fkey FOREIGN KEY ("UserId")
        REFERENCES public.users ("UserId") MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.licenses
    OWNER to postgres;

-- Table: public.licenseplans

-- DROP TABLE IF EXISTS public.licenseplans;

CREATE TABLE IF NOT EXISTS public.licenseplans
(
    "LicensePlansId" integer NOT NULL DEFAULT nextval('licenseplans_plan_id_seq'::regclass),
    "PlanName" character varying(50) COLLATE pg_catalog."default" NOT NULL,
    "Description" text COLLATE pg_catalog."default",
    "Price" numeric(10,2),
    "CreatedAt" timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT licenseplans_pkey PRIMARY KEY ("LicensePlansId"),
    CONSTRAINT licenseplans_plan_name_key UNIQUE ("PlanName")
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.licenseplans
    OWNER to postgres;

-- Table: public.PrimaryKeys

-- DROP TABLE IF EXISTS public."PrimaryKeys";

CREATE TABLE IF NOT EXISTS public."PrimaryKeys"
(
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY ( INCREMENT 1 START 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1 ),
    "PrimaryPublicKey" character varying(3000) COLLATE pg_catalog."default",
    "PrimaryPrivateKey" character varying(4000) COLLATE pg_catalog."default",
    CONSTRAINT "PrimaryKeys_pkey" PRIMARY KEY (id)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public."PrimaryKeys"
    OWNER to postgres;