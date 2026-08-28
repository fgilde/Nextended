-- Seed for the SQLite file of the demo — the connection called SCRATCH, which is a file on the
-- studio's own volume rather than a server. Run by the studio: WithSeedScript("seed").
--
-- A small, self-contained set with foreign keys in both directions, which is what makes the
-- development subset worth trying here: five rows of one table pull in the rows they point at.

CREATE TABLE countries (
    code  TEXT PRIMARY KEY,
    title TEXT NOT NULL
);

CREATE TABLE people (
    id         INTEGER PRIMARY KEY,
    name       TEXT NOT NULL,
    email      TEXT NOT NULL,
    city       TEXT,
    country    TEXT REFERENCES countries(code),
    phone      TEXT,
    salary     REAL,
    api_token  TEXT,
    signed_up  TEXT NOT NULL
);

CREATE TABLE notes (
    id        INTEGER PRIMARY KEY,
    person_id INTEGER REFERENCES people(id),
    body      TEXT NOT NULL,
    written   TEXT NOT NULL
);

INSERT INTO countries (code, title) VALUES
    ('gb', 'United Kingdom'), ('fi', 'Finland'), ('pt', 'Portugal'),
    ('us', 'United States'), ('ke', 'Kenya');

INSERT INTO people (id, name, email, city, country, phone, salary, api_token, signed_up) VALUES
    (1, 'Erika Mustermann', 'erika@real.example',  'London',   'gb', '+44 20 7000 0001', 61000, 'sk-live-a1', '2026-01-04'),
    (2, 'João Silva',       'joao@real.example',   'Lisbon',   'pt', '+351 21 000 0002', 54000, 'sk-live-b2', '2026-01-19'),
    (3, 'Amina Otieno',     'amina@real.example',  'Nairobi',  'ke', '+254 20 000 0003', 47000, 'sk-live-c3', '2026-02-02'),
    (4, 'Jussi Virtanen',   'jussi@real.example',  'Helsinki', 'fi', '+358 9 000 0004',  58000, 'sk-live-d4', '2026-02-21'),
    (5, 'Mary Johnson',     'mary@real.example',   'Boston',   'us', '+1 617 000 0005',  72000, 'sk-live-e5', '2026-03-07');

INSERT INTO notes (id, person_id, body, written) VALUES
    (1, 1, 'Called about the missing shift keys. Sending a replacement.', '2026-03-10'),
    (2, 1, 'Asked for an invoice copy for accounting.',                   '2026-04-02'),
    (3, 3, 'Prefers to be contacted in the afternoon.',                   '2026-04-14'),
    (4, 5, 'Wants a quote for twelve seats.',                             '2026-05-01');

-- Real-looking names, addresses, phone numbers, a salary and a secret: exactly the columns the
-- development subset replaces, and the keys it must leave alone.
CREATE INDEX ix_notes_person ON notes(person_id);
