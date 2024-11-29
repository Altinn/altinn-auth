-- Function: select_delegationchanges_by_id_range
DROP FUNCTION IF EXISTS delegation.select_delegationchanges_by_id_range(bigint, bigint);

CREATE OR REPLACE FUNCTION delegation.select_delegationchanges_by_id_range(
	_startid bigint,
	_endid bigint DEFAULT '9223372036854775807'::bigint)
    RETURNS TABLE(
        delegationchangeid integer,
        delegationchangetype delegation.delegationchangetype,
        altinnappid text,
        offeredbypartyid integer,
        coveredbypartyid integer,
        coveredbyuserid integer,
        performedbyuserid integer,
        blobstoragepolicypath text,
        blobstorageversionid text,
        created timestamp with time zone) 
    LANGUAGE 'sql'
    COST 100
    STABLE PARALLEL SAFE 
    ROWS 1000

AS $BODY$

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
    delegationChangeId BETWEEN _startId AND _endId
$BODY$;