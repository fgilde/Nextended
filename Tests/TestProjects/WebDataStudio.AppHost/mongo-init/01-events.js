// Documents for the demo's MongoDB, so its tree has collections with something in them.
//
// The mongo image runs every .js in /docker-entrypoint-initdb.d against the database named by
// MONGO_INITDB_DATABASE on the first start of an empty data volume.

const events = db.getSiblingDB("events");

// One collection whose documents agree on their shape, and one where they do not: the studio shows
// the fields it finds per document, and the second is what that is for.
events.sessions.insertMany([
  {
    account: "ada",
    started: new Date(Date.now() - 1000 * 60 * 60 * 26),
    ended: new Date(Date.now() - 1000 * 60 * 60 * 25),
    pages: 14,
    device: { kind: "laptop", os: "Linux", browser: "Firefox" },
    tags: ["beta"],
  },
  {
    account: "linus",
    started: new Date(Date.now() - 1000 * 60 * 60 * 20),
    ended: new Date(Date.now() - 1000 * 60 * 60 * 19),
    pages: 3,
    device: { kind: "phone", os: "Android", browser: "Chrome" },
    tags: [],
  },
  {
    account: "grace",
    started: new Date(Date.now() - 1000 * 60 * 90),
    ended: null,
    pages: 41,
    device: { kind: "desktop", os: "Windows", browser: "Edge" },
    tags: ["invited", "beta"],
  },
]);

events.telemetry.insertMany([
  { kind: "page", path: "/pricing", ms: 120, at: new Date() },
  { kind: "page", path: "/docs", ms: 240, at: new Date(), referrer: "/pricing" },
  { kind: "error", message: "timeout talking to payments", stack: ["pay.js:42", "checkout.js:9"], at: new Date() },
  { kind: "purchase", amount: 129.0, currency: "EUR", items: [{ sku: "KB-01", qty: 1 }], at: new Date() },
  { kind: "purchase", amount: 259.8, currency: "EUR", items: [{ sku: "MN-03", qty: 1 }, { sku: "CB-04", qty: 1 }], at: new Date() },
]);

// A capped collection and an index, so those are visible too.
events.createCollection("audit", { capped: true, size: 65536, max: 500 });
events.audit.insertMany([
  { who: "ada", did: "signed in", at: new Date() },
  { who: "grace", did: "exported invoices", at: new Date() },
]);

events.sessions.createIndex({ account: 1, started: -1 });
events.telemetry.createIndex({ kind: 1 });

print("seeded events: " + events.sessions.countDocuments() + " sessions, "
  + events.telemetry.countDocuments() + " telemetry, " + events.audit.countDocuments() + " audit");
