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
    coveredByPartyId,
    coveredByUserId,    
    performedByUserId,
    blobStoragePolicyPath,
    blobStorageVersionId,
    created
  FROM delegation.delegationChanges
  WHERE
    delegationChangeId BETWEEN _startId AND _endId
$BODY$;

-- Function: update delegation.get_current_change for correct select order coveredByPartyId before coveredByUserId to match delegation.delegationchanges column order
CREATE OR REPLACE FUNCTION delegation.get_current_change(
	_altinnappid character varying,
	_offeredbypartyid integer,
	_coveredbyuserid integer,
	_coveredbypartyid integer)
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
    ROWS 1

AS $BODY$

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
$BODY$;

