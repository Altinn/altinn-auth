-- Sequence-value coordination functions for the resource change feed, ported from
-- altinn-register (Migration/v0.13-seq-lock/00-functions.sql, with the tx_max_safeval fix for
-- sequence values above 32 bits from Migration/v0.63-fix-tx-max-safeval/00-functions.sql).
--
-- Problem being solved: with plain nextval(), a transaction can draw a lower sequence value
-- but commit after a transaction that drew a higher one. A feed consumer that pages past the
-- higher value would then never see the lower one. tx_nextval draws the value and takes shared
-- advisory locks that are held until the transaction ends; tx_max_safeval returns the largest
-- value that is safe to read (one less than the smallest value still held by a running
-- transaction). Readers cap their scan at this value so changes can never be skipped.

CREATE FUNCTION resourceregistry.tx_nextval(
  seq regclass
)
RETURNS bigint AS $$
DECLARE
  seq_id oid;
  next_val bigint;
BEGIN
  -- Make sure seq is a sequence
  SELECT "oid" INTO seq_id
  FROM pg_class
  WHERE "oid" = seq::oid AND "relkind" = 'S';

  IF seq_id IS NULL THEN
    RAISE EXCEPTION 'Relation %s is not a sequence', seq;
  END IF;

  -- Get the next value of the sequence
  SELECT nextval(seq) INTO next_val;

  -- Acquire advisory locks on the sequence and the drawn value, held until transaction end
  PERFORM pg_advisory_xact_lock_shared(seq::int, 0);
  PERFORM pg_advisory_xact_lock_shared(next_val - 1);

  -- Return the next value
  RETURN next_val;
END;
$$ LANGUAGE plpgsql;

CREATE FUNCTION resourceregistry.tx_max_safeval(
  seq regclass
)
RETURNS bigint AS $$
DECLARE
  seq_id oid;
  max_seq bigint;
BEGIN
  -- Make sure seq is a sequence
  SELECT "oid" INTO seq_id
  FROM pg_class
  WHERE "oid" = seq::oid AND "relkind" = 'S';

  IF seq_id IS NULL THEN
    RAISE EXCEPTION 'Relation %s is not a sequence', seq;
  END IF;

  -- Find the minimum seq across all running transactions
  -- note: we deal with two related locks here (correlated by pid and virtualtransaction)
  -- one is to know we've locked on the sequence, and the other is to know what value
  -- we've locked all values after
  -- The first one (seq_lock) is a two-int advisory lock with classid = the oid of the sequence, objid = 0, and objsubid = 2
  -- the second one (val_lock) is a bigint advisory lock split across classid and objid, with objsubid = 1
  SELECT min((val_lock.classid::bigint << 32) | val_lock.objid::bigint) INTO max_seq
  FROM pg_locks seq_lock
  INNER JOIN pg_locks val_lock
    ON  seq_lock.pid = val_lock.pid
    AND seq_lock.virtualtransaction = val_lock.virtualtransaction
  WHERE seq_lock.classid = seq::oid
    AND seq_lock.objid = 0
    AND seq_lock.objsubid = 2
    AND seq_lock.locktype = 'advisory'
    AND seq_lock.granted
    AND val_lock.objsubid = 1
    AND val_lock.locktype = 'advisory'
    AND val_lock.granted;

  -- If no locks are found, return the maximum possible bigint value
  IF max_seq IS NULL THEN
      RETURN 9223372036854775807;
  END IF;

  -- Return the maximum safe value
  RETURN max_seq;
END;
$$ LANGUAGE plpgsql;
