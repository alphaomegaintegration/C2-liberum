-- Parity fixtures (SQLite / .NET). Mirrored byte-for-byte by tools/_fixtures.asp for the oracle .mdb.
-- Fixed dates so DisplayDate renders identically on both apps (UI creation would stamp start_date=now,
-- diverging by seconds).

-- Align the admin rep (sid=1) to a canonical identical row.
UPDATE tblUsers SET
  fname='Administrator', firstname='Admin', lastname='User',
  email1='admin@localhost', email2='', department=1, dateformat='yyyy-mm-dd',
  IsRep=1, RepAccess=0, ListOnInoutBoard=1, inoutadmin=1, Language=1, password='admin',
  statuscode=0, statustext='At my desk', statusdate='2026-06-02 08:00:00'
WHERE sid=1;

-- One support category whose primary rep is the admin.
INSERT INTO categories (category_id, cname, rep_id) VALUES (1, 'General Support', 1);

-- Problem 1: OPEN, owned by admin, assigned to admin (rep).
INSERT INTO problems (id, uid, uemail, ulocation, uphone, rep, status, time_spent, category, priority,
  department, title, description, solution, start_date, close_date, due_date, first_response,
  entered_by, kb, emailsent, kb_inserted)
VALUES (1, 'admin', 'admin@localhost', '', '', 1, 1, 0, 1, 2, 1,
  'Cannot connect to the network drive',
  'User reports the shared X: drive is not mapping at login.',
  '', '2026-06-01 09:00:00', NULL, '2026-06-08 17:00:00', NULL, 1, 0, 0, 0);

-- Problem 2: CLOSED + Knowledge Base article.
INSERT INTO problems (id, uid, uemail, ulocation, uphone, rep, status, time_spent, category, priority,
  department, title, description, solution, start_date, close_date, due_date, first_response,
  entered_by, kb, emailsent, kb_inserted)
VALUES (2, 'admin', 'admin@localhost', '', '', 1, 100, 30, 1, 1, 1,
  'How do I reset my password',
  'User forgot their password and needs the reset procedure.',
  'Go to the logon page and click E-mail My Password, or contact the help desk to reset it.',
  '2026-05-20 10:00:00', '2026-05-20 11:30:00', '2026-05-27 17:00:00', '2026-05-20 10:20:00', 1, 1, 1, 0);

-- One public note on problem 1.
INSERT INTO tblNotes (id, note, addDate, uid, private)
VALUES (1, 'Verified the mapping script is missing for this user.', '2026-06-01 09:30:00', 'admin', 0);

-- Enable the In/Out board feature.
UPDATE tblConfig SET UseInoutBoard=1;

-- Advance the key counters past the seeded fixtures.
UPDATE db_keys SET problems=3, categories=2;
