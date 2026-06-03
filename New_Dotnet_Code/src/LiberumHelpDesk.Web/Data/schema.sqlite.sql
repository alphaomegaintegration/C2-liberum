-- schema.sqlite.sql
-- Faithful SQLite translation of db/helpdesk.sql (Liberum Help Desk v0.98, SQL Server 2000).
-- Translation rules (see plan): int->INTEGER, varchar/text->TEXT, datetime->TEXT (ISO-8601),
-- BIT->INTEGER, getdate()->datetime('now','localtime'). Manual integer PKs are preserved
-- (the db_keys + GetUnique sequence is load-bearing) -- NO AUTOINCREMENT.
-- Column names are kept EXACTLY as the original (SQLite identifiers are case-insensitive but
-- we match the casing used in the ASP SQL for clarity). The 'reps' table is intentionally
-- NOT created (dead code in the shipped app; see plan decision C1).

CREATE TABLE IF NOT EXISTS categories (
  category_id INTEGER NOT NULL PRIMARY KEY,
  cname       TEXT,
  rep_id      INTEGER
);

CREATE TABLE IF NOT EXISTS db_keys (
  problems    INTEGER,
  departments INTEGER,
  categories  INTEGER,
  users       INTEGER,
  Lang        INTEGER
);

CREATE TABLE IF NOT EXISTS departments (
  department_id INTEGER NOT NULL PRIMARY KEY,
  dname         TEXT
);

CREATE TABLE IF NOT EXISTS priority (
  priority_id INTEGER NOT NULL PRIMARY KEY,
  pname       TEXT
);

CREATE TABLE IF NOT EXISTS problems (
  id             INTEGER NOT NULL PRIMARY KEY,
  uid            TEXT,
  uemail         TEXT,
  ulocation      TEXT,
  uphone         TEXT,
  rep            INTEGER,
  status         INTEGER,
  time_spent     INTEGER,
  category       INTEGER,
  priority       INTEGER,
  department     INTEGER,
  title          TEXT,
  description    TEXT,
  solution       TEXT,
  start_date     TEXT,
  close_date     TEXT,
  due_date       TEXT,
  first_response TEXT,
  entered_by     INTEGER,
  kb             INTEGER,
  emailsent      INTEGER,
  kb_inserted    INTEGER
);

CREATE TABLE IF NOT EXISTS status (
  status_id INTEGER NOT NULL PRIMARY KEY,
  sname     TEXT
);

CREATE TABLE IF NOT EXISTS tblConfig (
  SiteName        TEXT,
  BaseURL         TEXT,
  AdminPass       TEXT,
  EmailType       INTEGER,
  SMTPServer      TEXT,
  HDName          TEXT,
  HDReply         TEXT,
  BaseEmail       TEXT,
  EnablePager     INTEGER,
  NotifyUser      INTEGER,
  EnableKB        INTEGER,
  DefaultPriority INTEGER,
  DefaultStatus   INTEGER,
  CloseStatus     INTEGER,
  AuthType        INTEGER,
  Version         TEXT,
  UseSelectUser   INTEGER,
  UseInoutBoard   INTEGER,
  KBFreeText      INTEGER,
  DefaultLanguage INTEGER,
  AllowImageUpload INTEGER,
  MaxImageSize    TEXT
);

CREATE TABLE IF NOT EXISTS tblConfig_Auth (
  ID   INTEGER NOT NULL PRIMARY KEY,
  Type TEXT
);

CREATE TABLE IF NOT EXISTS tblConfig_Email (
  ID   INTEGER NOT NULL PRIMARY KEY,
  Type TEXT
);

CREATE TABLE IF NOT EXISTS tblEmailMsg (
  type    TEXT NOT NULL PRIMARY KEY,
  subject TEXT,
  body    TEXT
);

-- NOTE: original has NO unique constraint on (id, variable); duplicate rows are possible and
-- the seeder relies on that (setup.asp inserts duplicates when Overwrite is off). Do NOT add one.
CREATE TABLE IF NOT EXISTS tblLangStrings (
  id       INTEGER NOT NULL,
  variable TEXT NOT NULL,
  LangText TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS tblLanguage (
  id        INTEGER NOT NULL,
  LangName  TEXT,
  Localized TEXT
);

-- tblNotes.id is a (non-unique) FK to problems.id; multiple notes per problem.
CREATE TABLE IF NOT EXISTS tblNotes (
  id      INTEGER NOT NULL,
  note    TEXT,
  addDate TEXT,
  uid     TEXT,
  private INTEGER
);

CREATE TABLE IF NOT EXISTS tblUsers (
  sid              INTEGER NOT NULL PRIMARY KEY,
  uid              TEXT,
  password         TEXT,
  fname            TEXT,
  email1           TEXT,
  email2           TEXT,
  phone            TEXT,
  location1        TEXT,
  location2        TEXT,
  department       INTEGER DEFAULT 0,
  IsRep            INTEGER DEFAULT 0,
  dtCreated        TEXT DEFAULT (datetime('now','localtime')),
  dtLastAccess     TEXT DEFAULT (datetime('now','localtime')),
  ListOnInoutBoard INTEGER NOT NULL DEFAULT 1,
  firstname        TEXT,
  lastname         TEXT,
  dateformat       TEXT NOT NULL DEFAULT 'yyyy-mm-dd',
  inoutadmin       INTEGER NOT NULL DEFAULT 0,
  phone_home       TEXT,
  phone_mobile     TEXT,
  jobfunction      TEXT,
  userresume       TEXT,
  statustext       TEXT,
  statuscode       INTEGER NOT NULL DEFAULT 0,
  statusdate       TEXT,
  Language         INTEGER,
  RepAccess        INTEGER NOT NULL DEFAULT 0
);
