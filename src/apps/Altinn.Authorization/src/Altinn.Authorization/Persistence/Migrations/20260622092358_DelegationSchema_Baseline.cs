using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.Platform.Authorization.Persistence.Migrations
{
    /// <summary>
    /// Baseline migration that reproduces the <c>delegation</c> schema exactly: the
    /// <c>delegationchangetype</c> enum, the <c>delegationchanges</c> table with its
    /// identity sequence, primary key and indexes, the lookup/insert functions, and the
    /// grants to <c>platform_authorization</c>. The SQL is the canonical
    /// <c>pg_dump --schema-only</c> of the established database, made idempotent
    /// (guarded <c>CREATE</c> / <c>IF NOT EXISTS</c> / <c>OR REPLACE</c>), so it is a
    /// no-op on databases that already have the schema and creates it on fresh ones.
    /// The objects are not modelled as EF entities, so the model snapshot stays empty
    /// and EF never manages or drops them; the raw-Npgsql repositories keep querying
    /// them unchanged.
    /// </summary>
    /// <inheritdoc />
    public partial class DelegationSchema_Baseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS delegation;");

            migrationBuilder.Sql(@"
DO $$ BEGIN
    CREATE TYPE delegation.delegationchangetype AS ENUM (
        'grant',
        'revoke',
        'revoke_last'
    );
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;");

            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS delegation.delegationchanges (
    delegationchangeid bigint NOT NULL,
    delegationchangetype delegation.delegationchangetype NOT NULL,
    altinnappid character varying NOT NULL,
    offeredbypartyid integer NOT NULL,
    coveredbypartyid integer,
    coveredbyuserid integer,
    performedbyuserid integer NOT NULL,
    blobstoragepolicypath character varying NOT NULL,
    blobstorageversionid character varying,
    created timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);");

            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_attribute
        WHERE attrelid = 'delegation.delegationchanges'::regclass
          AND attname = 'delegationchangeid'
          AND attidentity <> ''
    ) THEN
        ALTER TABLE delegation.delegationchanges ALTER COLUMN delegationchangeid ADD GENERATED ALWAYS AS IDENTITY (
            SEQUENCE NAME delegation.delegationchanges_delegationchangeid_seq
            START WITH 1
            INCREMENT BY 1
            NO MINVALUE
            NO MAXVALUE
            CACHE 1
        );
    END IF;
END $$;");

            migrationBuilder.Sql(@"
DO $$ BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'delegationchanges_pkey'
          AND connamespace = 'delegation'::regnamespace
    ) THEN
        ALTER TABLE ONLY delegation.delegationchanges
            ADD CONSTRAINT delegationchanges_pkey PRIMARY KEY (delegationchangeid);
    END IF;
END $$;");

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_altinnappid ON delegation.delegationchanges USING btree (altinnappid);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_coveredbyparty ON delegation.delegationchanges USING btree (coveredbypartyid);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_coveredbyuser ON delegation.delegationchanges USING btree (coveredbyuserid);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS idx_offeredby ON delegation.delegationchanges USING btree (offeredbypartyid);");

            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION delegation.get_all_changes(_altinnappid character varying, _offeredbypartyid integer, _coveredbyuserid integer, _coveredbypartyid integer) RETURNS SETOF delegation.delegationchanges
    LANGUAGE sql STABLE PARALLEL SAFE
    AS $$
  SELECT
    delegationChangeId,
    delegationChangeType,
    altinnAppId,
    offeredByPartyId,
    coveredByUserId,
    coveredByPartyId,
    performedByUserId,
    blobStoragePolicyPath,
    blobStorageVersionId,
    created
  FROM delegation.delegationChanges
  WHERE
    altinnAppId = _altinnAppId
    AND offeredByPartyId = _offeredByPartyId
    AND (_coveredByUserId IS NULL OR coveredByUserId = _coveredByUserId)
    AND (_coveredByPartyId IS NULL OR coveredByPartyId = _coveredByPartyId)
$$;");

            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION delegation.get_all_current_changes_coveredbypartyids(_altinnappids character varying[], _offeredbypartyids integer[], _coveredbypartyids integer[]) RETURNS SETOF delegation.delegationchanges
    LANGUAGE sql STABLE PARALLEL SAFE
    AS $$
  SELECT
    delegationchangeid,
    delegationChangeType,
    altinnappid,
    offeredbypartyid,
    coveredbypartyid,
    coveredbyuserid,
    performedbyuserid,
    blobstoragepolicypath,
    blobstorageversionid,
    created
  FROM delegation.delegationchanges
    INNER JOIN
    (
	  SELECT MAX(delegationChangeId) AS maxChange
	  FROM delegation.delegationchanges
	  WHERE
	    (_altinnappids IS NULL OR altinnAppId = ANY (_altinnAppIds))
	    AND (offeredByPartyId = ANY (_offeredByPartyIds))
	    AND coveredByPartyId = ANY (_coveredByPartyIds)
      GROUP BY altinnAppId, offeredByPartyId, coveredByPartyId
    ) AS selectMaxChange
    ON delegationChangeId = selectMaxChange.maxChange
$$;");

            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION delegation.get_all_current_changes_coveredbyuserids(_altinnappids character varying[], _offeredbypartyids integer[], _coveredbyuserids integer[]) RETURNS SETOF delegation.delegationchanges
    LANGUAGE sql STABLE PARALLEL SAFE
    AS $$
  SELECT
    delegationchangeid,
    delegationChangeType,
    altinnappid,
    offeredbypartyid,
    coveredbypartyid,
    coveredbyuserid,
    performedbyuserid,
    blobstoragepolicypath,
    blobstorageversionid,
    created
  FROM delegation.delegationchanges
    INNER JOIN
    (
	  SELECT MAX(delegationChangeId) AS maxChange
	  FROM delegation.delegationchanges
	  WHERE
        (_altinnappids IS NULL OR altinnAppId = ANY (_altinnAppIds))
        AND (offeredByPartyId = ANY (_offeredByPartyIds))
        AND coveredByUserId = ANY (_coveredByUserIds)
	  GROUP BY altinnAppId, offeredByPartyId, coveredByUserId
    ) AS selectMaxChange
    ON delegationChangeId = selectMaxChange.maxChange
$$;");

            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION delegation.get_all_current_changes_offeredbypartyid_only(_altinnappids character varying[], _offeredbypartyids integer[]) RETURNS SETOF delegation.delegationchanges
    LANGUAGE sql STABLE PARALLEL SAFE
    AS $$
  SELECT
    delegationchangeid,
    delegationChangeType,
    altinnappid,
    offeredbypartyid,
    coveredbypartyid,
    coveredbyuserid,
    performedbyuserid,
    blobstoragepolicypath,
    blobstorageversionid,
    created
  FROM delegation.delegationchanges
	INNER JOIN
	(
		SELECT MAX(delegationChangeId) AS maxChange
	 	FROM delegation.delegationchanges
		WHERE
		  (_altinnappids IS NULL OR altinnAppId = ANY (_altinnAppIds))
		  AND (offeredByPartyId = ANY (_offeredByPartyIds))
		GROUP BY altinnAppId, offeredByPartyId, coveredByPartyId, coveredByUserId
	) AS selectMaxChange
	ON delegationChangeId = selectMaxChange.maxChange
$$;");

            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION delegation.get_current_change(_altinnappid character varying, _offeredbypartyid integer, _coveredbyuserid integer, _coveredbypartyid integer) RETURNS TABLE(delegationchangeid integer, delegationchangetype delegation.delegationchangetype, altinnappid text, offeredbypartyid integer, coveredbypartyid integer, coveredbyuserid integer, performedbyuserid integer, blobstoragepolicypath text, blobstorageversionid text, created timestamp with time zone)
    LANGUAGE sql STABLE ROWS 1 PARALLEL SAFE
    AS $$

  SELECT
    delegationChangeId,
    delegationChangeType,
    altinnAppId,
    offeredByPartyId,
    coveredByPartyId,
    coveredByUserId,
    performedByUserId,
    blobStoragePolicyPath,
    blobStorageVersionId,
    created
  FROM delegation.delegationChanges
  WHERE
    altinnAppId = _altinnAppId
    AND offeredByPartyId = _offeredByPartyId
    AND (_coveredByUserId IS NULL OR coveredByUserId = _coveredByUserId)
    AND (_coveredByPartyId IS NULL OR coveredByPartyId = _coveredByPartyId)
  ORDER BY delegationChangeId DESC LIMIT 1
$$;");

            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION delegation.insert_delegationchange(_delegationchangetype delegation.delegationchangetype, _altinnappid character varying, _offeredbypartyid integer, _coveredbyuserid integer, _coveredbypartyid integer, _performedbyuserid integer, _blobstoragepolicypath character varying, _blobstorageversionid character varying) RETURNS SETOF delegation.delegationchanges
    LANGUAGE sql ROWS 1
    AS $$
  INSERT INTO delegation.delegationChanges(
    delegationChangeType,
    altinnAppId,
    offeredByPartyId,
    coveredByUserId,
    coveredByPartyId,
    performedByUserId,
    blobStoragePolicyPath,
    blobStorageVersionId
  )
  VALUES (
    _delegationChangeType,
    _altinnAppId,
    _offeredByPartyId,
    _coveredByUserId,
    _coveredByPartyId,
    _performedByUserId,
    _blobStoragePolicyPath,
    _blobStorageVersionId
  ) RETURNING *;
$$;");

            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION delegation.select_delegationchanges_by_id_range(_startid bigint, _endid bigint DEFAULT '9223372036854775807'::bigint) RETURNS TABLE(delegationchangeid integer, delegationchangetype delegation.delegationchangetype, altinnappid text, offeredbypartyid integer, coveredbypartyid integer, coveredbyuserid integer, performedbyuserid integer, blobstoragepolicypath text, blobstorageversionid text, created timestamp with time zone)
    LANGUAGE sql STABLE PARALLEL SAFE
    AS $$

  SELECT
    delegationChangeId,
    delegationChangeType,
    altinnAppId,
    offeredByPartyId,
    coveredByPartyId,
    coveredByUserId,
    performedByUserId,
    blobStoragePolicyPath,
    blobStorageVersionId,
    created
  FROM delegation.delegationChanges
  WHERE
    delegationChangeId BETWEEN _startId AND _endId
$$;");

            migrationBuilder.Sql("GRANT USAGE ON SCHEMA delegation TO platform_authorization;");
            migrationBuilder.Sql("GRANT ALL ON TABLE delegation.delegationchanges TO platform_authorization;");
            migrationBuilder.Sql("GRANT ALL ON SEQUENCE delegation.delegationchanges_delegationchangeid_seq TO platform_authorization;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentional no-op. This baseline adopts the pre-existing `delegation` schema and
            // its data; it is not reversible. Dropping the schema would destroy live delegation
            // data, and as noted on the class above EF never manages or drops these objects.
            // Rolling back past the baseline is not supported; reprovision instead.
        }
    }
}
