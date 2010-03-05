BEGIN TRANSACTION;

CREATE TABLE auth (
  UUID char(36) NOT NULL,
  passwordHash char(32) NOT NULL default '',
  passwordSalt char(32) NOT NULL default '',
  webLoginKey varchar(255) NOT NULL default '',
  accountType VARCHAR(32) NOT NULL DEFAULT 'UserAccount',
  PRIMARY KEY  (`UUID`)
);

CREATE TABLE tokens (
  UUID char(36) NOT NULL,
  token varchar(255) NOT NULL,
  validity datetime NOT NULL
);

COMMIT;
