-- Function: DROP delegation.insert_resourceregistrydelegationchange
DROP FUNCTION IF EXISTS delegation.insert_resourceregistrydelegationchange(delegation.delegationchangetype, text, int, int, int, int, int, text, text, timestamp with time zone);

-- Function: DROP delegation.select_current_resourceregistrydelegationchange
DROP FUNCTION IF EXISTS delegation.select_current_resourceregistrydelegationchange(text, int, int, int);

-- Function: DROP delegation.select_active_resourceregistrydelegationchanges_offeredby
DROP FUNCTION IF EXISTS delegation.select_active_resourceregistrydelegationchanges_offeredby(int, text[], text[]);

-- Function: DROP delegation.select_active_resourceregistrydelegationchanges_coveredbyuser
DROP FUNCTION IF EXISTS delegation.select_active_resourceregistrydelegationchanges_coveredbyuser(int, int[], text[], text[]);

-- Function: DROP delegation.select_active_resourceregistrydelegationchanges_coveredbypartys
DROP FUNCTION IF EXISTS delegation.select_active_resourceregistrydelegationchanges_coveredbypartys(int[], int[], text[], text[]);

-- Function: DROP delegation.select_active_resourceregistrydelegationchanges
DROP FUNCTION IF EXISTS delegation.select_active_resourceregistrydelegationchanges(int[], int[], text[], text[]);

-- Foreign Key: must go before the schema it references
ALTER TABLE IF EXISTS delegation.resourceregistrydelegationchanges
DROP CONSTRAINT IF EXISTS resourceregistrydelegationchanges_resourceid_fk;

-- Schema: accessmanagement
DROP SCHEMA IF EXISTS accessmanagement CASCADE;
