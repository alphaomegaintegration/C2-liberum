-- seed.sqlite.sql
-- Faithful translation of the INSERT seed data in db/helpdesk.sql (v0.98 fresh install).
-- Email message bodies reproduce the original CHAR(13) line separators exactly (SQLite char(13)).
-- Language strings are NOT seeded here -- they are imported from Data/lang/*.txt by DatabaseSeeder
-- (replicating setup.asp). The default admin user is NOT a row here: admin is the AdminPass gate.

INSERT INTO tblConfig
  (SiteName, BaseURL, AdminPass, EmailType, SMTPServer, HDName, HDReply,
   BaseEmail, EnablePager, NotifyUser, EnableKB, DefaultPriority, DefaultStatus, CloseStatus, AuthType,
   Version, UseSelectUser, UseInoutBoard, KBFreeText, DefaultLanguage, AllowImageUpload, MaxImageSize)
  VALUES
  ('Company Name', 'http://www.company.com/helpdesk', 'admin', 1, 'smtp.company.com',
   'Consultant', 'helpdesk@company.com', '@company.com', 0, 0, 2, 1, 1, 100, 2,
   '0.98', 1, 0, 0, 1, 0, '100000');

INSERT INTO tblConfig_Auth (ID, Type) VALUES (1, 'NT Authentication');
INSERT INTO tblConfig_Auth (ID, Type) VALUES (2, 'Database');
INSERT INTO tblConfig_Auth (ID, Type) VALUES (3, 'External Authentication');

INSERT INTO tblConfig_Email (ID, Type) VALUES (0, 'Disabled');
INSERT INTO tblConfig_Email (ID, Type) VALUES (1, 'CDONTS');
INSERT INTO tblConfig_Email (ID, Type) VALUES (2, 'JMail');
INSERT INTO tblConfig_Email (ID, Type) VALUES (3, 'ASPEmail');
INSERT INTO tblConfig_Email (ID, Type) VALUES (4, 'ASPMail');
INSERT INTO tblConfig_Email (ID, Type) VALUES (5, 'CDOSYS (Recommended)');

INSERT INTO tblEmailMsg(type, subject, body) VALUES
  ('repclose', 'HELPDESK: Problem [problemid] Closed',
   'The following problem has been closed.  You can view the problem at [rurl]' || char(13) || char(13) ||
   'PROBLEM DETAILS' || char(13) ||
   '---------------' || char(13) ||
   'ID: [problemid]' || char(13) ||
   'User: [uid]' || char(13) ||
   'Date: [startdate]' || char(13) ||
   'Title: [title]' || char(13) ||
   'Priority: [priority]' || char(13) ||
   'Category: [category]' || char(13) || char(13) ||
   'SOLUTION' || char(13) ||
   '--------' || char(13) ||
   '[solution]');

INSERT INTO tblEmailMsg(type, subject, body) VALUES
  ('repnew', 'HELPDESK: Problem [problemid] Assigned',
   'The following problem has been assigned to you.  You can update the problem at [rurl]' || char(13) || char(13) ||
   'PROBLEM DETAILS' || char(13) ||
   '---------------' || char(13) ||
   'ID: [problemid]' || char(13) ||
   'Date: [startdate]' || char(13) ||
   'Title: [title]' || char(13) ||
   'Priority: [priority]' || char(13) ||
   'Category: [category]' || char(13) || char(13) ||
   'USER INFORMATION' || char(13) ||
   '----------------' || char(13) ||
   'Username: [uid]' || char(13) ||
   'Email: [uemail]' || char(13) ||
   'Phone: [phone]' || char(13) ||
   'Location: [location]' || char(13) ||
   'Department: [department]' || char(13) || char(13) ||
   'DESCRIPTION' || char(13) ||
   '-----------' || char(13) ||
   '[description]');

INSERT INTO tblEmailMsg(type, subject, body) VALUES
  ('reppager', 'HELPDESK: Problem [problemid] Assigned',
   'Title: [title]' || char(13) ||
   'Priority: [priority]' || char(13) ||
   'User: [uid]');

INSERT INTO tblEmailMsg(type, subject, body) VALUES
  ('repupdate', 'HELPDESK: Problem [problemid] Updated',
   'The following problem has been updated.  You can view the problem at [rurl]' || char(13) || char(13) ||
   'PROBLEM DETAILS' || char(13) ||
   '---------------' || char(13) ||
   'ID: [problemid]' || char(13) ||
   'User: [uid]' || char(13) ||
   'Date: [startdate]' || char(13) ||
   'Title: [title]' || char(13) ||
   'Priority: [priority]' || char(13) ||
   'Category: [category]' || char(13) || char(13) ||
   'DESCRIPTION' || char(13) ||
   '-----------' || char(13) ||
   '[description]' || char(13) || char(13) ||
   'NOTES' || char(13) ||
   '-----------' || char(13) ||
   '[notes]');

INSERT INTO tblEmailMsg(type, subject, body) VALUES
  ('userclose', 'HELPDESK: Problem [problemid] Closed',
   'Your help desk problem has been closed.  You can view the solution below or at: [uurl]' || char(13) || char(13) ||
   'PROBLEM DETAILS' || char(13) ||
   '---------------' || char(13) ||
   'ID: [problemid]' || char(13) ||
   'User: [uid]' || char(13) ||
   'Date: [startdate]' || char(13) ||
   'Title: [title]' || char(13) || char(13) ||
   'SOLUTION' || char(13) ||
   '--------' || char(13) ||
   '[solution]');

INSERT INTO tblEmailMsg(type, subject, body) VALUES
  ('usernew', 'HELPDESK: Problem [problemid] Created',
   'Thank you for submitting your problem to the help desk.  You can view or update the problem at: [uurl]' || char(13) || char(13) ||
   'PROBLEM DETAILS' || char(13) ||
   '---------------' || char(13) ||
   'ID: [problemid]' || char(13) ||
   'User: [uid]' || char(13) ||
   'Date: [startdate]' || char(13) ||
   'Title: [title]' || char(13) || char(13) ||
   'DESCRIPTION' || char(13) ||
   '-----------' || char(13) ||
   '[description]');

INSERT INTO tblEmailMsg(type, subject, body) VALUES
  ('userupdate', 'HELPDESK: Problem [problemid] Updated',
   'Your help desk problem has been updated.  You can view the problem at: [uurl]' || char(13) || char(13) ||
   'PROBLEM DETAILS' || char(13) ||
   '---------------' || char(13) ||
   'ID: [problemid]' || char(13) ||
   'User: [uid]' || char(13) ||
   'Date: [startdate]' || char(13) ||
   'Title: [title]' || char(13) || char(13) ||
   'DESCRIPTION' || char(13) ||
   '-----------' || char(13) ||
   '[description]' || char(13) || char(13) ||
   'NOTES' || char(13) ||
   '-----------' || char(13) ||
   '[notes]');

INSERT INTO tblUsers(sid, uid, fname, email1, dateformat)
  VALUES (0, 'unknown', 'Unknown', 'none@localhost', 'yyyy-mm-dd');

INSERT INTO db_keys(problems, departments, categories, users, Lang)
  VALUES (1, 2, 1, 1, 2);

INSERT INTO status(status_id, sname) VALUES (0, 'UNKNOWN');
INSERT INTO status(status_id, sname) VALUES (1, 'OPEN');
INSERT INTO status(status_id, sname) VALUES (100, 'CLOSED');

INSERT INTO priority(priority_id, pname) VALUES (0, 'UNKNOWN');
INSERT INTO priority(priority_id, pname) VALUES (1, 'LOW');
INSERT INTO priority(priority_id, pname) VALUES (2, 'HIGH');

INSERT INTO departments(department_id, dname) VALUES (0, 'UNKNOWN');
INSERT INTO departments(department_id, dname) VALUES (1, 'Dept1');

INSERT INTO tblLanguage (id, LangName, Localized) VALUES (1, 'English', 'English');
