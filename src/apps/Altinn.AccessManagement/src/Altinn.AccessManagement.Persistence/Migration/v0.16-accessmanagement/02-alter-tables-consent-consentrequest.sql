-- Table: consent.consentrequest

ALTER TABLE consent.consentrequest 
ADD COLUMN portalviewmode consent.portal_view_mode NOT NULL DEFAULT 'hide';

